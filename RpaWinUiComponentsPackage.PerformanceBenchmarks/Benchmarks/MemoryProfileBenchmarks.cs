using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnostics.Windows.Configs;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid;
using RpaWinUiComponentsPackage.PerformanceBenchmarks.Helpers;

namespace RpaWinUiComponentsPackage.PerformanceBenchmarks.Benchmarks;

/// <summary>
/// Benchmarks focused on memory profiling
/// </summary>
[MemoryDiagnoser]
[ThreadingDiagnoser]
[EtwProfiler]
public class MemoryProfileBenchmarks
{
    private IAdvancedDataGridFacade _facade = null!;

    [Params(10_000, 100_000, 1_000_000)]
    public int RowCount { get; set; }

    [GlobalSetup]
    public async Task Setup()
    {
        _facade = BenchmarkHelper.CreateFacade(GridFeature.CellEdit, GridFeature.RowColumnOperations);

        _facade.AddColumn(BenchmarkHelper.CreateColumn("ID", typeof(int)));
        _facade.AddColumn(BenchmarkHelper.CreateColumn("Value", typeof(decimal)));

        for (int i = 0; i < RowCount; i++)
        {
            await _facade.AddRowAsync(new Dictionary<string, object?>
            {
                ["ID"] = i,
                ["Value"] = i * 1.5m
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
    public void GetAllRows_MemoryAllocation()
    {
        var rows = _facade.GetCurrentData();
    }

    [Benchmark]
    public async Task UpdateRows_MemoryPressure()
    {
        for (int i = 0; i < Math.Min(1000, RowCount); i++)
        {
            var row = new Dictionary<string, object?>
            {
                ["ID"] = i,
                ["Value"] = i * 2.5m
            };
            await _facade.UpdateRowAsync(i, row);
        }
    }
}
