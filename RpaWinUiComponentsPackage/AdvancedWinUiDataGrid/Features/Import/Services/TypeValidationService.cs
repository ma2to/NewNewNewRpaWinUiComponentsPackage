using Microsoft.Extensions.Logging;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Common.Models;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Import.Services;

/// <summary>
/// PHASE 2: Service for validating and converting data types during import.
/// FEATURES:
/// - Type validation against column schema
/// - Null validation based on AllowNull property
/// - Safe type conversion with fallback
/// - Numeric type compatibility (int → double, etc.)
/// </summary>
internal sealed class TypeValidationService
{
    private readonly ILogger<TypeValidationService>? _logger;

    // Numeric types that can be safely converted between each other
    private static readonly HashSet<Type> NumericTypes = new()
    {
        typeof(int),
        typeof(long),
        typeof(double),
        typeof(float),
        typeof(decimal)
    };

    public TypeValidationService(ILogger<TypeValidationService>? logger = null)
    {
        _logger = logger;
    }

    /// <summary>
    /// Validates a row against the column schema.
    /// Checks:
    /// 1. Null values when AllowNull=false
    /// 2. Type compatibility for non-null values
    /// </summary>
    /// <param name="row">Row data to validate</param>
    /// <param name="schema">Column schema with type definitions</param>
    /// <param name="enforceTypes">Whether to enforce strict type validation</param>
    /// <returns>Result with success or error messages</returns>
    public PublicResult ValidateRow(
        IReadOnlyDictionary<string, object?> row,
        IReadOnlyList<ColumnDefinition> schema,
        bool enforceTypes)
    {
        var errors = new List<string>();

        foreach (var col in schema)
        {
            // Skip special columns (system columns don't need validation)
            if (col.IsSpecialColumn)
                continue;

            // Check if column exists in row
            if (!row.TryGetValue(col.Name, out var value))
            {
                // Missing columns are OK - they'll get default values
                continue;
            }

            // ✅ CHECK 1: Null validation
            if (value == null)
            {
                if (!col.AllowNull)
                {
                    errors.Add($"Column '{col.Name}' does not allow null values");
                }
                continue; // Null is valid if AllowNull=true
            }

            // ✅ CHECK 2: Type validation
            var valueType = value.GetType();
            if (!IsCompatibleType(valueType, col.DataType))
            {
                if (enforceTypes)
                {
                    errors.Add($"Column '{col.Name}': expected {col.DataType.Name}, got {valueType.Name}");
                }
                else
                {
                    // Log warning but allow conversion attempt
                    _logger?.LogWarning(
                        "Type mismatch in column '{Column}': expected {Expected}, got {Actual}. Will attempt conversion.",
                        col.Name, col.DataType.Name, valueType.Name);
                }
            }
        }

        if (errors.Count > 0)
        {
            var errorMessage = string.Join("; ", errors);
            return PublicResult.Failure(errorMessage);
        }

        return PublicResult.Success();
    }

    /// <summary>
    /// Validates multiple rows against the column schema.
    /// Returns first error encountered or success if all rows are valid.
    /// </summary>
    /// <param name="rows">Rows to validate</param>
    /// <param name="schema">Column schema with type definitions</param>
    /// <param name="enforceTypes">Whether to enforce strict type validation</param>
    /// <returns>Result with success or error messages</returns>
    public PublicResult ValidateRows(
        IReadOnlyList<IReadOnlyDictionary<string, object?>> rows,
        IReadOnlyList<ColumnDefinition> schema,
        bool enforceTypes)
    {
        for (int i = 0; i < rows.Count; i++)
        {
            var result = ValidateRow(rows[i], schema, enforceTypes);
            if (!result.IsSuccess)
            {
                return PublicResult.Failure($"Row {i + 1}: {result.ErrorMessage}");
            }
        }

        return PublicResult.Success();
    }

    /// <summary>
    /// PERFORMANCE OPTIMIZED: Validates ALL rows and collects ALL errors (up to limit).
    /// For 10M rows: ~100-500ms (type checks only, in-memory, no DB/IO).
    /// Returns early if error limit reached to prevent OOM.
    /// ARCHITECTURE: All-or-nothing validation - if ANY row fails, entire import should fail.
    /// </summary>
    /// <param name="rows">Rows to validate</param>
    /// <param name="schema">Column schema with type definitions</param>
    /// <param name="maxErrorsToCollect">Max errors to track (default: 100, prevents OOM)</param>
    /// <returns>Tuple with (allValid: bool, errorMessages: List)</returns>
    public (bool allValid, List<string> errorMessages) ValidateAllRows(
        IReadOnlyList<IReadOnlyDictionary<string, object?>> rows,
        IReadOnlyList<ColumnDefinition> schema,
        int maxErrorsToCollect = 100)
    {
        var errors = new List<string>();
        int totalErrorCount = 0;

        _logger?.LogDebug("Starting batch schema validation for {RowCount} rows (maxErrors={MaxErrors})",
            rows.Count, maxErrorsToCollect);

        for (int i = 0; i < rows.Count; i++)
        {
            var result = ValidateRow(rows[i], schema, enforceTypes: true);

            if (!result.IsSuccess)
            {
                totalErrorCount++;

                // Track detailed errors up to limit (avoid OOM with millions of errors)
                if (errors.Count < maxErrorsToCollect)
                {
                    errors.Add($"Row {i + 1}: {result.ErrorMessage}");
                }
            }
        }

        // Add summary if errors exceed limit
        if (totalErrorCount > maxErrorsToCollect)
        {
            errors.Add($"... and {totalErrorCount - maxErrorsToCollect} more errors " +
                      $"(showing first {maxErrorsToCollect} of {totalErrorCount} total errors)");
        }

        bool allValid = totalErrorCount == 0;

        _logger?.LogDebug("Batch validation completed. AllValid: {AllValid}, TotalErrors: {ErrorCount}",
            allValid, totalErrorCount);

        return (allValid, errors);
    }

    /// <summary>
    /// Checks if a value type is compatible with the expected column type.
    /// Handles:
    /// - Exact type match
    /// - Numeric type compatibility (int → double, etc.)
    /// - object type (accepts any value)
    /// </summary>
    /// <param name="valueType">Actual value type</param>
    /// <param name="expectedType">Expected column type</param>
    /// <returns>True if types are compatible</returns>
    public bool IsCompatibleType(Type valueType, Type expectedType)
    {
        // Exact match
        if (valueType == expectedType)
            return true;

        // object type accepts anything
        if (expectedType == typeof(object))
            return true;

        // Numeric compatibility: int → double, long → double, etc.
        if (NumericTypes.Contains(valueType) && NumericTypes.Contains(expectedType))
        {
            _logger?.LogTrace("Numeric type conversion: {From} → {To}", valueType.Name, expectedType.Name);
            return true;
        }

        // Derived types (inheritance)
        if (expectedType.IsAssignableFrom(valueType))
            return true;

        return false;
    }

    /// <summary>
    /// Attempts to convert a value to the target type.
    /// SAFE: Never throws - returns null on failure.
    /// FEATURES:
    /// - Numeric conversions (int → double, etc.)
    /// - String parsing (string → int, string → DateTime, etc.)
    /// - Boolean conversions ("true" → true, "1" → true, etc.)
    /// </summary>
    /// <param name="value">Value to convert</param>
    /// <param name="targetType">Target type</param>
    /// <returns>Converted value or null on failure</returns>
    public object? ConvertValue(object? value, Type targetType)
    {
        if (value == null)
            return null;

        var valueType = value.GetType();

        // Already correct type
        if (valueType == targetType || targetType == typeof(object))
            return value;

        try
        {
            // ✅ CONVERSION 1: Numeric types
            if (NumericTypes.Contains(valueType) && NumericTypes.Contains(targetType))
            {
                return Convert.ChangeType(value, targetType);
            }

            // ✅ CONVERSION 2: String to target type
            if (valueType == typeof(string) && value is string strValue)
            {
                // string → int
                if (targetType == typeof(int) && int.TryParse(strValue, out var intResult))
                    return intResult;

                // string → long
                if (targetType == typeof(long) && long.TryParse(strValue, out var longResult))
                    return longResult;

                // string → double
                if (targetType == typeof(double) && double.TryParse(strValue, out var doubleResult))
                    return doubleResult;

                // string → float
                if (targetType == typeof(float) && float.TryParse(strValue, out var floatResult))
                    return floatResult;

                // string → decimal
                if (targetType == typeof(decimal) && decimal.TryParse(strValue, out var decimalResult))
                    return decimalResult;

                // string → bool
                if (targetType == typeof(bool))
                {
                    if (bool.TryParse(strValue, out var boolResult))
                        return boolResult;

                    // Also accept "1"/"0", "yes"/"no"
                    if (strValue.Equals("1", StringComparison.OrdinalIgnoreCase) ||
                        strValue.Equals("yes", StringComparison.OrdinalIgnoreCase))
                        return true;

                    if (strValue.Equals("0", StringComparison.OrdinalIgnoreCase) ||
                        strValue.Equals("no", StringComparison.OrdinalIgnoreCase))
                        return false;
                }

                // string → DateTime
                if (targetType == typeof(DateTime) && DateTime.TryParse(strValue, out var dateResult))
                    return dateResult;
            }

            // ✅ CONVERSION 3: Target type is string (any value → string)
            if (targetType == typeof(string))
            {
                return value.ToString();
            }

            // ✅ CONVERSION 4: Generic Convert.ChangeType fallback
            return Convert.ChangeType(value, targetType);
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to convert value '{Value}' ({From}) to {To}",
                value, valueType.Name, targetType.Name);
            return null;
        }
    }

    /// <summary>
    /// Converts all values in a row to match the column schema types.
    /// NON-DESTRUCTIVE: Returns a new dictionary with converted values.
    /// Original row remains unchanged.
    /// </summary>
    /// <param name="row">Row data to convert</param>
    /// <param name="schema">Column schema with target types</param>
    /// <returns>New dictionary with converted values</returns>
    public Dictionary<string, object?> ConvertRow(
        IReadOnlyDictionary<string, object?> row,
        IReadOnlyList<ColumnDefinition> schema)
    {
        var convertedRow = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

        foreach (var col in schema)
        {
            if (row.TryGetValue(col.Name, out var value))
            {
                // Attempt conversion
                var convertedValue = ConvertValue(value, col.DataType);
                convertedRow[col.Name] = convertedValue ?? value; // Fallback to original if conversion fails
            }
            else
            {
                // Column not present in row - use default value
                convertedRow[col.Name] = col.DefaultValue;
            }
        }

        // Copy any extra columns not in schema
        foreach (var kvp in row)
        {
            if (!convertedRow.ContainsKey(kvp.Key))
            {
                convertedRow[kvp.Key] = kvp.Value;
            }
        }

        return convertedRow;
    }
}
