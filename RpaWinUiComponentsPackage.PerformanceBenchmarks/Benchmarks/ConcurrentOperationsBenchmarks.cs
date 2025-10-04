using BenchmarkDotNet.Attributes;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Api.Models;
using RpaWinUiComponentsPackage.PerformanceBenchmarks.Helpers;

namespace RpaWinUiComponentsPackage.PerformanceBenchmarks.Benchmarks;

/// <summary>
/// Benchmarks for concurrent operations
/// Tests thread-safety and parallel processing performance
/// </summary>
[MemoryDiagnoser]
[ThreadingDiagnoser]
public class ConcurrentOperationsBenchmarks
{
    private IAdvancedDataGridFacade _facade = null!;

    [Params(10_000, 50_000)]
    public int RowCount { get; set; }

    [Params(2, 4, 8)]
    public int ConcurrencyLevel { get; set; }

    [GlobalSetup]
    public async Task Setup()
    {
        _facade = BenchmarkHelper.CreateFacade(
            GridFeature.RowColumnOperations,
            GridFeature.Sort,
            GridFeature.Filter,
            GridFeature.Search
        );

        _facade.AddColumn(BenchmarkHelper.CreateColumn("ID", typeof(int)));
        _facade.AddColumn(BenchmarkHelper.CreateColumn("Name", typeof(string)));
        _facade.AddColumn(BenchmarkHelper.CreateColumn("Value", typeof(decimal)));

        var random = new Random(42);
        for (int i = 0; i < RowCount; i++)
        {
            await _facade.AddRowAsync(new Dictionary<string, object?>
            {
                ["ID"] = random.Next(0, RowCount * 2),
                ["Name"] = $"Item{i}",
                ["Value"] = random.Next(1000, 100000)
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
    public async Task ConcurrentReads()
    {
        var tasks = new Task[ConcurrencyLevel];

        for (int i = 0; i < ConcurrencyLevel; i++)
        {
            tasks[i] = Task.Run(() =>
            {
                var data = _facade.GetCurrentData();
                return Task.CompletedTask;
            });
        }

        await Task.WhenAll(tasks);
    }

    [Benchmark]
    public async Task ConcurrentRowReads()
    {
        var tasks = new Task[ConcurrencyLevel];

        for (int i = 0; i < ConcurrencyLevel; i++)
        {
            int threadId = i;
            tasks[i] = Task.Run(() =>
            {
                // Each thread reads different rows
                int startRow = threadId * (RowCount / ConcurrencyLevel);
                int endRow = (threadId + 1) * (RowCount / ConcurrencyLevel);

                for (int row = startRow; row < endRow && row < RowCount; row++)
                {
                    var rowData = _facade.GetRow(row);
                }
                return Task.CompletedTask;
            });
        }

        await Task.WhenAll(tasks);
    }

    [Benchmark]
    public async Task ConcurrentSorts()
    {
        var tasks = new Task[ConcurrencyLevel];

        for (int i = 0; i < ConcurrencyLevel; i++)
        {
            int threadId = i;
            tasks[i] = Task.Run(async () =>
            {
                // Alternate between different columns and directions
                var column = threadId % 2 == 0 ? "ID" : "Name";
                var direction = threadId % 2 == 0 ? PublicSortDirection.Ascending : PublicSortDirection.Descending;

                await _facade.SortByColumnAsync(column, direction);
            });
        }

        await Task.WhenAll(tasks);
    }

    [Benchmark]
    public async Task ConcurrentFilters()
    {
        var tasks = new Task[ConcurrencyLevel];

        for (int i = 0; i < ConcurrencyLevel; i++)
        {
            int threadId = i;
            tasks[i] = Task.Run(async () =>
            {
                // Each thread applies different filter
                var threshold = 50000 + (threadId * 10000);
                await _facade.ApplyFilterAsync("Value", PublicFilterOperator.GreaterThan, threshold);
            });
        }

        await Task.WhenAll(tasks);
    }

    [Benchmark]
    public async Task ConcurrentSearches()
    {
        var tasks = new Task[ConcurrencyLevel];

        for (int i = 0; i < ConcurrencyLevel; i++)
        {
            int threadId = i;
            tasks[i] = Task.Run(async () =>
            {
                var searchText = $"Item{threadId * 100}";
                var command = new SearchDataCommand { SearchText = searchText };
                await _facade.SearchAsync(command);
            });
        }

        await Task.WhenAll(tasks);
    }

    [Benchmark]
    public async Task MixedConcurrentOperations()
    {
        var tasks = new Task[ConcurrencyLevel];

        for (int i = 0; i < ConcurrencyLevel; i++)
        {
            int threadId = i;
            tasks[i] = Task.Run(async () =>
            {
                // Each thread does a different operation
                switch (threadId % 4)
                {
                    case 0: // Read
                        var data = _facade.GetCurrentData();
                        break;
                    case 1: // Sort
                        await _facade.SortByColumnAsync("ID", PublicSortDirection.Ascending);
                        break;
                    case 2: // Filter
                        await _facade.ApplyFilterAsync("Value", PublicFilterOperator.GreaterThan, 50000);
                        break;
                    case 3: // Search
                        var command = new SearchDataCommand { SearchText = "Item100" };
                        await _facade.SearchAsync(command);
                        break;
                }
            });
        }

        await Task.WhenAll(tasks);
    }
}
