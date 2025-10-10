using BenchmarkDotNet.Attributes;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid;
using RpaWinUiComponentsPackage.PerformanceBenchmarks.Helpers;

namespace RpaWinUiComponentsPackage.PerformanceBenchmarks.Benchmarks;

/// <summary>
/// Specialized benchmarks for UI-specific operations
/// Tests UI dispatcher overhead, notifications, and rendering performance
/// </summary>
[MemoryDiagnoser]
[ThreadingDiagnoser]
public class UIPerformanceBenchmarks
{
    private IAdvancedDataGridFacade? _uiFacade;
    private IAdvancedDataGridFacade? _headlessFacade;

    [Params(10_000, 100_000, 1_000_000)]
    public int RowCount { get; set; }

    [GlobalSetup]
    public async Task Setup()
    {
        if (!UIBenchmarkHelper.IsUIAvailable())
        {
            Console.WriteLine("[SETUP] UI not available, UI benchmarks will be skipped");
            return;
        }

        Console.WriteLine($"[SETUP] Initializing UI facade for {RowCount:N0} rows...");

        // Create UI facade
        _uiFacade = UIBenchmarkHelper.CreateWithUI(
            5000,
            GridFeature.RowColumnOperations,
            GridFeature.Sort,
            GridFeature.Filter,
            GridFeature.Selection,
            GridFeature.UI
        );

        // Create headless facade for comparison
        _headlessFacade = UIBenchmarkHelper.CreateHeadless(
            5000,
            GridFeature.RowColumnOperations,
            GridFeature.Sort,
            GridFeature.Filter,
            GridFeature.Selection
        );

        // Setup columns
        _uiFacade.AddColumn(UIBenchmarkHelper.CreateColumn("ID", typeof(int)));
        _uiFacade.AddColumn(UIBenchmarkHelper.CreateColumn("Name", typeof(string)));
        _uiFacade.AddColumn(UIBenchmarkHelper.CreateColumn("Value", typeof(decimal)));

        _headlessFacade.AddColumn(UIBenchmarkHelper.CreateColumn("ID", typeof(int)));
        _headlessFacade.AddColumn(UIBenchmarkHelper.CreateColumn("Name", typeof(string)));
        _headlessFacade.AddColumn(UIBenchmarkHelper.CreateColumn("Value", typeof(decimal)));

        // Load data
        var random = new Random(42);
        for (int i = 0; i < RowCount; i++)
        {
            var row = new Dictionary<string, object?>
            {
                ["ID"] = i,
                ["Name"] = $"Row_{i}",
                ["Value"] = (decimal)(i * 1.5m)
            };

            await _uiFacade.AddRowAsync(row);
            await _headlessFacade.AddRowAsync(row);

            if ((i + 1) % 10000 == 0)
            {
                Console.WriteLine($"[SETUP] Loaded {i + 1:N0} rows");
            }
        }

        Console.WriteLine($"[SETUP] Completed loading {RowCount:N0} rows");
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        if (_uiFacade is IDisposable uiDisposable)
            uiDisposable.Dispose();

        if (_headlessFacade is IDisposable headlessDisposable)
            headlessDisposable.Dispose();

        UIBenchmarkHelper.Cleanup();
    }

    [Benchmark(Baseline = true)]
    public async Task Sort_Headless()
    {
        if (_headlessFacade == null)
        {
            Console.WriteLine("[SKIP] Headless facade not initialized");
            return;
        }

        await _headlessFacade.SortByColumnAsync("ID", PublicSortDirection.Ascending);
    }

    [Benchmark]
    public async Task Sort_WithUI()
    {
        if (_uiFacade == null)
        {
            Console.WriteLine("[SKIP] UI facade not initialized");
            return;
        }

        await _uiFacade.SortByColumnAsync("ID", PublicSortDirection.Ascending);
    }

    [Benchmark]
    public async Task Filter_Headless()
    {
        if (_headlessFacade == null)
        {
            Console.WriteLine("[SKIP] Headless facade not initialized");
            return;
        }

        await _headlessFacade.ApplyFilterAsync("ID", PublicFilterOperator.GreaterThan, RowCount / 2);
    }

    [Benchmark]
    public async Task Filter_WithUI()
    {
        if (_uiFacade == null)
        {
            Console.WriteLine("[SKIP] UI facade not initialized");
            return;
        }

        await _uiFacade.ApplyFilterAsync("ID", PublicFilterOperator.GreaterThan, RowCount / 2);
    }

    [Benchmark]
    public async Task Selection_Headless()
    {
        if (_headlessFacade == null)
        {
            Console.WriteLine("[SKIP] Headless facade not initialized");
            return;
        }

        // Select multiple rows
        var rowsToSelect = Math.Min(1000, RowCount);
        for (int i = 0; i < rowsToSelect; i += 10)
        {
            await _headlessFacade.SelectRowAsync(i);
        }

        // Clear selection
        await _headlessFacade.ClearSelectionAsync();
    }

    [Benchmark]
    public async Task Selection_WithUI()
    {
        if (_uiFacade == null)
        {
            Console.WriteLine("[SKIP] UI facade not initialized");
            return;
        }

        // Select multiple rows
        var rowsToSelect = Math.Min(1000, RowCount);
        for (int i = 0; i < rowsToSelect; i += 10)
        {
            await _uiFacade.SelectRowAsync(i);
        }

        // Clear selection
        await _uiFacade.ClearSelectionAsync();
    }

    [Benchmark]
    public async Task UINotifications_Overhead()
    {
        if (_uiFacade == null)
        {
            Console.WriteLine("[SKIP] UI facade not initialized");
            return;
        }

        Console.WriteLine($"[BENCHMARK] Testing UI notification overhead with {RowCount:N0} rows...");

        var startTime = DateTime.UtcNow;

        // Perform operations that trigger UI notifications
        await _uiFacade.SortByColumnAsync("ID", PublicSortDirection.Ascending);
        await _uiFacade.ApplyFilterAsync("Value", PublicFilterOperator.GreaterThan, 0);
        await _uiFacade.ClearFilterAsync();

        var elapsed = DateTime.UtcNow - startTime;
        Console.WriteLine($"[BENCHMARK] UI operations completed in {elapsed.TotalMilliseconds:F2}ms");
    }

    [Benchmark]
    public async Task RowUpdate_WithUI()
    {
        if (_uiFacade == null)
        {
            Console.WriteLine("[SKIP] UI facade not initialized");
            return;
        }

        Console.WriteLine($"[BENCHMARK] Testing row updates with UI...");

        var startTime = DateTime.UtcNow;
        var updateCount = Math.Min(1000, RowCount);

        for (int i = 0; i < updateCount; i++)
        {
            await _uiFacade.UpdateCellAsync(i, "Value", (decimal)(i * 2.0m));
        }

        var elapsed = DateTime.UtcNow - startTime;
        var updatesPerSec = updateCount / elapsed.TotalSeconds;
        Console.WriteLine($"[BENCHMARK] Updated {updateCount:N0} rows in {elapsed.TotalMilliseconds:F2}ms ({updatesPerSec:N0} updates/sec)");
    }

    [Benchmark]
    public async Task RowUpdate_Headless()
    {
        if (_headlessFacade == null)
        {
            Console.WriteLine("[SKIP] Headless facade not initialized");
            return;
        }

        Console.WriteLine($"[BENCHMARK] Testing row updates headless...");

        var startTime = DateTime.UtcNow;
        var updateCount = Math.Min(1000, RowCount);

        for (int i = 0; i < updateCount; i++)
        {
            await _headlessFacade.UpdateCellAsync(i, "Value", (decimal)(i * 2.0m));
        }

        var elapsed = DateTime.UtcNow - startTime;
        var updatesPerSec = updateCount / elapsed.TotalSeconds;
        Console.WriteLine($"[BENCHMARK] Updated {updateCount:N0} rows in {elapsed.TotalMilliseconds:F2}ms ({updatesPerSec:N0} updates/sec)");
    }

    [Benchmark]
    public void DataAccess_WithUI()
    {
        if (_uiFacade == null)
        {
            Console.WriteLine("[SKIP] UI facade not initialized");
            return;
        }

        var startTime = DateTime.UtcNow;

        var data = _uiFacade.GetCurrentData();
        var count = data.Count;

        var elapsed = DateTime.UtcNow - startTime;
        Console.WriteLine($"[BENCHMARK] Data access with UI: {elapsed.TotalMilliseconds:F2}ms for {count:N0} rows");
    }

    [Benchmark]
    public void DataAccess_Headless()
    {
        if (_headlessFacade == null)
        {
            Console.WriteLine("[SKIP] Headless facade not initialized");
            return;
        }

        var startTime = DateTime.UtcNow;

        var data = _headlessFacade.GetCurrentData();
        var count = data.Count;

        var elapsed = DateTime.UtcNow - startTime;
        Console.WriteLine($"[BENCHMARK] Data access headless: {elapsed.TotalMilliseconds:F2}ms for {count:N0} rows");
    }

    [Benchmark]
    public async Task BulkInsert_WithUI()
    {
        if (_uiFacade == null)
        {
            Console.WriteLine("[SKIP] UI facade not initialized");
            return;
        }

        Console.WriteLine($"[BENCHMARK] Testing bulk insert with UI...");

        // Clear existing data
        await _uiFacade.ClearAllRowsAsync();

        var startTime = DateTime.UtcNow;
        var insertCount = Math.Min(10000, RowCount);

        for (int i = 0; i < insertCount; i++)
        {
            await _uiFacade.AddRowAsync(new Dictionary<string, object?>
            {
                ["ID"] = i,
                ["Name"] = $"New_{i}",
                ["Value"] = (decimal)(i * 3.0m)
            });
        }

        var elapsed = DateTime.UtcNow - startTime;
        var insertsPerSec = insertCount / elapsed.TotalSeconds;
        Console.WriteLine($"[BENCHMARK] Bulk insert with UI: {insertCount:N0} rows in {elapsed.TotalMilliseconds:F2}ms ({insertsPerSec:N0} inserts/sec)");
    }

    [Benchmark]
    public async Task BulkInsert_Headless()
    {
        if (_headlessFacade == null)
        {
            Console.WriteLine("[SKIP] Headless facade not initialized");
            return;
        }

        Console.WriteLine($"[BENCHMARK] Testing bulk insert headless...");

        // Clear existing data
        await _headlessFacade.ClearAllRowsAsync();

        var startTime = DateTime.UtcNow;
        var insertCount = Math.Min(10000, RowCount);

        for (int i = 0; i < insertCount; i++)
        {
            await _headlessFacade.AddRowAsync(new Dictionary<string, object?>
            {
                ["ID"] = i,
                ["Name"] = $"New_{i}",
                ["Value"] = (decimal)(i * 3.0m)
            });
        }

        var elapsed = DateTime.UtcNow - startTime;
        var insertsPerSec = insertCount / elapsed.TotalSeconds;
        Console.WriteLine($"[BENCHMARK] Bulk insert headless: {insertCount:N0} rows in {elapsed.TotalMilliseconds:F2}ms ({insertsPerSec:N0} inserts/sec)");
    }
}
