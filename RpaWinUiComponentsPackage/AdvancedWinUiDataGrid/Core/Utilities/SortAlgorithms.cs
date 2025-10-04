namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Core.Utilities;

/// <summary>
/// Pure functional sorting algorithms pre maximum performance a testability
/// Stateless algorithms bez side effects
/// Thread-safe pre concurrent execution
/// </summary>
internal static class SortAlgorithms
{
    /// <summary>
    /// Extrahuje sortovateľnú hodnotu s inteligentnou type conversion
    /// </summary>
    public static object? GetSortValue(IReadOnlyDictionary<string, object?> row, string columnName)
    {
        if (row == null) throw new ArgumentNullException(nameof(row));
        if (string.IsNullOrEmpty(columnName)) throw new ArgumentException("Column name cannot be null or empty", nameof(columnName));

        if (!row.TryGetValue(columnName, out var value))
            return null;

        // Handle null values
        if (value == null)
            return null;

        // Return comparable types as-is
        if (value is IComparable)
            return value;

        // Try intelligent type conversion for strings
        if (value is string stringValue)
        {
            return ConvertStringToComparableType(stringValue);
        }

        return value;
    }

    /// <summary>
    /// Generuje multi-column sort keys
    /// </summary>
    public static IReadOnlyList<object?> GenerateSortKeys(
        IReadOnlyDictionary<string, object?> row,
        IReadOnlyList<string> columnNames)
    {
        if (row == null) throw new ArgumentNullException(nameof(row));
        if (columnNames == null) throw new ArgumentNullException(nameof(columnNames));

        var keys = new object?[columnNames.Count];
        for (int i = 0; i < columnNames.Count; i++)
        {
            keys[i] = GetSortValue(row, columnNames[i]);
        }
        return keys;
    }

    /// <summary>
    /// Porovná sort key arrays s support pre direction
    /// </summary>
    public static int CompareSortKeys(
        IReadOnlyList<object?> keys1,
        IReadOnlyList<object?> keys2,
        IReadOnlyList<bool> ascendingDirections)
    {
        if (keys1 == null) throw new ArgumentNullException(nameof(keys1));
        if (keys2 == null) throw new ArgumentNullException(nameof(keys2));
        if (ascendingDirections == null) throw new ArgumentNullException(nameof(ascendingDirections));

        var minLength = Math.Min(Math.Min(keys1.Count, keys2.Count), ascendingDirections.Count);

        for (int i = 0; i < minLength; i++)
        {
            var comparison = CompareValues(keys1[i], keys2[i]);
            if (comparison != 0)
            {
                return ascendingDirections[i] ? comparison : -comparison;
            }
        }

        return 0;
    }

    /// <summary>
    /// Zistí či je stĺpec sortovateľný
    /// </summary>
    public static bool IsColumnSortable(IEnumerable<IReadOnlyDictionary<string, object?>> data, string columnName)
    {
        if (data == null) throw new ArgumentNullException(nameof(data));
        if (string.IsNullOrEmpty(columnName)) return false;

        var sampleValues = data
            .Take(100)
            .Select(row => row.TryGetValue(columnName, out var value) ? value : null)
            .Where(value => value != null)
            .Take(10)
            .ToList();

        if (!sampleValues.Any()) return false;

        return sampleValues.All(value =>
            value is IComparable ||
            (value is string str && CanConvertToComparableType(str)));
    }

    /// <summary>
    /// Detekuje optimálny sort data type
    /// </summary>
    public static Type? DetectSortDataType(IEnumerable<IReadOnlyDictionary<string, object?>> data, string columnName)
    {
        if (data == null) throw new ArgumentNullException(nameof(data));
        if (string.IsNullOrEmpty(columnName)) return null;

        var sampleValues = data
            .Take(100)
            .Select(row => row.TryGetValue(columnName, out var value) ? value : null)
            .Where(value => value != null)
            .Take(20)
            .ToList();

        if (!sampleValues.Any()) return typeof(string);

        var typeCounts = sampleValues
            .GroupBy(value => GetEffectiveType(value))
            .ToDictionary(g => g.Key, g => g.Count());

        return typeCounts.OrderByDescending(kvp => kvp.Value).First().Key;
    }

    // PRIVATE HELPER METHODS

    private static int CompareValues(object? value1, object? value2)
    {
        if (value1 == null && value2 == null) return 0;
        if (value1 == null) return 1;
        if (value2 == null) return -1;

        if (value1.GetType() == value2.GetType() && value1 is IComparable comparable1)
        {
            return comparable1.CompareTo(value2);
        }

        if (TryCoerceToSameType(value1, value2, out var coerced1, out var coerced2))
        {
            if (coerced1 is IComparable coercedComparable)
            {
                return coercedComparable.CompareTo(coerced2);
            }
        }

        var str1 = value1.ToString() ?? string.Empty;
        var str2 = value2.ToString() ?? string.Empty;
        return string.Compare(str1, str2, StringComparison.OrdinalIgnoreCase);
    }

    private static object ConvertStringToComparableType(string value)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;

        if (double.TryParse(value, out var doubleValue))
            return doubleValue;

        if (DateTime.TryParse(value, out var dateValue))
            return dateValue;

        if (bool.TryParse(value, out var boolValue))
            return boolValue;

        return value;
    }

    private static bool CanConvertToComparableType(string value)
    {
        if (string.IsNullOrEmpty(value)) return true;

        return double.TryParse(value, out _) ||
               DateTime.TryParse(value, out _) ||
               bool.TryParse(value, out _);
    }

    private static Type GetEffectiveType(object? value)
    {
        if (value == null) return typeof(object);

        if (value is string str)
        {
            if (double.TryParse(str, out _)) return typeof(double);
            if (DateTime.TryParse(str, out _)) return typeof(DateTime);
            if (bool.TryParse(str, out _)) return typeof(bool);
        }

        return value.GetType();
    }

    private static bool TryCoerceToSameType(object value1, object value2, out object? coerced1, out object? coerced2)
    {
        coerced1 = value1;
        coerced2 = value2;

        if (TryConvertToDouble(value1, out var d1) && TryConvertToDouble(value2, out var d2))
        {
            coerced1 = d1;
            coerced2 = d2;
            return true;
        }

        if (TryConvertToDateTime(value1, out var dt1) && TryConvertToDateTime(value2, out var dt2))
        {
            coerced1 = dt1;
            coerced2 = dt2;
            return true;
        }

        return false;
    }

    private static bool TryConvertToDouble(object? value, out double result)
    {
        result = 0;

        return value switch
        {
            double d => (result = d) == d,
            float f => (result = f) == f,
            decimal dec => (result = (double)dec) == (double)dec,
            int i => (result = i) == i,
            long l => (result = l) == l,
            short s => (result = s) == s,
            byte b => (result = b) == b,
            string str => double.TryParse(str, out result),
            _ => false
        };
    }

    private static bool TryConvertToDateTime(object? value, out DateTime result)
    {
        result = default;

        return value switch
        {
            DateTime dt => (result = dt) == dt,
            DateTimeOffset dto => (result = dto.DateTime) == dto.DateTime,
            string str => DateTime.TryParse(str, out result),
            _ => false
        };
    }
}
