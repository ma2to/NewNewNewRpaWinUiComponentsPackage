using System;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Core.Constants;

/// <summary>
/// CORE: Constants for validation system configuration
/// ENTERPRISE: Centralized validation parameters
/// </summary>
internal static class ValidationConstants
{
    /// <summary>Default timeout for validation rules (2 seconds)</summary>
    public static readonly TimeSpan DefaultValidationTimeout = TimeSpan.FromSeconds(2);

    /// <summary>Maximum allowed timeout for validation rules (30 seconds)</summary>
    public static readonly TimeSpan MaxValidationTimeout = TimeSpan.FromSeconds(30);

    /// <summary>Minimum allowed timeout for validation rules (100 milliseconds)</summary>
    public static readonly TimeSpan MinValidationTimeout = TimeSpan.FromMilliseconds(100);

    /// <summary>Default timeout message when validation rule times out</summary>
    public const string TimeoutErrorMessage = "Timeout";

    /// <summary>Maximum number of validation errors to display per cell</summary>
    public const int MaxValidationErrorsPerCell = 10;

    /// <summary>Maximum priority value for validation rules</summary>
    public const int MaxValidationPriority = int.MaxValue;

    /// <summary>Default priority for validation rules when not specified</summary>
    public const int DefaultValidationPriority = 1000;
}