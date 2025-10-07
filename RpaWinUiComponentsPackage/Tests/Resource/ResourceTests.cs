using System.Diagnostics;
using RpaWinUiComponentsPackage.Tests.TestInfrastructure;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Api.Models;

namespace RpaWinUiComponentsPackage.Tests.Resource;

/// <summary>
/// Resource tests - CPU, Memory, Disk I/O monitoring
/// </summary>
public class ResourceTests : TestBase
{
    private PerformanceCounter? _cpuCounter;
    private PerformanceCounter? _ramCounter;

    public async Task<List<TestResult>> RunAllTests()
    {
        var results = new List<TestResult>();

        Console.WriteLine("╔════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║              RESOURCE TESTS - CPU/MEMORY/DISK              ║");
        Console.WriteLine("╚════════════════════════════════════════════════════════════╝");
        Console.WriteLine();

        results.Add(await TestCpuUsage());
        results.Add(await TestMemoryUsage());
        results.Add(await TestMemoryLeaks());
        results.Add(await TestGarbageCollection());
        results.Add(await TestThreadPoolUsage());
        results.Add(await TestWorkingSetGrowth());
        results.Add(await TestPeakMemory());

        return results;
    }

    private async Task<TestResult> TestCpuUsage()
    {
        var result = await MeasureAsync("CPU Usage - 100K rows", "Resource", async () =>
        {
            var process = Process.GetCurrentProcess();
            var cpuBefore = process.TotalProcessorTime;

            var facade = CreateFacade();
            SetupColumns(facade);
            await facade.AddRowsBatchAsync(GenerateTestData(100000));

            var cpuAfter = process.TotalProcessorTime;
            var cpuUsed = cpuAfter - cpuBefore;

            // CPU time measured: cpuUsed.TotalMilliseconds
        }, 100000);

        return result;
    }

    private async Task<TestResult> TestMemoryUsage()
    {
        var result = await MeasureAsync("Memory Usage - 100K rows", "Resource", async () =>
        {
            var memBefore = GC.GetTotalMemory(true);

            var facade = CreateFacade();
            SetupColumns(facade);
            await facade.AddRowsBatchAsync(GenerateTestData(100000));

            var memAfter = GC.GetTotalMemory(false);
            var memUsed = (memAfter - memBefore) / 1024.0 / 1024.0;

            // Memory used: memUsed MB
        });

        return result;
    }

    private async Task<TestResult> TestMemoryLeaks()
    {
        var result = await MeasureAsync("Memory Leak Detection", "Resource", async () =>
        {
            var memStart = GC.GetTotalMemory(true);
            var memReadings = new List<long>();

            for (int i = 0; i < 10; i++)
            {
                var facade = CreateFacade();
                SetupColumns(facade);
                await facade.AddRowsBatchAsync(GenerateTestData(1000));
                await facade.ClearAllRowsAsync();

                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();

                memReadings.Add(GC.GetTotalMemory(false));
            }

            var memEnd = GC.GetTotalMemory(true);
            var growth = (memEnd - memStart) / 1024.0 / 1024.0;
            var avgGrowthPerIteration = growth / 10;

            // Memory growth: growth MB, Avg per iteration: avgGrowthPerIteration MB

            if (growth > 50) // If grows more than 50MB
            {
                throw new Exception($"Possible memory leak detected: {growth:F2}MB growth");
            }
        });

        return result;
    }

    private async Task<TestResult> TestGarbageCollection()
    {
        var result = await MeasureAsync("Garbage Collection Impact", "Resource", async () =>
        {
            var gen0Before = GC.CollectionCount(0);
            var gen1Before = GC.CollectionCount(1);
            var gen2Before = GC.CollectionCount(2);

            var facade = CreateFacade();
            SetupColumns(facade);

            for (int i = 0; i < 100; i++)
            {
                await facade.AddRowsBatchAsync(GenerateTestData(1000));
                await facade.ClearAllRowsAsync();
            }

            var gen0After = GC.CollectionCount(0);
            var gen1After = GC.CollectionCount(1);
            var gen2After = GC.CollectionCount(2);

            // GC collections - Gen0: gen0After - gen0Before, Gen1: gen1After - gen1Before, Gen2: gen2After - gen2Before
        });

        return result;
    }

    private async Task<TestResult> TestThreadPoolUsage()
    {
        var result = await MeasureAsync("Thread Pool Usage", "Resource", async () =>
        {
            var process = Process.GetCurrentProcess();
            var threadsBefore = process.Threads.Count;

            var facade = CreateFacade(enableParallel: true);
            SetupColumns(facade);
            await facade.AddRowsBatchAsync(GenerateTestData(50000));

            var threadsAfter = process.Threads.Count;
            var threadsUsed = threadsAfter - threadsBefore;

            // Threads created: threadsUsed
        });

        return result;
    }

    private async Task<TestResult> TestWorkingSetGrowth()
    {
        var result = await MeasureAsync("Working Set Growth", "Resource", async () =>
        {
            var process = Process.GetCurrentProcess();
            process.Refresh();
            var wsBefore = process.WorkingSet64;

            var facade = CreateFacade();
            SetupColumns(facade);
            await facade.AddRowsBatchAsync(GenerateTestData(100000));

            process.Refresh();
            var wsAfter = process.WorkingSet64;
            var growth = (wsAfter - wsBefore) / 1024.0 / 1024.0;

            // Working Set Growth: growth MB
        });

        return result;
    }

    private async Task<TestResult> TestPeakMemory()
    {
        var result = await MeasureAsync("Peak Memory Usage", "Resource", async () =>
        {
            var process = Process.GetCurrentProcess();
            var peakBefore = process.PeakWorkingSet64;

            var facade = CreateFacade();
            SetupColumns(facade);

            // Load in batches to find peak
            for (int i = 0; i < 10; i++)
            {
                await facade.AddRowsBatchAsync(GenerateTestData(10000));
            }

            process.Refresh();
            var peakAfter = process.PeakWorkingSet64;
            var peak = peakAfter / 1024.0 / 1024.0;

            // Peak Memory: peak MB
        });

        return result;
    }
}
