using System.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Api.Models;

namespace RpaWinUiComponentsPackage.PerformanceTests;

/// <summary>
/// Simple headless performance test - no UI dependencies
/// Tests different batch sizes and row counts to find optimal configuration
/// </summary>
class SimplePerfTest
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("=".PadRight(80, '='));
        Console.WriteLine("ADVANCED DATA GRID - HEADLESS PERFORMANCE TESTS");
        Console.WriteLine("=".PadRight(80, '='));
        Console.WriteLine();

        var results = new List<TestResult>();

        // Test configurations
        var rowCounts = new[] { 10_000, 50_000, 100_000, 500_000, 1_000_000 };
        var batchSizes = new[] { 1_000, 5_000, 10_000, 20_000, 50_000 };

        foreach (var rowCount in rowCounts)
        {
            Console.WriteLine($"\n>>> Testing with {rowCount:N0} rows...");

            foreach (var batchSize in batchSizes)
            {
                Console.Write($"  BatchSize={batchSize,6:N0} ... ");

                var result = await RunTest(rowCount, batchSize);
                results.Add(result);

                Console.WriteLine($"{result.Duration.TotalSeconds,6:F2}s | {result.MemoryMB,6:F1} MB");

                // Force GC between tests
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
                await Task.Delay(500);
            }
        }

        // Generate report
        Console.WriteLine("\n" + "=".PadRight(80, '='));
        Console.WriteLine("RESULTS SUMMARY");
        Console.WriteLine("=".PadRight(80, '='));

        GenerateReport(results);
        GenerateRecommendations(results);

        Console.WriteLine("\nPress any key to exit...");
        Console.ReadKey();
    }

    static async Task<TestResult> RunTest(int rowCount, int batchSize)
    {
        var sw = Stopwatch.StartNew();
        var memBefore = GC.GetTotalMemory(true);

        try
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

            var facade = AdvancedDataGridFacadeFactory.CreateStandalone(options, NullLoggerFactory.Instance, null);

            // Add columns
            facade.AddColumn(new PublicColumnDefinition
            {
                Name = "ID",
                Header = "ID",
                DataType = typeof(int),
                Width = 100,
                IsVisible = true,
                IsSortable = true,
                IsFilterable = true
            });

            facade.AddColumn(new PublicColumnDefinition
            {
                Name = "Name",
                Header = "Name",
                DataType = typeof(string),
                Width = 200,
                IsVisible = true,
                IsSortable = true,
                IsFilterable = true
            });

            facade.AddColumn(new PublicColumnDefinition
            {
                Name = "Value",
                Header = "Value",
                DataType = typeof(double),
                Width = 100,
                IsVisible = true,
                IsSortable = true,
                IsFilterable = true
            });

            // Insert rows
            var random = new Random(42);
            for (int i = 0; i < rowCount; i++)
            {
                await facade.AddRowAsync(new Dictionary<string, object?>
                {
                    ["ID"] = random.Next(0, rowCount * 2),
                    ["Name"] = $"Item_{i}",
                    ["Value"] = random.NextDouble() * 1000
                });
            }

            // Sort operation
            await facade.SortByColumnAsync("ID", PublicSortDirection.Ascending);

            // Filter operation
            await facade.ApplyFilterAsync("Value", PublicFilterOperator.GreaterThan, 500.0);

            // Get data
            var data = facade.GetCurrentData();

            sw.Stop();
            var memAfter = GC.GetTotalMemory(false);

            return new TestResult
            {
                RowCount = rowCount,
                BatchSize = batchSize,
                Duration = sw.Elapsed,
                MemoryMB = (memAfter - memBefore) / 1024.0 / 1024.0,
                Success = true
            };
        }
        catch (Exception ex)
        {
            sw.Stop();
            return new TestResult
            {
                RowCount = rowCount,
                BatchSize = batchSize,
                Duration = sw.Elapsed,
                MemoryMB = 0,
                Success = false,
                Error = ex.Message
            };
        }
    }

    static void GenerateReport(List<TestResult> results)
    {
        Console.WriteLine("\nDetailed Results:");
        Console.WriteLine("-".PadRight(80, '-'));
        Console.WriteLine($"{"Rows",10} | {"Batch",8} | {"Time (s)",10} | {"Memory (MB)",12} | {"Status",10}");
        Console.WriteLine("-".PadRight(80, '-'));

        foreach (var result in results.OrderBy(r => r.RowCount).ThenBy(r => r.BatchSize))
        {
            var status = result.Success ? "OK" : "FAILED";
            Console.WriteLine($"{result.RowCount,10:N0} | {result.BatchSize,8:N0} | {result.Duration.TotalSeconds,10:F3} | {result.MemoryMB,12:F2} | {status,10}");
        }
    }

    static void GenerateRecommendations(List<TestResult> results)
    {
        Console.WriteLine("\n" + "=".PadRight(80, '='));
        Console.WriteLine("RECOMMENDATIONS");
        Console.WriteLine("=".PadRight(80, '='));

        var successResults = results.Where(r => r.Success).ToList();

        // Group by row count and find best batch size for each
        var grouped = successResults.GroupBy(r => r.RowCount);

        foreach (var group in grouped.OrderBy(g => g.Key))
        {
            var best = group.OrderBy(r => r.Duration).First();
            Console.WriteLine($"\n{group.Key,10:N0} rows: BatchSize = {best.BatchSize,6:N0} ({best.Duration.TotalSeconds,6:F2}s)");
        }

        // Overall best batch size (weighted by row count)
        var bestOverall = successResults
            .GroupBy(r => r.BatchSize)
            .Select(g => new
            {
                BatchSize = g.Key,
                AvgTime = g.Average(r => r.Duration.TotalSeconds),
                AvgMemory = g.Average(r => r.MemoryMB)
            })
            .OrderBy(x => x.AvgTime)
            .First();

        Console.WriteLine($"\n>>> OPTIMAL BATCH SIZE: {bestOverall.BatchSize:N0}");
        Console.WriteLine($"    Average Time: {bestOverall.AvgTime:F2}s");
        Console.WriteLine($"    Average Memory: {bestOverall.AvgMemory:F1} MB");
    }

    class TestResult
    {
        public int RowCount { get; set; }
        public int BatchSize { get; set; }
        public TimeSpan Duration { get; set; }
        public double MemoryMB { get; set; }
        public bool Success { get; set; }
        public string? Error { get; set; }
    }
}
