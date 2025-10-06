using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Common;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Common.Models;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Core.ValueObjects;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Api.Internal;

/// <summary>
/// Mapping extensions for column-related types
/// </summary>
internal static class ColumnMappings
{
    public static PublicColumnDefinition ToPublic(this ColumnDefinition column)
    {
        var sortDir = column.SortDirection switch
        {
            Common.SortDirection.None => PublicSortDirection.None,
            Common.SortDirection.Ascending => PublicSortDirection.Ascending,
            Common.SortDirection.Descending => PublicSortDirection.Descending,
            _ => PublicSortDirection.None
        };

        return new PublicColumnDefinition
        {
            Name = column.Name,
            Header = column.Header,
            DataType = column.DataType,
            SortDirection = sortDir,
            Width = column.Width,
            MinWidth = column.MinWidth,
            MaxWidth = column.MaxWidth,
            IsVisible = column.IsVisible,
            IsReadOnly = column.IsReadOnly,
            IsSortable = column.IsSortable,
            IsFilterable = column.IsFilterable,
            IsResizable = column.IsResizable,
            DisplayOrder = column.DisplayOrder,
            FormatString = column.FormatString,
            DefaultValue = column.DefaultValue,
            SpecialType = column.SpecialType.ToPublic()
        };
    }

    public static ColumnDefinition ToInternal(this PublicColumnDefinition column)
    {
        var sortDir = column.SortDirection switch
        {
            PublicSortDirection.None => Common.SortDirection.None,
            PublicSortDirection.Ascending => Common.SortDirection.Ascending,
            PublicSortDirection.Descending => Common.SortDirection.Descending,
            _ => Common.SortDirection.None
        };

        return new ColumnDefinition
        {
            Name = column.Name,
            Header = column.Header,
            DataType = column.DataType,
            SortDirection = sortDir,
            Width = column.Width,
            MinWidth = column.MinWidth,
            MaxWidth = column.MaxWidth,
            IsVisible = column.IsVisible,
            IsReadOnly = column.IsReadOnly,
            IsSortable = column.IsSortable,
            IsFilterable = column.IsFilterable,
            IsResizable = column.IsResizable,
            DisplayOrder = column.DisplayOrder,
            FormatString = column.FormatString,
            DefaultValue = column.DefaultValue,
            SpecialType = column.SpecialType.ToInternal()
        };
    }

    public static List<PublicColumnDefinition> ToPublicList(this IEnumerable<ColumnDefinition> columns)
    {
        return columns.Select(c => c.ToPublic()).ToList();
    }
}
