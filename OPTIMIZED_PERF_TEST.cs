using System.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Api.Models;

namespace RpaWinUiComponentsPackage.PerformanceTests;

/// <summary>
/// Optimized performance test using the new AddRowsBatchAsync method
/// </summary>
class OptimizedPerfTest
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("=================================================");
        Console.WriteLine("OPTIMIZED PERFORMANCE TEST - AddRowsBatchAsync");
        Console.WriteLine("=================================================");
        Console.WriteLine();

        var rowCounts = new[] { 10_000, 50_000, 100_000, 500_000, 1_000_000 };

        foreach (var rowCount in rowCounts)
        {
            await RunBatchTest(rowCount);
            Console.WriteLine();
        }

        Console.WriteLine("=================================================");
        Console.WriteLine("ALL TESTS COMPLETED");
        Console.WriteLine("=================================================");
    }

    static async Task RunBatchTest(int rowCount)
    {
        Console.WriteLine($"--- Testing {rowCount:N0} rows with AddRowsBatchAsync ---");

        var options = new AdvancedDataGridOptions
        {
            BatchSize = 10000,
            EnableParallelProcessing = true,
            EnableRealTimeValidation = false,
            LoggerFactory = NullLoggerFactory.Instance,
            OperationMode = PublicDataGridOperationMode.Headless
        };

        options.EnabledFeatures.Clear();
        options.EnabledFeatures.Add(GridFeature.RowColumnOperations);

        var facade = AdvancedDataGridFacadeFactory.CreateStandalone(options, NullLoggerFactory.Instance, null);

        // Add columns
        facade.AddColumn(new PublicColumnDefinition
        {
            Name = "ID",
            Header = "ID",
            DataType = typeof(int),
            IsVisible = true
        });
        facade.AddColumn(new PublicColumnDefinition
        {
            Name = "Name",
            Header = "Name",
            DataType = typeof(string),
            IsVisible = true
        });

        // Prepare batch data
        var random = new Random(42);
        var rowsData = new List<Dictionary<string, object?>>(rowCount);

        for (int i = 0; i < rowCount; i++)
        {
            rowsData.Add(new Dictionary<string, object?>
            {
                ["ID"] = random.Next(),
                ["Name"] = $"Row_{i}"
            });
        }

        // Measure batch insert
        var memBefore = GC.GetTotalMemory(true);
        var sw = Stopwatch.StartNew();

        var addedCount = await facade.AddRowsBatchAsync(rowsData);

        sw.Stop();
        var memAfter = GC.GetTotalMemory(false);

        var memoryMB = (memAfter - memBefore) / 1024.0 / 1024.0;
        var rowsPerSecond = rowCount / sw.Elapsed.TotalSeconds;

        Console.WriteLine($"  Duration: {sw.Elapsed.TotalSeconds:F2}s");
        Console.WriteLine($"  Memory: {memoryMB:F2} MB");
        Console.WriteLine($"  Throughput: {rowsPerSecond:N0} rows/s");
        Console.WriteLine($"  Added Count: {addedCount:N0}");
        Console.WriteLine($"  Status: {(addedCount == rowCount ? "✓ SUCCESS" : "✗ FAILED")}");
    }
}
