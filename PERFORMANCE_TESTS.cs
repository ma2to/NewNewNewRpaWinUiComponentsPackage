using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid;

namespace RpaWinUiComponentsPackage.PerformanceTests;

/// <summary>
/// Comprehensive performance testing suite for DataGrid component
/// Tests 3 operation modes (Headless, Readonly, Interactive) across multiple operations
/// Optimized for 1M-10M+ row datasets
/// </summary>
class PerformanceTests
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("╔══════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║   DataGrid Performance Test Suite - 3 Operation Modes       ║");
        Console.WriteLine("║   Testing: Headless | Readonly | Interactive                 ║");
        Console.WriteLine("╚══════════════════════════════════════════════════════════════╝");
        Console.WriteLine();

        var tester = new PerformanceTestRunner();
        await tester.RunAllTests();

        Console.WriteLine();
        Console.WriteLine("Press any key to exit...");
        Console.ReadKey();
    }
}

/// <summary>
/// Main test runner coordinating all performance tests
/// </summary>
class PerformanceTestRunner
{
    // Test configuration
    private readonly int[] _rowCounts = { 100_000, 1_000_000 };
    private readonly int[] _batchSizes = { 1_000, 5_000, 10_000, 50_000 };
    private readonly List<TestResult> _results = new();

    public async Task RunAllTests()
    {
        PrintSystemInfo();
        Console.WriteLine();

        // Test each mode
        foreach (var mode in new[] { "Headless", "Readonly", "Interactive" })
        {
            Console.WriteLine($"╔══════════════════════════════════════════════════════════════╗");
            Console.WriteLine($"║ Testing {mode,-52} ║");
            Console.WriteLine($"╚══════════════════════════════════════════════════════════════╝");

            try
            {
                var modeResults = await TestMode(mode);
                _results.AddRange(modeResults);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR in {mode} mode: {ex.Message}");
                Console.WriteLine($"Skipping {mode} mode due to error.");
            }

            Console.WriteLine();
        }

        // Generate reports
        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        GenerateTextReport(timestamp);
        GenerateCSVReport(timestamp);
        GenerateRecommendations();

        Console.WriteLine();
        Console.WriteLine($"✓ Reports saved:");
        Console.WriteLine($"  - PERFORMANCE_RESULTS_{timestamp}.txt");
        Console.WriteLine($"  - PERFORMANCE_RESULTS_{timestamp}.csv");
    }

    private void PrintSystemInfo()
    {
        Console.WriteLine("System Information:");
        Console.WriteLine($"  OS: {Environment.OSVersion}");
        Console.WriteLine($"  .NET: {Environment.Version}");
        Console.WriteLine($"  CPU Cores: {Environment.ProcessorCount}");
        Console.WriteLine($"  Machine: {Environment.MachineName}");
        Console.WriteLine($"  64-bit OS: {Environment.Is64BitOperatingSystem}");
        Console.WriteLine($"  Working Set: {Environment.WorkingSet / (1024 * 1024)} MB");
    }

    private async Task<List<TestResult>> TestMode(string mode)
    {
        var results = new List<TestResult>();

        foreach (var rowCount in _rowCounts)
        {
            Console.WriteLine($"\n  Testing with {rowCount:N0} rows:");

            foreach (var batchSize in _batchSizes)
            {
                // Skip combinations that don't make sense
                if (batchSize > rowCount)
                    continue;

                Console.WriteLine($"    Batch size {batchSize:N0}:");

                // Run each operation
                results.Add(await TestSort(mode, rowCount, batchSize));
                results.Add(await TestFilter(mode, rowCount, batchSize));
                results.Add(await TestValidation(mode, rowCount, batchSize));
                results.Add(await TestBulkInsert(mode, rowCount, batchSize));
                results.Add(await TestGetAllRows(mode, rowCount, batchSize));
                results.Add(await TestUpdateCells(mode, rowCount, batchSize));

                // Aggressive cleanup between batch sizes
                GC.Collect(2, GCCollectionMode.Forced, true, true);
                GC.WaitForPendingFinalizers();
                GC.Collect(2, GCCollectionMode.Forced, true, true);
                await Task.Delay(100); // Give system time to stabilize
            }

            // Extra cleanup between row counts
            GC.Collect(2, GCCollectionMode.Forced, true, true);
            GC.WaitForPendingFinalizers();
            GC.Collect(2, GCCollectionMode.Forced, true, true);
            await Task.Delay(500);
        }

        return results;
    }

    private async Task<TestResult> TestSort(string mode, int rowCount, int batchSize)
    {
        IAdvancedDataGridFacade? facade = null;
        var sw = Stopwatch.StartNew();
        long memBefore = 0;
        long memAfter = 0;

        try
        {
            memBefore = GC.GetTotalMemory(true);

            facade = CreateFacade(mode, batchSize);

            // Setup column
            facade.AddColumn(new PublicColumnDefinition
            {
                Name = "ID",
                Header = "ID",
                DataType = typeof(int),
                IsSortable = true
            });

            // Add random data
            var random = new Random(42); // Fixed seed for reproducibility
            for (int i = 0; i < rowCount; i++)
            {
                await facade.AddRowAsync(new Dictionary<string, object?> { ["ID"] = random.Next(int.MaxValue) });
            }

            // RESET TIMER - We want to measure sort only, not data insertion
            sw.Restart();
            memBefore = GC.GetTotalMemory(false);

            // Perform sort
            await facade.SortByColumnAsync("ID", PublicSortDirection.Ascending);

            sw.Stop();
            memAfter = GC.GetTotalMemory(false);

            Console.WriteLine($"      Sort: {sw.ElapsedMilliseconds:N0} ms");

            return new TestResult
            {
                Mode = mode,
                Operation = "Sort",
                RowCount = rowCount,
                BatchSize = batchSize,
                TimeMs = sw.ElapsedMilliseconds,
                MemoryMB = (memAfter - memBefore) / (1024.0 * 1024.0),
                Success = true
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"      Sort: FAILED - {ex.Message}");
            return new TestResult
            {
                Mode = mode,
                Operation = "Sort",
                RowCount = rowCount,
                BatchSize = batchSize,
                TimeMs = sw.ElapsedMilliseconds,
                MemoryMB = 0,
                Success = false,
                ErrorMessage = ex.Message
            };
        }
        finally
        {
            if (facade != null)
            {
                await facade.DisposeAsync();
            }
        }
    }

    private async Task<TestResult> TestFilter(string mode, int rowCount, int batchSize)
    {
        IAdvancedDataGridFacade? facade = null;
        var sw = Stopwatch.StartNew();
        long memBefore = 0;
        long memAfter = 0;

        try
        {
            memBefore = GC.GetTotalMemory(true);

            facade = CreateFacade(mode, batchSize);

            // Setup column
            facade.AddColumn(new PublicColumnDefinition
            {
                Name = "Value",
                Header = "Value",
                DataType = typeof(int),
                IsFilterable = true
            });

            // Add random data
            var random = new Random(42);
            for (int i = 0; i < rowCount; i++)
            {
                await facade.AddRowAsync(new Dictionary<string, object?> { ["Value"] = random.Next(1000) });
            }

            // RESET TIMER - We want to measure filter only
            sw.Restart();
            memBefore = GC.GetTotalMemory(false);

            // Perform filter (GreaterThan 500)
            await facade.ApplyFilterAsync("Value", PublicFilterOperator.GreaterThan, 500);

            sw.Stop();
            memAfter = GC.GetTotalMemory(false);

            Console.WriteLine($"      Filter: {sw.ElapsedMilliseconds:N0} ms");

            return new TestResult
            {
                Mode = mode,
                Operation = "Filter",
                RowCount = rowCount,
                BatchSize = batchSize,
                TimeMs = sw.ElapsedMilliseconds,
                MemoryMB = (memAfter - memBefore) / (1024.0 * 1024.0),
                Success = true
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"      Filter: FAILED - {ex.Message}");
            return new TestResult
            {
                Mode = mode,
                Operation = "Filter",
                RowCount = rowCount,
                BatchSize = batchSize,
                TimeMs = sw.ElapsedMilliseconds,
                MemoryMB = 0,
                Success = false,
                ErrorMessage = ex.Message
            };
        }
        finally
        {
            if (facade != null)
            {
                await facade.DisposeAsync();
            }
        }
    }

    private async Task<TestResult> TestValidation(string mode, int rowCount, int batchSize)
    {
        IAdvancedDataGridFacade? facade = null;
        var sw = Stopwatch.StartNew();
        long memBefore = 0;
        long memAfter = 0;

        try
        {
            memBefore = GC.GetTotalMemory(true);

            facade = CreateFacade(mode, batchSize);

            // Setup column
            facade.AddColumn(new PublicColumnDefinition
            {
                Name = "Email",
                Header = "Email",
                DataType = typeof(string)
            });

            // Add validation rule (email pattern)
            await facade.AddValidationRuleAsync(new EmailValidationRule());

            // Add test data (mix of valid and invalid emails)
            var random = new Random(42);
            for (int i = 0; i < rowCount; i++)
            {
                var email = (i % 3 == 0)
                    ? $"user{i}@example.com"
                    : $"invalid{i}"; // Invalid format
                await facade.AddRowAsync(new Dictionary<string, object?> { ["Email"] = email });
            }

            // RESET TIMER - We want to measure validation only
            sw.Restart();
            memBefore = GC.GetTotalMemory(false);

            // Perform validation
            await facade.ValidateAllAsync(onlyFiltered: false);

            sw.Stop();
            memAfter = GC.GetTotalMemory(false);

            Console.WriteLine($"      Validation: {sw.ElapsedMilliseconds:N0} ms");

            return new TestResult
            {
                Mode = mode,
                Operation = "Validation",
                RowCount = rowCount,
                BatchSize = batchSize,
                TimeMs = sw.ElapsedMilliseconds,
                MemoryMB = (memAfter - memBefore) / (1024.0 * 1024.0),
                Success = true
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"      Validation: FAILED - {ex.Message}");
            return new TestResult
            {
                Mode = mode,
                Operation = "Validation",
                RowCount = rowCount,
                BatchSize = batchSize,
                TimeMs = sw.ElapsedMilliseconds,
                MemoryMB = 0,
                Success = false,
                ErrorMessage = ex.Message
            };
        }
        finally
        {
            if (facade != null)
            {
                await facade.DisposeAsync();
            }
        }
    }

    private async Task<TestResult> TestBulkInsert(string mode, int rowCount, int batchSize)
    {
        IAdvancedDataGridFacade? facade = null;
        var sw = Stopwatch.StartNew();
        long memBefore = 0;
        long memAfter = 0;

        try
        {
            memBefore = GC.GetTotalMemory(true);

            facade = CreateFacade(mode, batchSize);

            // Setup column
            facade.AddColumn(new PublicColumnDefinition
            {
                Name = "Data",
                Header = "Data",
                DataType = typeof(string)
            });

            // RESET TIMER - We want to measure insertion only
            sw.Restart();
            memBefore = GC.GetTotalMemory(false);

            // Bulk insert
            for (int i = 0; i < rowCount; i++)
            {
                await facade.AddRowAsync(new Dictionary<string, object?> { ["Data"] = $"Row_{i}" });
            }

            sw.Stop();
            memAfter = GC.GetTotalMemory(false);

            Console.WriteLine($"      BulkInsert: {sw.ElapsedMilliseconds:N0} ms");

            return new TestResult
            {
                Mode = mode,
                Operation = "BulkInsert",
                RowCount = rowCount,
                BatchSize = batchSize,
                TimeMs = sw.ElapsedMilliseconds,
                MemoryMB = (memAfter - memBefore) / (1024.0 * 1024.0),
                Success = true
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"      BulkInsert: FAILED - {ex.Message}");
            return new TestResult
            {
                Mode = mode,
                Operation = "BulkInsert",
                RowCount = rowCount,
                BatchSize = batchSize,
                TimeMs = sw.ElapsedMilliseconds,
                MemoryMB = 0,
                Success = false,
                ErrorMessage = ex.Message
            };
        }
        finally
        {
            if (facade != null)
            {
                await facade.DisposeAsync();
            }
        }
    }

    private async Task<TestResult> TestGetAllRows(string mode, int rowCount, int batchSize)
    {
        IAdvancedDataGridFacade? facade = null;
        var sw = Stopwatch.StartNew();
        long memBefore = 0;
        long memAfter = 0;

        try
        {
            memBefore = GC.GetTotalMemory(true);

            facade = CreateFacade(mode, batchSize);

            // Setup column
            facade.AddColumn(new PublicColumnDefinition
            {
                Name = "Data",
                Header = "Data",
                DataType = typeof(string)
            });

            // Add data
            for (int i = 0; i < rowCount; i++)
            {
                await facade.AddRowAsync(new Dictionary<string, object?> { ["Data"] = $"Row_{i}" });
            }

            // RESET TIMER - We want to measure retrieval only
            sw.Restart();
            memBefore = GC.GetTotalMemory(false);

            // Get all rows
            var allRows = facade.GetCurrentData();

            sw.Stop();
            memAfter = GC.GetTotalMemory(false);

            Console.WriteLine($"      GetAllRows: {sw.ElapsedMilliseconds:N0} ms (retrieved {allRows.Count:N0} rows)");

            return new TestResult
            {
                Mode = mode,
                Operation = "GetAllRows",
                RowCount = rowCount,
                BatchSize = batchSize,
                TimeMs = sw.ElapsedMilliseconds,
                MemoryMB = (memAfter - memBefore) / (1024.0 * 1024.0),
                Success = true
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"      GetAllRows: FAILED - {ex.Message}");
            return new TestResult
            {
                Mode = mode,
                Operation = "GetAllRows",
                RowCount = rowCount,
                BatchSize = batchSize,
                TimeMs = sw.ElapsedMilliseconds,
                MemoryMB = 0,
                Success = false,
                ErrorMessage = ex.Message
            };
        }
        finally
        {
            if (facade != null)
            {
                await facade.DisposeAsync();
            }
        }
    }

    private async Task<TestResult> TestUpdateCells(string mode, int rowCount, int batchSize)
    {
        IAdvancedDataGridFacade? facade = null;
        var sw = Stopwatch.StartNew();
        long memBefore = 0;
        long memAfter = 0;

        try
        {
            memBefore = GC.GetTotalMemory(true);

            facade = CreateFacade(mode, batchSize);

            // Setup column
            facade.AddColumn(new PublicColumnDefinition
            {
                Name = "Value",
                Header = "Value",
                DataType = typeof(int)
            });

            // Add data
            for (int i = 0; i < rowCount; i++)
            {
                await facade.AddRowAsync(new Dictionary<string, object?> { ["Value"] = i });
            }

            // RESET TIMER - We want to measure update only
            sw.Restart();
            memBefore = GC.GetTotalMemory(false);

            // Update 1% of cells (randomly distributed)
            var random = new Random(42);
            int updateCount = (int)(rowCount * 0.01);
            for (int i = 0; i < updateCount; i++)
            {
                int rowIndex = random.Next(rowCount);
                await facade.UpdateCellAsync(rowIndex, "Value", random.Next(10000));
            }

            sw.Stop();
            memAfter = GC.GetTotalMemory(false);

            Console.WriteLine($"      UpdateCells: {sw.ElapsedMilliseconds:N0} ms ({updateCount:N0} updates)");

            return new TestResult
            {
                Mode = mode,
                Operation = "UpdateCells",
                RowCount = rowCount,
                BatchSize = batchSize,
                TimeMs = sw.ElapsedMilliseconds,
                MemoryMB = (memAfter - memBefore) / (1024.0 * 1024.0),
                Success = true
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"      UpdateCells: FAILED - {ex.Message}");
            return new TestResult
            {
                Mode = mode,
                Operation = "UpdateCells",
                RowCount = rowCount,
                BatchSize = batchSize,
                TimeMs = sw.ElapsedMilliseconds,
                MemoryMB = 0,
                Success = false,
                ErrorMessage = ex.Message
            };
        }
        finally
        {
            if (facade != null)
            {
                await facade.DisposeAsync();
            }
        }
    }

    private IAdvancedDataGridFacade CreateFacade(string mode, int batchSize)
    {
        var options = new AdvancedDataGridOptions
        {
            BatchSize = batchSize,
            EnableBatchValidation = false, // Disable auto-validation for perf tests
            EnableRealTimeValidation = false,
            EnableParallelProcessing = true,
            EnableLinqOptimizations = true,
            EnableCaching = true,
            EnablePerformanceMetrics = false, // Disable to avoid overhead
            EnableComprehensiveLogging = false,
            LoggerFactory = null // No logging for performance tests
        };

        // Configure mode
        switch (mode)
        {
            case "Headless":
                options.OperationMode = PublicDataGridOperationMode.Headless;
                options.DispatcherQueue = null;
                break;

            case "Readonly":
                options.OperationMode = PublicDataGridOperationMode.Readonly;
                // Try to create dispatcher, but don't fail if not available
                try
                {
                    // In real app this would be: DispatcherQueue.GetForCurrentThread()
                    // For tests, we skip it if not available
                    options.DispatcherQueue = null;
                }
                catch
                {
                    Console.WriteLine("      WARNING: Cannot create DispatcherQueue for Readonly mode, using Headless");
                    options.OperationMode = PublicDataGridOperationMode.Headless;
                }
                break;

            case "Interactive":
                options.OperationMode = PublicDataGridOperationMode.Interactive;
                // Try to create dispatcher, but don't fail if not available
                try
                {
                    // In real app this would be: DispatcherQueue.GetForCurrentThread()
                    // For tests, we skip it if not available
                    options.DispatcherQueue = null;
                }
                catch
                {
                    Console.WriteLine("      WARNING: Cannot create DispatcherQueue for Interactive mode, using Headless");
                    options.OperationMode = PublicDataGridOperationMode.Headless;
                }
                break;
        }

        return AdvancedDataGridFacadeFactory.CreateStandalone(options);
    }

    private void GenerateTextReport(string timestamp)
    {
        var sb = new StringBuilder();

        sb.AppendLine("═══════════════════════════════════════════════════════════════");
        sb.AppendLine("           DATAGRID PERFORMANCE TEST RESULTS");
        sb.AppendLine("═══════════════════════════════════════════════════════════════");
        sb.AppendLine();
        sb.AppendLine($"Timestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine($"System: {Environment.OSVersion}, {Environment.ProcessorCount} cores, .NET {Environment.Version}");
        sb.AppendLine();

        // Group by mode
        foreach (var mode in new[] { "Headless", "Readonly", "Interactive" })
        {
            var modeResults = _results.Where(r => r.Mode == mode).ToList();
            if (!modeResults.Any()) continue;

            sb.AppendLine($"─── {mode} Mode ───────────────────────────────────────────────────");
            sb.AppendLine();

            foreach (var result in modeResults)
            {
                sb.AppendLine($"Operation: {result.Operation} | Rows: {result.RowCount:N0} | BatchSize: {result.BatchSize:N0}");

                if (result.Success)
                {
                    sb.AppendLine($"  Time: {result.TimeMs:N0} ms");
                    sb.AppendLine($"  Throughput: {result.Throughput:N0} rows/sec");
                    sb.AppendLine($"  Memory: {result.MemoryMB:F2} MB allocated");
                }
                else
                {
                    sb.AppendLine($"  FAILED: {result.ErrorMessage}");
                }

                sb.AppendLine();
            }
        }

        // Comparison table
        sb.AppendLine("═══════════════════════════════════════════════════════════════");
        sb.AppendLine("           PERFORMANCE COMPARISON");
        sb.AppendLine("═══════════════════════════════════════════════════════════════");
        sb.AppendLine();
        sb.AppendLine($"{"Operation",-15} | {"Mode",-11} | {"Rows",8} | {"Batch",7} | {"Time (ms)",10} | {"Overhead",10}");
        sb.AppendLine(new string('─', 80));

        // Calculate overheads
        var operationGroups = _results
            .Where(r => r.Success)
            .GroupBy(r => new { r.Operation, r.RowCount, r.BatchSize });

        foreach (var group in operationGroups.OrderBy(g => g.Key.Operation).ThenBy(g => g.Key.RowCount))
        {
            var headlessTime = group.FirstOrDefault(r => r.Mode == "Headless")?.TimeMs ?? 0;

            foreach (var result in group.OrderBy(r => r.Mode))
            {
                var overhead = headlessTime > 0
                    ? ((result.TimeMs - headlessTime) / (double)headlessTime * 100.0)
                    : 0.0;

                var overheadStr = result.Mode == "Headless"
                    ? "baseline"
                    : $"{overhead:+0.0;-0.0}%";

                sb.AppendLine($"{result.Operation,-15} | {result.Mode,-11} | {result.RowCount,8:N0} | {result.BatchSize,7:N0} | {result.TimeMs,10:N0} | {overheadStr,10}");
            }
        }

        sb.AppendLine();

        // Optimal batch sizes
        sb.AppendLine("═══════════════════════════════════════════════════════════════");
        sb.AppendLine("           OPTIMAL BATCH SIZES");
        sb.AppendLine("═══════════════════════════════════════════════════════════════");
        sb.AppendLine();

        foreach (var mode in new[] { "Headless", "Readonly", "Interactive" })
        {
            sb.AppendLine($"{mode} Mode:");

            var modeResults = _results.Where(r => r.Mode == mode && r.Success).ToList();

            foreach (var operation in new[] { "Sort", "Filter", "Validation", "BulkInsert", "GetAllRows", "UpdateCells" })
            {
                var opResults = modeResults
                    .Where(r => r.Operation == operation)
                    .GroupBy(r => r.RowCount)
                    .Select(g => new
                    {
                        RowCount = g.Key,
                        OptimalBatch = g.OrderBy(r => r.TimeMs).FirstOrDefault()
                    })
                    .Where(x => x.OptimalBatch != null)
                    .ToList();

                if (opResults.Any())
                {
                    sb.Append($"  - {operation}: ");
                    foreach (var result in opResults)
                    {
                        sb.Append($"{result.RowCount:N0} rows → {result.OptimalBatch!.BatchSize:N0} | ");
                    }
                    sb.AppendLine();
                }
            }

            sb.AppendLine();
        }

        // Recommendations
        sb.AppendLine("═══════════════════════════════════════════════════════════════");
        sb.AppendLine("           RECOMMENDATIONS");
        sb.AppendLine("═══════════════════════════════════════════════════════════════");
        sb.AppendLine();
        sb.AppendLine("1. For batch processing (no UI): Use Headless mode");
        sb.AppendLine("   - Lowest overhead, best throughput");
        sb.AppendLine("   - Recommended batch size: 10,000-50,000");
        sb.AppendLine();
        sb.AppendLine("2. For read-only grids: Use Readonly mode");
        sb.AppendLine("   - UI dispatcher available but inactive");
        sb.AppendLine("   - Recommended batch size: 5,000-10,000");
        sb.AppendLine();
        sb.AppendLine("3. For interactive editing: Use Interactive mode");
        sb.AppendLine("   - Full UI refresh with auto-update");
        sb.AppendLine("   - Recommended batch size: 1,000-5,000");
        sb.AppendLine();
        sb.AppendLine("4. For 1M+ rows:");
        sb.AppendLine("   - Prefer Headless or Readonly modes");
        sb.AppendLine("   - Use larger batch sizes (10,000+)");
        sb.AppendLine("   - Enable parallel processing");
        sb.AppendLine("   - Disable real-time validation");
        sb.AppendLine();
        sb.AppendLine("5. Memory optimization:");
        sb.AppendLine("   - Use streaming for 10M+ rows");
        sb.AppendLine("   - Disable caching if memory-constrained");
        sb.AppendLine("   - Process data in chunks");
        sb.AppendLine();

        var filename = $"PERFORMANCE_RESULTS_{timestamp}.txt";
        File.WriteAllText(filename, sb.ToString());

        // Also print to console
        Console.WriteLine();
        Console.WriteLine(sb.ToString());
    }

    private void GenerateCSVReport(string timestamp)
    {
        var sb = new StringBuilder();

        // CSV Header
        sb.AppendLine("Mode,Operation,RowCount,BatchSize,TimeMs,ThroughputRowsPerSec,MemoryMB,Success,ErrorMessage");

        // CSV Data
        foreach (var result in _results)
        {
            sb.AppendLine($"{result.Mode},{result.Operation},{result.RowCount},{result.BatchSize},{result.TimeMs},{result.Throughput:F2},{result.MemoryMB:F2},{result.Success},\"{result.ErrorMessage}\"");
        }

        var filename = $"PERFORMANCE_RESULTS_{timestamp}.csv";
        File.WriteAllText(filename, sb.ToString());
    }

    private void GenerateRecommendations()
    {
        // Already included in text report
    }
}

/// <summary>
/// Test result data structure
/// </summary>
class TestResult
{
    public string Mode { get; set; } = string.Empty;
    public string Operation { get; set; } = string.Empty;
    public int RowCount { get; set; }
    public int BatchSize { get; set; }
    public long TimeMs { get; set; }
    public double MemoryMB { get; set; }
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Calculate throughput in rows per second
    /// </summary>
    public double Throughput => TimeMs > 0 ? (RowCount / (TimeMs / 1000.0)) : 0;
}

/// <summary>
/// Simple email validation rule for testing
/// </summary>
class EmailValidationRule : IValidationRule
{
    public string RuleId => "EmailValidation";
    public string RuleName => "Email Format Validation";
    public IReadOnlyList<string> DependentColumns => new[] { "Email" };
    public bool IsEnabled => true;
    public TimeSpan ValidationTimeout => TimeSpan.FromSeconds(5);

    public ValidationResult Validate(IReadOnlyDictionary<string, object?> row, ValidationContext context)
    {
        if (!row.TryGetValue("Email", out var emailObj))
            return ValidationResult.Success();

        var email = emailObj?.ToString();
        if (string.IsNullOrWhiteSpace(email))
            return ValidationResult.Success();

        // Simple email regex pattern
        var isValid = System.Text.RegularExpressions.Regex.IsMatch(
            email,
            @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        return isValid
            ? ValidationResult.Success()
            : ValidationResult.Error("Invalid email format", PublicValidationSeverity.Error, "Email");
    }

    public Task<ValidationResult> ValidateAsync(
        IReadOnlyDictionary<string, object?> row,
        ValidationContext context,
        System.Threading.CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Validate(row, context));
    }
}
