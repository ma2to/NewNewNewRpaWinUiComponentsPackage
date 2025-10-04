using System.Text.RegularExpressions;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Common;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Core.Utilities;

/// <summary>
/// ENTERPRISE: Pure functional search and filter algorithms for maximum performance and testability
/// FUNCTIONAL PARADIGM: Stateless algorithms without side effects
/// HYBRID APPROACH: Functional algorithms within OOP service architecture
/// THREAD SAFE: Immutable functions suitable for concurrent execution
/// </summary>
internal static class SearchFilterAlgorithms
{
    /// <summary>
    /// PURE FUNCTION: Evaluate filter criteria against row data with comprehensive operator support
    /// PERFORMANCE: Optimized comparison logic without state dependencies
    /// ENTERPRISE: Complete filter operator implementation for business scenarios
    /// </summary>
    public static bool EvaluateFilter(
        IReadOnlyDictionary<string, object?> row,
        string columnName,
        FilterOperator filterOperator,
        object? filterValue,
        bool caseSensitive = false)
    {
        if (row == null) throw new ArgumentNullException(nameof(row));
        if (string.IsNullOrEmpty(columnName)) throw new ArgumentException("Column name cannot be null or empty", nameof(columnName));

        if (!row.TryGetValue(columnName, out var value))
            return false;

        return filterOperator switch
        {
            FilterOperator.Equals => CompareValues(value, filterValue) == 0,
            FilterOperator.NotEquals => CompareValues(value, filterValue) != 0,
            FilterOperator.GreaterThan => CompareValues(value, filterValue) > 0,
            FilterOperator.GreaterThanOrEqual => CompareValues(value, filterValue) >= 0,
            FilterOperator.LessThan => CompareValues(value, filterValue) < 0,
            FilterOperator.LessThanOrEqual => CompareValues(value, filterValue) <= 0,
            FilterOperator.Contains => ContainsValue(value, filterValue, caseSensitive),
            FilterOperator.NotContains => !ContainsValue(value, filterValue, caseSensitive),
            FilterOperator.StartsWith => StartsWithValue(value, filterValue, caseSensitive),
            FilterOperator.EndsWith => EndsWithValue(value, filterValue, caseSensitive),
            FilterOperator.IsNull => value == null,
            FilterOperator.IsNotNull => value != null,
            FilterOperator.IsEmpty => IsEmptyValue(value),
            FilterOperator.IsNotEmpty => !IsEmptyValue(value),
            _ => false
        };
    }

    /// <summary>
    /// PURE FUNCTION: Type-safe value comparison with intelligent type coercion
    /// ENTERPRISE: Comprehensive type handling for business data scenarios
    /// PERFORMANCE: Optimized comparison paths for common data types
    /// </summary>
    public static int CompareValues(object? value1, object? value2)
    {
        // Handle null cases first
        if (value1 == null && value2 == null) return 0;
        if (value1 == null) return -1;
        if (value2 == null) return 1;

        // Direct equality check for performance
        if (value1.Equals(value2)) return 0;

        // Type-specific comparisons for optimal performance
        if (value1 is IComparable comparable1 && value2 is IComparable comparable2)
        {
            // Attempt direct comparison if types match
            if (value1.GetType() == value2.GetType())
            {
                return comparable1.CompareTo(comparable2);
            }

            // Numeric type conversions
            if (TryConvertToDouble(value1, out var double1) && TryConvertToDouble(value2, out var double2))
            {
                return double1.CompareTo(double2);
            }

            // DateTime conversions
            if (TryConvertToDateTime(value1, out var date1) && TryConvertToDateTime(value2, out var date2))
            {
                return date1.CompareTo(date2);
            }
        }

        // Final fallback to string comparison
        var str1 = value1.ToString() ?? string.Empty;
        var str2 = value2.ToString() ?? string.Empty;
        return string.Compare(str1, str2, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// PURE FUNCTION: Advanced pattern matching with regex and text search support
    /// PERFORMANCE: Optimized string operations with early returns
    /// FLEXIBILITY: Support for both regex and simple text matching
    /// </summary>
    public static bool IsMatch(
        string text,
        string searchText,
        bool useRegex = false,
        bool caseSensitive = false)
    {
        if (string.IsNullOrEmpty(text)) return string.IsNullOrEmpty(searchText);
        if (string.IsNullOrEmpty(searchText)) return true;

        if (useRegex)
        {
            return IsRegexMatch(text, searchText, caseSensitive);
        }

        var comparison = caseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
        return text.Contains(searchText, comparison);
    }

    /// <summary>
    /// PURE FUNCTION: Production-safe regex matching with timeout and fallback
    /// ENTERPRISE: Comprehensive error handling for production scenarios
    /// RESILIENCE: Automatic fallback strategies for regex failures
    /// </summary>
    public static bool IsRegexMatch(object? value, object? pattern, bool caseSensitive = false)
    {
        if (value == null || pattern == null) return false;

        var text = value.ToString() ?? string.Empty;
        var regexPattern = pattern.ToString() ?? string.Empty;

        if (string.IsNullOrEmpty(regexPattern)) return true;

        try
        {
            var options = caseSensitive ? RegexOptions.None : RegexOptions.IgnoreCase;
            // Add timeout for production safety (100ms timeout)
            return Regex.IsMatch(text, regexPattern, options, TimeSpan.FromMilliseconds(100));
        }
        catch (RegexMatchTimeoutException)
        {
            // Fallback to simple contains for timeout scenarios
            return IsMatch(text, regexPattern, false, caseSensitive);
        }
        catch (ArgumentException)
        {
            // Invalid regex pattern - fallback to literal matching
            return IsMatch(text, regexPattern, false, caseSensitive);
        }
    }

    /// <summary>
    /// PURE FUNCTION: Fuzzy string matching using Levenshtein distance
    /// ALGORITHM: Calculates similarity score between two strings (0.0 = no match, 1.0 = exact match)
    /// </summary>
    public static double CalculateFuzzyMatchScore(string text, string searchText)
    {
        if (string.IsNullOrEmpty(text) && string.IsNullOrEmpty(searchText)) return 1.0;
        if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(searchText)) return 0.0;

        var distance = LevenshteinDistance(text.ToLowerInvariant(), searchText.ToLowerInvariant());
        var maxLength = Math.Max(text.Length, searchText.Length);
        return 1.0 - ((double)distance / maxLength);
    }

    /// <summary>
    /// PURE FUNCTION: Calculate Levenshtein distance between two strings
    /// ALGORITHM: Dynamic programming approach for edit distance
    /// </summary>
    private static int LevenshteinDistance(string s, string t)
    {
        var n = s.Length;
        var m = t.Length;
        var d = new int[n + 1, m + 1];

        if (n == 0) return m;
        if (m == 0) return n;

        for (var i = 0; i <= n; i++) d[i, 0] = i;
        for (var j = 0; j <= m; j++) d[0, j] = j;

        for (var i = 1; i <= n; i++)
        {
            for (var j = 1; j <= m; j++)
            {
                var cost = (t[j - 1] == s[i - 1]) ? 0 : 1;
                d[i, j] = Math.Min(
                    Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                    d[i - 1, j - 1] + cost);
            }
        }

        return d[n, m];
    }

    /// <summary>
    /// PURE FUNCTION: Calculate relevance score for search result ranking
    /// SCORING: Exact match > Starts with > Contains > Fuzzy match
    /// </summary>
    public static double CalculateRelevanceScore(string text, string searchText, bool caseSensitive = false)
    {
        if (string.IsNullOrEmpty(text)) return 0.0;
        if (string.IsNullOrEmpty(searchText)) return 1.0;

        var comparison = caseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

        // Exact match gets highest score
        if (text.Equals(searchText, comparison))
            return 1.0;

        // Starts with gets high score
        if (text.StartsWith(searchText, comparison))
            return 0.8;

        // Contains gets medium score
        if (text.Contains(searchText, comparison))
        {
            // Bonus for earlier position
            var index = text.IndexOf(searchText, comparison);
            var positionBonus = 1.0 - ((double)index / text.Length * 0.2);
            return 0.6 * positionBonus;
        }

        // Fuzzy match gets lower score
        var fuzzyScore = CalculateFuzzyMatchScore(text, searchText);
        return fuzzyScore > 0.7 ? fuzzyScore * 0.5 : 0.0;
    }

    /// <summary>
    /// PURE FUNCTION: String containment check with case sensitivity support
    /// TEXT PROCESSING: Efficient substring matching for search operations
    /// </summary>
    private static bool ContainsValue(object? value, object? searchValue, bool caseSensitive)
    {
        if (value == null || searchValue == null) return false;

        var text = value.ToString() ?? string.Empty;
        var search = searchValue.ToString() ?? string.Empty;

        var comparison = caseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
        return text.Contains(search, comparison);
    }

    /// <summary>
    /// PURE FUNCTION: String prefix matching with case sensitivity support
    /// TEXT PROCESSING: Efficient prefix matching for filter operations
    /// </summary>
    private static bool StartsWithValue(object? value, object? prefix, bool caseSensitive)
    {
        if (value == null || prefix == null) return false;

        var text = value.ToString() ?? string.Empty;
        var prefixText = prefix.ToString() ?? string.Empty;

        var comparison = caseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
        return text.StartsWith(prefixText, comparison);
    }

    /// <summary>
    /// PURE FUNCTION: String suffix matching with case sensitivity support
    /// TEXT PROCESSING: Efficient suffix matching for filter operations
    /// </summary>
    private static bool EndsWithValue(object? value, object? suffix, bool caseSensitive)
    {
        if (value == null || suffix == null) return false;

        var text = value.ToString() ?? string.Empty;
        var suffixText = suffix.ToString() ?? string.Empty;

        var comparison = caseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
        return text.EndsWith(suffixText, comparison);
    }

    /// <summary>
    /// PURE FUNCTION: Comprehensive emptiness evaluation for different data types
    /// BUSINESS LOGIC: Enterprise-grade empty value detection
    /// </summary>
    private static bool IsEmptyValue(object? value)
    {
        return value switch
        {
            null => true,
            string str => string.IsNullOrWhiteSpace(str),
            Array array => array.Length == 0,
            System.Collections.ICollection collection => collection.Count == 0,
            _ => false
        };
    }

    /// <summary>
    /// PURE FUNCTION: Safe numeric conversion with comprehensive type support
    /// TYPE SAFETY: Exception-free numeric type conversion for comparisons
    /// </summary>
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

    /// <summary>
    /// PURE FUNCTION: Safe DateTime conversion with format tolerance
    /// TYPE SAFETY: Exception-free DateTime type conversion for comparisons
    /// </summary>
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
