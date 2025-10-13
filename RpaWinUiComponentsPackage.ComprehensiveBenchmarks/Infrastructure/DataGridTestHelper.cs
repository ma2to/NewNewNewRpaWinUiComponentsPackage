using Microsoft.Extensions.Logging;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Common.Models;
using System.Data;

namespace RpaWinUiComponentsPackage.ComprehensiveBenchmarks.Infrastructure;

/// <summary>
/// Helper class for creating and configuring DataGrid instances for testing
/// </summary>
public static class DataGridTestHelper
{
    /// <summary>
    /// Creates a DataGrid facade for Interactive Mode using public API
    /// </summary>
    public static IAdvancedDataGridFacade CreateInteractiveGrid(
        ILoggerFactory? loggerFactory = null,
        Action<AdvancedDataGridOptions>? configureOptions = null)
    {
        // Create logger factory if not provided
        if (loggerFactory == null)
        {
            loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddConsole();
                builder.SetMinimumLevel(LogLevel.Warning);
            });
        }

        // Configure options for Interactive Mode
        var options = new AdvancedDataGridOptions
        {
            OperationMode = PublicDataGridOperationMode.Interactive,
            EnableParallelProcessing = true,
            EnableLinqOptimizations = true,
            EnableCaching = true,
            BatchSize = 1000,
            MinimumLogLevel = LogLevel.Warning
        };

        configureOptions?.Invoke(options);

        // Use public factory API - handles all internal DI registration
        return AdvancedDataGridFacadeFactory.CreateStandalone(options, loggerFactory);
    }

    /// <summary>
    /// Creates a DataGrid facade for Headless Mode using public API
    /// </summary>
    public static IAdvancedDataGridFacade CreateHeadlessGrid(
        ILoggerFactory? loggerFactory = null,
        Action<AdvancedDataGridOptions>? configureOptions = null)
    {
        // Create logger factory if not provided
        if (loggerFactory == null)
        {
            loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddConsole();
                builder.SetMinimumLevel(LogLevel.Error); // Less logging for headless
            });
        }

        // Configure options for Headless Mode
        var options = new AdvancedDataGridOptions
        {
            OperationMode = PublicDataGridOperationMode.Headless,
            EnableParallelProcessing = true,
            EnableLinqOptimizations = true,
            EnableCaching = true,
            BatchSize = 5000, // Larger batch size for headless
            ParallelProcessingThreshold = 500,
            MinimumLogLevel = LogLevel.Error,
            EnablePerformanceMetrics = true,
            DegreeOfParallelism = Environment.ProcessorCount * 2
        };

        configureOptions?.Invoke(options);

        // Use public factory API - handles all internal DI registration
        return AdvancedDataGridFacadeFactory.CreateStandalone(options, loggerFactory);
    }

    /// <summary>
    /// Converts test data to DataTable for import
    /// </summary>
    public static DataTable ConvertToDataTable(List<List<object>> data, List<string> columnHeaders)
    {
        var dt = new DataTable();

        // Add columns
        for (int i = 0; i < columnHeaders.Count; i++)
        {
            dt.Columns.Add(columnHeaders[i], typeof(object));
        }

        // Add rows
        foreach (var row in data)
        {
            var dataRow = dt.NewRow();
            for (int i = 0; i < Math.Min(row.Count, columnHeaders.Count); i++)
            {
                dataRow[i] = row[i] ?? DBNull.Value;
            }
            dt.Rows.Add(dataRow);
        }

        return dt;
    }

    /// <summary>
    /// Converts test data to Dictionary format
    /// </summary>
    public static List<IReadOnlyDictionary<string, object?>> ConvertToDictionaries(
        List<List<object>> data,
        List<string> columnHeaders)
    {
        var result = new List<IReadOnlyDictionary<string, object?>>();

        foreach (var row in data)
        {
            var dict = new Dictionary<string, object?>();
            for (int i = 0; i < Math.Min(row.Count, columnHeaders.Count); i++)
            {
                dict[columnHeaders[i]] = row[i];
            }
            result.Add(dict);
        }

        return result;
    }

    /// <summary>
    /// Creates ImportDataCommand from test data
    /// </summary>
    public static ImportDataCommand CreateImportCommand(
        List<List<object>> data,
        List<string> columnHeaders,
        PublicImportMode mode = PublicImportMode.Replace)
    {
        var dt = ConvertToDataTable(data, columnHeaders);

        return new ImportDataCommand
        {
            DataTableData = dt,
            Mode = mode,
            ValidateAfterImport = false // Disable validation for performance tests
        };
    }

    /// <summary>
    /// Creates column definitions from headers
    /// </summary>
    public static List<PublicColumnDefinition> CreateColumnDefinitions(List<string> columnHeaders)
    {
        var columns = new List<PublicColumnDefinition>();

        for (int i = 0; i < columnHeaders.Count; i++)
        {
            columns.Add(new PublicColumnDefinition
            {
                Name = columnHeaders[i],
                Header = columnHeaders[i],
                IsVisible = true,
                Width = 100,
                IsReadOnly = false
            });
        }

        return columns;
    }
}
