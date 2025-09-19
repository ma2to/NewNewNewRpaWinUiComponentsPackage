using System;
using System.Threading;
using System.Threading.Tasks;
using RpaWinUiComponentsPackage.AdvancedWinUiLogger.Core.ValueObjects;
using RpaWinUiComponentsPackage.AdvancedWinUiLogger.Core.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiLogger.Core.Constants;

namespace RpaWinUiComponentsPackage.AdvancedWinUiLogger.Application.UseCases.LoggingOperations;

/// <summary>
/// USE CASE: Write single log entry with validation and persistence
/// SINGLE RESPONSIBILITY: Log entry writing business logic
/// ENTERPRISE: Comprehensive entry processing with validation and error handling
/// </summary>
internal sealed class WriteLogEntryUseCase
{
    private readonly ILoggerRepository _repository;
    private readonly ILoggerSessionManager _sessionManager;
    private readonly IConfigurationValidator _configurationValidator;

    /// <summary>
    /// DEPENDENCY INJECTION: Constructor with required dependencies
    /// </summary>
    public WriteLogEntryUseCase(
        ILoggerRepository repository,
        ILoggerSessionManager sessionManager,
        IConfigurationValidator configurationValidator)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _sessionManager = sessionManager ?? throw new ArgumentNullException(nameof(sessionManager));
        _configurationValidator = configurationValidator ?? throw new ArgumentNullException(nameof(configurationValidator));
    }

    /// <summary>
    /// ENTERPRISE: Execute log entry writing with comprehensive validation
    /// BUSINESS LOGIC: Complete workflow for single entry processing
    /// </summary>
    public async Task<Result<bool>> ExecuteAsync(
        LogEntry entry,
        Guid? sessionId = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // 1. Validate input parameters
            var validationResult = ValidateInput(entry);
            if (validationResult.IsFailure)
                return validationResult;

            // 2. Get or validate session
            var sessionResult = GetValidSession(sessionId);
            if (sessionResult.IsFailure)
                return Result<bool>.Failure($"Session validation failed: {sessionResult.Error}");

            var session = sessionResult.Value;

            // 3. Check if entry should be logged based on configuration
            if (!session.Configuration.ShouldLog(entry.Level))
                return Result<bool>.Success(false); // Filtered out, but not an error

            // 4. Add entry to session (handles buffering)
            var addResult = session.AddLogEntry(entry);
            if (addResult.IsFailure)
                return Result<bool>.Failure($"Failed to add entry to session: {addResult.Error}");

            // 5. Check if immediate persistence is needed
            if (ShouldPersistImmediately(entry, session.Configuration))
            {
                var persistResult = await _repository.WriteLogEntryAsync(entry, cancellationToken);
                if (persistResult.IsFailure)
                    return Result<bool>.Failure($"Failed to persist entry: {persistResult.Error}");
            }

            // 6. Check if file rotation is needed
            if (session.Configuration.EnableAutoRotation && !string.IsNullOrEmpty(session.CurrentLogFilePath))
            {
                var rotationCheckResult = await CheckAndPerformRotationIfNeeded(session, cancellationToken);
                if (rotationCheckResult.IsFailure)
                {
                    // Log rotation failure shouldn't fail the write operation
                    // But we should log this issue for monitoring
                    return Result<bool>.Success(true);
                }
            }

            return Result<bool>.Success(true);
        }
        catch (OperationCanceledException)
        {
            return Result<bool>.Failure("Operation was cancelled");
        }
        catch (Exception ex)
        {
            return Result<bool>.Failure($"Unexpected error during log entry writing: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// VALIDATION: Validate input parameters
    /// BUSINESS RULES: Entry validation and sanity checks
    /// </summary>
    private Result<bool> ValidateInput(LogEntry entry)
    {
        if (entry == null)
            return Result<bool>.Failure("Log entry cannot be null");

        if (string.IsNullOrWhiteSpace(entry.Message))
            return Result<bool>.Failure("Log entry message cannot be empty");

        if (entry.Timestamp == default)
            return Result<bool>.Failure("Log entry must have a valid timestamp");

        // Validate message length to prevent extremely large entries
        if (entry.Message.Length > 10000) // 10KB limit
            return Result<bool>.Failure("Log entry message exceeds maximum allowed length");

        return Result<bool>.Success(true);
    }

    /// <summary>
    /// SESSION MANAGEMENT: Get valid session for logging
    /// BUSINESS LOGIC: Session resolution and validation
    /// </summary>
    private Result<Core.Entities.LoggerSession> GetValidSession(Guid? sessionId)
    {
        try
        {
            Core.Entities.LoggerSession session;

            if (sessionId.HasValue)
            {
                var specificSessionResult = _sessionManager.GetSession(sessionId.Value);
                if (specificSessionResult.IsFailure)
                    return Result<Core.Entities.LoggerSession>.Failure($"Specified session not found: {sessionId}");

                session = specificSessionResult.Value;
            }
            else
            {
                var activeSessionResult = _sessionManager.GetActiveSession();
                if (activeSessionResult.IsFailure)
                    return Result<Core.Entities.LoggerSession>.Failure("No active session available and no session ID specified");

                session = activeSessionResult.Value;
            }

            if (!session.IsActive)
                return Result<Core.Entities.LoggerSession>.Failure("Session is not active");

            return Result<Core.Entities.LoggerSession>.Success(session);
        }
        catch (Exception ex)
        {
            return Result<Core.Entities.LoggerSession>.Failure($"Error retrieving session: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// BUSINESS LOGIC: Determine if entry should be persisted immediately
    /// PERFORMANCE: Intelligent persistence decision making
    /// </summary>
    private bool ShouldPersistImmediately(LogEntry entry, LoggerConfiguration configuration)
    {
        // Critical entries should be persisted immediately
        if (entry.Level >= Microsoft.Extensions.Logging.LogLevel.Error)
            return true;

        // Persist immediately if not using background logging
        if (!configuration.EnableBackgroundLogging)
            return true;

        // Persist immediately if buffer size is 1 (real-time mode)
        if (configuration.BufferSize <= 1)
            return true;

        return false;
    }

    /// <summary>
    /// FILE MANAGEMENT: Check and perform rotation if needed
    /// BUSINESS LOGIC: Automatic file rotation workflow
    /// </summary>
    private async Task<Result<bool>> CheckAndPerformRotationIfNeeded(
        Core.Entities.LoggerSession session,
        CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrEmpty(session.CurrentLogFilePath))
                return Result<bool>.Success(true);

            // Check current file size
            var fileSizeResult = await _repository.GetFileSizeAsync(session.CurrentLogFilePath, cancellationToken);
            if (fileSizeResult.IsFailure)
                return Result<bool>.Failure($"Failed to check file size: {fileSizeResult.Error}");

            var currentFileSize = fileSizeResult.Value;
            var maxSizeBytes = session.Configuration.MaxFileSizeBytes;

            // Check if rotation is needed
            if (maxSizeBytes.HasValue && currentFileSize >= maxSizeBytes.Value)
            {
                // Generate new file path
                var newFilePath = session.Configuration.GetCurrentLogFilePath();

                // Perform rotation
                var rotationResult = await _repository.RotateLogFileAsync(
                    session.CurrentLogFilePath,
                    newFilePath,
                    cancellationToken);

                if (rotationResult.IsFailure)
                    return Result<bool>.Failure($"File rotation failed: {rotationResult.Error}");

                // Update session with new file path
                var updateResult = session.RecordRotation(rotationResult.Value);
                if (updateResult.IsFailure)
                    return Result<bool>.Failure($"Failed to update session after rotation: {updateResult.Error}");
            }

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            return Result<bool>.Failure($"Error during rotation check: {ex.Message}", ex);
        }
    }
}