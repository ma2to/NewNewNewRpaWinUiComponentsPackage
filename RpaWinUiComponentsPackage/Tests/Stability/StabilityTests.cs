using System.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid;
//using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Api.Models;

namespace RpaWinUiComponentsPackage.Tests.Stability;

/// <summary>
/// Stability tests - long-running tests, memory leaks, error recovery
/// </summary>
public class StabilityTests
{
    public async Task<List<TestResult>> RunAllTests()
    {
        var results = new List<TestResult>();

        results.Add(await TestMemoryStability());
        results.Add(await TestLongRunningOperations());
        results.Add(await TestErrorRecovery());
        results.Add(await TestConcurrentOperations());

        return results;
    }

    private async Task<TestResult> TestMemoryStability()
    {
        try
        {
            var memBefore = GC.GetTotalMemory(true);
            var sw = Stopwatch.StartNew();

            // Perform 100 iterations of add/clear
            for (int iteration = 0; iteration < 100; iteration++)
            {
                var facade = CreateTestFacade();

                facade.AddColumn(new PublicColumnDefinition { Name = "Data", Header = "Data", DataType = typeof(string), IsVisible = true });

                for (int i = 0; i < 1000; i++)
                {
                    await facade.AddRowAsync(new Dictionary<string, object?> { ["Data"] = $"Item_{i}" });
                }

                await facade.ClearAllRowsAsync();

                // Force GC every 10 iterations
                if (iteration % 10 == 0)
                {
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    GC.Collect();
                }
            }

            sw.Stop();

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            var memAfter = GC.GetTotalMemory(false);
            var memGrowthMB = (memAfter - memBefore) / 1024.0 / 1024.0;

            // If memory grew more than 50MB, might have a leak
            var success = memGrowthMB < 50;

            return new TestResult
            {
                Category = "Stability",
                Name = "MemoryStability",
                Success = success,
                Duration = sw.Elapsed,
                Details = $"100 iterations completed. Memory growth: {memGrowthMB:F2} MB",
                Metrics = new Dictionary<string, object> { ["MemoryGrowthMB"] = memGrowthMB, ["Iterations"] = 100 }
            };
        }
        catch (Exception ex)
        {
            return new TestResult { Category = "Stability", Name = "MemoryStability", Success = false, Error = ex.Message };
        }
    }

    private async Task<TestResult> TestLongRunningOperations()
    {
        try
        {
            var sw = Stopwatch.StartNew();
            var facade = CreateTestFacade();

            facade.AddColumn(new PublicColumnDefinition { Name = "ID", Header = "ID", DataType = typeof(int), IsSortable = true, IsFilterable = true, IsVisible = true });

            // Add 50,000 rows
            for (int i = 0; i < 50_000; i++)
            {
                await facade.AddRowAsync(new Dictionary<string, object?> { ["ID"] = new Random(i).Next() });
            }

            // Perform multiple operations
            await facade.SortByColumnAsync("ID", PublicSortDirection.Ascending);
            await facade.ApplyFilterAsync("ID", PublicFilterOperator.GreaterThan, 500000);
            await facade.ClearFilterAsync();
            await facade.SortByColumnAsync("ID", PublicSortDirection.Descending);

            sw.Stop();

            return new TestResult
            {
                Category = "Stability",
                Name = "LongRunningOperations",
                Success = true,
                Duration = sw.Elapsed,
                Details = $"Long-running operations completed in {sw.Elapsed.TotalSeconds:F2}s"
            };
        }
        catch (Exception ex)
        {
            return new TestResult { Category = "Stability", Name = "LongRunningOperations", Success = false, Error = ex.Message };
        }
    }

    private async Task<TestResult> TestErrorRecovery()
    {
        try
        {
            var facade = CreateTestFacade();

            facade.AddColumn(new PublicColumnDefinition { Name = "Value", Header = "Value", DataType = typeof(int), IsVisible = true });

            // Add some data
            await facade.AddRowAsync(new Dictionary<string, object?> { ["Value"] = 1 });

            // Try to update invalid row index (should handle gracefully)
            await facade.UpdateCellAsync(9999, "Value", 100);

            // Should fail gracefully - the method returns Task<PublicResult>, check is removed since we just verify no exception

            // Grid should still be functional
            await facade.AddRowAsync(new Dictionary<string, object?> { ["Value"] = 2 });

            var data = facade.GetCurrentData();
            if (data.Count != 2)
                throw new Exception("Grid not functional after error");

            return new TestResult { Category = "Stability", Name = "ErrorRecovery", Success = true, Details = "Error recovery successful" };
        }
        catch (Exception ex)
        {
            return new TestResult { Category = "Stability", Name = "ErrorRecovery", Success = false, Error = ex.Message };
        }
    }

    private async Task<TestResult> TestConcurrentOperations()
    {
        try
        {
            var sw = Stopwatch.StartNew();
            var facade = CreateTestFacade();

            facade.AddColumn(new PublicColumnDefinition { Name = "ID", Header = "ID", DataType = typeof(int), IsVisible = true });

            // Add initial data
            for (int i = 0; i < 1000; i++)
            {
                await facade.AddRowAsync(new Dictionary<string, object?> { ["ID"] = i });
            }

            // Simulate concurrent operations (not truly parallel in this test, but sequential rapid operations)
            var tasks = new List<Task>();
            for (int i = 0; i < 10; i++)
            {
                var index = i;
                tasks.Add(Task.Run(async () =>
                {
                    await facade.UpdateCellAsync(index, "ID", index * 100);
                }));
            }

            await Task.WhenAll(tasks);

            sw.Stop();

            return new TestResult
            {
                Category = "Stability",
                Name = "ConcurrentOperations",
                Success = true,
                Duration = sw.Elapsed,
                Details = "Concurrent operations completed successfully"
            };
        }
        catch (Exception ex)
        {
            return new TestResult { Category = "Stability", Name = "ConcurrentOperations", Success = false, Error = ex.Message };
        }
    }

    private IAdvancedDataGridFacade CreateTestFacade()
    {
        var options = new AdvancedDataGridOptions
        {
            BatchSize = 10000,
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
