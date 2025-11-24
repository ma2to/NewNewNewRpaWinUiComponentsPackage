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
            // ✅ FIX #27.2: ALWAYS use opaque light red (#FFEBEE), ignore ThemeManager cached value
            // REASON: ThemeManager may have stale cached theme with old "#1EFF0000" value
            // USER COMPLAINT: "chyba s ciernym background sa aj tak nevyriesila" (after 30+ fix attempts!)
            // SOLUTION: Hardcode opaque light red to ensure consistency with data cell validation errors
            // NOTE: #FFEBEE = RGB(255, 235, 238) - same as CellElementColors.Error.Background default
            var brush = Features.Optimization.BrushPool.GetBrush(
                Windows.UI.Color.FromArgb(255, 255, 235, 238)  // #FFEBEE - opaque light red
            );

            _logger?.LogDebug("🎨 CONVERTER RESULT: FORCED OPAQUE LIGHT RED (#FFEBEE = 255,235,238)");
            return brush;
        }

        // No validation alert - return default background (opaque white for visibility)
        // ✅ FIX #27.2: Use hardcoded white instead of ThemeManager for consistency
        var whiteBrush = Features.Optimization.BrushPool.GetBrush(
            Windows.UI.Color.FromArgb(255, 255, 255, 255)  // Opaque white
        );

        _logger?.LogDebug("🎨 CONVERTER RESULT: WHITE (255,255,255)");
        return whiteBrush;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException("ConvertBack is not supported for ValidationAlertBackgroundConverter");
    }
}
