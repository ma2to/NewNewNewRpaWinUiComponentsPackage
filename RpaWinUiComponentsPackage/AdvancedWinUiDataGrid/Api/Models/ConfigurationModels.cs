namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid;

/// <summary>
/// Public model for DataGrid configuration
/// </summary>
public class PublicDataGridConfiguration
{
    /// <summary>
    /// Configuration name
    /// </summary>
    public string Name { get; init; } = "Default";

    /// <summary>
    /// Column configurations
    /// </summary>
    public IReadOnlyList<PublicColumnDefinition> Columns { get; init; } = Array.Empty<PublicColumnDefinition>();

    /// <summary>
    /// Sort descriptors
    /// </summary>
    public IReadOnlyList<PublicSortDescriptor> SortDescriptors { get; init; } = Array.Empty<PublicSortDescriptor>();

    /// <summary>
    /// Filter descriptors
    /// </summary>
    public IReadOnlyList<PublicFilterDescriptor> FilterDescriptors { get; init; } = Array.Empty<PublicFilterDescriptor>();

    /// <summary>
    /// Grid theme
    /// </summary>
    public PublicGridTheme? Theme { get; init; }

    /// <summary>
    /// Auto row height enabled
    /// </summary>
    public bool AutoRowHeightEnabled { get; init; }

    /// <summary>
    /// Virtualization enabled
    /// </summary>
    public bool VirtualizationEnabled { get; init; } = true;

    /// <summary>
    /// Batch updates enabled
    /// </summary>
    public bool EnableBatchUpdates { get; init; } = true;

    /// <summary>
    /// Validation enabled
    /// </summary>
    public bool EnableValidation { get; init; } = true;

    /// <summary>
    /// Zebra rows enabled
    /// </summary>
    public bool EnableZebraRows { get; init; } = false;

    /// <summary>
    /// Editing enabled
    /// </summary>
    public bool EditingEnabled { get; init; }

    /// <summary>
    /// Shortcuts enabled
    /// </summary>
    public bool ShortcutsEnabled { get; init; }

    /// <summary>
    /// Minimum column width
    /// </summary>
    public int MinimumColumnWidth { get; init; } = 50;

    /// <summary>
    /// Maximum column width
    /// </summary>
    public int MaximumColumnWidth { get; init; } = 500;

    /// <summary>
    /// Default row height
    /// </summary>
    public int DefaultRowHeight { get; init; } = 32;

    /// <summary>
    /// Custom settings dictionary
    /// </summary>
    public IReadOnlyDictionary<string, object?> CustomSettings { get; init; } = new Dictionary<string, object?>();

    public static PublicDataGridConfiguration Default => new();
}
