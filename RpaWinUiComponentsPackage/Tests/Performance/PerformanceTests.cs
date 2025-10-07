using System.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid;
//using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Api.Models;

namespace RpaWinUiComponentsPackage.Tests.Performance;

/// <summary>
/// Performance tests for all grid operations
/// Tests different row counts and batch sizes to find optimal configuration
/// </summary>
public class PerformanceTests
{
    public async Task<List<TestResult>> RunAllTests()
    {
        var results = new List<TestResult>();

        // Test: Add Rows Performance
        results.Add(await TestAddRowsPerformance(10_000, 5_000));
        results.Add(await TestAddRowsPerformance(50_000, 10_000));
        results.Add(await TestAddRowsPerformance(100_000, 20_000));

        // Test: Sort Performance
        results.Add(await TestSortPerformance(10_000, 5_000));
        results.Add(await TestSortPerformance(50_000, 10_000));

        // Test: Filter Performance
        results.Add(await TestFilterPerformance(10_000, 5_000));
        results.Add(await TestFilterPerformance(50_000, 10_000));

        // Test: Search Performance
        results.Add(await TestSearchPerformance(10_000, 5_000));

        // Test: Update Performance
        results.Add(await TestUpdatePerformance(10_000, 5_000));

        return results;
    }

    private async Task<TestResult> TestAddRowsPerformance(int rowCount, int batchSize)
    {
        var sw = Stopwatch.StartNew();
        var memBefore = GC.GetTotalMemory(true);

        try
        {
            var facade = CreateTestFacade(batchSize);

            // Add columns
            facade.AddColumn(new PublicColumnDefinition { Name = "ID", Header = "ID", DataType = typeof(int), IsVisible = true });
            facade.AddColumn(new PublicColumnDefinition { Name = "Name", Header = "Name", DataType = typeof(string), IsVisible = true });

            // Add rows
            for (int i = 0; i < rowCount; i++)
            {
                await facade.AddRowAsync(new Dictionary<string, object?> { ["ID"] = i, ["Name"] = $"Item_{i}" });
            }

            sw.Stop();
            var memAfter = GC.GetTotalMemory(false);

            return new TestResult
            {
                Category = "Performance",
                Name = $"AddRows_{rowCount:N0}_Batch{batchSize:N0}",
                Success = true,
                Duration = sw.Elapsed,
                Details = $"Added {rowCount:N0} rows with batch size {batchSize:N0}",
                Metrics = new Dictionary<string, object>
                {
                    ["RowCount"] = rowCount,
                    ["BatchSize"] = batchSize,
                    ["MemoryMB"] = (memAfter - memBefore) / 1024.0 / 1024.0
                }
            };
        }
        catch (Exception ex)
        {
            sw.Stop();
            return new TestResult
            {
                Category = "Performance",
                Name = $"AddRows_{rowCount:N0}_Batch{batchSize:N0}",
                Success = false,
                Duration = sw.Elapsed,
                Error = ex.Message
            };
        }
    }

    private async Task<TestResult> TestSortPerformance(int rowCount, int batchSize)
    {
        try
        {
            var facade = CreateTestFacade(batchSize);

            // Setup
            facade.AddColumn(new PublicColumnDefinition { Name = "ID", Header = "ID", DataType = typeof(int), IsSortable = true, IsVisible = true });
            for (int i = 0; i < rowCount; i++)
            {
                await facade.AddRowAsync(new Dictionary<string, object?> { ["ID"] = new Random().Next() });
            }

            // Test sort
            var sw = Stopwatch.StartNew();
            await facade.SortByColumnAsync("ID", PublicSortDirection.Ascending);
            sw.Stop();

            return new TestResult
            {
                Category = "Performance",
                Name = $"Sort_{rowCount:N0}_Batch{batchSize:N0}",
                Success = true,
                Duration = sw.Elapsed,
                Details = $"Sorted {rowCount:N0} rows",
                Metrics = new Dictionary<string, object> { ["RowCount"] = rowCount, ["BatchSize"] = batchSize }
            };
        }
        catch (Exception ex)
        {
            return new TestResult { Category = "Performance", Name = $"Sort_{rowCount:N0}_Batch{batchSize:N0}", Success = false, Error = ex.Message };
        }
    }

    private async Task<TestResult> TestFilterPerformance(int rowCount, int batchSize)
    {
        try
        {
            var facade = CreateTestFacade(batchSize);

            // Setup
            facade.AddColumn(new PublicColumnDefinition { Name = "Value", Header = "Value", DataType = typeof(int), IsFilterable = true, IsVisible = true });
            for (int i = 0; i < rowCount; i++)
            {
                await facade.AddRowAsync(new Dictionary<string, object?> { ["Value"] = i });
            }

            // Test filter
            var sw = Stopwatch.StartNew();
            await facade.ApplyFilterAsync("Value", PublicFilterOperator.GreaterThan, rowCount / 2);
            sw.Stop();

            var filtered = facade.GetCurrentData();

            return new TestResult
            {
                Category = "Performance",
                Name = $"Filter_{rowCount:N0}_Batch{batchSize:N0}",
                Success = true,
                Duration = sw.Elapsed,
                Details = $"Filtered {rowCount:N0} rows, found {filtered.Count} matches",
                Metrics = new Dictionary<string, object> { ["RowCount"] = rowCount, ["FilteredCount"] = filtered.Count }
            };
        }
        catch (Exception ex)
        {
            return new TestResult { Category = "Performance", Name = $"Filter_{rowCount:N0}_Batch{batchSize:N0}", Success = false, Error = ex.Message };
        }
    }

    private async Task<TestResult> TestSearchPerformance(int rowCount, int batchSize)
    {
        try
        {
            var facade = CreateTestFacade(batchSize);

            // Setup
            facade.AddColumn(new PublicColumnDefinition { Name = "Name", Header = "Name", DataType = typeof(string), IsVisible = true });
            for (int i = 0; i < rowCount; i++)
            {
                await facade.AddRowAsync(new Dictionary<string, object?> { ["Name"] = $"Item_{i}" });
            }

            // Test search
            var sw = Stopwatch.StartNew();
            var command = new SearchDataCommand(
                facade.GetCurrentData(),
                "Item_500",
                TargetColumns: new[] { "Name" }
            );
            var result = await facade.SearchAsync(command);
            sw.Stop();

            return new TestResult
            {
                Category = "Performance",
                Name = $"Search_{rowCount:N0}_Batch{batchSize:N0}",
                Success = result.IsSuccess,
                Duration = sw.Elapsed,
                Details = $"Searched {rowCount:N0} rows, found {result.TotalMatchesFound} matches",
                Metrics = new Dictionary<string, object> { ["RowCount"] = rowCount, ["FoundCount"] = result.TotalMatchesFound }
            };
        }
        catch (Exception ex)
        {
            return new TestResult { Category = "Performance", Name = $"Search_{rowCount:N0}_Batch{batchSize:N0}", Success = false, Error = ex.Message };
        }
    }

    private async Task<TestResult> TestUpdatePerformance(int rowCount, int batchSize)
    {
        try
        {
            var facade = CreateTestFacade(batchSize);

            // Setup
            facade.AddColumn(new PublicColumnDefinition { Name = "Value", Header = "Value", DataType = typeof(int), IsVisible = true });
            for (int i = 0; i < rowCount; i++)
            {
                await facade.AddRowAsync(new Dictionary<string, object?> { ["Value"] = i });
            }

            // Test update (sample 100 cells)
            var sw = Stopwatch.StartNew();
            var updates = Math.Min(100, rowCount);
            for (int i = 0; i < updates; i++)
            {
                await facade.UpdateCellAsync(i, "Value", 9999);
            }
            sw.Stop();

            return new TestResult
            {
                Category = "Performance",
                Name = $"Update_{updates}_Cells_Batch{batchSize:N0}",
                Success = true,
                Duration = sw.Elapsed,
                Details = $"Updated {updates} cells in grid with {rowCount:N0} rows",
                Metrics = new Dictionary<string, object> { ["UpdateCount"] = updates, ["TotalRows"] = rowCount }
            };
        }
        catch (Exception ex)
        {
            return new TestResult { Category = "Performance", Name = $"Update_Batch{batchSize:N0}", Success = false, Error = ex.Message };
        }
    }

    private IAdvancedDataGridFacade CreateTestFacade(int batchSize)
    {
        var options = new AdvancedDataGridOptions
        {
            BatchSize = batchSize,
            EnableParallelProcessing = true,
            EnableRealTimeValidation = false,
            LoggerFactory = NullLoggerFactory.Instance,
            OperationMode = PublicDataGridOperationMode.Headless
        };

        options.EnabledFeatures.Clear();
        options.EnabledFeatures.Add(GridFeature.Sort);
        options.EnabledFeatures.Add(GridFeature.Filter);
        options.EnabledFeatures.Add(GridFeature.Search);
        options.EnabledFeatures.Add(GridFeature.RowColumnOperations);

        return AdvancedDataGridFacadeFactory.CreateStandalone(options, NullLoggerFactory.Instance, null);
    }
}
