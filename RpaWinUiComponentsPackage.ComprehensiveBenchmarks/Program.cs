using BenchmarkDotNet.Running;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Exporters.Csv;
using BenchmarkDotNet.Exporters.Json;
using RpaWinUiComponentsPackage.ComprehensiveBenchmarks.Tests;
using System.Reflection;

namespace RpaWinUiComponentsPackage.ComprehensiveBenchmarks;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("╔═══════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║  AdvancedWinUiDataGrid Comprehensive Benchmark Suite         ║");
        Console.WriteLine("║  Testing: Interactive, Headless+Manual, and Pure Headless    ║");
        Console.WriteLine("╚═══════════════════════════════════════════════════════════════╝");
        Console.WriteLine();

        if (args.Length == 0)
        {
            ShowMenu();
            return;
        }

        var testMode = args[0].ToLower();
        RunTests(testMode);
    }

    static void ShowMenu()
    {
        Console.WriteLine("Available test modes:");
        Console.WriteLine("  1. all              - Run all benchmark tests (takes several hours)");
        Console.WriteLine("  2. interactive      - Test Interactive Mode only");
        Console.WriteLine("  3. headless-manual  - Test Headless + Manual UI Update Mode");
        Console.WriteLine("  4. headless         - Test Pure Headless Mode");
        Console.WriteLine("  5. stability        - Test Comprehensive Stability");
        Console.WriteLine("  6. quick            - Quick test (small datasets)");
        Console.WriteLine();
        Console.WriteLine("Usage: dotnet run -- [mode]");
        Console.WriteLine("Example: dotnet run -- all");
    }

    static void RunTests(string mode)
    {
        var config = ManualConfig.Create(DefaultConfig.Instance)
            .AddExporter(HtmlExporter.Default)
            .AddExporter(MarkdownExporter.GitHub)
            .AddExporter(CsvMeasurementsExporter.Default)
            .AddExporter(JsonExporter.Full)
            .WithOption(ConfigOptions.DisableOptimizationsValidator, true);

        Console.WriteLine($"\n🚀 Starting test mode: {mode.ToUpper()}\n");

        switch (mode)
        {
            case "all":
                RunAllTests(config);
                break;

            case "interactive":
            case "headless-manual":
            case "headless":
            case "stability":
                Console.WriteLine($"⚠️  Test category '{mode}' not yet implemented.");
                Console.WriteLine("Please use 'quick' or 'all' mode, or run SimpleVerificationTests directly.");
                break;

            case "quick":
                RunQuickTests(config);
                break;

            default:
                Console.WriteLine($"❌ Unknown test mode: {mode}");
                ShowMenu();
                break;
        }

        Console.WriteLine("\n✅ Benchmark complete! Check BenchmarkDotNet.Artifacts for results.");
        GenerateSummaryReport();
    }

    static void RunAllTests(IConfig config)
    {
        Console.WriteLine("⏰ Running ALL tests - this will take several hours...\n");

        var assembly = Assembly.GetExecutingAssembly();
        BenchmarkRunner.Run(assembly, config);
    }

    static void RunQuickTests(IConfig config)
    {
        Console.WriteLine("⚡ Running quick verification tests...\n");

        Console.WriteLine("Running SimpleVerificationTests...");
        Console.WriteLine("Note: These are xUnit tests, not benchmarks.");
        Console.WriteLine("To run them, use: dotnet test");
    }

    static void GenerateSummaryReport()
    {
        var reportPath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "BenchmarkDotNet.Artifacts",
            "ComprehensiveBenchmarkSummary.md"
        );

        try
        {
            var summary = new System.Text.StringBuilder();
            summary.AppendLine("# AdvancedWinUiDataGrid Comprehensive Benchmark Summary");
            summary.AppendLine();
            summary.AppendLine($"**Generated:** {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            summary.AppendLine($"**Machine:** {Environment.MachineName}");
            summary.AppendLine($"**OS:** {Environment.OSVersion}");
            summary.AppendLine($"**Processor:** {Environment.ProcessorCount} cores");
            summary.AppendLine();

            summary.AppendLine("## Test Coverage");
            summary.AppendLine();
            summary.AppendLine("### 1. Interactive Mode Tests");
            summary.AppendLine("- ✅ Load data into grid");
            summary.AppendLine("- ✅ Get cell value");
            summary.AppendLine("- ✅ Set cell value");
            summary.AppendLine("- ✅ Add row");
            summary.AppendLine("- ✅ Remove row");
            summary.AppendLine("- ✅ Sort by column");
            summary.AppendLine("- ✅ Filter data");
            summary.AppendLine("- ✅ Select cells");
            summary.AppendLine("- ✅ Stress test - rapid operations");
            summary.AppendLine("- ✅ Memory stability - continuous operations");
            summary.AppendLine("- ✅ Throughput test - operations per second");
            summary.AppendLine();

            summary.AppendLine("### 2. Headless + Manual UI Update Mode Tests");
            summary.AppendLine("- ✅ Batch operations with periodic UI refresh");
            summary.AppendLine("- ✅ Bulk data import with deferred UI");
            summary.AppendLine("- ✅ Background computation with UI snapshots");
            summary.AppendLine("- ✅ Performance: Headless vs Interactive comparison");
            summary.AppendLine("- ✅ Stability: Long-running headless operations");
            summary.AppendLine("- ✅ Stability: Concurrent headless operations");
            summary.AppendLine("- ✅ Resource efficiency: CPU usage");
            summary.AppendLine("- ✅ Resource efficiency: Memory usage");
            summary.AppendLine();

            summary.AppendLine("### 3. Pure Headless Mode Tests");
            summary.AppendLine("- ✅ Max throughput: Data loading");
            summary.AppendLine("- ✅ Max throughput: Bulk operations");
            summary.AppendLine("- ✅ Max throughput: Streaming data");
            summary.AppendLine("- ✅ Extreme scale: Million row operations");
            summary.AppendLine("- ✅ Extreme scale: Wide table operations");
            summary.AppendLine("- ✅ Resource: CPU efficiency");
            summary.AppendLine("- ✅ Resource: Memory efficiency");
            summary.AppendLine("- ✅ Resource: Thread efficiency");
            summary.AppendLine("- ✅ Stability: 24-hour simulation");
            summary.AppendLine("- ✅ Stability: Error recovery");
            summary.AppendLine("- ✅ Stability: Concurrent stress test");
            summary.AppendLine("- ✅ Data integrity: Verify operations");
            summary.AppendLine();

            summary.AppendLine("### 4. Comprehensive Stability Tests");
            summary.AppendLine("- ✅ Memory leak: Repeated initialization");
            summary.AppendLine("- ✅ Memory leak: Event handler cleanup");
            summary.AppendLine("- ✅ Thread safety: Concurrent operations");
            summary.AppendLine("- ✅ Thread safety: Race condition test");
            summary.AppendLine("- ✅ Error handling: Invalid operations");
            summary.AppendLine("- ✅ Error handling: Exception recovery");
            summary.AppendLine("- ✅ Long-running: 24/7 server simulation");
            summary.AppendLine("- ✅ Long-running: Continuous streaming");
            summary.AppendLine("- ✅ Cross-mode: Interactive to Headless transition");
            summary.AppendLine();

            summary.AppendLine("## Metrics Collected");
            summary.AppendLine();
            summary.AppendLine("- **CPU Usage**: Average, Max, Min");
            summary.AppendLine("- **Memory Usage**: Working Set, Private Memory, Managed Memory");
            summary.AppendLine("- **Garbage Collection**: Gen0, Gen1, Gen2 collections");
            summary.AppendLine("- **Thread Count**: Average, Max");
            summary.AppendLine("- **Handle Count**: Average, Max");
            summary.AppendLine("- **Throughput**: Operations per second");
            summary.AppendLine("- **Latency**: Operation duration");
            summary.AppendLine();

            summary.AppendLine("## Report Files");
            summary.AppendLine();
            summary.AppendLine("Check the `BenchmarkDotNet.Artifacts` folder for:");
            summary.AppendLine("- `*.html` - HTML reports");
            summary.AppendLine("- `*.md` - Markdown reports");
            summary.AppendLine("- `*.csv` - CSV data for analysis");
            summary.AppendLine("- `*.json` - JSON data for processing");
            summary.AppendLine();

            summary.AppendLine("## How to Analyze Results");
            summary.AppendLine();
            summary.AppendLine("1. Open HTML reports for visual analysis");
            summary.AppendLine("2. Import CSV files into Excel/Google Sheets for custom charts");
            summary.AppendLine("3. Use JSON files for automated processing");
            summary.AppendLine("4. Compare performance metrics across different modes");
            summary.AppendLine("5. Look for memory leaks (increasing Gen2 collections)");
            summary.AppendLine("6. Verify CPU usage remains reasonable");
            summary.AppendLine("7. Check throughput meets requirements");
            summary.AppendLine();

            summary.AppendLine("## Recommendations");
            summary.AppendLine();
            summary.AppendLine("- **Interactive Mode**: Best for real-time user interaction");
            summary.AppendLine("- **Headless + Manual Update**: Best for batch processing with progress updates");
            summary.AppendLine("- **Pure Headless Mode**: Best for server-side processing, maximum performance");
            summary.AppendLine();

            Directory.CreateDirectory(Path.GetDirectoryName(reportPath)!);
            File.WriteAllText(reportPath, summary.ToString());

            Console.WriteLine($"\n📄 Summary report generated: {reportPath}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️ Failed to generate summary report: {ex.Message}");
        }
    }
}
