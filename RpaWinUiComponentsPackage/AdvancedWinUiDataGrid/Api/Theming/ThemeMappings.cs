using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Api.Models;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Color;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Api.Theming;

/// <summary>
/// Extension mappings for theme models
/// </summary>
internal static class ThemeMappings
{
    #region GridTheme Mappings

    /// <summary>
    /// Converts public grid theme to internal grid theme
    /// </summary>
    internal static GridTheme ToInternal(this PublicGridTheme source)
    {
        return new GridTheme
        {
            ThemeName = source.ThemeName,
            CellColors = source.CellColors.ToInternal(),
            RowColors = source.RowColors.ToInternal(),
            HeaderColors = source.HeaderColors.ToInternal(),
            ValidationColors = source.ValidationColors.ToInternal(),
            SelectionColors = source.SelectionColors.ToInternal(),
            BorderColors = source.BorderColors.ToInternal()
        };
    }

    /// <summary>
    /// Converts internal grid theme to public grid theme
    /// </summary>
    internal static PublicGridTheme ToPublic(this GridTheme source)
    {
        return new PublicGridTheme
        {
            ThemeName = source.ThemeName,
            CellColors = source.CellColors.ToPublic(),
            RowColors = source.RowColors.ToPublic(),
            HeaderColors = source.HeaderColors.ToPublic(),
            ValidationColors = source.ValidationColors.ToPublic(),
            SelectionColors = source.SelectionColors.ToPublic(),
            BorderColors = source.BorderColors.ToPublic()
        };
    }

    #endregion

    #region CellColors Mappings

    /// <summary>
    /// Converts public cell colors to internal cell colors
    /// </summary>
    internal static CellColors ToInternal(this PublicCellColors source)
    {
        return new CellColors
        {
            DefaultBackground = source.DefaultBackground,
            DefaultForeground = source.DefaultForeground,
            HoverBackground = source.HoverBackground,
            HoverForeground = source.HoverForeground,
            FocusedBackground = source.FocusedBackground,
            FocusedForeground = source.FocusedForeground,
            DisabledBackground = source.DisabledBackground,
            DisabledForeground = source.DisabledForeground,
            ReadOnlyBackground = source.ReadOnlyBackground,
            ReadOnlyForeground = source.ReadOnlyForeground
        };
    }

    /// <summary>
    /// Converts internal cell colors to public cell colors
    /// </summary>
    internal static PublicCellColors ToPublic(this CellColors source)
    {
        return new PublicCellColors
        {
            DefaultBackground = source.DefaultBackground,
            DefaultForeground = source.DefaultForeground,
            HoverBackground = source.HoverBackground,
            HoverForeground = source.HoverForeground,
            FocusedBackground = source.FocusedBackground,
            FocusedForeground = source.FocusedForeground,
            DisabledBackground = source.DisabledBackground,
            DisabledForeground = source.DisabledForeground,
            ReadOnlyBackground = source.ReadOnlyBackground,
            ReadOnlyForeground = source.ReadOnlyForeground
        };
    }

    #endregion

    #region RowColors Mappings

    /// <summary>
    /// Converts public row colors to internal row colors
    /// </summary>
    internal static RowColors ToInternal(this PublicRowColors source)
    {
        return new RowColors
        {
            EvenRowBackground = source.EvenRowBackground,
            OddRowBackground = source.OddRowBackground,
            HoverBackground = source.HoverBackground,
            SelectedBackground = source.SelectedBackground,
            SelectedForeground = source.SelectedForeground,
            SelectedInactiveBackground = source.SelectedInactiveBackground,
            SelectedInactiveForeground = source.SelectedInactiveForeground
        };
    }

    /// <summary>
    /// Converts internal row colors to public row colors
    /// </summary>
    internal static PublicRowColors ToPublic(this RowColors source)
    {
        return new PublicRowColors
        {
            EvenRowBackground = source.EvenRowBackground,
            OddRowBackground = source.OddRowBackground,
            HoverBackground = source.HoverBackground,
            SelectedBackground = source.SelectedBackground,
            SelectedForeground = source.SelectedForeground,
            SelectedInactiveBackground = source.SelectedInactiveBackground,
            SelectedInactiveForeground = source.SelectedInactiveForeground
        };
    }

    #endregion

    #region HeaderColors Mappings

    /// <summary>
    /// Converts public header colors to internal header colors
    /// </summary>
    internal static HeaderColors ToInternal(this PublicHeaderColors source)
    {
        return new HeaderColors
        {
            Background = source.Background,
            Foreground = source.Foreground,
            HoverBackground = source.HoverBackground,
            PressedBackground = source.PressedBackground,
            SortIndicatorColor = source.SortIndicatorColor
        };
    }

    /// <summary>
    /// Converts internal header colors to public header colors
    /// </summary>
    internal static PublicHeaderColors ToPublic(this HeaderColors source)
    {
        return new PublicHeaderColors
        {
            Background = source.Background,
            Foreground = source.Foreground,
            HoverBackground = source.HoverBackground,
            PressedBackground = source.PressedBackground,
            SortIndicatorColor = source.SortIndicatorColor
        };
    }

    #endregion

    #region ValidationColors Mappings

    /// <summary>
    /// Converts public validation colors to internal validation colors
    /// </summary>
    internal static ValidationColors ToInternal(this PublicValidationColors source)
    {
        return new ValidationColors
        {
            ErrorBackground = source.ErrorBackground,
            ErrorForeground = source.ErrorForeground,
            ErrorBorder = source.ErrorBorder,
            WarningBackground = source.WarningBackground,
            WarningForeground = source.WarningForeground,
            WarningBorder = source.WarningBorder,
            InfoBackground = source.InfoBackground,
            InfoForeground = source.InfoForeground,
            InfoBorder = source.InfoBorder
        };
    }

    /// <summary>
    /// Converts internal validation colors to public validation colors
    /// </summary>
    internal static PublicValidationColors ToPublic(this ValidationColors source)
    {
        return new PublicValidationColors
        {
            ErrorBackground = source.ErrorBackground,
            ErrorForeground = source.ErrorForeground,
            ErrorBorder = source.ErrorBorder,
            WarningBackground = source.WarningBackground,
            WarningForeground = source.WarningForeground,
            WarningBorder = source.WarningBorder,
            InfoBackground = source.InfoBackground,
            InfoForeground = source.InfoForeground,
            InfoBorder = source.InfoBorder
        };
    }

    #endregion

    #region SelectionColors Mappings

    /// <summary>
    /// Converts public selection colors to internal selection colors
    /// </summary>
    internal static SelectionColors ToInternal(this PublicSelectionColors source)
    {
        return new SelectionColors
        {
            SelectionBorder = source.SelectionBorder,
            SelectionFill = source.SelectionFill,
            MultiSelectionBackground = source.MultiSelectionBackground,
            MultiSelectionForeground = source.MultiSelectionForeground
        };
    }

    /// <summary>
    /// Converts internal selection colors to public selection colors
    /// </summary>
    internal static PublicSelectionColors ToPublic(this SelectionColors source)
    {
        return new PublicSelectionColors
        {
            SelectionBorder = source.SelectionBorder,
            SelectionFill = source.SelectionFill,
            MultiSelectionBackground = source.MultiSelectionBackground,
            MultiSelectionForeground = source.MultiSelectionForeground
        };
    }

    #endregion

    #region BorderColors Mappings

    /// <summary>
    /// Converts public border colors to internal border colors
    /// </summary>
    internal static BorderColors ToInternal(this PublicBorderColors source)
    {
        return new BorderColors
        {
            CellBorder = source.CellBorder,
            RowBorder = source.RowBorder,
            ColumnBorder = source.ColumnBorder,
            GridBorder = source.GridBorder,
            FocusedCellBorder = source.FocusedCellBorder
        };
    }

    /// <summary>
    /// Converts internal border colors to public border colors
    /// </summary>
    internal static PublicBorderColors ToPublic(this BorderColors source)
    {
        return new PublicBorderColors
        {
            CellBorder = source.CellBorder,
            RowBorder = source.RowBorder,
            ColumnBorder = source.ColumnBorder,
            GridBorder = source.GridBorder,
            FocusedCellBorder = source.FocusedCellBorder
        };
    }

    #endregion
}
