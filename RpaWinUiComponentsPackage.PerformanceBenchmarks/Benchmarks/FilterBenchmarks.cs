using BenchmarkDotNet.Attributes;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Api.Models;
using RpaWinUiComponentsPackage.PerformanceBenchmarks.Helpers;

namespace RpaWinUiComponentsPackage.PerformanceBenchmarks.Benchmarks;

/// <summary>
/// Benchmarks for Filter operations
/// </summary>
[MemoryDiagnoser]
[ThreadingDiagnoser]
public class FilterBenchmarks
{
    private IAdvancedDataGridFacade _facade = null!;

    [Params(1000, 10_000, 100_000)]
    public int RowCount { get; set; }

    [GlobalSetup]
    public async Task Setup()
    {
        _facade = BenchmarkHelper.CreateFacade(GridFeature.Filter, GridFeature.RowColumnOperations);

        _facade.AddColumn(BenchmarkHelper.CreateColumn("Age", typeof(int)));
        _facade.AddColumn(BenchmarkHelper.CreateColumn("Department", typeof(string)));

        var random = new Random(42);
        var departments = new[] { "IT", "HR", "Sales", "Marketing" };

        for (int i = 0; i < RowCount; i++)
        {
            await _facade.AddRowAsync(new Dictionary<string, object?>
            {
                ["Age"] = random.Next(18, 70),
                ["Department"] = departments[random.Next(departments.Length)]
            });
        }
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        if (_facade is IDisposable disposable)
            disposable.Dispose();
    }

    [Benchmark]
    public async Task FilterByEquals()
    {
        await _facade.ApplyFilterAsync("Department", PublicFilterOperator.Equals, "IT");
    }

    [Benchmark]
    public async Task FilterByGreaterThan()
    {
        await _facade.ApplyFilterAsync("Age", PublicFilterOperator.GreaterThan, 30);
    }
}
