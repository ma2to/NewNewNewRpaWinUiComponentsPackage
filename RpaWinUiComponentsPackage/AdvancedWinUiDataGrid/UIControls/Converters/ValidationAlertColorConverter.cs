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
            // ✅ CRITICAL FIX: Use Windows.UI.Color EXPLICITLY
            // REASON: BrushPool expects Windows.UI.Color, NOT Microsoft.UI.Color
            // PREVIOUS BUG: Ambiguous Color.FromArgb resolved to wrong type → BLACK background (13th fix attempt!)
            // ROOT CAUSE: Type mismatch between Microsoft.UI.Color and Windows.UI.Color → struct corruption
            // SOLUTION: Explicit namespace qualification ensures correct type
            return Features.Optimization.BrushPool.GetBrush(
                Windows.UI.Color.FromArgb(255, 255, 220, 220)  // Opaque light red
            );
        }

        // No validation alert - return default background (opaque white for visibility)
        if (_themeManager?.CellDefaultBackground != null)
        {
            return _themeManager.CellDefaultBackground;
        }
        // ✅ CRITICAL FIX: Convert Microsoft.UI.Colors.White to Windows.UI.Color
        // REASON: BrushPool.GetBrush expects Windows.UI.Color parameter
        // PREVIOUS BUG: Type mismatch caused by passing Microsoft.UI.Color → BLACK background
        // SOLUTION: Create Windows.UI.Color from RGBA values
        return Features.Optimization.BrushPool.GetBrush(
            Windows.UI.Color.FromArgb(255, 255, 255, 255)  // Opaque white
        );
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException("ConvertBack is not supported for ValidationAlertBackgroundConverter");
    }
}
