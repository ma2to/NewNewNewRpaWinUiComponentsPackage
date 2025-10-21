using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using Microsoft.Extensions.Logging;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.ViewModels;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Viewport;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Api;

namespace RpaWinUiComponentsPackage.ComprehensiveBenchmarks.Tests;

/// <summary>
/// Benchmarks for UI Virtualization (ViewportManager + ElementFactory).
/// Tests memory usage, viewport update performance, and element recycling.
///
/// TARGET METRICS:
/// - Memory reduction: 70-80% (460 MB → 35-55 MB for 100 rows)
/// - Viewport update: < 50ms for 50 rows
/// - Element recycling: < 5ms per element
/// </summary>
[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class UIVirtualizationBenchmarks
{
    private DataGridViewModel? _viewModel;
    private ViewportManager? _viewportManager;
    private ThemeManager? _themeManager;
    private ILogger<ViewportManager>? _logger;

    [Params(100, 1000, 10000)]
    public int RowCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        // Create ThemeManager
        _themeManager = new ThemeManager(null);

        // Create ViewModel with test data
        _viewModel = new DataGridViewModel(null, null, _themeManager);

        // Initialize columns
        var columnNames = new[] { "ID", "Name", "Value", "Status", "Date" };
        var options = new AdvancedDataGridOptions();
        _viewModel.InitializeColumns(columnNames, options);

        // Generate test data
        var testData = GenerateTestData(RowCount);
        _viewModel.LoadRows(testData);

        // Create ViewportManager
        _logger = Microsoft.Extensions.Logging.Abstractions.NullLogger<ViewportManager>.Instance;
        _viewportManager = new ViewportManager(_viewModel, _themeManager, _logger);
        _viewportManager.TotalRowCount = RowCount;
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _viewportManager?.Dispose();
        _viewModel = null;
    }

    /// <summary>
    /// Tests viewport update performance (load POCO data + create ViewModels).
    /// Target: < 50ms for 50 visible rows.
    /// </summary>
    [Benchmark]
    public async Task UpdateViewport_50Rows()
    {
        await _viewportManager!.UpdateViewportAsync(0, 49);
    }

    /// <summary>
    /// Tests viewport update with scrolling (simulates user scrolling).
    /// Target: < 100ms for scroll to middle of dataset.
    /// </summary>
    [Benchmark]
    public async Task ScrollToMiddle()
    {
        var middleStart = RowCount / 2;
        var middleEnd = middleStart + 49;
        await _viewportManager!.UpdateViewportAsync(middleStart, middleEnd);
    }

    /// <summary>
    /// Tests viewport invalidation (dispose all ViewModels).
    /// Target: < 20ms for cleanup.
    /// </summary>
    [Benchmark]
    public void InvalidateViewport()
    {
        _viewportManager!.InvalidateCache();
    }

    /// <summary>
    /// Tests multiple viewport updates (simulates rapid scrolling).
    /// Target: < 500ms for 10 scroll operations.
    /// </summary>
    [Benchmark]
    public async Task RapidScrolling_10Updates()
    {
        for (int i = 0; i < 10; i++)
        {
            var start = i * 50;
            var end = start + 49;
            if (end >= RowCount) break;

            await _viewportManager!.UpdateViewportAsync(start, end);
        }
    }

    /// <summary>
    /// Tests ViewModel disposal performance.
    /// Target: < 1ms per row.
    /// </summary>
    [Benchmark]
    public async Task CreateAndDisposeViewModels_50Rows()
    {
        // Create ViewModels
        await _viewportManager!.UpdateViewportAsync(0, 49);

        // Dispose them
        _viewportManager.InvalidateCache();
    }

    /// <summary>
    /// Measures memory usage of DataGridViewModel with all rows.
    /// Baseline measurement (WITHOUT virtualization).
    /// </summary>
    [Benchmark]
    public void Baseline_AllViewModelsInMemory()
    {
        // All rows are already in _viewModel.Rows (non-virtualized)
        var rowCount = _viewModel!.Rows.Count;
        var cellCount = _viewModel.Rows.Sum(r => r.Cells.Count);

        // Force GC to measure accurate memory
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
    }

    /// <summary>
    /// Measures memory usage with ViewportManager (WITH virtualization).
    /// Should be 70-80% lower than baseline.
    /// </summary>
    [Benchmark]
    public async Task Virtualized_OnlyViewportInMemory()
    {
        // Load only viewport (50 rows)
        await _viewportManager!.UpdateViewportAsync(0, 49);

        // Force GC to measure accurate memory
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
    }

    /// <summary>
    /// Tests POCO cache performance (convert ViewModels → POCO).
    /// Target: < 10ms for 1000 rows.
    /// </summary>
    [Benchmark]
    public async Task POCOCacheLoad_Viewport()
    {
        // Invalidate first to force reload
        _viewportManager!.InvalidateCache();

        // Load viewport (will create POCO cache)
        await _viewportManager.UpdateViewportAsync(0, 999);
    }

    /// <summary>
    /// Tests ViewModel creation from POCO.
    /// Target: < 1ms per row.
    /// </summary>
    [Benchmark]
    public async Task CreateViewModelsFromPOCO_50Rows()
    {
        // Pre-load POCO cache
        await _viewportManager!.UpdateViewportAsync(0, 999);

        // Invalidate ViewModels (keep POCO)
        _viewportManager.InvalidateCache();

        // Re-create ViewModels from POCO (should be fast)
        await _viewportManager.UpdateViewportAsync(0, 49);
    }

    /// <summary>
    /// Generates test data for benchmarks.
    /// </summary>
    private List<IReadOnlyDictionary<string, object?>> GenerateTestData(int rowCount)
    {
        var data = new List<IReadOnlyDictionary<string, object?>>();
        var random = new Random(42); // Seed for reproducibility

        for (int i = 0; i < rowCount; i++)
        {
            var row = new Dictionary<string, object?>
            {
                { "__rowId", $"row_{i}" },
                { "ID", i },
                { "Name", $"Item_{i}" },
                { "Value", random.Next(1000, 9999) },
                { "Status", i % 2 == 0 ? "Active" : "Inactive" },
                { "Date", DateTime.Now.AddDays(-random.Next(0, 365)).ToString("yyyy-MM-dd") }
            };

            data.Add(row);
        }

        return data;
    }
}

/// <summary>
/// Comparison benchmarks: Non-virtualized vs Virtualized.
/// Shows memory and performance improvements.
/// </summary>
[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
public class VirtualizationComparisonBenchmarks
{
    private DataGridViewModel? _nonVirtualizedViewModel;
    private DataGridViewModel? _virtualizedViewModel;
    private ViewportManager? _viewportManager;

    [Params(100, 500, 1000)]
    public int RowCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        var themeManager = new ThemeManager(null);
        var columnNames = new[] { "ID", "Name", "Value", "Status" };
        var options = new AdvancedDataGridOptions();

        // Non-virtualized: All rows in memory
        _nonVirtualizedViewModel = new DataGridViewModel(null, null, themeManager);
        _nonVirtualizedViewModel.InitializeColumns(columnNames, options);
        _nonVirtualizedViewModel.LoadRows(GenerateTestData(RowCount));

        // Virtualized: Use ViewportManager
        _virtualizedViewModel = new DataGridViewModel(null, null, themeManager);
        _virtualizedViewModel.InitializeColumns(columnNames, options);
        _virtualizedViewModel.LoadRows(GenerateTestData(RowCount));

        var logger = Microsoft.Extensions.Logging.Abstractions.NullLogger<ViewportManager>.Instance;
        _viewportManager = new ViewportManager(_virtualizedViewModel, themeManager, logger);
        _viewportManager.TotalRowCount = RowCount;
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _viewportManager?.Dispose();
    }

    [Benchmark(Baseline = true)]
    public void NonVirtualized_AllRowsInMemory()
    {
        // Simulate accessing all rows
        var totalCells = 0;
        foreach (var row in _nonVirtualizedViewModel!.Rows)
        {
            totalCells += row.Cells.Count;
        }

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
    }

    [Benchmark]
    public async Task Virtualized_OnlyViewportInMemory()
    {
        // Load only viewport (50 rows)
        await _viewportManager!.UpdateViewportAsync(0, 49);

        // Access ViewModels
        var totalCells = 0;
        for (int i = 0; i < 50; i++)
        {
            var vm = _viewportManager.GetRowViewModel(i);
            if (vm != null)
            {
                totalCells += vm.Cells.Count;
            }
        }

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
    }

    private List<IReadOnlyDictionary<string, object?>> GenerateTestData(int rowCount)
    {
        var data = new List<IReadOnlyDictionary<string, object?>>();

        for (int i = 0; i < rowCount; i++)
        {
            data.Add(new Dictionary<string, object?>
            {
                { "__rowId", $"row_{i}" },
                { "ID", i },
                { "Name", $"Item_{i}" },
                { "Value", i * 100 },
                { "Status", i % 2 == 0 ? "A" : "B" }
            });
        }

        return data;
    }
}
