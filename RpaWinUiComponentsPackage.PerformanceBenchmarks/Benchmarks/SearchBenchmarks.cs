using BenchmarkDotNet.Attributes;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Api.Models;
using RpaWinUiComponentsPackage.PerformanceBenchmarks.Helpers;

namespace RpaWinUiComponentsPackage.PerformanceBenchmarks.Benchmarks;

/// <summary>
/// Benchmarks for Search operations
/// </summary>
[MemoryDiagnoser]
[ThreadingDiagnoser]
public class SearchBenchmarks
{
    private IAdvancedDataGridFacade _facade = null!;

    [Params(1000, 10_000, 100_000)]
    public int RowCount { get; set; }

    [GlobalSetup]
    public async Task Setup()
    {
        _facade = BenchmarkHelper.CreateFacade(GridFeature.Search, GridFeature.RowColumnOperations);

        _facade.AddColumn(BenchmarkHelper.CreateColumn("Name", typeof(string)));
        _facade.AddColumn(BenchmarkHelper.CreateColumn("Email", typeof(string)));

        for (int i = 0; i < RowCount; i++)
        {
            await _facade.AddRowAsync(new Dictionary<string, object?>
            {
                ["Name"] = $"User{i}",
                ["Email"] = $"user{i}@example.com"
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
    public async Task BasicSearch()
    {
        var command = new SearchDataCommand { SearchText = "User500" };
        await _facade.SearchAsync(command);
    }
}
