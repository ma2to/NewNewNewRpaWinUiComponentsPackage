namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Common;

/// <summary>
/// DDD: Security level enumeration
/// </summary>
internal enum SecurityLevel
{
    None,
    Basic,
    Standard,
    Enhanced,
    Maximum
}

/// <summary>
/// DDD: Access permission enumeration
/// </summary>
[Flags]
internal enum AccessPermission
{
    None = 0,
    Read = 1,
    Write = 2,
    Delete = 4,
    Execute = 8,
    Admin = 16,
    All = Read | Write | Delete | Execute | Admin
}

/// <summary>
/// DDD: Security validation result
/// </summary>
internal sealed record SecurityValidationResult
{
    public bool IsValid { get; init; }
    public SecurityLevel Level { get; init; }
    public IReadOnlyList<string> Errors { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> Warnings { get; init; } = Array.Empty<string>();

    public static SecurityValidationResult Valid(SecurityLevel level) =>
        new() { IsValid = true, Level = level };

    public static SecurityValidationResult Invalid(IEnumerable<string> errors) =>
        new() { IsValid = false, Errors = errors.ToList() };
}

/// <summary>
/// DDD: Authorization result
/// </summary>
internal sealed record AuthorizationResult
{
    public bool IsAuthorized { get; init; }
    public AccessPermission GrantedPermissions { get; init; }
    public string? DenialReason { get; init; }
    public DateTime ValidUntil { get; init; } = DateTime.UtcNow.AddHours(8);

    public static AuthorizationResult Authorized(AccessPermission permissions) =>
        new() { IsAuthorized = true, GrantedPermissions = permissions };

    public static AuthorizationResult Denied(string reason) =>
        new() { IsAuthorized = false, DenialReason = reason };
}
