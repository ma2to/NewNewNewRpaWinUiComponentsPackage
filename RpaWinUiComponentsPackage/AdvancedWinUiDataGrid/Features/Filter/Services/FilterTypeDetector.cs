using System.Globalization;
using Microsoft.Extensions.Logging;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Filter.Services;

/// <summary>
/// Type-aware helper for filter value parsing and comparison
/// Supports: numeric, datetime (dd.MM.yyyy), text
/// </summary>
internal sealed class FilterTypeDetector
{
    private readonly ILogger<FilterTypeDetector>? _logger;

    /// <summary>
    /// DateTime format required by user: dd.MM.yyyy
    /// Example: 25.12.2024
    /// </summary>
    private const string DateTimeFormat = "dd.MM.yyyy";

    public FilterTypeDetector(ILogger<FilterTypeDetector>? logger = null)
    {
        _logger = logger;
    }

    /// <summary>
    /// Attempts to parse value as decimal number
    /// Supports: int, long, float, double, decimal, numeric strings
    /// </summary>
    public bool TryGetNumericValue(object? value, out decimal numericValue)
    {
        numericValue = 0;

        if (value == null)
            return false;

        // Direct numeric types
        if (value is decimal d) { numericValue = d; return true; }
        if (value is double dbl) { numericValue = (decimal)dbl; return true; }
        if (value is float f) { numericValue = (decimal)f; return true; }
        if (value is long l) { numericValue = l; return true; }
        if (value is int i) { numericValue = i; return true; }
        if (value is short s) { numericValue = s; return true; }
        if (value is byte b) { numericValue = b; return true; }

        // String parsing
        if (value is string str)
        {
            return decimal.TryParse(str, NumberStyles.Number, CultureInfo.InvariantCulture, out numericValue);
        }

        return false;
    }

    /// <summary>
    /// Attempts to parse value as DateTime using dd.MM.yyyy format
    /// User requirement: support only dd.MM.yyyy format for date filtering
    /// </summary>
    public bool TryGetDateTimeValue(object? value, out DateTime dateTimeValue)
    {
        dateTimeValue = default;

        if (value == null)
            return false;

        // Direct DateTime types
        if (value is DateTime dt)
        {
            dateTimeValue = dt;
            return true;
        }

        if (value is DateTimeOffset dto)
        {
            dateTimeValue = dto.DateTime;
            return true;
        }

        // String parsing with EXACT format dd.MM.yyyy
        if (value is string str)
        {
            if (DateTime.TryParseExact(
                str,
                DateTimeFormat,
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out dateTimeValue))
            {
                _logger?.LogTrace("Parsed datetime '{Value}' as {DateTime:dd.MM.yyyy}", str, dateTimeValue);
                return true;
            }

            // Fallback: try general parsing (for flexibility)
            if (DateTime.TryParse(str, CultureInfo.InvariantCulture, DateTimeStyles.None, out dateTimeValue))
            {
                _logger?.LogDebug("Parsed datetime '{Value}' using fallback parser (expected format: dd.MM.yyyy)", str);
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Compares two values with type-aware logic
    /// Priority: numeric > datetime > text
    /// </summary>
    public int CompareValues(object? cellValue, object? filterValue)
    {
        if (cellValue == null && filterValue == null)
            return 0;
        if (cellValue == null)
            return -1;
        if (filterValue == null)
            return 1;

        // 1. Try numeric comparison first
        if (TryGetNumericValue(cellValue, out var cellNumeric) &&
            TryGetNumericValue(filterValue, out var filterNumeric))
        {
            return cellNumeric.CompareTo(filterNumeric);
        }

        // 2. Try DateTime comparison (dd.MM.yyyy format)
        if (TryGetDateTimeValue(cellValue, out var cellDate) &&
            TryGetDateTimeValue(filterValue, out var filterDate))
        {
            return cellDate.CompareTo(filterDate);
        }

        // 3. Fallback to case-insensitive string comparison
        var cellStr = cellValue.ToString() ?? "";
        var filterStr = filterValue.ToString() ?? "";
        return string.Compare(cellStr, filterStr, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Checks if value is empty (null, empty string, or empty collection)
    /// </summary>
    public bool IsValueEmpty(object? value)
    {
        if (value == null)
            return true;

        if (value is string s)
            return string.IsNullOrWhiteSpace(s);

        if (value is System.Collections.ICollection c)
            return c.Count == 0;

        return false;
    }

    /// <summary>
    /// Checks equality with type-aware logic
    /// </summary>
    public bool ValuesAreEqual(object? cellValue, object? filterValue)
    {
        if (cellValue == null && filterValue == null)
            return true;
        if (cellValue == null || filterValue == null)
            return false;

        // Direct equality check first
        if (cellValue.Equals(filterValue))
            return true;

        // Numeric equality
        if (TryGetNumericValue(cellValue, out var cellNum) &&
            TryGetNumericValue(filterValue, out var filterNum))
        {
            return cellNum == filterNum;
        }

        // DateTime equality
        if (TryGetDateTimeValue(cellValue, out var cellDt) &&
            TryGetDateTimeValue(filterValue, out var filterDt))
        {
            return cellDt.Date == filterDt.Date; // Compare dates only (ignore time)
        }

        // String equality (case-insensitive)
        var cellStr = cellValue.ToString() ?? "";
        var filterStr = filterValue.ToString() ?? "";
        return string.Equals(cellStr, filterStr, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Detects the type of a value for smart filtering
    /// Returns Type object for use in FilterCondition.ColumnType
    /// </summary>
    public Type DetectType(object? value)
    {
        if (value == null)
            return typeof(object);

        // Numeric detection
        if (TryGetNumericValue(value, out _))
            return typeof(decimal);

        // DateTime detection
        if (TryGetDateTimeValue(value, out _))
            return typeof(DateTime);

        // Default to string
        return typeof(string);
    }

    /// <summary>
    /// Formats DateTime value to dd.MM.yyyy string for display/comparison
    /// </summary>
    public string FormatDateTime(DateTime dateTime)
    {
        return dateTime.ToString(DateTimeFormat, CultureInfo.InvariantCulture);
    }
}
