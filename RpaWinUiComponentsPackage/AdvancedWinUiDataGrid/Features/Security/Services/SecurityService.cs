using Microsoft.Extensions.Logging;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Common.Models;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Common;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Core.ValueObjects;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Security.Commands;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Security.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Infrastructure.Logging.Interfaces;
using System.Collections.Concurrent;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Security.Services;

/// <summary>
/// INTERNAL: Security service implementation
/// THREAD SAFE: Thread-safe security operations
/// </summary>
internal sealed class SecurityService : ISecurityService
{
    private readonly ILogger<SecurityService> _logger;
    private readonly IOperationLogger<SecurityService> _operationLogger;
    private readonly ConcurrentDictionary<string, AccessPermission> _permissions = new();
    private SecurityLevel _currentLevel = SecurityLevel.Standard;

    public SecurityService(
        ILogger<SecurityService> logger,
        IOperationLogger<SecurityService> operationLogger)
    {
        _logger = logger;
        _operationLogger = operationLogger;
    }

    public async Task<Result<AuthorizationResult>> ValidateAccessAsync(ValidateAccessCommand command)
    {
        using var scope = _operationLogger.LogOperationStart(nameof(ValidateAccessAsync),
            new { operation = command.Operation, requiredPermission = command.RequiredPermission });

        try
        {
            // Get permissions for operation
            var hasPermission = _permissions.TryGetValue(command.Operation, out var granted) &&
                               granted.HasFlag(command.RequiredPermission);

            if (!hasPermission)
            {
                // Default: grant Read permission
                if (command.RequiredPermission == AccessPermission.Read)
                {
                    hasPermission = true;
                    granted = AccessPermission.Read;
                }
            }

            var result = hasPermission
                ? AuthorizationResult.Authorized(granted)
                : AuthorizationResult.Denied($"Missing required permission: {command.RequiredPermission}");

            _logger.LogInformation("Access validation: Operation={Operation}, Authorized={Authorized}",
                command.Operation, result.IsAuthorized);

            scope.MarkSuccess(result);
            return await Task.FromResult(Result<AuthorizationResult>.Success(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Access validation failed for operation {Operation}", command.Operation);
            scope.MarkFailure(ex);
            return Result<AuthorizationResult>.Failure($"Access validation failed: {ex.Message}");
        }
    }

    public async Task<Result<bool>> SetPermissionsAsync(SetPermissionsCommand command)
    {
        using var scope = _operationLogger.LogOperationStart(nameof(SetPermissionsAsync),
            new { target = command.Target, permissions = command.Permissions });

        try
        {
            _permissions.AddOrUpdate(command.Target, command.Permissions, (key, old) => command.Permissions);

            _logger.LogInformation("Permissions set: Target={Target}, Permissions={Permissions}",
                command.Target, command.Permissions);

            scope.MarkSuccess(true);
            return await Task.FromResult(Result<bool>.Success(true));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set permissions for {Target}", command.Target);
            scope.MarkFailure(ex);
            return Result<bool>.Failure($"Set permissions failed: {ex.Message}");
        }
    }

    public async Task<Result<SecurityValidationResult>> ValidateSecurityAsync(ValidateSecurityCommand command)
    {
        using var scope = _operationLogger.LogOperationStart(nameof(ValidateSecurityAsync),
            new { requiredLevel = command.RequiredLevel });

        try
        {
            var errors = new List<string>();

            // Basic validation
            if (command.Input == null)
            {
                errors.Add("Input cannot be null");
            }

            // Check against dangerous patterns (simplified)
            if (command.Input is string str && str.Contains("<script", StringComparison.OrdinalIgnoreCase))
            {
                errors.Add("Potentially dangerous script content detected");
            }

            var result = errors.Any()
                ? SecurityValidationResult.Invalid(errors)
                : SecurityValidationResult.Valid(_currentLevel);

            _logger.LogInformation("Security validation: Valid={IsValid}, Level={Level}",
                result.IsValid, result.Level);

            scope.MarkSuccess(result);
            return await Task.FromResult(Result<SecurityValidationResult>.Success(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Security validation failed");
            scope.MarkFailure(ex);
            return Result<SecurityValidationResult>.Failure($"Security validation failed: {ex.Message}");
        }
    }

    public SecurityLevel GetCurrentSecurityLevel()
    {
        return _currentLevel;
    }
}
