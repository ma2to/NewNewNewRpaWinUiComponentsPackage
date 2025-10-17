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
            return new SolidColorBrush(Colors.Red);
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
        if (value is bool hasAlert && hasAlert)
        {
            // Light red background for validation errors
            return new SolidColorBrush(Color.FromArgb(30, 255, 0, 0));
        }

        return _themeManager?.CellDefaultBackground ?? new SolidColorBrush(Colors.White);
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException("ConvertBack is not supported for ValidationAlertBackgroundConverter");
    }
}
