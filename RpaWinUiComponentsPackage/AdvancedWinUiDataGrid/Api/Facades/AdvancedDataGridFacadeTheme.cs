using System.Data;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Import.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Export.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Validation.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.CopyPaste.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Selection.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Column.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Filter.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.AutoRowHeight.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Initialization.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Initialization.Commands;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Initialization.Models;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Common;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Api.Mappings;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Common.Models;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Configuration;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Infrastructure.Persistence.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Infrastructure.Logging.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Infrastructure.Logging.NullPattern;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid;

/// <summary>
/// Partial class containing Theme and Color Management + Theme Mapping Methods
/// </summary>
public sealed partial class AdvancedDataGridFacade : IAdvancedDataGridFacade
{
    #region Theme and Color Management

    /// <summary>
    /// Applies a theme to the grid
    /// </summary>
    public async Task<PublicResult> ApplyThemeAsync(PublicGridTheme theme)
    {
        ThrowIfDisposed();
        if (theme == null)
            throw new ArgumentNullException(nameof(theme));

        _logger.LogDebug("Applying theme: {ThemeName}", theme.ThemeName);

        var internalTheme = MapToInternalTheme(theme);
        _themeService.ApplyTheme(internalTheme);

        // Refresh UI to apply theme changes
        await RefreshUIAsync();

        _logger.LogInformation("Theme applied successfully: {ThemeName}", theme.ThemeName);
        return PublicResult.Success();
    }

    /// <summary>
    /// Gets the current active theme
    /// </summary>
    public PublicGridTheme GetCurrentTheme()
    {
        ThrowIfDisposed();
        var internalTheme = _themeService.GetCurrentTheme();
        return MapToPublicTheme(internalTheme);
    }

    /// <summary>
    /// Resets theme to default
    /// </summary>
    public async Task<PublicResult> ResetThemeToDefaultAsync()
    {
        ThrowIfDisposed();
        _logger.LogDebug("Resetting theme to default");

        _themeService.ResetToDefault();

        // Refresh UI to apply theme changes
        await RefreshUIAsync();

        _logger.LogInformation("Theme reset to default successfully");
        return PublicResult.Success();
    }

    /// <summary>
    /// Updates specific cell colors without changing entire theme
    /// </summary>
    public async Task<PublicResult> UpdateCellColorsAsync(PublicCellColors cellColors)
    {
        ThrowIfDisposed();
        if (cellColors == null)
            throw new ArgumentNullException(nameof(cellColors));

        _logger.LogDebug("Updating cell colors");

        var internalCellColors = MapToInternalCellColors(cellColors);
        _themeService.UpdateCellColors(internalCellColors);

        // Refresh UI to apply color changes
        await RefreshUIAsync();

        _logger.LogInformation("Cell colors updated successfully");
        return PublicResult.Success();
    }

    /// <summary>
    /// Updates specific row colors without changing entire theme
    /// </summary>
    public async Task<PublicResult> UpdateRowColorsAsync(PublicRowColors rowColors)
    {
        ThrowIfDisposed();
        if (rowColors == null)
            throw new ArgumentNullException(nameof(rowColors));

        _logger.LogDebug("Updating row colors");

        var internalRowColors = MapToInternalRowColors(rowColors);
        _themeService.UpdateRowColors(internalRowColors);

        // Refresh UI to apply color changes
        await RefreshUIAsync();

        _logger.LogInformation("Row colors updated successfully");
        return PublicResult.Success();
    }

    /// <summary>
    /// Updates specific validation colors without changing entire theme
    /// </summary>
    public async Task<PublicResult> UpdateValidationColorsAsync(PublicValidationColors validationColors)
    {
        ThrowIfDisposed();
        if (validationColors == null)
            throw new ArgumentNullException(nameof(validationColors));

        _logger.LogDebug("Updating validation colors");

        var internalValidationColors = MapToInternalValidationColors(validationColors);
        _themeService.UpdateValidationColors(internalValidationColors);

        // Refresh UI to apply color changes
        await RefreshUIAsync();

        _logger.LogInformation("Validation colors updated successfully");
        return PublicResult.Success();
    }

    /// <summary>
    /// Creates a dark theme preset
    /// </summary>
    public PublicGridTheme CreateDarkTheme()
    {
        ThrowIfDisposed();
        _logger.LogDebug("Creating dark theme preset");
        return MapToPublicTheme(Features.Color.GridTheme.Dark);
    }

    /// <summary>
    /// Creates a light theme preset
    /// </summary>
    public PublicGridTheme CreateLightTheme()
    {
        ThrowIfDisposed();
        _logger.LogDebug("Creating light theme preset");
        return MapToPublicTheme(Features.Color.GridTheme.Light);
    }

    /// <summary>
    /// Creates a high contrast theme preset
    /// </summary>
    public PublicGridTheme CreateHighContrastTheme()
    {
        ThrowIfDisposed();
        _logger.LogDebug("Creating high contrast theme preset");
        return MapToPublicTheme(Features.Color.GridTheme.HighContrast);
    }

    #region Theme Mapping Methods

    private static Features.Color.GridTheme MapToInternalTheme(PublicGridTheme publicTheme)
    {
        return new Features.Color.GridTheme
        {
            ThemeName = publicTheme.ThemeName,
            CellColors = MapToInternalCellColors(publicTheme.CellColors),
            RowColors = MapToInternalRowColors(publicTheme.RowColors),
            HeaderColors = MapToInternalHeaderColors(publicTheme.HeaderColors),
            ValidationColors = MapToInternalValidationColors(publicTheme.ValidationColors),
            SelectionColors = MapToInternalSelectionColors(publicTheme.SelectionColors),
            BorderColors = MapToInternalBorderColors(publicTheme.BorderColors)
        };
    }

    private static PublicGridTheme MapToPublicTheme(Features.Color.GridTheme internalTheme)
    {
        return new PublicGridTheme
        {
            ThemeName = internalTheme.ThemeName,
            CellColors = MapToPublicCellColors(internalTheme.CellColors),
            RowColors = MapToPublicRowColors(internalTheme.RowColors),
            HeaderColors = MapToPublicHeaderColors(internalTheme.HeaderColors),
            ValidationColors = MapToPublicValidationColors(internalTheme.ValidationColors),
            SelectionColors = MapToPublicSelectionColors(internalTheme.SelectionColors),
            BorderColors = MapToPublicBorderColors(internalTheme.BorderColors)
        };
    }

    private static Features.Color.CellColors MapToInternalCellColors(PublicCellColors publicColors)
    {
        return new Features.Color.CellColors
        {
            DefaultBackground = publicColors.DefaultBackground,
            DefaultForeground = publicColors.DefaultForeground,
            HoverBackground = publicColors.HoverBackground,
            HoverForeground = publicColors.HoverForeground,
            FocusedBackground = publicColors.FocusedBackground,
            FocusedForeground = publicColors.FocusedForeground,
            DisabledBackground = publicColors.DisabledBackground,
            DisabledForeground = publicColors.DisabledForeground,
            ReadOnlyBackground = publicColors.ReadOnlyBackground,
            ReadOnlyForeground = publicColors.ReadOnlyForeground
        };
    }

    private static PublicCellColors MapToPublicCellColors(Features.Color.CellColors internalColors)
    {
        return new PublicCellColors
        {
            DefaultBackground = internalColors.DefaultBackground,
            DefaultForeground = internalColors.DefaultForeground,
            HoverBackground = internalColors.HoverBackground,
            HoverForeground = internalColors.HoverForeground,
            FocusedBackground = internalColors.FocusedBackground,
            FocusedForeground = internalColors.FocusedForeground,
            DisabledBackground = internalColors.DisabledBackground,
            DisabledForeground = internalColors.DisabledForeground,
            ReadOnlyBackground = internalColors.ReadOnlyBackground,
            ReadOnlyForeground = internalColors.ReadOnlyForeground
        };
    }

    private static Features.Color.RowColors MapToInternalRowColors(PublicRowColors publicColors)
    {
        return new Features.Color.RowColors
        {
            EvenRowBackground = publicColors.EvenRowBackground,
            OddRowBackground = publicColors.OddRowBackground,
            HoverBackground = publicColors.HoverBackground,
            SelectedBackground = publicColors.SelectedBackground,
            SelectedForeground = publicColors.SelectedForeground,
            SelectedInactiveBackground = publicColors.SelectedInactiveBackground,
            SelectedInactiveForeground = publicColors.SelectedInactiveForeground
        };
    }

    private static PublicRowColors MapToPublicRowColors(Features.Color.RowColors internalColors)
    {
        return new PublicRowColors
        {
            EvenRowBackground = internalColors.EvenRowBackground,
            OddRowBackground = internalColors.OddRowBackground,
            HoverBackground = internalColors.HoverBackground,
            SelectedBackground = internalColors.SelectedBackground,
            SelectedForeground = internalColors.SelectedForeground,
            SelectedInactiveBackground = internalColors.SelectedInactiveBackground,
            SelectedInactiveForeground = internalColors.SelectedInactiveForeground
        };
    }

    private static Features.Color.HeaderColors MapToInternalHeaderColors(PublicHeaderColors publicColors)
    {
        return new Features.Color.HeaderColors
        {
            Background = publicColors.Background,
            Foreground = publicColors.Foreground,
            HoverBackground = publicColors.HoverBackground,
            PressedBackground = publicColors.PressedBackground,
            SortIndicatorColor = publicColors.SortIndicatorColor
        };
    }

    private static PublicHeaderColors MapToPublicHeaderColors(Features.Color.HeaderColors internalColors)
    {
        return new PublicHeaderColors
        {
            Background = internalColors.Background,
            Foreground = internalColors.Foreground,
            HoverBackground = internalColors.HoverBackground,
            PressedBackground = internalColors.PressedBackground,
            SortIndicatorColor = internalColors.SortIndicatorColor
        };
    }

    private static Features.Color.ValidationColors MapToInternalValidationColors(PublicValidationColors publicColors)
    {
        return new Features.Color.ValidationColors
        {
            ErrorBackground = publicColors.ErrorBackground,
            ErrorForeground = publicColors.ErrorForeground,
            ErrorBorder = publicColors.ErrorBorder,
            WarningBackground = publicColors.WarningBackground,
            WarningForeground = publicColors.WarningForeground,
            WarningBorder = publicColors.WarningBorder,
            InfoBackground = publicColors.InfoBackground,
            InfoForeground = publicColors.InfoForeground,
            InfoBorder = publicColors.InfoBorder
        };
    }

    private static PublicValidationColors MapToPublicValidationColors(Features.Color.ValidationColors internalColors)
    {
        return new PublicValidationColors
        {
            ErrorBackground = internalColors.ErrorBackground,
            ErrorForeground = internalColors.ErrorForeground,
            ErrorBorder = internalColors.ErrorBorder,
            WarningBackground = internalColors.WarningBackground,
            WarningForeground = internalColors.WarningForeground,
            WarningBorder = internalColors.WarningBorder,
            InfoBackground = internalColors.InfoBackground,
            InfoForeground = internalColors.InfoForeground,
            InfoBorder = internalColors.InfoBorder
        };
    }

    private static Features.Color.SelectionColors MapToInternalSelectionColors(PublicSelectionColors publicColors)
    {
        return new Features.Color.SelectionColors
        {
            SelectionBorder = publicColors.SelectionBorder,
            SelectionFill = publicColors.SelectionFill,
            MultiSelectionBackground = publicColors.MultiSelectionBackground,
            MultiSelectionForeground = publicColors.MultiSelectionForeground
        };
    }

    private static PublicSelectionColors MapToPublicSelectionColors(Features.Color.SelectionColors internalColors)
    {
        return new PublicSelectionColors
        {
            SelectionBorder = internalColors.SelectionBorder,
            SelectionFill = internalColors.SelectionFill,
            MultiSelectionBackground = internalColors.MultiSelectionBackground,
            MultiSelectionForeground = internalColors.MultiSelectionForeground
        };
    }

    private static Features.Color.BorderColors MapToInternalBorderColors(PublicBorderColors publicColors)
    {
        return new Features.Color.BorderColors
        {
            CellBorder = publicColors.CellBorder,
            RowBorder = publicColors.RowBorder,
            ColumnBorder = publicColors.ColumnBorder,
            GridBorder = publicColors.GridBorder,
            FocusedCellBorder = publicColors.FocusedCellBorder
        };
    }

    private static PublicBorderColors MapToPublicBorderColors(Features.Color.BorderColors internalColors)
    {
        return new PublicBorderColors
        {
            CellBorder = internalColors.CellBorder,
            RowBorder = internalColors.RowBorder,
            ColumnBorder = internalColors.ColumnBorder,
            GridBorder = internalColors.GridBorder,
            FocusedCellBorder = internalColors.FocusedCellBorder
        };
    }

    #endregion

    #endregion
}

