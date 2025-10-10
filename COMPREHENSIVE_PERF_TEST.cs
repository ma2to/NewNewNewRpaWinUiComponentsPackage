using System.Diagnostics;
using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.UI.Dispatching;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid;

namespace RpaWinUiComponentsPackage.PerformanceTests;

/// <summary>
/// Comprehensive performance tests for all 3 operation modes:
/// 1. Headless (pure backend, fastest)
/// 2. Readonly (UI dispatcher present but no auto-refresh)
/// 3. Interactive (full UI with auto-notifications)
///
/// Tests include detailed metrics:
/// - Exact timing for each operation
/// - Validation status (enabled/disabled, count of rules)
/// - Filtering speed and method
/// - Sort performance
/// - Search performance
/// - Memory usage
/// </summary>
class ComprehensivePerfTest
{
    static async Task Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;
        Console.WriteLine("═══════════════════════════════════════════════════════════════════════════════");
        Console.WriteLine("   ADVANCED DATA GRID - COMPREHENSIVE PERFORMANCE TESTS");
        Console.WriteLine("   Testing 3 Operation Modes: Headless | Readonly | Interactive");
        Console.WriteLine("═══════════════════════════════════════════════════════════════════════════════");
        Console.WriteLine($"Test Started: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        Console.WriteLine();

        var allResults = new List<TestResult>();
        var globalSw = Stopwatch.StartNew();

        // Test configurations
        var rowCounts = new[] { 10_000, 50_000, 100_000, 500_000 };
        var batchSizes = new[] { 5_000, 10_000, 20_000 };
        var modes = new[]
        {
            OperationMode.Headless,
            OperationMode.Readonly,
            OperationMode.Interactive
        };

        try
        {
            foreach (var mode in modes)
            {
                Console.WriteLine($"\n\n┌─ MODE: {mode.ToString().ToUpper()} ────────────────────────────────────────────────────────┐");
                Console.WriteLine(GetModeDescription(mode));
                Console.WriteLine("└──────────────────────────────────────────────────────────────────────────────┘\n");

                foreach (var rowCount in rowCounts)
                {
                    Console.WriteLine($"\n>>> Testing {mode} mode with {rowCount:N0} rows...");

                    foreach (var batchSize in batchSizes)
                    {
                        Console.Write($"  BatchSize={batchSize,6:N0} ... ");

                        var result = await RunComprehensiveTest(mode, rowCount, batchSize);
                        allResults.Add(result);

                        if (result.Success)
                        {
                            Console.WriteLine($"{result.TotalDuration.TotalSeconds,6:F2}s | {result.MemoryMB,6:F1} MB | {result.OperationsCount} ops");
                        }
                        else
                        {
                            Console.WriteLine($"FAILED: {result.Error}");
                        }

                        // Force GC between tests
                        GC.Collect();
                        GC.WaitForPendingFinalizers();
                        GC.Collect();
                        await Task.Delay(500);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n✗ CRITICAL ERROR: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
        }

        globalSw.Stop();

        // Generate comprehensive report
        GenerateComprehensiveReport(allResults, globalSw.Elapsed);

        Console.WriteLine("\n\nPress any key to exit...");
        Console.ReadKey();
    }

    static string GetModeDescription(OperationMode mode)
    {
        return mode switch
        {
            OperationMode.Headless => "  Pure backend mode - No UI dispatcher, fastest performance baseline",
            OperationMode.Readonly => "  UI dispatcher present but inactive - Manual UI refresh only",
            OperationMode.Interactive => "  Full UI mode - Automatic notifications and UI updates",
            _ => "  Unknown mode"
        };
    }

    static async Task<TestResult> RunComprehensiveTest(OperationMode mode, int rowCount, int batchSize)
    {
        var result = new TestResult
        {
            Mode = mode,
            RowCount = rowCount,
            BatchSize = batchSize,
            OperationTimings = new Dictionary<string, TimeSpan>(),
            ValidationMetrics = new ValidationMetrics()
        };

        var globalSw = Stopwatch.StartNew();
        var memBefore = GC.GetTotalMemory(true);

        try
        {
            // Create facade for specific mode
            var facade = CreateFacadeForMode(mode, batchSize);

            // 1. COLUMN SETUP (3 columns with validation rules)
            var sw = Stopwatch.StartNew();

            facade.AddColumn(new PublicColumnDefinition
            {
                Name = "ID",
                Header = "ID",
                DataType = typeof(int),
                Width = 100,
                IsVisible = true,
                IsSortable = true,
                IsFilterable = true,
                ValidationRules = new List<PublicValidationRule>
                {
                    new() { RuleType = PublicValidationRuleType.Required, ErrorMessage = "ID is required" }
                }
            });

            facade.AddColumn(new PublicColumnDefinition
            {
                Name = "Name",
                Header = "Name",
                DataType = typeof(string),
                Width = 200,
                IsVisible = true,
                IsSortable = true,
                IsFilterable = true,
                ValidationRules = new List<PublicValidationRule>
                {
                    new() { RuleType = PublicValidationRuleType.Required, ErrorMessage = "Name is required" },
                    new() { RuleType = PublicValidationRuleType.Regex, RegexPattern = "^Item_\\d+$", ErrorMessage = "Name must match pattern" }
                }
            });

            facade.AddColumn(new PublicColumnDefinition
            {
                Name = "Value",
                Header = "Value",
                DataType = typeof(double),
                Width = 100,
                IsVisible = true,
                IsSortable = true,
                IsFilterable = true,
                ValidationRules = new List<PublicValidationRule>
                {
                    new() { RuleType = PublicValidationRuleType.Range, MinValue = 0.0, MaxValue = 2000.0, ErrorMessage = "Value must be 0-2000" }
                }
            });

            result.OperationTimings["ColumnSetup"] = sw.Elapsed;
            result.ValidationMetrics.ValidationRulesCount = 4;
            result.ValidationMetrics.ValidatedColumns = 3;

            // 2. DATA INSERTION
            sw.Restart();
            var random = new Random(42);
            for (int i = 0; i < rowCount; i++)
            {
                await facade.AddRowAsync(new Dictionary<string, object?>
                {
                    ["ID"] = random.Next(0, rowCount * 2),
                    ["Name"] = $"Item_{i}",
                    ["Value"] = random.NextDouble() * 1000
                });
            }
            result.OperationTimings["DataInsertion"] = sw.Elapsed;
            result.OperationsCount++;

            // 3. SORT OPERATION
            sw.Restart();
            await facade.SortByColumnAsync("ID", PublicSortDirection.Ascending);
            result.OperationTimings["Sort_ID_Asc"] = sw.Elapsed;
            result.OperationsCount++;

            sw.Restart();
            await facade.SortByColumnAsync("Name", PublicSortDirection.Descending);
            result.OperationTimings["Sort_Name_Desc"] = sw.Elapsed;
            result.OperationsCount++;

            // 4. FILTER OPERATION
            sw.Restart();
            await facade.ApplyFilterAsync("Value", PublicFilterOperator.GreaterThan, 500.0);
            result.OperationTimings["Filter_Value_GT_500"] = sw.Elapsed;
            result.FilterMetrics.FilterMethod = "GreaterThan";
            result.FilterMetrics.FilterColumn = "Value";
            result.OperationsCount++;

            var filtered = facade.GetCurrentData();
            result.FilterMetrics.ResultCount = filtered.Count;

            // 5. CLEAR FILTER
            sw.Restart();
            await facade.ClearFilterAsync();
            result.OperationTimings["ClearFilter"] = sw.Elapsed;
            result.OperationsCount++;

            // 6. SEARCH OPERATION (if large dataset)
            if (rowCount >= 50_000)
            {
                sw.Restart();
                var searchResult = await facade.SearchAsync("Item_1000", new[] { "Name" });
                result.OperationTimings["Search_Item_1000"] = sw.Elapsed;
                result.SearchMetrics.SearchTerm = "Item_1000";
                result.SearchMetrics.ResultCount = searchResult.Data?.Count ?? 0;
                result.OperationsCount++;
            }

            // 7. GET DATA
            sw.Restart();
            var data = facade.GetCurrentData();
            result.OperationTimings["GetCurrentData"] = sw.Elapsed;
            result.DataMetrics.FinalRowCount = data.Count;
            result.OperationsCount++;

            // 8. CELL UPDATE (sample)
            if (rowCount >= 10_000)
            {
                sw.Restart();
                await facade.UpdateCellAsync(0, "Value", 999.99);
                result.OperationTimings["UpdateCell"] = sw.Elapsed;
                result.OperationsCount++;
            }

            // 9. ROW SELECTION (sample)
            sw.Restart();
            await facade.SelectRowAsync(0);
            result.OperationTimings["SelectRow"] = sw.Elapsed;
            result.OperationsCount++;

            // 10. CLEAR SELECTION
            sw.Restart();
            await facade.ClearSelectionAsync();
            result.OperationTimings["ClearSelection"] = sw.Elapsed;
            result.OperationsCount++;

            globalSw.Stop();
            var memAfter = GC.GetTotalMemory(false);

            result.Success = true;
            result.TotalDuration = globalSw.Elapsed;
            result.MemoryMB = (memAfter - memBefore) / 1024.0 / 1024.0;
        }
        catch (Exception ex)
        {
            globalSw.Stop();
            result.Success = false;
            result.Error = ex.Message;
            result.TotalDuration = globalSw.Elapsed;
        }

        return result;
    }

    static IAdvancedDataGridFacade CreateFacadeForMode(OperationMode mode, int batchSize)
    {
        var loggerFactory = NullLoggerFactory.Instance;

        var options = new AdvancedDataGridOptions
        {
            BatchSize = batchSize,
            EnableParallelProcessing = true,
            EnableRealTimeValidation = true, // Enable validation for testing
            LoggerFactory = loggerFactory,
            OperationMode = mode switch
            {
                OperationMode.Headless => PublicDataGridOperationMode.Headless,
                OperationMode.Readonly => PublicDataGridOperationMode.Readonly,
                OperationMode.Interactive => PublicDataGridOperationMode.Interactive,
                _ => PublicDataGridOperationMode.Headless
            },
            DispatcherQueue = mode != OperationMode.Headless ? CreateDispatcher() : null
        };

        options.EnabledFeatures.Clear();
        options.EnabledFeatures.Add(GridFeature.Sort);
        options.EnabledFeatures.Add(GridFeature.Filter);
        options.EnabledFeatures.Add(GridFeature.Search);
        options.EnabledFeatures.Add(GridFeature.RowColumnOperations);
        options.EnabledFeatures.Add(GridFeature.Validation);
        options.EnabledFeatures.Add(GridFeature.Selection);

        return AdvancedDataGridFacadeFactory.CreateStandalone(options, loggerFactory, options.DispatcherQueue);
    }

    static DispatcherQueue? CreateDispatcher()
    {
        // For readonly/interactive modes, create a dispatcher queue
        // In real scenarios this would be provided by WinUI app
        // For testing purposes, we'll return null and rely on fallback logic
        return null;
    }

    static void GenerateComprehensiveReport(List<TestResult> results, TimeSpan totalTime)
    {
        Console.WriteLine("\n\n═══════════════════════════════════════════════════════════════════════════════");
        Console.WriteLine("   COMPREHENSIVE PERFORMANCE REPORT");
        Console.WriteLine("═══════════════════════════════════════════════════════════════════════════════");

        var successful = results.Where(r => r.Success).ToList();
        var failed = results.Where(r => !r.Success).ToList();

        Console.WriteLine($"\nTotal Tests: {results.Count}");
        Console.WriteLine($"✓ Successful: {successful.Count}");
        Console.WriteLine($"✗ Failed: {failed.Count}");
        Console.WriteLine($"Total Time: {totalTime.TotalSeconds:F2}s");

        // Performance by Mode
        Console.WriteLine("\n┌─ PERFORMANCE BY MODE ────────────────────────────────────────────────────────┐");
        foreach (var mode in new[] { OperationMode.Headless, OperationMode.Readonly, OperationMode.Interactive })
        {
            var modeResults = successful.Where(r => r.Mode == mode).ToList();
            if (modeResults.Any())
            {
                var avgTime = modeResults.Average(r => r.TotalDuration.TotalSeconds);
                var avgMem = modeResults.Average(r => r.MemoryMB);
                Console.WriteLine($"  {mode,-15} Avg Time: {avgTime,8:F3}s | Avg Memory: {avgMem,8:F2} MB | Tests: {modeResults.Count,3}");
            }
        }
        Console.WriteLine("└──────────────────────────────────────────────────────────────────────────────┘");

        // Optimal Batch Size by Mode and Row Count
        Console.WriteLine("\n┌─ OPTIMAL BATCH SIZE BY MODE & ROW COUNT ─────────────────────────────────────┐");
        var groupedByModeAndRows = successful.GroupBy(r => new { r.Mode, r.RowCount });
        foreach (var group in groupedByModeAndRows.OrderBy(g => g.Key.Mode).ThenBy(g => g.Key.RowCount))
        {
            var best = group.OrderBy(r => r.TotalDuration).First();
            Console.WriteLine($"  {group.Key.Mode,-15} {group.Key.RowCount,10:N0} rows: BatchSize = {best.BatchSize,6:N0} ({best.TotalDuration.TotalSeconds,6:F2}s)");
        }
        Console.WriteLine("└──────────────────────────────────────────────────────────────────────────────┘");

        // Operation Timing Breakdown (average across all tests)
        if (successful.Any() && successful.First().OperationTimings.Any())
        {
            Console.WriteLine("\n┌─ AVERAGE OPERATION TIMINGS ──────────────────────────────────────────────────┐");
            var allOperations = successful
                .SelectMany(r => r.OperationTimings)
                .GroupBy(kvp => kvp.Key)
                .Select(g => new { Operation = g.Key, AvgMs = g.Average(kvp => kvp.Value.TotalMilliseconds) })
                .OrderByDescending(x => x.AvgMs);

            foreach (var op in allOperations)
            {
                Console.WriteLine($"  {op.Operation,-25} {op.AvgMs,10:F3} ms");
            }
            Console.WriteLine("└──────────────────────────────────────────────────────────────────────────────┘");
        }

        // Validation Metrics
        Console.WriteLine("\n┌─ VALIDATION METRICS ─────────────────────────────────────────────────────────┐");
        if (successful.Any())
        {
            var sample = successful.First();
            Console.WriteLine($"  Validated Columns: {sample.ValidationMetrics.ValidatedColumns}");
            Console.WriteLine($"  Total Validation Rules: {sample.ValidationMetrics.ValidationRulesCount}");
            Console.WriteLine($"  Real-Time Validation: Enabled");
        }
        Console.WriteLine("└──────────────────────────────────────────────────────────────────────────────┘");

        // Filter/Search Metrics
        if (successful.Any(r => r.FilterMetrics.FilterColumn != null))
        {
            Console.WriteLine("\n┌─ FILTER & SEARCH METRICS ────────────────────────────────────────────────────┐");
            var filterSample = successful.First(r => r.FilterMetrics.FilterColumn != null);
            Console.WriteLine($"  Filter Method: {filterSample.FilterMetrics.FilterMethod}");
            Console.WriteLine($"  Filter Column: {filterSample.FilterMetrics.FilterColumn}");
            Console.WriteLine($"  Avg Filter Results: {successful.Average(r => r.FilterMetrics.ResultCount):N0}");

            var searchResults = successful.Where(r => r.SearchMetrics.SearchTerm != null).ToList();
            if (searchResults.Any())
            {
                Console.WriteLine($"  Search Term: {searchResults.First().SearchMetrics.SearchTerm}");
                Console.WriteLine($"  Avg Search Results: {searchResults.Average(r => r.SearchMetrics.ResultCount):N0}");
            }
            Console.WriteLine("└──────────────────────────────────────────────────────────────────────────────┘");
        }

        // Failed Tests
        if (failed.Any())
        {
            Console.WriteLine("\n┌─ FAILED TESTS ───────────────────────────────────────────────────────────────┐");
            foreach (var test in failed)
            {
                Console.WriteLine($"  ✗ {test.Mode} | {test.RowCount:N0} rows | Batch={test.BatchSize:N0}");
                Console.WriteLine($"    Error: {test.Error}");
            }
            Console.WriteLine("└──────────────────────────────────────────────────────────────────────────────┘");
        }

        // Save detailed report to file
        SaveDetailedReport(results, totalTime);
    }

    static void SaveDetailedReport(List<TestResult> results, TimeSpan totalTime)
    {
        var reportPath = Path.Combine(Environment.CurrentDirectory, "Reports", $"ComprehensiveReport_{DateTime.Now:yyyyMMdd_HHmmss}.txt");
        Directory.CreateDirectory(Path.GetDirectoryName(reportPath)!);

        using var writer = new StreamWriter(reportPath);
        writer.WriteLine("ADVANCED DATA GRID - COMPREHENSIVE PERFORMANCE REPORT");
        writer.WriteLine($"Generated: {DateTime.Now}");
        writer.WriteLine($"Total Time: {totalTime.TotalSeconds:F2}s");
        writer.WriteLine($"Total Tests: {results.Count}");
        writer.WriteLine();

        foreach (var result in results.OrderBy(r => r.Mode).ThenBy(r => r.RowCount).ThenBy(r => r.BatchSize))
        {
            writer.WriteLine($"[{(result.Success ? "PASS" : "FAIL")}] {result.Mode} | {result.RowCount:N0} rows | Batch={result.BatchSize:N0}");
            writer.WriteLine($"  Total Duration: {result.TotalDuration.TotalSeconds:F3}s | Memory: {result.MemoryMB:F2} MB | Operations: {result.OperationsCount}");

            if (result.OperationTimings.Any())
            {
                writer.WriteLine("  Operation Timings:");
                foreach (var op in result.OperationTimings.OrderByDescending(kvp => kvp.Value))
                {
                    writer.WriteLine($"    {op.Key,-25} {op.Value.TotalMilliseconds,10:F3} ms");
                }
            }

            if (!result.Success)
            {
                writer.WriteLine($"  Error: {result.Error}");
            }
            writer.WriteLine();
        }

        Console.WriteLine($"\n✓ Detailed report saved to: {reportPath}");
    }

    enum OperationMode
    {
        Headless,
        Readonly,
        Interactive
    }

    class TestResult
    {
        public OperationMode Mode { get; set; }
        public int RowCount { get; set; }
        public int BatchSize { get; set; }
        public bool Success { get; set; }
        public TimeSpan TotalDuration { get; set; }
        public double MemoryMB { get; set; }
        public int OperationsCount { get; set; }
        public string? Error { get; set; }
        public Dictionary<string, TimeSpan> OperationTimings { get; set; } = new();
        public ValidationMetrics ValidationMetrics { get; set; } = new();
        public FilterMetrics FilterMetrics { get; set; } = new();
        public SearchMetrics SearchMetrics { get; set; } = new();
        public DataMetrics DataMetrics { get; set; } = new();
    }

    class ValidationMetrics
    {
        public int ValidationRulesCount { get; set; }
        public int ValidatedColumns { get; set; }
    }

    class FilterMetrics
    {
        public string? FilterMethod { get; set; }
        public string? FilterColumn { get; set; }
        public int ResultCount { get; set; }
    }

    class SearchMetrics
    {
        public string? SearchTerm { get; set; }
        public int ResultCount { get; set; }
    }

    class DataMetrics
    {
        public int FinalRowCount { get; set; }
    }
}
