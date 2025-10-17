namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid;

/// <summary>
/// Public UI-friendly row view model for MVVM binding
/// </summary>
public sealed class PublicRowViewModel
{
    /// <summary>
    /// Row index in the grid
    /// </summary>
    public int Index { get; init; }

    /// <summary>
    /// Whether this row is selected
    /// </summary>
    public bool IsSelected { get; init; }

    /// <summary>
    /// Whether this row is valid (no validation errors)
    /// </summary>
    public bool IsValid { get; init; }

    /// <summary>
    /// List of validation error messages for this row
    /// </summary>
    public IReadOnlyList<string> ValidationErrors { get; init; } = Array.Empty<string>();

    /// <summary>
    /// List of detailed validation errors
    /// </summary>
    public IReadOnlyList<PublicValidationErrorViewModel> ValidationErrorDetails { get; init; } = Array.Empty<PublicValidationErrorViewModel>();

    /// <summary>
    /// Cell values for this row (column name → value)
    /// </summary>
    public IReadOnlyDictionary<string, object?> CellValues { get; init; } = new Dictionary<string, object?>();
}

/// <summary>
/// Public UI-friendly column view model for MVVM binding
/// </summary>
public sealed class PublicColumnViewModel
{
    /// <summary>
    /// Internal column name
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Display name for UI
    /// </summary>
    public string DisplayName { get; init; } = string.Empty;

    /// <summary>
    /// Whether column is visible
    /// </summary>
    public bool IsVisible { get; init; } = true;

    /// <summary>
    /// Column width in pixels
    /// </summary>
    public double Width { get; init; } = 100;

    /// <summary>
    /// Whether column is read-only
    /// </summary>
    public bool IsReadOnly { get; init; }

    /// <summary>
    /// Data type name (String, Int32, Double, etc.)
    /// </summary>
    public string DataType { get; init; } = "String";

    /// <summary>
    /// Current sort direction (None, Ascending, Descending)
    /// </summary>
    public string? SortDirection { get; init; }
}

/// <summary>
/// Public UI-friendly validation error view model
/// </summary>
public sealed class PublicValidationErrorViewModel
{
    /// <summary>
    /// Row index where error occurred
    /// </summary>
    public int RowIndex { get; init; }

    /// <summary>
    /// Unique stable row identifier (RowID) where error occurred.
    /// CRITICAL: Use this for UI mapping as RowIndex changes after sort/filter.
    /// </summary>
    public string RowId { get; init; } = string.Empty;

    /// <summary>
    /// Column name where error occurred
    /// </summary>
    public string ColumnName { get; init; } = string.Empty;

    /// <summary>
    /// Error message
    /// </summary>
    public string Message { get; init; } = string.Empty;

    /// <summary>
    /// Severity (Error, Warning, Info)
    /// </summary>
    public string Severity { get; init; } = "Error";

    /// <summary>
    /// Error code for categorization
    /// </summary>
    public string ErrorCode { get; init; } = string.Empty;
}
