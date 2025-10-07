
namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid;

/// <summary>
/// Public column definition DTO
/// </summary>
public class PublicColumnDefinition
{
    public string Name { get; set; } = string.Empty;
    public string Header { get; set; } = string.Empty;
    public Type DataType { get; set; } = typeof(string);
    public PublicSortDirection SortDirection { get; set; } = PublicSortDirection.None;
    public double Width { get; set; } = 100.0;
    public double MinWidth { get; set; } = 50.0;
    public double MaxWidth { get; set; } = double.MaxValue;
    public bool IsVisible { get; set; } = true;
    public bool IsReadOnly { get; set; } = false;
    public bool IsSortable { get; set; } = true;
    public bool IsFilterable { get; set; } = true;
    public bool IsResizable { get; set; } = true;
    public int DisplayOrder { get; set; } = 0;
    public string? FormatString { get; set; }
    public object? DefaultValue { get; set; }
    public PublicSpecialColumnType SpecialType { get; set; } = PublicSpecialColumnType.None;
    public List<PublicValidationRule>? ValidationRules { get; set; }
}

public enum PublicSpecialColumnType
{
    None = 0,
    RowNumber = 1,
    Checkbox = 2,
    ValidationAlerts = 3
}
