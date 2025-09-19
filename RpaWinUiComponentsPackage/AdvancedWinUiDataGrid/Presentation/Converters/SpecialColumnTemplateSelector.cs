using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Presentation.ViewModels;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Presentation.Converters;

/// <summary>
/// PRESENTATION: Template selector for different types of data grid cells
/// SPECIALIZATION: Selects appropriate template based on cell type and column special type
/// PERFORMANCE: Optimized template selection for various cell rendering scenarios
/// </summary>
internal sealed class SpecialColumnTemplateSelector : DataTemplateSelector
{
    #region Template Properties

    /// <summary>Template for standard data cells with text input</summary>
    public DataTemplate? StandardCellTemplate { get; set; }

    /// <summary>Template for checkbox cells</summary>
    public DataTemplate? CheckBoxCellTemplate { get; set; }

    /// <summary>Template for delete button cells</summary>
    public DataTemplate? DeleteButtonCellTemplate { get; set; }

    /// <summary>Template for row number cells</summary>
    public DataTemplate? RowNumberCellTemplate { get; set; }

    /// <summary>Template for validation alerts cells</summary>
    public DataTemplate? ValidationAlertsCellTemplate { get; set; }

    /// <summary>Template for read-only cells</summary>
    public DataTemplate? ReadOnlyCellTemplate { get; set; }

    /// <summary>Template for numeric cells with right alignment</summary>
    public DataTemplate? NumericCellTemplate { get; set; }

    /// <summary>Template for date/time cells with date picker</summary>
    public DataTemplate? DateTimeCellTemplate { get; set; }

    #endregion

    protected override DataTemplate? SelectTemplateCore(object item, DependencyObject container)
    {
        if (item is not CellViewModel cellViewModel)
            return StandardCellTemplate;

        // Select template based on special cell types first
        if (cellViewModel.IsCheckBoxCell)
            return CheckBoxCellTemplate ?? StandardCellTemplate;

        if (cellViewModel.IsDeleteButtonCell)
            return DeleteButtonCellTemplate ?? StandardCellTemplate;

        if (cellViewModel.IsRowNumberCell)
            return RowNumberCellTemplate ?? StandardCellTemplate;

        if (cellViewModel.IsValidationAlertsCell)
            return ValidationAlertsCellTemplate ?? StandardCellTemplate;

        // Select template based on read-only state
        if (cellViewModel.IsReadOnly && ReadOnlyCellTemplate != null)
            return ReadOnlyCellTemplate;

        // Select template based on data type
        if (cellViewModel.Value != null)
        {
            var valueType = cellViewModel.Value.GetType();

            // Numeric types
            if (IsNumericType(valueType) && NumericCellTemplate != null)
                return NumericCellTemplate;

            // Date/Time types
            if (IsDateTimeType(valueType) && DateTimeCellTemplate != null)
                return DateTimeCellTemplate;
        }

        // Default to standard template
        return StandardCellTemplate;
    }

    #region Helper Methods

    /// <summary>Determines if the type is numeric</summary>
    private static bool IsNumericType(Type type)
    {
        return type == typeof(int) || type == typeof(int?) ||
               type == typeof(long) || type == typeof(long?) ||
               type == typeof(decimal) || type == typeof(decimal?) ||
               type == typeof(double) || type == typeof(double?) ||
               type == typeof(float) || type == typeof(float?) ||
               type == typeof(short) || type == typeof(short?) ||
               type == typeof(byte) || type == typeof(byte?);
    }

    /// <summary>Determines if the type is date/time related</summary>
    private static bool IsDateTimeType(Type type)
    {
        return type == typeof(DateTime) || type == typeof(DateTime?) ||
               type == typeof(DateTimeOffset) || type == typeof(DateTimeOffset?) ||
               type == typeof(TimeSpan) || type == typeof(TimeSpan?);
    }

    #endregion
}

/// <summary>
/// PRESENTATION: Template selector for column headers based on column type
/// SPECIALIZATION: Different header styles for special columns vs standard data columns
/// </summary>
internal sealed class ColumnHeaderTemplateSelector : DataTemplateSelector
{
    #region Template Properties

    /// <summary>Template for standard column headers</summary>
    public DataTemplate? StandardHeaderTemplate { get; set; }

    /// <summary>Template for special column headers (icons, minimal text)</summary>
    public DataTemplate? SpecialColumnHeaderTemplate { get; set; }

    /// <summary>Template for sortable column headers with sort indicators</summary>
    public DataTemplate? SortableHeaderTemplate { get; set; }

    /// <summary>Template for resizable column headers with resize handle</summary>
    public DataTemplate? ResizableHeaderTemplate { get; set; }

    #endregion

    protected override DataTemplate? SelectTemplateCore(object item, DependencyObject container)
    {
        if (item is not DataColumnViewModel columnViewModel)
            return StandardHeaderTemplate;

        // Special columns get minimal header templates
        if (!columnViewModel.IsDataColumn && SpecialColumnHeaderTemplate != null)
            return SpecialColumnHeaderTemplate;

        // Resizable columns get resize handles
        if (columnViewModel.IsResizable && ResizableHeaderTemplate != null)
            return ResizableHeaderTemplate;

        // Sortable columns get sort indicators
        if (columnViewModel.IsSortable && SortableHeaderTemplate != null)
            return SortableHeaderTemplate;

        // Default to standard header template
        return StandardHeaderTemplate;
    }
}

/// <summary>
/// PRESENTATION: Style selector for different validation states
/// STYLING: Applies different styles based on validation severity and cell state
/// </summary>
internal sealed class ValidationStyleSelector : StyleSelector
{
    #region Style Properties

    /// <summary>Style for cells with no validation issues</summary>
    public Style? NormalCellStyle { get; set; }

    /// <summary>Style for cells with validation errors</summary>
    public Style? ErrorCellStyle { get; set; }

    /// <summary>Style for cells with validation warnings</summary>
    public Style? WarningCellStyle { get; set; }

    /// <summary>Style for cells with validation info messages</summary>
    public Style? InfoCellStyle { get; set; }

    /// <summary>Style for cells with unsaved changes</summary>
    public Style? ModifiedCellStyle { get; set; }

    /// <summary>Style for read-only cells</summary>
    public Style? ReadOnlyCellStyle { get; set; }

    #endregion

    protected override Style? SelectStyleCore(object item, DependencyObject container)
    {
        if (item is not CellViewModel cellViewModel)
            return NormalCellStyle;

        // Read-only cells get special styling
        if (cellViewModel.IsReadOnly && ReadOnlyCellStyle != null)
            return ReadOnlyCellStyle;

        // Validation states take precedence
        if (cellViewModel.HasValidationErrors)
        {
            return cellViewModel.HighestSeverity switch
            {
                Core.Enums.ValidationSeverity.Error => ErrorCellStyle ?? NormalCellStyle,
                Core.Enums.ValidationSeverity.Warning => WarningCellStyle ?? NormalCellStyle,
                Core.Enums.ValidationSeverity.Info => InfoCellStyle ?? NormalCellStyle,
                _ => NormalCellStyle
            };
        }

        // Modified cells get special styling
        if (cellViewModel.HasUnsavedChanges && ModifiedCellStyle != null)
            return ModifiedCellStyle;

        // Default style
        return NormalCellStyle;
    }
}