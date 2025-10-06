namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Api.Models;

/// <summary>
/// Public grid configuration
/// </summary>
public sealed class PublicGridConfiguration
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
    public bool VirtualizationEnabled { get; init; }

    /// <summary>
    /// Editing enabled
    /// </summary>
    public bool EditingEnabled { get; init; }

    /// <summary>
    /// Shortcuts enabled
    /// </summary>
    public bool ShortcutsEnabled { get; init; }

    /// <summary>
    /// Custom settings dictionary
    /// </summary>
    public IReadOnlyDictionary<string, object?> CustomSettings { get; init; } = new Dictionary<string, object?>();
}
