using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Windows.UI;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.ViewModels;
using Microsoft.Extensions.Logging;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.UIControls.Converters;

/// <summary>
/// Converts HasValidationAlert boolean to appropriate Foreground brush.
/// Used for ValidationAlerts column text color.
/// </summary>
internal sealed class ValidationAlertForegroundConverter : IValueConverter
{
    private readonly ThemeManager? _themeManager;

    public ValidationAlertForegroundConverter(ThemeManager? themeManager)
    {
        _themeManager = themeManager;
    }

    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is bool hasAlert && hasAlert)
        {
            return _themeManager?.ValidationAlertsErrorForeground ?? new SolidColorBrush(Colors.Red);
        }

        return _themeManager?.CellDefaultForeground ?? new SolidColorBrush(Colors.Black);
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException("ConvertBack is not supported for ValidationAlertForegroundConverter");
    }
}

/// <summary>
/// Converts HasValidationAlert boolean to appropriate Background brush.
/// Used for ValidationAlerts column background color.
/// </summary>
internal sealed class ValidationAlertBackgroundConverter : IValueConverter
{
    private readonly ThemeManager? _themeManager;
    private readonly ILogger? _logger;

    public ValidationAlertBackgroundConverter(ThemeManager? themeManager, ILogger? logger = null)
    {
        _themeManager = themeManager;
        _logger = logger;
    }

    public object Convert(object value, Type targetType, object parameter, string language)
    {
        // ✅ CRITICAL FIX #23: Add comprehensive logging to diagnose BLACK background issue
        // ROOT CAUSE: Previous 30 fix attempts failed because converter execution was never verified
        // SOLUTION: Log every converter call to confirm binding is working
        _logger?.LogDebug("🎨 ValidationAlertBackgroundConverter.Convert CALLED: value={Value}, type={ValueType}, hasAlert={HasAlert}",
            value, value?.GetType().Name ?? "null", value is bool b && b);

        if (value is bool hasAlert && hasAlert)
        {
            // Has validation alert - return error background (light red)
            SolidColorBrush? brush;
            if (_themeManager?.ValidationAlertsErrorBackground != null)
            {
                brush = _themeManager.ValidationAlertsErrorBackground;
                _logger?.LogDebug("🎨 CONVERTER RESULT: ThemeManager error background, brush={BrushType}", brush.GetType().Name);
                return brush;
            }

            brush = Features.Optimization.BrushPool.GetBrush(
                Windows.UI.Color.FromArgb(255, 255, 220, 220)  // Opaque light red
            );
            _logger?.LogDebug("🎨 CONVERTER RESULT: LIGHT RED (255,220,220) from BrushPool, brush={BrushType}", brush?.GetType().Name ?? "null");
            return brush;
        }

        // No validation alert - return default background (opaque white for visibility)
        SolidColorBrush? whiteBrush;
        if (_themeManager?.CellDefaultBackground != null)
        {
            whiteBrush = _themeManager.CellDefaultBackground;
            _logger?.LogDebug("🎨 CONVERTER RESULT: ThemeManager default background, brush={BrushType}", whiteBrush.GetType().Name);
            return whiteBrush;
        }

        whiteBrush = Features.Optimization.BrushPool.GetBrush(
            Windows.UI.Color.FromArgb(255, 255, 255, 255)  // Opaque white
        );
        _logger?.LogDebug("🎨 CONVERTER RESULT: WHITE (255,255,255) from BrushPool, brush={BrushType}", whiteBrush?.GetType().Name ?? "null");
        return whiteBrush;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException("ConvertBack is not supported for ValidationAlertBackgroundConverter");
    }
}
