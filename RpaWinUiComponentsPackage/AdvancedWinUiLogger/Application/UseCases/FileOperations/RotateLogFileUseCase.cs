using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using RpaWinUiComponentsPackage.AdvancedWinUiLogger.Core.ValueObjects;
using RpaWinUiComponentsPackage.AdvancedWinUiLogger.Core.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiLogger.Core.Constants;

namespace RpaWinUiComponentsPackage.AdvancedWinUiLogger.Application.UseCases.FileOperations;

/// <summary>
/// USE CASE: Rotate log file with validation and cleanup
/// SINGLE RESPONSIBILITY: File rotation business logic
/// ENTERPRISE: Complete rotation workflow with comprehensive error handling
/// </summary>
internal sealed class RotateLogFileUseCase
{
    private readonly ILoggerRepository _repository;
    private readonly IFileRotationService _rotationService;
    private readonly IConfigurationValidator _configurationValidator;

    /// <summary>
    /// DEPENDENCY INJECTION: Constructor with required dependencies
    /// </summary>
    public RotateLogFileUseCase(
        ILoggerRepository repository,
        IFileRotationService rotationService,
        IConfigurationValidator configurationValidator)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _rotationService = rotationService ?? throw new ArgumentNullException(nameof(rotationService));
        _configurationValidator = configurationValidator ?? throw new ArgumentNullException(nameof(configurationValidator));
    }

    /// <summary>
    /// ENTERPRISE: Execute file rotation with comprehensive validation and cleanup
    /// BUSINESS LOGIC: Complete rotation workflow with pre and post processing
    /// </summary>
    public async Task<Result<RotationResult>> ExecuteAsync(
        string currentFilePath,
        LoggerConfiguration configuration,
        bool forceRotation = false,
        CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;

        try
        {
            // 1. Validate input parameters
            var validationResult = ValidateInput(currentFilePath, configuration);
            if (validationResult.IsFailure)
                return Result<RotationResult>.Failure(validationResult.Error);

            // 2. Check if rotation is actually needed (unless forced)
            if (!forceRotation)
            {
                var needsRotationResult = await _rotationService.ShouldRotateAsync(currentFilePath, configuration, cancellationToken);
                if (needsRotationResult.IsFailure)
                    return Result<RotationResult>.Failure($"Failed to check rotation necessity: {needsRotationResult.Error}");

                if (!needsRotationResult.Value)
                {
                    var currentSizeResult = await _repository.GetFileSizeAsync(currentFilePath, cancellationToken);
                    var currentSize = currentSizeResult.IsSuccess ? currentSizeResult.Value : 0;

                    return Result<RotationResult>.Success(
                        RotationResult.NotNeeded(currentFilePath, currentSize));
                }
            }

            // 3. Validate rotation feasibility
            var targetDirectory = Path.GetDirectoryName(currentFilePath) ?? configuration.LogDirectory;
            var feasibilityResult = await _rotationService.ValidateRotationAsync(currentFilePath, targetDirectory, cancellationToken);
            if (feasibilityResult.IsFailure)
                return Result<RotationResult>.Failure($"Rotation validation failed: {feasibilityResult.Error}");

            // 4. Ensure current file is not in use
            var fileInUseResult = await _repository.IsFileInUseAsync(currentFilePath, cancellationToken);
            if (fileInUseResult.IsFailure)
                return Result<RotationResult>.Failure($"Failed to check file usage: {fileInUseResult.Error}");

            if (fileInUseResult.Value)
            {
                // Try to flush and release the file
                var flushResult = await _repository.FlushAsync(cancellationToken);
                if (flushResult.IsFailure)
                    return Result<RotationResult>.Failure($"Failed to flush file before rotation: {flushResult.Error}");

                // Wait a moment and check again
                await Task.Delay(100, cancellationToken);

                var secondCheckResult = await _repository.IsFileInUseAsync(currentFilePath, cancellationToken);
                if (secondCheckResult.IsSuccess && secondCheckResult.Value)
                    return Result<RotationResult>.Failure(LoggerConstants.ErrorFileInUse);
            }

            // 5. Get current file information
            var currentFileInfoResult = await _repository.GetLogFileInfoAsync(currentFilePath, cancellationToken);
            if (currentFileInfoResult.IsFailure)
                return Result<RotationResult>.Failure($"Failed to get current file info: {currentFileInfoResult.Error}");

            var currentFileInfo = currentFileInfoResult.Value;

            // 6. Generate new file name for rotation
            var newFileNameResult = _rotationService.GenerateRotatedFileName(currentFilePath, DateTime.UtcNow);
            if (newFileNameResult.IsFailure)
                return Result<RotationResult>.Failure($"Failed to generate rotated file name: {newFileNameResult.Error}");

            var newFilePath = newFileNameResult.Value;

            // 7. Perform the actual rotation
            var rotationResult = await _rotationService.RotateFileAsync(currentFilePath, configuration, cancellationToken);
            if (rotationResult.IsFailure)
                return rotationResult;

            var rotation = rotationResult.Value;

            // 8. Initialize new log file
            var initializeResult = await _repository.InitializeLogFileAsync(rotation.NewFilePath!, cancellationToken);
            if (initializeResult.IsFailure)
            {
                // Try to restore the original file if initialization fails
                await TryRestoreOriginalFile(currentFilePath, rotation.NewFilePath!, cancellationToken);
                return Result<RotationResult>.Failure($"Failed to initialize new log file: {initializeResult.Error}");
            }

            // 9. Perform cleanup if enabled
            if (configuration.EnableAutoCleanup)
            {
                var cleanupResult = await PerformPostRotationCleanup(configuration, cancellationToken);
                if (cleanupResult.IsFailure)
                {
                    // Cleanup failure shouldn't fail the rotation, but should be logged
                    rotation = rotation with
                    {
                        ErrorMessage = $"Rotation succeeded but cleanup failed: {cleanupResult.Error}"
                    };
                }
            }

            // 10. Calculate final operation duration
            var operationDuration = DateTime.UtcNow - startTime;
            var finalRotation = rotation with
            {
                OperationDuration = operationDuration,
                RotationType = forceRotation ? RotationType.Manual : RotationType.SizeBased
            };

            return Result<RotationResult>.Success(finalRotation);
        }
        catch (OperationCanceledException)
        {
            var cancelledDuration = DateTime.UtcNow - startTime;
            return Result<RotationResult>.Failure("Rotation operation was cancelled");
        }
        catch (Exception ex)
        {
            var errorDuration = DateTime.UtcNow - startTime;
            return Result<RotationResult>.Failure($"Unexpected error during file rotation: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// VALIDATION: Validate input parameters for rotation
    /// BUSINESS RULES: Parameter validation and sanity checks
    /// </summary>
    private Result<bool> ValidateInput(string currentFilePath, LoggerConfiguration configuration)
    {
        if (string.IsNullOrWhiteSpace(currentFilePath))
            return Result<bool>.Failure("Current file path cannot be null or empty");

        if (configuration == null)
            return Result<bool>.Failure(LoggerConstants.ErrorNullConfiguration);

        if (!File.Exists(currentFilePath))
            return Result<bool>.Failure($"Current log file does not exist: {currentFilePath}");

        var configValidationResult = configuration.Validate();
        if (configValidationResult.IsFailure)
            return Result<bool>.Failure($"Invalid configuration: {configValidationResult.Error}");

        // Validate file path length
        if (currentFilePath.Length > LoggerConstants.MaxPathLength)
            return Result<bool>.Failure("File path exceeds maximum allowed length");

        // Validate file name
        var fileName = Path.GetFileName(currentFilePath);
        if (string.IsNullOrWhiteSpace(fileName))
            return Result<bool>.Failure("Invalid file name in path");

        return Result<bool>.Success(true);
    }

    /// <summary>
    /// RECOVERY: Try to restore original file if rotation fails
    /// ERROR HANDLING: Graceful degradation and rollback
    /// </summary>
    private async Task<Result<bool>> TryRestoreOriginalFile(
        string originalPath,
        string newPath,
        CancellationToken cancellationToken)
    {
        try
        {
            if (File.Exists(newPath) && !File.Exists(originalPath))
            {
                File.Move(newPath, originalPath);
                return Result<bool>.Success(true);
            }

            return Result<bool>.Success(false);
        }
        catch (Exception ex)
        {
            return Result<bool>.Failure($"Failed to restore original file: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// CLEANUP: Perform post-rotation cleanup operations
    /// MAINTENANCE: Automated file lifecycle management
    /// </summary>
    private async Task<Result<bool>> PerformPostRotationCleanup(
        LoggerConfiguration configuration,
        CancellationToken cancellationToken)
    {
        try
        {
            var cleanupResult = await _rotationService.CleanupRotatedFilesAsync(
                configuration.LogDirectory,
                configuration,
                cancellationToken);

            if (cleanupResult.IsFailure)
                return Result<bool>.Failure($"Cleanup operation failed: {cleanupResult.Error}");

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            return Result<bool>.Failure($"Unexpected error during cleanup: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// ENTERPRISE: Execute rotation with retry logic
    /// RELIABILITY: Resilient rotation with automatic retry
    /// </summary>
    public async Task<Result<RotationResult>> ExecuteWithRetryAsync(
        string currentFilePath,
        LoggerConfiguration configuration,
        int maxRetries = 3,
        bool forceRotation = false,
        CancellationToken cancellationToken = default)
    {
        Result<RotationResult> lastResult = Result<RotationResult>.Failure("No attempts made");

        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            lastResult = await ExecuteAsync(currentFilePath, configuration, forceRotation, cancellationToken);

            if (lastResult.IsSuccess)
                return lastResult;

            // Don't retry for certain types of errors
            if (IsNonRetryableError(lastResult.Error))
                break;

            // Wait before retry (exponential backoff)
            if (attempt < maxRetries)
            {
                var delay = TimeSpan.FromMilliseconds(Math.Pow(2, attempt) * 100);
                await Task.Delay(delay, cancellationToken);
            }
        }

        return lastResult;
    }

    /// <summary>
    /// ERROR CLASSIFICATION: Determine if error is retryable
    /// RELIABILITY: Smart retry logic based on error type
    /// </summary>
    private bool IsNonRetryableError(string errorMessage)
    {
        if (string.IsNullOrEmpty(errorMessage))
            return false;

        var nonRetryableErrors = new[]
        {
            LoggerConstants.ErrorInvalidLogDirectory,
            LoggerConstants.ErrorInvalidFileName,
            LoggerConstants.ErrorPermissionDenied,
            "does not exist",
            "Invalid configuration"
        };

        foreach (var nonRetryableError in nonRetryableErrors)
        {
            if (errorMessage.Contains(nonRetryableError, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }
}