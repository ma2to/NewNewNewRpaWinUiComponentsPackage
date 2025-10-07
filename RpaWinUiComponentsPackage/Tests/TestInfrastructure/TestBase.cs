using System.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid;
//using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Api.Models;

namespace RpaWinUiComponentsPackage.Tests.TestInfrastructure;

/// <summary>
/// Base class for all tests with common utilities and metrics collection
/// </summary>
public abstract class TestBase
{
    public class TestMetrics
    {
        public TimeSpan Duration { get; set; }
        public long MemoryUsedBytes { get; set; }
        public double MemoryUsedMB => MemoryUsedBytes / 1024.0 / 1024.0;
        public long PeakWorkingSet { get; set; }
        public double PeakWorkingSetMB => PeakWorkingSet / 1024.0 / 1024.0;
        public TimeSpan CpuTime { get; set; }
        public int ThreadCount { get; set; }
        public double ThroughputPerSecond { get; set; }
        public Dictionary<string, object> CustomMetrics { get; set; } = new();
    }

    public class TestResult
    {
        public string TestName { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public Exception? Exception { get; set; }
        public TestMetrics Metrics { get; set; } = new();
        public string Details { get; set; } = string.Empty;
    }

    protected IAdvancedDataGridFacade CreateFacade(
        PublicDataGridOperationMode mode = PublicDataGridOperationMode.Headless,
        int batchSize = 10000,
        bool enableValidation = false,
        bool enableParallel = true)
    {
        var options = new AdvancedDataGridOptions
        {
            BatchSize = batchSize,
            EnableParallelProcessing = enableParallel,
            EnableRealTimeValidation = enableValidation,
            LoggerFactory = NullLoggerFactory.Instance,
            OperationMode = mode
        };

        options.EnabledFeatures.Clear();
        options.EnabledFeatures.Add(GridFeature.RowColumnOperations);
        options.EnabledFeatures.Add(GridFeature.Sort);
        options.EnabledFeatures.Add(GridFeature.Filter);
        options.EnabledFeatures.Add(GridFeature.Search);
        options.EnabledFeatures.Add(GridFeature.Selection);
        options.EnabledFeatures.Add(GridFeature.Validation);
        options.EnabledFeatures.Add(GridFeature.Import);
        options.EnabledFeatures.Add(GridFeature.Export);
        options.EnabledFeatures.Add(GridFeature.CopyPaste);

        return AdvancedDataGridFacadeFactory.CreateStandalone(options, NullLoggerFactory.Instance, null);
    }

    protected async Task<(TestResult result, T returnValue)> MeasureAsync<T>(
        string testName,
        string category,
        Func<Task<T>> testAction,
        int operationCount = 1)
    {
        var result = new TestResult
        {
            TestName = testName,
            Category = category
        };

        var process = Process.GetCurrentProcess();
        var memBefore = GC.GetTotalMemory(true);
        var cpuBefore = process.TotalProcessorTime;
        var threadsBefore = process.Threads.Count;

        var sw = Stopwatch.StartNew();
        T returnValue = default!;

        try
        {
            returnValue = await testAction();
            result.Success = true;
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorMessage = ex.Message;
            result.Exception = ex;
        }
        finally
        {
            sw.Stop();

            var memAfter = GC.GetTotalMemory(false);
            var cpuAfter = process.TotalProcessorTime;

            result.Metrics.Duration = sw.Elapsed;
            result.Metrics.MemoryUsedBytes = memAfter - memBefore;
            result.Metrics.PeakWorkingSet = process.PeakWorkingSet64;
            result.Metrics.CpuTime = cpuAfter - cpuBefore;
            result.Metrics.ThreadCount = process.Threads.Count - threadsBefore;

            if (operationCount > 0)
            {
                result.Metrics.ThroughputPerSecond = operationCount / sw.Elapsed.TotalSeconds;
            }
        }

        return (result, returnValue);
    }

    protected async Task<TestResult> MeasureAsync(
        string testName,
        string category,
        Func<Task> testAction,
        int operationCount = 1)
    {
        var (result, _) = await MeasureAsync(testName, category, async () =>
        {
            await testAction();
            return 0;
        }, operationCount);

        return result;
    }

    protected List<Dictionary<string, object?>> GenerateTestData(int rowCount, int columnCount = 5, int seed = 42)
    {
        var random = new Random(seed);
        var data = new List<Dictionary<string, object?>>(rowCount);

        for (int i = 0; i < rowCount; i++)
        {
            var row = new Dictionary<string, object?>();
            row["ID"] = i;
            row["Name"] = $"Row_{i}";
            row["Value"] = random.NextDouble() * 1000;
            row["Status"] = i % 2 == 0 ? "Active" : "Inactive";
            row["Category"] = $"Cat_{i % 10}";

            for (int c = 5; c < columnCount; c++)
            {
                row[$"Col{c}"] = random.Next(0, 100);
            }

            data.Add(row);
        }

        return data;
    }

    protected void SetupColumns(IAdvancedDataGridFacade facade, int columnCount = 5)
    {
        facade.AddColumn(new PublicColumnDefinition
        {
            Name = "ID",
            Header = "ID",
            DataType = typeof(int),
            IsSortable = true,
            IsFilterable = true,
            IsVisible = true
        });

        facade.AddColumn(new PublicColumnDefinition
        {
            Name = "Name",
            Header = "Name",
            DataType = typeof(string),
            IsSortable = true,
            IsFilterable = true,
            IsVisible = true
        });

        facade.AddColumn(new PublicColumnDefinition
        {
            Name = "Value",
            Header = "Value",
            DataType = typeof(double),
            IsSortable = true,
            IsFilterable = true,
            IsVisible = true
        });

        facade.AddColumn(new PublicColumnDefinition
        {
            Name = "Status",
            Header = "Status",
            DataType = typeof(string),
            IsSortable = true,
            IsFilterable = true,
            IsVisible = true
        });

        facade.AddColumn(new PublicColumnDefinition
        {
            Name = "Category",
            Header = "Category",
            DataType = typeof(string),
            IsSortable = true,
            IsFilterable = true,
            IsVisible = true
        });

        for (int i = 5; i < columnCount; i++)
        {
            facade.AddColumn(new PublicColumnDefinition
            {
                Name = $"Col{i}",
                Header = $"Column {i}",
                DataType = typeof(int),
                IsSortable = true,
                IsFilterable = true,
                IsVisible = true
            });
        }
    }
}
