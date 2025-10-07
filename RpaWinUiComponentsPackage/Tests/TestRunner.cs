using System.Diagnostics;
using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid;
//using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Api.Models;

namespace RpaWinUiComponentsPackage.Tests;

/// <summary>
/// Main test runner - executes all test suites and generates comprehensive reports
/// </summary>
public class TestRunner
{
    public static async Task Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;
        Console.WriteLine("═══════════════════════════════════════════════════════════════════════════════");
        Console.WriteLine("   ADVANCED DATA GRID - COMPREHENSIVE TEST SUITE");
        Console.WriteLine("═══════════════════════════════════════════════════════════════════════════════");
        Console.WriteLine($"Test Started: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        Console.WriteLine();

        var allResults = new List<TestResult>();
        var sw = Stopwatch.StartNew();

        try
        {
            // 1. Performance Tests
            Console.WriteLine("\n┌─ PERFORMANCE TESTS ──────────────────────────────────────────────────────────┐");
            var perfTests = new Performance.PerformanceTests();
            var perfResults = await perfTests.RunAllTests();
            allResults.AddRange(perfResults);
            Console.WriteLine("└──────────────────────────────────────────────────────────────────────────────┘");

            // 2. Functional Tests
            Console.WriteLine("\n┌─ FUNCTIONAL TESTS ───────────────────────────────────────────────────────────┐");
            var funcTests = new Functional.FunctionalTests();
            var funcResults = await funcTests.RunAllTests();
            allResults.AddRange(funcResults);
            Console.WriteLine("└──────────────────────────────────────────────────────────────────────────────┘");

            // 3. Stability Tests
            Console.WriteLine("\n┌─ STABILITY TESTS ────────────────────────────────────────────────────────────┐");
            var stabilityTests = new Stability.StabilityTests();
            var stabilityResults = await stabilityTests.RunAllTests();
            allResults.AddRange(stabilityResults);
            Console.WriteLine("└──────────────────────────────────────────────────────────────────────────────┘");

            // 4. Load Tests
            Console.WriteLine("\n┌─ LOAD TESTS ─────────────────────────────────────────────────────────────────┐");
            var loadTests = new Load.LoadTests();
            var loadResults = await loadTests.RunAllTests();
            allResults.AddRange(loadResults);
            Console.WriteLine("└──────────────────────────────────────────────────────────────────────────────┘");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n✗ CRITICAL ERROR: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
        }

        sw.Stop();

        // Generate Final Report
        GenerateFinalReport(allResults, sw.Elapsed);

        Console.WriteLine("\n\nPress any key to exit...");
        Console.ReadKey();
    }

    static void GenerateFinalReport(List<TestResult> results, TimeSpan totalTime)
    {
        Console.WriteLine("\n\n═══════════════════════════════════════════════════════════════════════════════");
        Console.WriteLine("   FINAL REPORT");
        Console.WriteLine("═══════════════════════════════════════════════════════════════════════════════");

        var passed = results.Count(r => r.Success);
        var failed = results.Count(r => !r.Success);
        var passRate = results.Count > 0 ? (passed * 100.0 / results.Count) : 0;

        Console.WriteLine($"\nTotal Tests: {results.Count}");
        Console.WriteLine($"✓ Passed: {passed} ({passRate:F1}%)");
        Console.WriteLine($"✗ Failed: {failed}");
        Console.WriteLine($"Total Time: {totalTime.TotalSeconds:F2}s");

        // Group by category
        var byCategory = results.GroupBy(r => r.Category);
        Console.WriteLine("\n┌─ BY CATEGORY ────────────────────────────────────────────────────────────────┐");
        foreach (var group in byCategory.OrderBy(g => g.Key))
        {
            var catPassed = group.Count(r => r.Success);
            var catTotal = group.Count();
            var catRate = (catPassed * 100.0 / catTotal);
            Console.WriteLine($"  {group.Key,-20} {catPassed,3}/{catTotal,3} ({catRate,5:F1}%)");
        }
        Console.WriteLine("└──────────────────────────────────────────────────────────────────────────────┘");

        // Performance Summary
        var perfResults = results.Where(r => r.Category == "Performance" && r.Success).ToList();
        if (perfResults.Any())
        {
            Console.WriteLine("\n┌─ PERFORMANCE SUMMARY ────────────────────────────────────────────────────────┐");
            Console.WriteLine($"  Fastest Operation: {perfResults.OrderBy(r => r.Duration).First().Name,-30} {perfResults.OrderBy(r => r.Duration).First().Duration.TotalMilliseconds,8:F2}ms");
            Console.WriteLine($"  Slowest Operation: {perfResults.OrderByDescending(r => r.Duration).First().Name,-30} {perfResults.OrderByDescending(r => r.Duration).First().Duration.TotalMilliseconds,8:F2}ms");
            Console.WriteLine($"  Average Duration:  {perfResults.Average(r => r.Duration.TotalMilliseconds),51:F2}ms");
            Console.WriteLine("└──────────────────────────────────────────────────────────────────────────────┘");
        }

        // Failed Tests Details
        var failedTests = results.Where(r => !r.Success).ToList();
        if (failedTests.Any())
        {
            Console.WriteLine("\n┌─ FAILED TESTS ───────────────────────────────────────────────────────────────┐");
            foreach (var test in failedTests)
            {
                Console.WriteLine($"  ✗ {test.Category}/{test.Name}");
                Console.WriteLine($"    Error: {test.Error}");
            }
            Console.WriteLine("└──────────────────────────────────────────────────────────────────────────────┘");
        }

        // Save to file
        var reportPath = Path.Combine("Tests", "Reports", $"TestReport_{DateTime.Now:yyyyMMdd_HHmmss}.txt");
        Directory.CreateDirectory(Path.GetDirectoryName(reportPath)!);

        using var writer = new StreamWriter(reportPath);
        writer.WriteLine("ADVANCED DATA GRID - TEST REPORT");
        writer.WriteLine($"Generated: {DateTime.Now}");
        writer.WriteLine($"Total Tests: {results.Count} | Passed: {passed} | Failed: {failed}");
        writer.WriteLine();

        foreach (var result in results.OrderBy(r => r.Category).ThenBy(r => r.Name))
        {
            writer.WriteLine($"[{(result.Success ? "PASS" : "FAIL")}] {result.Category}/{result.Name} - {result.Duration.TotalMilliseconds:F2}ms");
            if (!string.IsNullOrEmpty(result.Details))
            {
                writer.WriteLine($"  Details: {result.Details}");
            }
            if (!result.Success && !string.IsNullOrEmpty(result.Error))
            {
                writer.WriteLine($"  Error: {result.Error}");
            }
        }

        Console.WriteLine($"\n✓ Report saved to: {reportPath}");
    }
}

public class TestResult
{
    public string Category { get; set; } = "";
    public string Name { get; set; } = "";
    public bool Success { get; set; }
    public TimeSpan Duration { get; set; }
    public string Details { get; set; } = "";
    public string Error { get; set; } = "";
    public Dictionary<string, object> Metrics { get; set; } = new();
}
