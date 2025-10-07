using BenchmarkDotNet.Attributes;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Api.Models;
using RpaWinUiComponentsPackage.PerformanceBenchmarks.Helpers;

namespace RpaWinUiComponentsPackage.PerformanceBenchmarks.Benchmarks;

/// <summary>
/// Benchmarks to compare different batch sizes with and without UI
/// </summary>
[MemoryDiagnoser]
[ThreadingDiagnoser]
public class BatchSizeBenchmarks
{
    private const int TotalRows = 100_000;

    [Params(1000, 5000, 10000)]
    public int BatchSize { get; set; }

    [Benchmark(Baseline = true)]
    public async Task Sort_Headless_BatchSize()
    {
        var facade = BenchmarkHelper.CreateFacadeWithBatchSize(BatchSize, GridFeature.Sort, GridFeature.RowColumnOperations);

        facade.AddColumn(BenchmarkHelper.CreateColumn("ID", typeof(int)));

        var random = new Random(42);
        for (int i = 0; i < TotalRows; i++)
        {
            await facade.AddRowAsync(new Dictionary<string, object?> { ["ID"] = random.Next(0, TotalRows * 2) });
        }

        await facade.SortByColumnAsync("ID", PublicSortDirection.Ascending);

        if (facade is IDisposable disposable) disposable.Dispose();
    }

    [Benchmark]
    public async Task Sort_WithUI_BatchSize()
    {
        if (!UIBenchmarkHelper.IsUIAvailable())
        {
            // Skip UI benchmarks if dispatcher not available
            Console.WriteLine("[SKIP] UI not available for Sort_WithUI_BatchSize");
            return;
        }

        var facade = UIBenchmarkHelper.CreateWithUI(BatchSize, GridFeature.Sort, GridFeature.RowColumnOperations);

        facade.AddColumn(UIBenchmarkHelper.CreateColumn("ID", typeof(int)));

        var random = new Random(42);
        for (int i = 0; i < TotalRows; i++)
        {
            await facade.AddRowAsync(new Dictionary<string, object?> { ["ID"] = random.Next(0, TotalRows * 2) });
        }

        await facade.SortByColumnAsync("ID", PublicSortDirection.Ascending);

        if (facade is IDisposable disposable) disposable.Dispose();
    }

    [Benchmark]
    public async Task Filter_Headless_BatchSize()
    {
        var facade = BenchmarkHelper.CreateFacadeWithBatchSize(BatchSize, GridFeature.Filter, GridFeature.RowColumnOperations);

        facade.AddColumn(BenchmarkHelper.CreateColumn("Value", typeof(int)));

        var random = new Random(42);
        for (int i = 0; i < TotalRows; i++)
        {
            await facade.AddRowAsync(new Dictionary<string, object?> { ["Value"] = random.Next(0, 1000) });
        }

        await facade.ApplyFilterAsync("Value", PublicFilterOperator.GreaterThan, 500);

        if (facade is IDisposable disposable) disposable.Dispose();
    }

    [Benchmark]
    public async Task Filter_WithUI_BatchSize()
    {
        if (!UIBenchmarkHelper.IsUIAvailable())
        {
            Console.WriteLine("[SKIP] UI not available for Filter_WithUI_BatchSize");
            return;
        }

        var facade = UIBenchmarkHelper.CreateWithUI(BatchSize, GridFeature.Filter, GridFeature.RowColumnOperations);

        facade.AddColumn(UIBenchmarkHelper.CreateColumn("Value", typeof(int)));

        var random = new Random(42);
        for (int i = 0; i < TotalRows; i++)
        {
            await facade.AddRowAsync(new Dictionary<string, object?> { ["Value"] = random.Next(0, 1000) });
        }

        await facade.ApplyFilterAsync("Value", PublicFilterOperator.GreaterThan, 500);

        if (facade is IDisposable disposable) disposable.Dispose();
    }

    [Benchmark]
    public async Task Validation_Headless_BatchSize()
    {
        var facade = BenchmarkHelper.CreateFacadeWithBatchSize(BatchSize, GridFeature.Validation, GridFeature.RowColumnOperations);

        var column = BenchmarkHelper.CreateColumn("Email", typeof(string));
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
        for (int i = 0; i < TotalRows; i++)
        {
            var isValid = random.Next(0, 2) == 0;
            var email = isValid ? $"user{i}@example.com" : $"invalid-{i}";
            await facade.AddRowAsync(new Dictionary<string, object?> { ["Email"] = email });
        }

        await facade.ValidateAllAsync();

        if (facade is IDisposable disposable) disposable.Dispose();
    }

    [Benchmark]
    public async Task Validation_WithUI_BatchSize()
    {
        if (!UIBenchmarkHelper.IsUIAvailable())
        {
            Console.WriteLine("[SKIP] UI not available for Validation_WithUI_BatchSize");
            return;
        }

        var facade = UIBenchmarkHelper.CreateWithUI(BatchSize, GridFeature.Validation, GridFeature.RowColumnOperations);

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
        for (int i = 0; i < TotalRows; i++)
        {
            var isValid = random.Next(0, 2) == 0;
            var email = isValid ? $"user{i}@example.com" : $"invalid-{i}";
            await facade.AddRowAsync(new Dictionary<string, object?> { ["Email"] = email });
        }

        await facade.ValidateAllAsync();

        if (facade is IDisposable disposable) disposable.Dispose();
    }
}
