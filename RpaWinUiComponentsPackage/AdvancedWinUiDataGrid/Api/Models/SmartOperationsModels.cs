namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid;

/// <summary>
/// Public pattern information detected in data
/// </summary>
public sealed class PublicPatternInfo
{
    /// <summary>
    /// Type of pattern detected
    /// </summary>
    public PublicPatternType PatternType { get; init; }

    /// <summary>
    /// Pattern description
    /// </summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>
    /// Confidence level (0.0-1.0)
    /// </summary>
    public double Confidence { get; init; }

    /// <summary>
    /// Pattern parameters
    /// </summary>
    public IReadOnlyDictionary<string, object?> Parameters { get; init; } = new Dictionary<string, object?>();
}

/// <summary>
/// Pattern type enumeration
/// </summary>
public enum PublicPatternType
{
    None = 0,
    Numeric = 1,
    Date = 2,
    Text = 3,
    Custom = 4
}

/// <summary>
/// Public anomaly information
/// </summary>
public sealed class PublicAnomalyInfo
{
    /// <summary>
    /// Row index of anomaly
    /// </summary>
    public int RowIndex { get; init; }

    /// <summary>
    /// Column name
    /// </summary>
    public string ColumnName { get; init; } = string.Empty;

    /// <summary>
    /// Anomalous value
    /// </summary>
    public object? Value { get; init; }

    /// <summary>
    /// Anomaly score (higher = more anomalous)
    /// </summary>
    public double Score { get; init; }

    /// <summary>
    /// Anomaly reason/description
    /// </summary>
    public string Reason { get; init; } = string.Empty;
}
