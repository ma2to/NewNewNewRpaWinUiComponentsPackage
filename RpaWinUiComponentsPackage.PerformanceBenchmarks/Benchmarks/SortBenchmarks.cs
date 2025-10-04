using BenchmarkDotNet.Attributes;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Api.Models;
using RpaWinUiComponentsPackage.PerformanceBenchmarks.Helpers;

namespace RpaWinUiComponentsPackage.PerformanceBenchmarks.Benchmarks;

/// <summary>
/// Benchmarks for Sort operations
/// Tests sorting performance with different data types
/// </summary>
[MemoryDiagnoser]
[ThreadingDiagnoser]
public class SortBenchmarks
{
    private IAdvancedDataGridFacade _facade = null!;

    [Params(1000, 10_000, 100_000)]
    public int RowCount { get; set; }

    [GlobalSetup]
    public async Task Setup()
    {
        _facade = BenchmarkHelper.CreateFacade(GridFeature.Sort, GridFeature.RowColumnOperations);

        // Add columns
        _facade.AddColumn(BenchmarkHelper.CreateColumn("ID", typeof(int)));
        _facade.AddColumn(BenchmarkHelper.CreateColumn("Name", typeof(string)));
        _facade.AddColumn(BenchmarkHelper.CreateColumn("Salary", typeof(decimal)));

        // Add unsorted data
        var random = new Random(42);
        for (int i = 0; i < RowCount; i++)
        {
            await _facade.AddRowAsync(new Dictionary<string, object?>
            {
                ["ID"] = random.Next(0, RowCount * 2),
                ["Name"] = $"User{random.Next(0, RowCount)}",
                ["Salary"] = random.Next(20000, 150000)
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
    public async Task SortByInteger()
    {
        await _facade.SortByColumnAsync("ID", PublicSortDirection.Ascending);
    }

    [Benchmark]
    public async Task SortByString()
    {
        await _facade.SortByColumnAsync("Name", PublicSortDirection.Ascending);
    }

    [Benchmark]
    public async Task SortByDecimal()
    {
        await _facade.SortByColumnAsync("Salary", PublicSortDirection.Descending);
    }
}
