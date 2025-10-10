using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnostics.Windows.Configs;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid;
using RpaWinUiComponentsPackage.PerformanceBenchmarks.Helpers;

namespace RpaWinUiComponentsPackage.PerformanceBenchmarks.Benchmarks;

/// <summary>
/// Stress tests with very large datasets (1M - 10M rows)
/// Tests system limits, memory pressure, and GC behavior
/// </summary>
[MemoryDiagnoser]
[ThreadingDiagnoser]
[EtwProfiler]
public class LargeDatasetStressTests
{
    private IAdvancedDataGridFacade _facade = null!;

    [Params(1_000_000, 5_000_000, 10_000_000)]
    public int RowCount { get; set; }

    [GlobalSetup]
    public async Task Setup()
    {
        _facade = BenchmarkHelper.CreateFacade(
            GridFeature.RowColumnOperations,
            GridFeature.Sort,
            GridFeature.Filter
        );

        _facade.AddColumn(BenchmarkHelper.CreateColumn("ID", typeof(int)));
        _facade.AddColumn(BenchmarkHelper.CreateColumn("Value", typeof(decimal)));

        Console.WriteLine($"[SETUP] Loading {RowCount:N0} rows...");
        var startTime = DateTime.UtcNow;

        // Add rows in batches to show progress
        const int reportInterval = 100_000;
        for (int i = 0; i < RowCount; i++)
        {
            await _facade.AddRowAsync(new Dictionary<string, object?>
            {
                ["ID"] = i,
                ["Value"] = i * 1.5m
            });

            if ((i + 1) % reportInterval == 0)
            {
                var elapsed = DateTime.UtcNow - startTime;
                var rowsPerSec = (i + 1) / elapsed.TotalSeconds;
                Console.WriteLine($"[SETUP] Loaded {i + 1:N0} rows ({rowsPerSec:N0} rows/sec)");
            }
        }

        var totalTime = DateTime.UtcNow - startTime;
        Console.WriteLine($"[SETUP] Completed loading {RowCount:N0} rows in {totalTime.TotalSeconds:F2}s");
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        if (_facade is IDisposable disposable)
            disposable.Dispose();
    }

    [Benchmark]
    public void FullDatasetIteration()
    {
        var data = _facade.GetCurrentData();
        long sum = 0;

        foreach (var row in data)
        {
            if (row.TryGetValue("ID", out var id) && id is int intId)
            {
                sum += intId;
            }
        }

        Console.WriteLine($"[BENCHMARK] Sum: {sum:N0}");
    }

    [Benchmark]
    public async Task SortLargeDataset()
    {
        Console.WriteLine($"[BENCHMARK] Sorting {RowCount:N0} rows...");
        var startTime = DateTime.UtcNow;

        await _facade.SortByColumnAsync("ID", PublicSortDirection.Descending);

        var elapsed = DateTime.UtcNow - startTime;
        Console.WriteLine($"[BENCHMARK] Sort completed in {elapsed.TotalSeconds:F2}s");
    }

    [Benchmark]
    public async Task FilterLargeDataset()
    {
        Console.WriteLine($"[BENCHMARK] Filtering {RowCount:N0} rows...");
        var startTime = DateTime.UtcNow;

        var filteredCount = await _facade.ApplyFilterAsync("ID", PublicFilterOperator.GreaterThan, RowCount / 2);

        var elapsed = DateTime.UtcNow - startTime;
        Console.WriteLine($"[BENCHMARK] Filter completed in {elapsed.TotalSeconds:F2}s, {filteredCount:N0} rows matched");
    }

    [Benchmark]
    public void MemoryFootprint()
    {
        // Force GC to get accurate memory measurement
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var memoryBefore = GC.GetTotalMemory(false);

        var data = _facade.GetCurrentData();
        var rowCount = data.Count;

        var memoryAfter = GC.GetTotalMemory(false);
        var memoryUsed = memoryAfter - memoryBefore;

        Console.WriteLine($"[BENCHMARK] Memory used: {memoryUsed / 1024 / 1024:N2} MB for {rowCount:N0} rows");
        Console.WriteLine($"[BENCHMARK] Memory per row: {memoryUsed / (double)rowCount:N2} bytes");
    }

    [Benchmark]
    public async Task SequentialRowAccess()
    {
        Console.WriteLine($"[BENCHMARK] Sequential access to {RowCount:N0} rows...");
        var startTime = DateTime.UtcNow;

        long sum = 0;
        for (int i = 0; i < RowCount; i++)
        {
            var row = _facade.GetRow(i);
            if (row != null && row.TryGetValue("ID", out var id) && id is int intId)
            {
                sum += intId;
            }
        }

        var elapsed = DateTime.UtcNow - startTime;
        var rowsPerSec = RowCount / elapsed.TotalSeconds;
        Console.WriteLine($"[BENCHMARK] Sequential access: {rowsPerSec:N0} rows/sec, Sum: {sum:N0}");

        await Task.CompletedTask;
    }

    [Benchmark]
    public async Task RandomRowAccess()
    {
        Console.WriteLine($"[BENCHMARK] Random access to 10,000 rows from {RowCount:N0} dataset...");
        var startTime = DateTime.UtcNow;

        var random = new Random(42);
        long sum = 0;
        const int accessCount = 10_000;

        for (int i = 0; i < accessCount; i++)
        {
            int rowIndex = random.Next(0, RowCount);
            var row = _facade.GetRow(rowIndex);
            if (row != null && row.TryGetValue("ID", out var id) && id is int intId)
            {
                sum += intId;
            }
        }

        var elapsed = DateTime.UtcNow - startTime;
        var accessPerSec = accessCount / elapsed.TotalSeconds;
        Console.WriteLine($"[BENCHMARK] Random access: {accessPerSec:N0} accesses/sec, Sum: {sum:N0}");

        await Task.CompletedTask;
    }
}
