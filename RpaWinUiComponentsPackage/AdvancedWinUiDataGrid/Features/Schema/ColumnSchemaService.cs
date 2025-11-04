using Microsoft.Extensions.Logging;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Common;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Common.Models;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Schema;

/// <summary>
/// Service for managing and validating column schema definitions.
/// FEATURES:
/// - Validates reserved column names (special columns like __rowId, __Checkbox, etc.)
/// - Handles duplicate column names (auto-rename to Name_1, Name_2, etc.)
/// - Validates DataType support (string, int, double, DateTime, bool, decimal, long, float)
/// - Provides schema query methods
/// </summary>
internal sealed class ColumnSchemaService
{
    private readonly ILogger<ColumnSchemaService> _logger;
    private List<ColumnDefinition> _schema = new();

    // Reserved names for special columns that cannot be used for user-defined columns
    private static readonly HashSet<string> ReservedNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "__rowId",
        "__Checkbox",
        "__RowNumber",
        "__ValidationAlerts",
        "__DeleteRow",
        "__InsertRow"
    };

    // Supported data types for columns
    private static readonly HashSet<Type> SupportedDataTypes = new()
    {
        typeof(string),
        typeof(int),
        typeof(double),
        typeof(DateTime),
        typeof(bool),
        typeof(decimal),
        typeof(long),
        typeof(float),
        typeof(object) // Fallback for dynamic/untyped columns
    };

    public ColumnSchemaService(ILogger<ColumnSchemaService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Defines and validates column schema.
    /// ARCHITECTURE:
    /// 1. Validates reserved names → throws if user tries to use __rowId, __Checkbox, etc.
    /// 2. Validates DataTypes → checks if types are supported
    /// 3. Handles duplicates → auto-renames to Name_1, Name_2, etc.
    /// 4. Stores normalized schema
    /// </summary>
    /// <param name="columns">Column definitions to validate and normalize</param>
    /// <returns>PublicResult with normalized schema or error messages</returns>
    public PublicResult<List<ColumnDefinition>> DefineColumns(IEnumerable<ColumnDefinition> columns)
    {
        _logger.LogInformation("Defining column schema...");

        var columnList = columns.ToList();
        if (columnList.Count == 0)
        {
            return PublicResult<List<ColumnDefinition>>.Failure("No columns provided");
        }

        var errors = new List<string>();
        var normalizedColumns = new List<ColumnDefinition>();
        var nameCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        foreach (var col in columnList)
        {
            // Validate column name exists
            if (string.IsNullOrWhiteSpace(col.Name))
            {
                errors.Add($"Column at index {normalizedColumns.Count} has empty name");
                continue;
            }

            // ✅ CHECK 1: Reserved names validation
            if (IsReservedName(col.Name))
            {
                errors.Add($"Column name '{col.Name}' is reserved for special columns. Reserved names: {string.Join(", ", ReservedNames)}");
                continue;
            }

            // ✅ CHECK 2: DataType validation
            if (!SupportedDataTypes.Contains(col.DataType))
            {
                errors.Add($"Column '{col.Name}': DataType '{col.DataType.Name}' is not supported. " +
                          $"Supported types: {string.Join(", ", SupportedDataTypes.Select(t => t.Name))}");
                continue;
            }

            // ✅ CHECK 3: Duplicate handling - auto-rename
            var originalName = col.Name;
            if (nameCounts.ContainsKey(col.Name))
            {
                nameCounts[col.Name]++;
                var newName = $"{col.Name}_{nameCounts[col.Name]}";

                _logger.LogWarning("Duplicate column name '{OriginalName}' detected - renaming to '{NewName}'",
                    originalName, newName);

                col.Name = newName;
                nameCounts[newName] = 0; // Track the new name too
            }
            else
            {
                nameCounts[col.Name] = 0;
            }

            // Set default header if not provided
            if (string.IsNullOrWhiteSpace(col.Header))
            {
                col.Header = col.Name;
            }

            normalizedColumns.Add(col);

            _logger.LogDebug("Column defined: Name='{Name}', DataType={DataType}, AllowNull={AllowNull}, Width={Width}",
                col.Name, col.DataType.Name, col.AllowNull, col.Width);
        }

        if (errors.Any())
        {
            _logger.LogError("Column schema validation failed with {ErrorCount} errors", errors.Count);
            var errorMessage = string.Join("; ", errors);
            return PublicResult<List<ColumnDefinition>>.Failure(errorMessage);
        }

        _schema = normalizedColumns;

        _logger.LogInformation("✓ Column schema defined successfully: {ColumnCount} columns", _schema.Count);
        return PublicResult<List<ColumnDefinition>>.Success(_schema);
    }

    /// <summary>
    /// Gets column definition by name (case-insensitive).
    /// </summary>
    public ColumnDefinition? GetColumnByName(string name)
    {
        return _schema.FirstOrDefault(c =>
            string.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Checks if a column name is reserved for special columns.
    /// </summary>
    public bool IsReservedName(string name)
    {
        return ReservedNames.Contains(name);
    }

    /// <summary>
    /// Gets the current column schema (read-only).
    /// </summary>
    public IReadOnlyList<ColumnDefinition> GetSchema()
    {
        return _schema.AsReadOnly();
    }

    /// <summary>
    /// Clears the current schema.
    /// </summary>
    public void ClearSchema()
    {
        _logger.LogInformation("Clearing column schema");
        _schema.Clear();
    }

    /// <summary>
    /// Checks if a schema has been defined.
    /// </summary>
    public bool HasSchema()
    {
        return _schema.Count > 0;
    }
}
