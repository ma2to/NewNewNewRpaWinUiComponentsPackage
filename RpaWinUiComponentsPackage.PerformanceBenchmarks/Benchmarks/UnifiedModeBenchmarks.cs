using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Toolchains.InProcess.Emit;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Api.Models;
using RpaWinUiComponentsPackage.PerformanceBenchmarks.Helpers;

namespace RpaWinUiComponentsPackage.PerformanceBenchmarks.Benchmarks;

/// <summary>
/// Unified benchmarks comparing all three operation modes side-by-side
/// Tests the performance characteristics of Headless, Readonly, and Interactive modes
/// Optimized for testing 100K to 10M+ rows
/// Using InProcess toolchain to avoid WinUI MSIX/PRI packaging issues
/// </summary>
[MemoryDiagnoser]
[ThreadingDiagnoser]
[InProcessAttribute]
public class UnifiedModeBenchmarks
{
    /// <summary>
    /// Benchmark operation modes
    /// </summary>
    public enum BenchmarkMode
    {
        /// <summary>Pure backend mode - no UI dispatcher, fastest performance</summary>
        Headless,

        /// <summary>Headless+UI mode - UI dispatcher present but no auto-refresh</summary>
        Readonly,

        /// <summary>Full UI mode - automatic UI notifications and refresh</summary>
        Interactive
    }

    /// <summary>
    /// The operation mode to test
    /// NOTE: Interactive mode disabled - requires STA thread and UI message pump
    /// </summary>
    [Params(BenchmarkMode.Headless, BenchmarkMode.Readonly)]
    public BenchmarkMode Mode { get; set; }

    /// <summary>
    /// Number of rows to test with
    /// </summary>
    [Params(100_000, 1_000_000)]
    public int RowCount { get; set; }

    /// <summary>
    /// Batch size for operations
    /// </summary>
    [Params(5000, 10000)]
    public int BatchSize { get; set; }

    /// <summary>
    /// Creates a facade configured for the current benchmark mode
    /// </summary>
    /// <param name="features">Grid features to enable</param>
    /// <returns>Configured facade instance</returns>
    private IAdvancedDataGridFacade CreateFacadeForMode(params GridFeature[] features)
    {
        return Mode switch
        {
            BenchmarkMode.Headless => UIBenchmarkHelper.CreateHeadless(BatchSize, features),
            BenchmarkMode.Readonly => UIBenchmarkHelper.CreateReadonly(BatchSize, features),
            BenchmarkMode.Interactive => UIBenchmarkHelper.CreateWithUI(BatchSize, features),
            _ => throw new InvalidOperationException($"Unknown mode: {Mode}")
        };
    }

    /// <summary>
    /// Benchmark: Sort operation across all modes
    /// Tests sorting performance with randomized integer data
    /// </summary>
    [Benchmark]
    public async Task Sort_UnifiedMode()
    {
        var facade = CreateFacadeForMode(GridFeature.Sort, GridFeature.RowColumnOperations);

        try
        {
            facade.AddColumn(UIBenchmarkHelper.CreateColumn("ID", typeof(int)));

            var random = new Random(42);
            for (int i = 0; i < RowCount; i++)
            {
                await facade.AddRowAsync(new Dictionary<string, object?> { ["ID"] = random.Next(0, RowCount * 2) });
            }

            await facade.SortByColumnAsync("ID", PublicSortDirection.Ascending);
        }
        finally
        {
            if (facade is IDisposable disposable) disposable.Dispose();
        }
    }

    /// <summary>
    /// Benchmark: Filter operation across all modes
    /// Tests filtering performance with numeric comparisons
    /// </summary>
    [Benchmark]
    public async Task Filter_UnifiedMode()
    {
        var facade = CreateFacadeForMode(GridFeature.Filter, GridFeature.RowColumnOperations);

        try
        {
            facade.AddColumn(UIBenchmarkHelper.CreateColumn("Value", typeof(int)));

            var random = new Random(42);
            for (int i = 0; i < RowCount; i++)
            {
                await facade.AddRowAsync(new Dictionary<string, object?> { ["Value"] = random.Next(0, 1000) });
            }

            await facade.ApplyFilterAsync("Value", PublicFilterOperator.GreaterThan, 500);
        }
        finally
        {
            if (facade is IDisposable disposable) disposable.Dispose();
        }
    }

    /// <summary>
    /// Benchmark: Validation operation across all modes
    /// Tests validation performance with regex-based email validation
    /// </summary>
    [Benchmark]
    public async Task Validation_UnifiedMode()
    {
        var facade = CreateFacadeForMode(GridFeature.Validation, GridFeature.RowColumnOperations);

        try
        {
            var column = UIBenchmarkHelper.CreateColumn("Email", typeof(string));
            column.ValidationRules = new List<PublicValidationRule>
            {
                new PublicValidationRule
                {
                    RuleType = PublicValidationRuleType.Regex,
                    ErrorMessage = "Invalid email",
                    RegexPattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$"
                }
            };
            facade.AddColumn(column);

            var random = new Random(42);
            for (int i = 0; i < RowCount; i++)
            {
                var isValid = random.Next(0, 2) == 0;
                var email = isValid ? $"user{i}@example.com" : $"invalid-{i}";
                await facade.AddRowAsync(new Dictionary<string, object?> { ["Email"] = email });
            }

            await facade.ValidateAllAsync();
        }
        finally
        {
            if (facade is IDisposable disposable) disposable.Dispose();
        }
    }

    /// <summary>
    /// Benchmark: Bulk insert operation across all modes
    /// Tests raw insertion performance
    /// </summary>
    [Benchmark]
    public async Task BulkInsert_UnifiedMode()
    {
        var facade = CreateFacadeForMode(GridFeature.RowColumnOperations);

        try
        {
            facade.AddColumn(UIBenchmarkHelper.CreateColumn("ID", typeof(int)));
            facade.AddColumn(UIBenchmarkHelper.CreateColumn("Name", typeof(string)));
            facade.AddColumn(UIBenchmarkHelper.CreateColumn("Value", typeof(double)));

            for (int i = 0; i < RowCount; i++)
            {
                await facade.AddRowAsync(new Dictionary<string, object?>
                {
                    ["ID"] = i,
                    ["Name"] = $"Item_{i}",
                    ["Value"] = i * 1.5
                });
            }
        }
        finally
        {
            if (facade is IDisposable disposable) disposable.Dispose();
        }
    }

    /// <summary>
    /// Benchmark: GetAllRows operation across all modes
    /// Tests data retrieval performance
    /// </summary>
    [Benchmark]
    public async Task GetAllRows_UnifiedMode()
    {
        var facade = CreateFacadeForMode(GridFeature.RowColumnOperations);

        try
        {
            facade.AddColumn(UIBenchmarkHelper.CreateColumn("ID", typeof(int)));

            // Insert data
            for (int i = 0; i < RowCount; i++)
            {
                await facade.AddRowAsync(new Dictionary<string, object?> { ["ID"] = i });
            }

            // Retrieve all rows
            var data = facade.GetCurrentData();

            if (data == null || data.Count == 0)
            {
                throw new InvalidOperationException("Failed to retrieve rows");
            }
        }
        finally
        {
            if (facade is IDisposable disposable) disposable.Dispose();
        }
    }

    /// <summary>
    /// Benchmark: Combined operations (Insert + Sort + Filter)
    /// Tests real-world scenario with multiple operations
    /// </summary>
    [Benchmark]
    public async Task CombinedOperations_UnifiedMode()
    {
        var facade = CreateFacadeForMode(
            GridFeature.Sort,
            GridFeature.Filter,
            GridFeature.RowColumnOperations);

        try
        {
            facade.AddColumn(UIBenchmarkHelper.CreateColumn("ID", typeof(int)));
            facade.AddColumn(UIBenchmarkHelper.CreateColumn("Category", typeof(string)));
            facade.AddColumn(UIBenchmarkHelper.CreateColumn("Value", typeof(int)));

            var random = new Random(42);
            var categories = new[] { "A", "B", "C", "D", "E" };

            // Insert data
            for (int i = 0; i < RowCount; i++)
            {
                await facade.AddRowAsync(new Dictionary<string, object?>
                {
                    ["ID"] = i,
                    ["Category"] = categories[i % categories.Length],
                    ["Value"] = random.Next(0, 1000)
                });
            }

            // Sort by ID
            await facade.SortByColumnAsync("ID", PublicSortDirection.Descending);

            // Filter by Category
            await facade.ApplyFilterAsync("Category", PublicFilterOperator.Equals, "A");

            // Get filtered count
            var data = facade.GetCurrentData();

            if (data == null)
            {
                throw new InvalidOperationException("Combined operations failed");
            }
        }
        finally
        {
            if (facade is IDisposable disposable) disposable.Dispose();
        }
    }

    /// <summary>
    /// Benchmark: Update operations across all modes
    /// Tests update performance with cell value modifications
    /// </summary>
    [Benchmark]
    public async Task UpdateCells_UnifiedMode()
    {
        var facade = CreateFacadeForMode(GridFeature.RowColumnOperations);

        try
        {
            facade.AddColumn(UIBenchmarkHelper.CreateColumn("ID", typeof(int)));
            facade.AddColumn(UIBenchmarkHelper.CreateColumn("Value", typeof(string)));

            // Insert data
            for (int i = 0; i < RowCount; i++)
            {
                await facade.AddRowAsync(new Dictionary<string, object?>
                {
                    ["ID"] = i,
                    ["Value"] = $"Original_{i}"
                });
            }

            // Update every 100th row (1% of data)
            var updateCount = RowCount / 100;
            for (int i = 0; i < updateCount; i++)
            {
                var rowId = i * 100;
                await facade.UpdateCellAsync(rowId, "Value", $"Updated_{rowId}");
            }
        }
        finally
        {
            if (facade is IDisposable disposable) disposable.Dispose();
        }
    }

    /// <summary>
    /// Benchmark: Delete operations across all modes
    /// Tests delete performance
    /// </summary>
    [Benchmark]
    public async Task DeleteRows_UnifiedMode()
    {
        var facade = CreateFacadeForMode(GridFeature.RowColumnOperations);

        try
        {
            facade.AddColumn(UIBenchmarkHelper.CreateColumn("ID", typeof(int)));

            // Insert data
            for (int i = 0; i < RowCount; i++)
            {
                await facade.AddRowAsync(new Dictionary<string, object?> { ["ID"] = i });
            }

            // Delete every 10th row (10% of data)
            var deleteCount = RowCount / 10;
            for (int i = 0; i < deleteCount; i++)
            {
                var rowId = i * 10;
                await facade.RemoveRowAsync(rowId);
            }
        }
        finally
        {
            if (facade is IDisposable disposable) disposable.Dispose();
        }
    }
}
