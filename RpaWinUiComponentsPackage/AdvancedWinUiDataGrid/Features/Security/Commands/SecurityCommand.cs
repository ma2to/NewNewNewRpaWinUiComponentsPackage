using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Common;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Core.ValueObjects;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Security.Commands;

/// <summary>
/// COMMAND PATTERN: Validate access command
/// </summary>
internal sealed record ValidateAccessCommand
{
    public required string Operation { get; init; }
    public required AccessPermission RequiredPermission { get; init; }
    public string? UserContext { get; init; }
    public CancellationToken CancellationToken { get; init; } = default;

    public static ValidateAccessCommand Create(string operation, AccessPermission permission) =>
        new() { Operation = operation, RequiredPermission = permission };

    public static ValidateAccessCommand ForRead(string operation) =>
        new() { Operation = operation, RequiredPermission = AccessPermission.Read };

    public static ValidateAccessCommand ForWrite(string operation) =>
        new() { Operation = operation, RequiredPermission = AccessPermission.Write };
}

/// <summary>
/// COMMAND PATTERN: Set permissions command
/// </summary>
internal sealed record SetPermissionsCommand
{
    public required string Target { get; init; }
    public required AccessPermission Permissions { get; init; }
    public TimeSpan? ValidityDuration { get; init; }
    public CancellationToken CancellationToken { get; init; } = default;

    public static SetPermissionsCommand Create(string target, AccessPermission permissions) =>
        new() { Target = target, Permissions = permissions };

    public static SetPermissionsCommand Grant(string target, AccessPermission permissions, TimeSpan validity) =>
        new() { Target = target, Permissions = permissions, ValidityDuration = validity };
}

/// <summary>
/// COMMAND PATTERN: Security validation command
/// </summary>
internal sealed record ValidateSecurityCommand
{
    public required object Input { get; init; }
    public SecurityLevel RequiredLevel { get; init; } = SecurityLevel.Standard;
    public bool PerformDeepValidation { get; init; } = false;
    public CancellationToken CancellationToken { get; init; } = default;

    public static ValidateSecurityCommand Create(object input, SecurityLevel level = SecurityLevel.Standard) =>
        new() { Input = input, RequiredLevel = level };
}
