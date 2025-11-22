using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Windows.UI;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.ViewModels;

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

    public ValidationAlertBackgroundConverter(ThemeManager? themeManager)
    {
        _themeManager = themeManager;
    }

    public object Convert(object value, Type targetType, object parameter, string language)
    {
        // ✅ PROFESSIONAL FIX: Use BrushPool for guaranteed brush availability
        // REASON: Creating new SolidColorBrush in converter can fail → binding fail → Black background
        // SOLUTION: Use BrushPool which caches brushes and never returns null

        if (value is bool hasAlert && hasAlert)
        {
            // Has validation alert - return error background (light red)
            if (_themeManager?.ValidationAlertsErrorBackground != null)
            {
                return _themeManager.ValidationAlertsErrorBackground;
            }
            // ✅ CRITICAL FIX: Change alpha from 30 to 255 (opaque light red)
            // REASON: Alpha=30 appears black on dark backgrounds
            // USER ISSUE: "Black background in validAlerts column when sorting with errors"
            // SOLUTION: Use opaque light red (255, 255, 220, 220) for consistent visibility
            return Features.Optimization.BrushPool.GetBrush(Color.FromArgb(255, 255, 220, 220));
        }

        // No validation alert - return default background (white)
        if (_themeManager?.CellDefaultBackground != null)
        {
            return _themeManager.CellDefaultBackground;
        }
        // ✅ CRITICAL: Use BrushPool instead of new SolidColorBrush
        return Features.Optimization.BrushPool.GetBrush(Colors.White);
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException("ConvertBack is not supported for ValidationAlertBackgroundConverter");
    }
}
