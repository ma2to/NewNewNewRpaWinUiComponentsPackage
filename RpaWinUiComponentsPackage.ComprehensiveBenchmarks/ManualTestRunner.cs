using RpaWinUiComponentsPackage.ComprehensiveBenchmarks.Tests;
using Xunit.Abstractions;
using System.Text;

namespace RpaWinUiComponentsPackage.ComprehensiveBenchmarks;

/// <summary>
/// Manual test runner that doesn't rely on xUnit test host
/// </summary>
public class ManualTestRunner
{
    public static async Task Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;
        Console.WriteLine("╔═══════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║  AdvancedWinUiDataGrid - Manual Test Runner                  ║");
        Console.WriteLine("║  Running SimpleVerificationTests without xUnit host          ║");
        Console.WriteLine("╚═══════════════════════════════════════════════════════════════╝");
        Console.WriteLine();

        var output = new ConsoleTestOutputHelper();
        var testsPassed = 0;
        var testsFailed = 0;

        // Test 1: Interactive Mode Import
        Console.WriteLine("══════════════════════════════════════════════════════════════");
        Console.WriteLine("Test 1: Interactive Mode - Import Data");
        Console.WriteLine("══════════════════════════════════════════════════════════════");
        try
        {
            var test = new SimpleVerificationTests(output);
            await test.Test_InteractiveMode_ImportData_ShouldSucceed();
            test.Dispose();
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("✅ PASSED");
            Console.ResetColor();
            testsPassed++;
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"❌ FAILED: {ex.Message}");
            Console.WriteLine($"Stack: {ex.StackTrace}");
            Console.ResetColor();
            testsFailed++;
        }
        Console.WriteLine();

        // Test 2: Headless Mode Import
        Console.WriteLine("══════════════════════════════════════════════════════════════");
        Console.WriteLine("Test 2: Headless Mode - Import Data");
        Console.WriteLine("══════════════════════════════════════════════════════════════");
        try
        {
            var test = new SimpleVerificationTests(output);
            await test.Test_HeadlessMode_ImportData_ShouldSucceed();
            test.Dispose();
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("✅ PASSED");
            Console.ResetColor();
            testsPassed++;
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"❌ FAILED: {ex.Message}");
            Console.WriteLine($"Stack: {ex.StackTrace}");
            Console.ResetColor();
            testsFailed++;
        }
        Console.WriteLine();

        // Test 3: Get Current Data
        Console.WriteLine("══════════════════════════════════════════════════════════════");
        Console.WriteLine("Test 3: Get Current Data After Import");
        Console.WriteLine("══════════════════════════════════════════════════════════════");
        try
        {
            var test = new SimpleVerificationTests(output);
            await test.Test_GetCurrentData_AfterImport_ShouldReturnData();
            test.Dispose();
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("✅ PASSED");
            Console.ResetColor();
            testsPassed++;
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"❌ FAILED: {ex.Message}");
            Console.WriteLine($"Stack: {ex.StackTrace}");
            Console.ResetColor();
            testsFailed++;
        }
        Console.WriteLine();

        // Test 4: Add Row
        Console.WriteLine("══════════════════════════════════════════════════════════════");
        Console.WriteLine("Test 4: Add Row");
        Console.WriteLine("══════════════════════════════════════════════════════════════");
        try
        {
            var test = new SimpleVerificationTests(output);
            await test.Test_AddRow_ShouldIncreaseRowCount();
            test.Dispose();
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("✅ PASSED");
            Console.ResetColor();
            testsPassed++;
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"❌ FAILED: {ex.Message}");
            Console.WriteLine($"Stack: {ex.StackTrace}");
            Console.ResetColor();
            testsFailed++;
        }
        Console.WriteLine();

        // Test 5: Remove Row
        Console.WriteLine("══════════════════════════════════════════════════════════════");
        Console.WriteLine("Test 5: Remove Row");
        Console.WriteLine("══════════════════════════════════════════════════════════════");
        try
        {
            var test = new SimpleVerificationTests(output);
            await test.Test_RemoveRow_ShouldDecreaseRowCount();
            test.Dispose();
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("✅ PASSED");
            Console.ResetColor();
            testsPassed++;
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"❌ FAILED: {ex.Message}");
            Console.WriteLine($"Stack: {ex.StackTrace}");
            Console.ResetColor();
            testsFailed++;
        }
        Console.WriteLine();

        // Test 6: Export Data
        Console.WriteLine("══════════════════════════════════════════════════════════════");
        Console.WriteLine("Test 6: Export Data After Import");
        Console.WriteLine("══════════════════════════════════════════════════════════════");
        try
        {
            var test = new SimpleVerificationTests(output);
            await test.Test_ExportData_AfterImport_ShouldSucceed();
            test.Dispose();
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("✅ PASSED");
            Console.ResetColor();
            testsPassed++;
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"❌ FAILED: {ex.Message}");
            Console.WriteLine($"Stack: {ex.StackTrace}");
            Console.ResetColor();
            testsFailed++;
        }
        Console.WriteLine();

        // Test 7: Performance Comparison
        Console.WriteLine("══════════════════════════════════════════════════════════════");
        Console.WriteLine("Test 7: Performance Comparison - Headless vs Interactive");
        Console.WriteLine("══════════════════════════════════════════════════════════════");
        try
        {
            var test = new SimpleVerificationTests(output);
            await test.Test_PerformanceComparison_HeadlessVsInteractive();
            test.Dispose();
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("✅ PASSED");
            Console.ResetColor();
            testsPassed++;
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"❌ FAILED: {ex.Message}");
            Console.WriteLine($"Stack: {ex.StackTrace}");
            Console.ResetColor();
            testsFailed++;
        }
        Console.WriteLine();

        // Summary
        Console.WriteLine();
        Console.WriteLine("╔═══════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║                    TEST SUMMARY                               ║");
        Console.WriteLine("╚═══════════════════════════════════════════════════════════════╝");
        Console.WriteLine($"Total Tests: {testsPassed + testsFailed}");
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"Passed:      {testsPassed}");
        Console.ResetColor();
        if (testsFailed > 0)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Failed:      {testsFailed}");
            Console.ResetColor();
        }
        Console.WriteLine();

        if (testsFailed == 0)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("🎉 All tests passed!");
            Console.ResetColor();
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("⚠️  Some tests failed. Check output above for details.");
            Console.ResetColor();
        }

        Environment.Exit(testsFailed > 0 ? 1 : 0);
    }

    /// <summary>
    /// Simple console-based test output helper
    /// </summary>
    private class ConsoleTestOutputHelper : ITestOutputHelper
    {
        public void WriteLine(string message)
        {
            Console.WriteLine($"  {message}");
        }

        public void WriteLine(string format, params object[] args)
        {
            Console.WriteLine($"  {string.Format(format, args)}");
        }
    }
}
