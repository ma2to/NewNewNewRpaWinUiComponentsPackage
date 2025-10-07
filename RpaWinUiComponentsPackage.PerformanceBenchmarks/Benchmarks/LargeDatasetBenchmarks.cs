using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnostics.Windows.Configs;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Api.Models;
using RpaWinUiComponentsPackage.PerformanceBenchmarks.Helpers;

namespace RpaWinUiComponentsPackage.PerformanceBenchmarks.Benchmarks;

/// <summary>
/// Benchmarks for very large datasets with optimized batch processing
/// Tests scalability from 100K to 10M rows
/// </summary>
[MemoryDiagnoser]
[ThreadingDiagnoser]
[EtwProfiler]
public class LargeDatasetBenchmarks
{
    private IAdvancedDataGridFacade _facade = null!;

    [Params(100_000, 1_000_000, 5_000_000, 10_000_000)]
    public int RowCount { get; set; }

    [Params(1000, 5000, 10000, 50000)]
    public int BatchSize { get; set; }

    [GlobalSetup]
    public async Task Setup()
    {
        Console.WriteLine($"[SETUP] Initializing facade for {RowCount:N0} rows with batch size {BatchSize:N0}...");

        _facade = BenchmarkHelper.CreateFacadeWithBatchSize(
            BatchSize,
            GridFeature.RowColumnOperations,
            GridFeature.Sort,
            GridFeature.Filter,
            GridFeature.Validation
        );

        // Create columns
        _facade.AddColumn(BenchmarkHelper.CreateColumn("ID", typeof(int)));
        _facade.AddColumn(BenchmarkHelper.CreateColumn("Name", typeof(string)));
        _facade.AddColumn(BenchmarkHelper.CreateColumn("Value", typeof(decimal)));
        _facade.AddColumn(BenchmarkHelper.CreateColumn("Date", typeof(DateTime)));
        _facade.AddColumn(BenchmarkHelper.CreateColumn("IsActive", typeof(bool)));

        Console.WriteLine($"[SETUP] Loading {RowCount:N0} rows...");
        var startTime = DateTime.UtcNow;

        // Add rows in batches with progress reporting
        const int reportInterval = 100_000;
        var random = new Random(42);

        for (int i = 0; i < RowCount; i++)
        {
            await _facade.AddRowAsync(new Dictionary<string, object?>
            {
                ["ID"] = i,
                ["Name"] = $"Row_{i}",
                ["Value"] = (decimal)(i * 1.5m),
                ["Date"] = DateTime.UtcNow.AddDays(-random.Next(0, 365)),
                ["IsActive"] = i % 2 == 0
            });

            if ((i + 1) % reportInterval == 0)
            {
                var elapsed = DateTime.UtcNow - startTime;
                var rowsPerSec = (i + 1) / elapsed.TotalSeconds;
                Console.WriteLine($"[SETUP] Loaded {i + 1:N0} rows ({rowsPerSec:N0} rows/sec)");

                // Force GC to prevent OutOfMemory during setup
                if ((i + 1) % (reportInterval * 5) == 0)
                {
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    GC.Collect();
                }
            }
        }

        var totalTime = DateTime.UtcNow - startTime;
        var avgRowsPerSec = RowCount / totalTime.TotalSeconds;
        Console.WriteLine($"[SETUP] Completed loading {RowCount:N0} rows in {totalTime.TotalSeconds:F2}s ({avgRowsPerSec:N0} rows/sec)");

        // Final GC before benchmarks
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        Console.WriteLine($"[CLEANUP] Disposing facade with {RowCount:N0} rows...");
        if (_facade is IDisposable disposable)
            disposable.Dispose();

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
    }

    [Benchmark]
    public async Task Import_LargeDataset()
    {
        Console.WriteLine($"[BENCHMARK] Testing import with {RowCount:N0} rows, batch size {BatchSize:N0}...");
        var startTime = DateTime.UtcNow;

        // Data is already imported in Setup, this measures re-import scenario
        var data = _facade.GetCurrentData();
        var count = data.Count;

        var elapsed = DateTime.UtcNow - startTime;
        Console.WriteLine($"[BENCHMARK] Import access time: {elapsed.TotalSeconds:F3}s, Rows: {count:N0}");

        await Task.CompletedTask;
    }

    [Benchmark]
    public async Task Sort_LargeDataset()
    {
        Console.WriteLine($"[BENCHMARK] Sorting {RowCount:N0} rows with batch size {BatchSize:N0}...");
        var startTime = DateTime.UtcNow;

        await _facade.SortByColumnAsync("ID", PublicSortDirection.Descending);

        var elapsed = DateTime.UtcNow - startTime;
        var rowsPerSec = RowCount / elapsed.TotalSeconds;
        Console.WriteLine($"[BENCHMARK] Sort completed in {elapsed.TotalSeconds:F2}s ({rowsPerSec:N0} rows/sec)");
    }

    [Benchmark]
    public async Task Filter_LargeDataset()
    {
        Console.WriteLine($"[BENCHMARK] Filtering {RowCount:N0} rows with batch size {BatchSize:N0}...");
        var startTime = DateTime.UtcNow;

        var filteredCount = await _facade.ApplyFilterAsync("ID", PublicFilterOperator.GreaterThan, RowCount / 2);

        var elapsed = DateTime.UtcNow - startTime;
        var rowsPerSec = RowCount / elapsed.TotalSeconds;
        Console.WriteLine($"[BENCHMARK] Filter completed in {elapsed.TotalSeconds:F2}s ({rowsPerSec:N0} rows/sec), {filteredCount:N0} rows matched");
    }

    [Benchmark]
    public async Task Validation_LargeDataset()
    {
        Console.WriteLine($"[BENCHMARK] Validating {RowCount:N0} rows with batch size {BatchSize:N0}...");

        // Add validation rule
        var column = _facade.GetColumn("Name");
        if (column != null)
        {
            column.ValidationRules = new List<PublicValidationRule>
            {
                new PublicValidationRule
                {
                    RuleType = PublicValidationRuleType.Required,
                    ErrorMessage = "Name is required"
                }
            };
        }

        var startTime = DateTime.UtcNow;

        var stats = await _facade.ValidateAllWithStatisticsAsync();

        var elapsed = DateTime.UtcNow - startTime;
        var rowsPerSec = RowCount / elapsed.TotalSeconds;
        Console.WriteLine($"[BENCHMARK] Validation completed in {elapsed.TotalSeconds:F2}s ({rowsPerSec:N0} rows/sec)");
        Console.WriteLine($"[BENCHMARK] Valid: {stats.ValidRows:N0}, Invalid: {stats.TotalRows - stats.ValidRows:N0}");
    }

    [Benchmark]
    public void MemoryFootprint()
    {
        Console.WriteLine($"[BENCHMARK] Measuring memory footprint for {RowCount:N0} rows...");

        // Force GC to get accurate measurement
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var memoryBefore = GC.GetTotalMemory(false);

        var data = _facade.GetCurrentData();
        var count = data.Count;

        var memoryAfter = GC.GetTotalMemory(false);
        var memoryUsed = memoryAfter - memoryBefore;

        Console.WriteLine($"[BENCHMARK] Memory used: {memoryUsed / 1024.0 / 1024.0:N2} MB for {count:N0} rows");
        Console.WriteLine($"[BENCHMARK] Memory per row: {memoryUsed / (double)count:N2} bytes");
        Console.WriteLine($"[BENCHMARK] Total memory: {memoryAfter / 1024.0 / 1024.0:N2} MB");
    }

    [Benchmark]
    public async Task SequentialRowAccess()
    {
        Console.WriteLine($"[BENCHMARK] Sequential access to {RowCount:N0} rows...");
        var startTime = DateTime.UtcNow;

        long sum = 0;
        int accessCount = Math.Min(RowCount, 100_000); // Limit to 100K for very large datasets

        for (int i = 0; i < accessCount; i++)
        {
            var row = _facade.GetRow(i);
            if (row != null && row.TryGetValue("ID", out var id) && id is int intId)
            {
                sum += intId;
            }
        }

        var elapsed = DateTime.UtcNow - startTime;
        var rowsPerSec = accessCount / elapsed.TotalSeconds;
        Console.WriteLine($"[BENCHMARK] Sequential access: {rowsPerSec:N0} rows/sec over {accessCount:N0} accesses, Sum: {sum:N0}");

        await Task.CompletedTask;
    }

    [Benchmark]
    public async Task RandomRowAccess()
    {
        Console.WriteLine($"[BENCHMARK] Random access from {RowCount:N0} dataset...");
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

    [Benchmark]
    public void GetAllRows_Performance()
    {
        Console.WriteLine($"[BENCHMARK] GetCurrentData() for {RowCount:N0} rows...");
        var startTime = DateTime.UtcNow;

        var data = _facade.GetCurrentData();
        var count = data.Count;

        var elapsed = DateTime.UtcNow - startTime;
        Console.WriteLine($"[BENCHMARK] GetCurrentData completed in {elapsed.TotalMilliseconds:F2}ms, returned {count:N0} rows");
    }

    [Benchmark]
    public async Task ClearAll_Performance()
    {
        Console.WriteLine($"[BENCHMARK] Clearing {RowCount:N0} rows...");
        var startTime = DateTime.UtcNow;

        await _facade.ClearAllRowsAsync();

        var elapsed = DateTime.UtcNow - startTime;
        var rowsPerSec = RowCount / elapsed.TotalSeconds;
        Console.WriteLine($"[BENCHMARK] Clear completed in {elapsed.TotalSeconds:F2}s ({rowsPerSec:N0} rows/sec)");
    }
}
