using RpaWinUiComponentsPackage.Tests.Comprehensive;

namespace RpaWinUiComponentsPackage;

/// <summary>
/// Main runner for comprehensive API tests covering ALL public methods
/// Tests: Performance, Stability, Load, Memory, CPU, and Unit tests
/// </summary>
class AllApiTestsRunner
{
    static async Task Main(string[] args)
    {
        Console.Clear();
        Console.WriteLine("═══════════════════════════════════════════════════════════════════");
        Console.WriteLine("  COMPREHENSIVE API TESTS - ADVANCED DATAGRID");
        Console.WriteLine("  Testing ALL Public Methods + Performance + Resources");
        Console.WriteLine("═══════════════════════════════════════════════════════════════════");
        Console.WriteLine();

        var allTests = new ComprehensiveApiTests();
        var results = await allTests.RunAllTests();

        Console.WriteLine();
        Console.WriteLine("═══════════════════════════════════════════════════════════════════");
        Console.WriteLine($"  COMPLETED: {results.Count} tests executed");
        Console.WriteLine($"  PASSED: {results.Count(r => r.Success)}");
        Console.WriteLine($"  FAILED: {results.Count(r => !r.Success)}");
        Console.WriteLine("═══════════════════════════════════════════════════════════════════");
    }
}
