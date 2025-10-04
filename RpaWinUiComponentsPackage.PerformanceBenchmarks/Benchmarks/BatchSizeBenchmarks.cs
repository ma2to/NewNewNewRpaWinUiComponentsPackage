using BenchmarkDotNet.Attributes;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Api.Models;
using RpaWinUiComponentsPackage.PerformanceBenchmarks.Helpers;

namespace RpaWinUiComponentsPackage.PerformanceBenchmarks.Benchmarks;

/// <summary>
/// Benchmarks to compare different batch sizes
/// </summary>
[MemoryDiagnoser]
[ThreadingDiagnoser]
public class BatchSizeBenchmarks
{
    private const int TotalRows = 100_000;

    [Params(1000, 5000, 10000)]
    public int BatchSize { get; set; }

    [Benchmark]
    public async Task SortWithBatchSize()
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
}
