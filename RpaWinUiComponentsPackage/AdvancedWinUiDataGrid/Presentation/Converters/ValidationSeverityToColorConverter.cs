using System;
using Microsoft.UI;
using Microsoft.UI.Xaml.Data;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Core.Enums;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Presentation.Converters;

/// <summary>
/// PRESENTATION: Converts ValidationSeverity enum to appropriate color for UI display
/// STYLING: Provides consistent color mapping for validation feedback
/// </summary>
internal sealed class ValidationSeverityToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is ValidationSeverity severity)
        {
            return severity switch
            {
                ValidationSeverity.Error => Colors.Red,
                ValidationSeverity.Warning => Colors.Orange,
                ValidationSeverity.Info => Colors.Blue,
                _ => Colors.Transparent
            };
        }

        return Colors.Transparent;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException("ValidationSeverityToColorConverter does not support ConvertBack");
    }
}

/// <summary>
/// PRESENTATION: Converts boolean values to Visibility enum for UI element visibility
/// COMMON: Standard converter for show/hide functionality
/// </summary>
internal sealed class BooleanToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is bool boolValue)
        {
            return boolValue ? Microsoft.UI.Xaml.Visibility.Visible : Microsoft.UI.Xaml.Visibility.Collapsed;
        }

        return Microsoft.UI.Xaml.Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        if (value is Microsoft.UI.Xaml.Visibility visibility)
        {
            return visibility == Microsoft.UI.Xaml.Visibility.Visible;
        }

        return false;
    }
}

/// <summary>
/// PRESENTATION: Negates boolean values for inverse binding scenarios
/// COMMON: Used for enabling/disabling controls based on inverse conditions
/// </summary>
internal sealed class BooleanNegationConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is bool boolValue)
        {
            return !boolValue;
        }

        return true;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        if (value is bool boolValue)
        {
            return !boolValue;
        }

        return false;
    }
}

/// <summary>
/// PRESENTATION: Converts any object to its string representation for display
/// FORMATTING: Handles null values and provides consistent string conversion
/// </summary>
internal sealed class ObjectToStringConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value == null)
            return string.Empty;

        if (value is DateTime dateTime)
            return dateTime.ToString("yyyy-MM-dd HH:mm:ss");

        if (value is decimal decimalValue)
            return decimalValue.ToString("F2");

        if (value is double doubleValue)
            return doubleValue.ToString("F2");

        if (value is float floatValue)
            return floatValue.ToString("F2");

        if (value is bool boolValue)
            return boolValue ? "Yes" : "No";

        return value.ToString() ?? string.Empty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        var stringValue = value?.ToString() ?? string.Empty;

        // Attempt to convert back based on target type
        if (targetType == typeof(int) || targetType == typeof(int?))
        {
            return int.TryParse(stringValue, out var intValue) ? intValue : (object?)null;
        }

        if (targetType == typeof(decimal) || targetType == typeof(decimal?))
        {
            return decimal.TryParse(stringValue, out var decimalValue) ? decimalValue : (object?)null;
        }

        if (targetType == typeof(double) || targetType == typeof(double?))
        {
            return double.TryParse(stringValue, out var doubleValue) ? doubleValue : (object?)null;
        }

        if (targetType == typeof(bool) || targetType == typeof(bool?))
        {
            return bool.TryParse(stringValue, out var boolValue) ? boolValue : (object?)null;
        }

        if (targetType == typeof(DateTime) || targetType == typeof(DateTime?))
        {
            return DateTime.TryParse(stringValue, out var dateValue) ? dateValue : (object?)null;
        }

        return stringValue;
    }
}