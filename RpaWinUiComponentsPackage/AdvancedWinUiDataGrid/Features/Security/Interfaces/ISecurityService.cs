using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Common.Models;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Common;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Core.ValueObjects;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Security.Commands;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Security.Interfaces;

/// <summary>
/// INTERNAL: Security service interface
/// THREAD SAFE: All operations are thread-safe
/// </summary>
internal interface ISecurityService
{
    /// <summary>
    /// Validate access permissions
    /// </summary>
    Task<Result<AuthorizationResult>> ValidateAccessAsync(ValidateAccessCommand command);

    /// <summary>
    /// Set access permissions
    /// </summary>
    Task<Result<bool>> SetPermissionsAsync(SetPermissionsCommand command);

    /// <summary>
    /// Validate security constraints
    /// </summary>
    Task<Result<SecurityValidationResult>> ValidateSecurityAsync(ValidateSecurityCommand command);

    /// <summary>
    /// Get current security level
    /// </summary>
    SecurityLevel GetCurrentSecurityLevel();
}
