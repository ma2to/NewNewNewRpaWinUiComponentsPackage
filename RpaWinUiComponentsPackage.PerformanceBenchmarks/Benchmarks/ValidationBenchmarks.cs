using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnostics.Windows.Configs;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Api.Models;
using RpaWinUiComponentsPackage.PerformanceBenchmarks.Helpers;

namespace RpaWinUiComponentsPackage.PerformanceBenchmarks.Benchmarks;

/// <summary>
/// Benchmarks for Validation operations
/// Tests validation performance with different row counts
/// </summary>
[MemoryDiagnoser]
[ThreadingDiagnoser]
public class ValidationBenchmarks
{
    private IAdvancedDataGridFacade _facade = null!;

    [Params(1000, 10_000, 100_000)]
    public int RowCount { get; set; }

    [GlobalSetup]
    public async Task Setup()
    {
        _facade = BenchmarkHelper.CreateFacade(GridFeature.Validation, GridFeature.RowColumnOperations);

        // Add columns
        _facade.AddColumn(BenchmarkHelper.CreateColumn("ID", typeof(string)));
        _facade.AddColumn(BenchmarkHelper.CreateColumn("Email", typeof(string)));
        _facade.AddColumn(BenchmarkHelper.CreateColumn("Age", typeof(int)));

        // Add test data
        for (int i = 0; i < RowCount; i++)
        {
            var row = new Dictionary<string, object?>
            {
                ["ID"] = $"ID{i}",
                ["Email"] = $"user{i}@example.com",
                ["Age"] = 20 + (i % 50)
            };
            await _facade.AddRowAsync(row);
        }
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        if (_facade is IDisposable disposable)
            disposable.Dispose();
    }

    [Benchmark]
    public async Task ValidateAll()
    {
        await _facade.ValidateAllAsync();
    }

    [Benchmark]
    public async Task ValidateWithStatistics()
    {
        await _facade.ValidateAllWithStatisticsAsync();
    }
}
