namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid;

/// <summary>
/// Public filter descriptor defining filter operation for a column
/// </summary>
public sealed class PublicFilterDescriptor
{
    /// <summary>
    /// Column name to filter
    /// </summary>
    public string ColumnName { get; init; } = string.Empty;

    /// <summary>
    /// Filter operator
    /// </summary>
    public PublicFilterOperator Operator { get; init; } = PublicFilterOperator.Equals;

    /// <summary>
    /// Filter value
    /// </summary>
    public object? Value { get; init; }

    /// <summary>
    /// Second value for range filters
    /// </summary>
    public object? Value2 { get; init; }

    /// <summary>
    /// Whether filter is case sensitive (for string comparisons)
    /// </summary>
    public bool CaseSensitive { get; init; } = false;
}
