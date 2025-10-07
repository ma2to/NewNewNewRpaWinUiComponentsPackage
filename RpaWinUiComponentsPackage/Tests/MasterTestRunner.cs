using System.Diagnostics;
using System.Text;
using RpaWinUiComponentsPackage.Tests.Comprehensive;
using RpaWinUiComponentsPackage.Tests.Resource;
using RpaWinUiComponentsPackage.Tests.Stress;
using RpaWinUiComponentsPackage.Tests.Unit;
using RpaWinUiComponentsPackage.Tests.Performance;
using RpaWinUiComponentsPackage.Tests.Functional;
using RpaWinUiComponentsPackage.Tests.Stability;
using RpaWinUiComponentsPackage.Tests.Load;

namespace RpaWinUiComponentsPackage.Tests;

/// <summary>
/// Master Test Runner - Executes ALL test suites and generates comprehensive report
/// </summary>
public class MasterTestRunner
{
    public static async Task Main(string[] args)
    {
        try
        {
            Console.OutputEncoding = Encoding.UTF8;
            var globalSw = Stopwatch.StartNew();

            Console.Clear();
            PrintHeader();

            var allResults = new List<dynamic>();
            var process = Process.GetCurrentProcess();
            var memStart = GC.GetTotalMemory(true);

            // 1. COMPREHENSIVE API TESTS
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("\n█ PHASE 1: COMPREHENSIVE API TESTS");
            Console.ResetColor();
            var apiTests = new ComprehensiveApiTests();
            var apiResults = await apiTests.RunAllTests();
            allResults.AddRange(apiResults);

        // 2. UNIT TESTS
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("\n█ PHASE 2: UNIT TESTS");
        Console.ResetColor();
        var unitTests = new UnitTests();
        var unitResults = await unitTests.RunAllTests();
        allResults.AddRange(unitResults);

        // 3. PERFORMANCE TESTS
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("\n█ PHASE 3: PERFORMANCE TESTS");
        Console.ResetColor();
        var perfTests = new PerformanceTests();
        var perfResults = await perfTests.RunAllTests();
        allResults.AddRange(perfResults);

        // 4. FUNCTIONAL TESTS
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("\n█ PHASE 4: FUNCTIONAL TESTS");
        Console.ResetColor();
        var funcTests = new FunctionalTests();
        var funcResults = await funcTests.RunAllTests();
        allResults.AddRange(funcResults);

        // 5. STABILITY TESTS
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("\n█ PHASE 5: STABILITY TESTS");
        Console.ResetColor();
        var stabilityTests = new StabilityTests();
        var stabilityResults = await stabilityTests.RunAllTests();
        allResults.AddRange(stabilityResults);

        // 6. LOAD TESTS
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("\n█ PHASE 6: LOAD TESTS");
        Console.ResetColor();
        var loadTests = new LoadTests();
        var loadResults = await loadTests.RunAllTests();
        allResults.AddRange(loadResults);

        // 7. RESOURCE TESTS (CPU/Memory)
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("\n█ PHASE 7: RESOURCE TESTS (CPU/Memory/Disk)");
        Console.ResetColor();
        var resourceTests = new ResourceTests();
        var resourceResults = await resourceTests.RunAllTests();
        allResults.AddRange(resourceResults);

        // 8. STRESS TESTS
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("\n█ PHASE 8: STRESS TESTS");
        Console.ResetColor();
        var stressTests = new StressTests();
        var stressResults = await stressTests.RunAllTests();
        allResults.AddRange(stressResults);

            globalSw.Stop();
            var memEnd = GC.GetTotalMemory(false);

            // GENERATE FINAL REPORT
            GenerateFinalReport(allResults, globalSw.Elapsed, memStart, memEnd, process);

            // SAVE TO FILE
            await SaveReportToFile(allResults, globalSw.Elapsed);

            Console.WriteLine("\n\nPress any key to exit...");
            Console.ReadKey();
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("\n\n╔══════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║                    FATAL ERROR                               ║");
            Console.WriteLine("╚══════════════════════════════════════════════════════════════╝");
            Console.WriteLine($"\nException Type: {ex.GetType().Name}");
            Console.WriteLine($"Message: {ex.Message}");
            Console.WriteLine($"\nStack Trace:\n{ex.StackTrace}");

            if (ex.InnerException != null)
            {
                Console.WriteLine($"\nInner Exception: {ex.InnerException.GetType().Name}");
                Console.WriteLine($"Inner Message: {ex.InnerException.Message}");
                Console.WriteLine($"Inner Stack Trace:\n{ex.InnerException.StackTrace}");
            }

            Console.ResetColor();
            Console.WriteLine("\n\nPress any key to exit...");
            Console.ReadKey();
            Environment.Exit(1);
        }
    }

    private static void PrintHeader()
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("╔══════════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║                                                                  ║");
        Console.WriteLine("║        ADVANCED DATAGRID - COMPREHENSIVE TEST SUITE              ║");
        Console.WriteLine("║                                                                  ║");
        Console.WriteLine("║  ✓ ALL Public API Methods                                        ║");
        Console.WriteLine("║  ✓ Performance Tests (Speed & Throughput)                        ║");
        Console.WriteLine("║  ✓ Resource Tests (CPU, Memory, Disk I/O)                        ║");
        Console.WriteLine("║  ✓ Stability Tests (Long-running, Leaks)                         ║");
        Console.WriteLine("║  ✓ Load Tests (Large datasets)                                   ║");
        Console.WriteLine("║  ✓ Stress Tests (Extreme scenarios)                              ║");
        Console.WriteLine("║  ✓ Unit Tests (Individual components)                            ║");
        Console.WriteLine("║                                                                  ║");
        Console.WriteLine("╚══════════════════════════════════════════════════════════════════╝");
        Console.ResetColor();
    }

    private static void GenerateFinalReport(List<dynamic> allResults, TimeSpan totalDuration, long memStart, long memEnd, Process process)
    {
        Console.WriteLine("\n\n");
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("╔══════════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║                        FINAL REPORT                              ║");
        Console.WriteLine("╚══════════════════════════════════════════════════════════════════╝");
        Console.ResetColor();

        var totalTests = allResults.Count;
        var passedTests = allResults.Count(r => r.Success);
        var failedTests = totalTests - passedTests;
        var successRate = (double)passedTests / totalTests * 100;

        Console.WriteLine($"\n📊 OVERALL STATISTICS:");
        Console.WriteLine($"   Total Tests:     {totalTests}");
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"   ✓ Passed:        {passedTests} ({successRate:F1}%)");
        Console.ResetColor();

        if (failedTests > 0)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"   ✗ Failed:        {failedTests} ({(100 - successRate):F1}%)");
            Console.ResetColor();
        }

        Console.WriteLine($"\n⏱️  TIMING:");
        Console.WriteLine($"   Total Duration:  {totalDuration.TotalSeconds:F2}s ({totalDuration.TotalMinutes:F2}min)");
        Console.WriteLine($"   Avg per test:    {totalDuration.TotalMilliseconds / totalTests:F2}ms");

        Console.WriteLine($"\n💾 MEMORY:");
        var memUsed = (memEnd - memStart) / 1024.0 / 1024.0;
        Console.WriteLine($"   Memory Used:     {memUsed:F2}MB");
        Console.WriteLine($"   Peak Working Set:{process.PeakWorkingSet64 / 1024.0 / 1024.0:F2}MB");
        Console.WriteLine($"   GC Gen0:         {GC.CollectionCount(0)}");
        Console.WriteLine($"   GC Gen1:         {GC.CollectionCount(1)}");
        Console.WriteLine($"   GC Gen2:         {GC.CollectionCount(2)}");

        // Category breakdown
        Console.WriteLine($"\n📁 BREAKDOWN BY CATEGORY:");
        var byCategory = allResults.GroupBy(r => r.Category);
        foreach (var category in byCategory.OrderByDescending(g => g.Count()))
        {
            var catPassed = category.Count(r => r.Success);
            var catTotal = category.Count();
            var catRate = (double)catPassed / catTotal * 100;

            var color = catRate == 100 ? ConsoleColor.Green : catRate >= 80 ? ConsoleColor.Yellow : ConsoleColor.Red;
            Console.ForegroundColor = color;
            Console.WriteLine($"   {category.Key,-20}: {catPassed,3}/{catTotal,3} ({catRate:F0}%)");
            Console.ResetColor();
        }

        // Top 10 slowest tests
        Console.WriteLine($"\n🐌 TOP 10 SLOWEST TESTS:");
        var slowest = allResults.OrderByDescending(r => r.Metrics.Duration).Take(10);
        int rank = 1;
        foreach (var test in slowest)
        {
            Console.WriteLine($"   {rank++,2}. {test.TestName,-45}: {test.Metrics.Duration.TotalMilliseconds,8:F2}ms");
        }

        // Top 10 memory intensive tests
        Console.WriteLine($"\n💾 TOP 10 MEMORY INTENSIVE TESTS:");
        var memIntensive = allResults.OrderByDescending(r => r.Metrics.MemoryUsedMB).Take(10);
        rank = 1;
        foreach (var test in memIntensive)
        {
            Console.WriteLine($"   {rank++,2}. {test.TestName,-45}: {test.Metrics.MemoryUsedMB,8:F2}MB");
        }

        // Failed tests
        if (failedTests > 0)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"\n❌ FAILED TESTS:");
            Console.ResetColor();
            var failed = allResults.Where(r => !r.Success);
            foreach (var test in failed)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"   ✗ {test.Category,-15} | {test.TestName}");
                if (!string.IsNullOrEmpty(test.ErrorMessage))
                {
                    Console.ForegroundColor = ConsoleColor.DarkRed;
                    Console.WriteLine($"     Error: {test.ErrorMessage}");
                }
                Console.ResetColor();
            }
        }

        // Performance metrics
        if (allResults.Any(r => r.Metrics.ThroughputPerSecond > 0))
        {
            Console.WriteLine($"\n⚡ PERFORMANCE HIGHLIGHTS:");
            var perfTests = allResults.Where(r => r.Metrics.ThroughputPerSecond > 0).OrderByDescending(r => r.Metrics.ThroughputPerSecond).Take(5);
            foreach (var test in perfTests)
            {
                Console.WriteLine($"   {test.TestName,-45}: {test.Metrics.ThroughputPerSecond,12:N0} ops/s");
            }
        }

        Console.WriteLine("\n");
        Console.ForegroundColor = successRate == 100 ? ConsoleColor.Green : ConsoleColor.Yellow;
        Console.WriteLine("═══════════════════════════════════════════════════════════════════");
        if (successRate == 100)
        {
            Console.WriteLine("                    ✓ ALL TESTS PASSED!                           ");
        }
        else
        {
            Console.WriteLine($"                  {passedTests}/{totalTests} TESTS PASSED                    ");
        }
        Console.WriteLine("═══════════════════════════════════════════════════════════════════");
        Console.ResetColor();
    }

    private static async Task SaveReportToFile(List<dynamic> allResults, TimeSpan totalDuration)
    {
        var reportPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"TestReport_{DateTime.Now:yyyyMMdd_HHmmss}.md");

        var sb = new StringBuilder();
        sb.AppendLine("# Advanced DataGrid - Comprehensive Test Report");
        sb.AppendLine($"\n**Generated:** {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine($"**Duration:** {totalDuration.TotalSeconds:F2}s ({totalDuration.TotalMinutes:F2}min)");
        sb.AppendLine($"**Total Tests:** {allResults.Count}");
        sb.AppendLine($"**Passed:** {allResults.Count(r => r.Success)}");
        sb.AppendLine($"**Failed:** {allResults.Count(r => !r.Success)}");
        sb.AppendLine("\n---\n");

        sb.AppendLine("## Test Results by Category\n");
        var byCategory = allResults.GroupBy(r => r.Category);
        foreach (var category in byCategory.OrderBy(g => g.Key))
        {
            sb.AppendLine($"### {category.Key}\n");
            sb.AppendLine("| Test Name | Status | Duration | Memory | Throughput |");
            sb.AppendLine("|-----------|--------|----------|--------|------------|");

            foreach (var test in category)
            {
                var status = test.Success ? "✓ PASS" : "✗ FAIL";
                var duration = $"{test.Metrics.Duration.TotalMilliseconds:F2}ms";
                var memory = $"{test.Metrics.MemoryUsedMB:F2}MB";
                var throughput = test.Metrics.ThroughputPerSecond > 0 ? $"{test.Metrics.ThroughputPerSecond:N0} ops/s" : "-";

                sb.AppendLine($"| {test.TestName} | {status} | {duration} | {memory} | {throughput} |");
            }

            sb.AppendLine();
        }

        await File.WriteAllTextAsync(reportPath, sb.ToString());

        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"\n📄 Report saved to: {reportPath}");
        Console.ResetColor();
    }
}
