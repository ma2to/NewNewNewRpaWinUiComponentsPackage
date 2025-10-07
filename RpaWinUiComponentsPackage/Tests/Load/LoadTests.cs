using System.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Api.Models;

namespace RpaWinUiComponentsPackage.Tests.Load;

/// <summary>
/// Load tests - stress testing with large datasets
/// </summary>
public class LoadTests
{
    public async Task<List<TestResult>> RunAllTests()
    {
        var results = new List<TestResult>();

        results.Add(await TestLargeDataset_100K());
        results.Add(await TestLargeDataset_500K());
        results.Add(await TestLargeDataset_1M());
        results.Add(await TestHighFrequencyUpdates());

        return results;
    }

    private async Task<TestResult> TestLargeDataset_100K()
    {
        return await TestLargeDataset(100_000, "100K");
    }

    private async Task<TestResult> TestLargeDataset_500K()
    {
        return await TestLargeDataset(500_000, "500K");
    }

    private async Task<TestResult> TestLargeDataset_1M()
    {
        return await TestLargeDataset(1_000_000, "1M");
    }

    private async Task<TestResult> TestLargeDataset(int rowCount, string label)
    {
        try
        {
            var sw = Stopwatch.StartNew();
            var memBefore = GC.GetTotalMemory(true);

            var facade = CreateTestFacade(20000);

            facade.AddColumn(new PublicColumnDefinition { Name = "ID", Header = "ID", DataType = typeof(int), IsSortable = true, IsFilterable = true, IsVisible = true });
            facade.AddColumn(new PublicColumnDefinition { Name = "Name", Header = "Name", DataType = typeof(string), IsVisible = true });
            facade.AddColumn(new PublicColumnDefinition { Name = "Value", Header = "Value", DataType = typeof(double), IsVisible = true });

            // Add rows
            var random = new Random(42);
            for (int i = 0; i < rowCount; i++)
            {
                await facade.AddRowAsync(new Dictionary<string, object?>
                {
                    ["ID"] = random.Next(),
                    ["Name"] = $"Item_{i}",
                    ["Value"] = random.NextDouble() * 1000
                });
            }

            // Perform operations
            await facade.SortByColumnAsync("ID", PublicSortDirection.Ascending);
            await facade.ApplyFilterAsync("Value", PublicFilterOperator.GreaterThan, 500.0);
            await facade.ClearFilterAsync();

            sw.Stop();
            var memAfter = GC.GetTotalMemory(false);

            return new TestResult
            {
                Category = "Load",
                Name = $"LargeDataset_{label}",
                Success = true,
                Duration = sw.Elapsed,
                Details = $"Processed {rowCount:N0} rows in {sw.Elapsed.TotalSeconds:F2}s",
                Metrics = new Dictionary<string, object>
                {
                    ["RowCount"] = rowCount,
                    ["MemoryMB"] = (memAfter - memBefore) / 1024.0 / 1024.0
                }
            };
        }
        catch (Exception ex)
        {
            return new TestResult { Category = "Load", Name = $"LargeDataset_{label}", Success = false, Error = ex.Message };
        }
    }

    private async Task<TestResult> TestHighFrequencyUpdates()
    {
        try
        {
            var sw = Stopwatch.StartNew();
            var facade = CreateTestFacade(10000);

            facade.AddColumn(new PublicColumnDefinition { Name = "Value", Header = "Value", DataType = typeof(int), IsVisible = true });

            // Add initial data
            for (int i = 0; i < 10_000; i++)
            {
                await facade.AddRowAsync(new Dictionary<string, object?> { ["Value"] = i });
            }

            // Perform 1000 updates
            for (int i = 0; i < 1000; i++)
            {
                await facade.UpdateCellAsync(i % 10_000, "Value", i * 10);
            }

            sw.Stop();

            return new TestResult
            {
                Category = "Load",
                Name = "HighFrequencyUpdates",
                Success = true,
                Duration = sw.Elapsed,
                Details = $"Performed 1000 updates in {sw.Elapsed.TotalMilliseconds:F2}ms"
            };
        }
        catch (Exception ex)
        {
            return new TestResult { Category = "Load", Name = "HighFrequencyUpdates", Success = false, Error = ex.Message };
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
        options.EnabledFeatures.Add(GridFeature.RowColumnOperations);

        return AdvancedDataGridFacadeFactory.CreateStandalone(options, NullLoggerFactory.Instance, null);
    }
}
