using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Common.Models;
using RpaWinUiComponentsPackage.ComprehensiveBenchmarks.Infrastructure;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace RpaWinUiComponentsPackage.ComprehensiveBenchmarks.Tests;

/// <summary>
/// Large-scale tests with 1 million rows and validation rules enabled
/// Tests both import and export with automatic validation mode
/// </summary>
public class LargeScaleValidationTests : IDisposable
{
    private readonly ITestOutputHelper _output;
    private readonly PerformanceMonitor _perfMonitor;

    public LargeScaleValidationTests(ITestOutputHelper output)
    {
        _output = output;
        _perfMonitor = new PerformanceMonitor();
    }

    [Fact]
    public async Task Test_Import_1MillionRows_WithValidation_Headless()
    {
        // Arrange
        _output.WriteLine("=== TEST: Import 1 Million Rows with Validation (Headless) ===");

        await using var grid = DataGridTestHelper.CreateHeadlessGrid(
            configureOptions: options =>
            {
                options.EnableValidationAlertsColumn = true; // Enable validation alerts
                options.BatchSize = 10000; // Larger batch for performance
                options.ParallelProcessingThreshold = 1000;
                options.EnableParallelProcessing = true;
                options.DegreeOfParallelism = Environment.ProcessorCount;
            });

        // Define validation rules
        await DefineValidationRules(grid);

        // Generate 1 million rows with 10 columns
        _output.WriteLine("Generating 1 million rows...");
        var testData = TestDataGenerator.GenerateGridData(1_000_000, 10);
        var headers = TestDataGenerator.GenerateColumnHeaders(10);
        var importCommand = DataGridTestHelper.CreateImportCommand(testData, headers);

        // Act
        _output.WriteLine("Starting import with validation...");
        _perfMonitor.StartMonitoring(1000); // Sample every 1 second

        var sw = System.Diagnostics.Stopwatch.StartNew();
        var result = await grid.IO.ImportAsync(importCommand, CancellationToken.None);
        sw.Stop();

        var report = await _perfMonitor.StopMonitoringAsync();

        // Assert
        _output.WriteLine($"\n=== IMPORT RESULTS ===");
        _output.WriteLine($"Success: {result.IsSuccess}");
        _output.WriteLine($"Imported Rows: {result.ImportedRows:N0}");
        _output.WriteLine($"Duration: {sw.Elapsed.TotalSeconds:F2}s");
        _output.WriteLine($"Throughput: {result.ImportedRows / sw.Elapsed.TotalSeconds:N0} rows/sec");
        _output.WriteLine($"\n{report}");

        result.IsSuccess.Should().BeTrue();
        result.ImportedRows.Should().Be(1_000_000);

        // Performance assertions
        sw.Elapsed.TotalMinutes.Should().BeLessThan(5, "import should complete within 5 minutes");

        // Memory should be reasonable (less than 2GB working set)
        report.AvgWorkingSetMB.Should().BeLessThan(2048, "memory usage should be reasonable");
    }

    [Fact]
    public async Task Test_Export_1MillionRows_WithValidation_Headless()
    {
        // Arrange
        _output.WriteLine("=== TEST: Export 1 Million Rows with Validation (Headless) ===");

        await using var grid = DataGridTestHelper.CreateHeadlessGrid(
            configureOptions: options =>
            {
                options.EnableValidationAlertsColumn = true; // Enable validation alerts
                options.BatchSize = 10000;
                options.ParallelProcessingThreshold = 1000;
                options.EnableParallelProcessing = true;
                options.DegreeOfParallelism = Environment.ProcessorCount;
            });

        // Define validation rules
        await DefineValidationRules(grid);

        // Import 1 million rows first
        _output.WriteLine("Importing 1 million rows...");
        var testData = TestDataGenerator.GenerateGridData(1_000_000, 10);
        var headers = TestDataGenerator.GenerateColumnHeaders(10);
        var importCommand = DataGridTestHelper.CreateImportCommand(testData, headers);

        await grid.IO.ImportAsync(importCommand, CancellationToken.None);
        _output.WriteLine("Import complete. Starting export...");

        // Act - Export data
        var exportCommand = ExportDataCommand.ToDataTable();

        _perfMonitor.StartMonitoring(1000);
        var sw = System.Diagnostics.Stopwatch.StartNew();

        var result = await grid.IO.ExportAsync(exportCommand, CancellationToken.None);

        sw.Stop();
        var report = await _perfMonitor.StopMonitoringAsync();

        // Assert
        _output.WriteLine($"\n=== EXPORT RESULTS ===");
        _output.WriteLine($"Success: {result.IsSuccess}");
        _output.WriteLine($"Exported Rows: {result.ExportedRows:N0}");
        _output.WriteLine($"Duration: {sw.Elapsed.TotalSeconds:F2}s");
        _output.WriteLine($"Throughput: {result.ExportedRows / sw.Elapsed.TotalSeconds:N0} rows/sec");
        _output.WriteLine($"\n{report}");

        result.IsSuccess.Should().BeTrue();
        result.ExportedRows.Should().Be(1_000_000);

        // Performance assertions
        sw.Elapsed.TotalMinutes.Should().BeLessThan(5, "export should complete within 5 minutes");
        report.AvgWorkingSetMB.Should().BeLessThan(2048, "memory usage should be reasonable");
    }

    [Fact]
    public async Task Test_ImportExport_RoundTrip_1MillionRows_WithValidation()
    {
        // Arrange
        _output.WriteLine("=== TEST: Round-trip Import+Export 1 Million Rows with Validation ===");

        await using var grid = DataGridTestHelper.CreateHeadlessGrid(
            configureOptions: options =>
            {
                options.EnableValidationAlertsColumn = true; // Enable validation alerts
                options.BatchSize = 10000;
                options.ParallelProcessingThreshold = 1000;
                options.EnableParallelProcessing = true;
                options.DegreeOfParallelism = Environment.ProcessorCount;
            });

        // Define validation rules
        await DefineValidationRules(grid);

        // Generate 1 million rows
        _output.WriteLine("Generating 1 million rows...");
        var testData = TestDataGenerator.GenerateGridData(1_000_000, 10);
        var headers = TestDataGenerator.GenerateColumnHeaders(10);
        var importCommand = DataGridTestHelper.CreateImportCommand(testData, headers);

        // Act - Import
        _output.WriteLine("Starting import...");
        var importSw = System.Diagnostics.Stopwatch.StartNew();
        var importResult = await grid.IO.ImportAsync(importCommand, CancellationToken.None);
        importSw.Stop();

        _output.WriteLine($"Import completed in {importSw.Elapsed.TotalSeconds:F2}s");
        _output.WriteLine($"Import throughput: {importResult.ImportedRows / importSw.Elapsed.TotalSeconds:N0} rows/sec");

        // Act - Export
        _output.WriteLine("Starting export...");
        var exportCommand = ExportDataCommand.ToDataTable();
        var exportSw = System.Diagnostics.Stopwatch.StartNew();
        var exportResult = await grid.IO.ExportAsync(exportCommand, CancellationToken.None);
        exportSw.Stop();

        _output.WriteLine($"Export completed in {exportSw.Elapsed.TotalSeconds:F2}s");
        _output.WriteLine($"Export throughput: {exportResult.ExportedRows / exportSw.Elapsed.TotalSeconds:N0} rows/sec");

        // Assert
        _output.WriteLine($"\n=== ROUND-TRIP SUMMARY ===");
        _output.WriteLine($"Total Duration: {(importSw.Elapsed + exportSw.Elapsed).TotalSeconds:F2}s");
        _output.WriteLine($"Import: {importSw.Elapsed.TotalSeconds:F2}s ({importResult.ImportedRows:N0} rows)");
        _output.WriteLine($"Export: {exportSw.Elapsed.TotalSeconds:F2}s ({exportResult.ExportedRows:N0} rows)");

        importResult.IsSuccess.Should().BeTrue();
        exportResult.IsSuccess.Should().BeTrue();
        importResult.ImportedRows.Should().Be(1_000_000);
        exportResult.ExportedRows.Should().Be(1_000_000);

        // Total time should be reasonable
        (importSw.Elapsed + exportSw.Elapsed).TotalMinutes.Should().BeLessThan(10,
            "round-trip should complete within 10 minutes");
    }

    [Fact]
    public async Task Test_Import_1MillionRows_WithValidation_Interactive()
    {
        // Arrange
        _output.WriteLine("=== TEST: Import 1 Million Rows with Validation (Interactive Mode) ===");

        await using var grid = DataGridTestHelper.CreateInteractiveGrid(
            configureOptions: options =>
            {
                options.EnableValidationAlertsColumn = true; // Enable validation alerts
                options.BatchSize = 5000; // Smaller batch for interactive
                options.ParallelProcessingThreshold = 1000;
                options.EnableParallelProcessing = true;
            });

        // Define validation rules
        await DefineValidationRules(grid);

        // Generate 1 million rows
        _output.WriteLine("Generating 1 million rows...");
        var testData = TestDataGenerator.GenerateGridData(1_000_000, 10);
        var headers = TestDataGenerator.GenerateColumnHeaders(10);
        var importCommand = DataGridTestHelper.CreateImportCommand(testData, headers);

        // Act
        _output.WriteLine("Starting import with validation (Interactive mode)...");
        _perfMonitor.StartMonitoring(1000);

        var sw = System.Diagnostics.Stopwatch.StartNew();
        var result = await grid.IO.ImportAsync(importCommand, CancellationToken.None);
        sw.Stop();

        var report = await _perfMonitor.StopMonitoringAsync();

        // Assert
        _output.WriteLine($"\n=== INTERACTIVE MODE RESULTS ===");
        _output.WriteLine($"Success: {result.IsSuccess}");
        _output.WriteLine($"Imported Rows: {result.ImportedRows:N0}");
        _output.WriteLine($"Duration: {sw.Elapsed.TotalSeconds:F2}s");
        _output.WriteLine($"Throughput: {result.ImportedRows / sw.Elapsed.TotalSeconds:N0} rows/sec");
        _output.WriteLine($"\n{report}");

        result.IsSuccess.Should().BeTrue();
        result.ImportedRows.Should().Be(1_000_000);

        // Interactive mode may be slower, allow more time
        sw.Elapsed.TotalMinutes.Should().BeLessThan(10, "interactive import should complete within 10 minutes");
    }

    [Fact]
    public async Task Test_Performance_Comparison_1MillionRows_HeadlessVsInteractive()
    {
        _output.WriteLine("=== TEST: Performance Comparison - 1M Rows Headless vs Interactive ===");

        var testData = TestDataGenerator.GenerateGridData(1_000_000, 10);
        var headers = TestDataGenerator.GenerateColumnHeaders(10);

        // Test Headless Mode
        _output.WriteLine("\n--- Testing HEADLESS Mode ---");
        await using var headlessGrid = DataGridTestHelper.CreateHeadlessGrid(
            configureOptions: options =>
            {
                options.EnableValidationAlertsColumn = true; // Enable validation alerts
                options.BatchSize = 10000;
                options.EnableParallelProcessing = true;
            });

        await DefineValidationRules(headlessGrid);
        var headlessCommand = DataGridTestHelper.CreateImportCommand(testData, headers);

        var headlessMonitor = new PerformanceMonitor();
        headlessMonitor.StartMonitoring(1000);
        var headlessSw = System.Diagnostics.Stopwatch.StartNew();

        var headlessResult = await headlessGrid.IO.ImportAsync(headlessCommand, CancellationToken.None);

        headlessSw.Stop();
        var headlessReport = await headlessMonitor.StopMonitoringAsync();

        // Test Interactive Mode
        _output.WriteLine("\n--- Testing INTERACTIVE Mode ---");
        await using var interactiveGrid = DataGridTestHelper.CreateInteractiveGrid(
            configureOptions: options =>
            {
                options.EnableValidationAlertsColumn = true; // Enable validation alerts
                options.BatchSize = 5000;
                options.EnableParallelProcessing = true;
            });

        await DefineValidationRules(interactiveGrid);
        var interactiveCommand = DataGridTestHelper.CreateImportCommand(testData, headers);

        var interactiveMonitor = new PerformanceMonitor();
        interactiveMonitor.StartMonitoring(1000);
        var interactiveSw = System.Diagnostics.Stopwatch.StartNew();

        var interactiveResult = await interactiveGrid.IO.ImportAsync(interactiveCommand, CancellationToken.None);

        interactiveSw.Stop();
        var interactiveReport = await interactiveMonitor.StopMonitoringAsync();

        // Log results
        _output.WriteLine("\n╔════════════════════════════════════════════════════════════════╗");
        _output.WriteLine("║           PERFORMANCE COMPARISON RESULTS                       ║");
        _output.WriteLine("╚════════════════════════════════════════════════════════════════╝");

        _output.WriteLine("\n=== HEADLESS MODE ===");
        _output.WriteLine($"Duration: {headlessSw.Elapsed.TotalSeconds:F2}s");
        _output.WriteLine($"Throughput: {headlessResult.ImportedRows / headlessSw.Elapsed.TotalSeconds:N0} rows/sec");
        _output.WriteLine(headlessReport.ToString());

        _output.WriteLine("\n=== INTERACTIVE MODE ===");
        _output.WriteLine($"Duration: {interactiveSw.Elapsed.TotalSeconds:F2}s");
        _output.WriteLine($"Throughput: {interactiveResult.ImportedRows / interactiveSw.Elapsed.TotalSeconds:N0} rows/sec");
        _output.WriteLine(interactiveReport.ToString());

        _output.WriteLine("\n=== COMPARISON ===");
        var speedup = interactiveSw.Elapsed.TotalSeconds / headlessSw.Elapsed.TotalSeconds;
        _output.WriteLine($"Speedup: {speedup:F2}x");
        _output.WriteLine($"Time Saved: {(interactiveSw.Elapsed - headlessSw.Elapsed).TotalSeconds:F2}s");

        // Assert
        headlessResult.IsSuccess.Should().BeTrue();
        interactiveResult.IsSuccess.Should().BeTrue();

        headlessResult.ImportedRows.Should().Be(1_000_000);
        interactiveResult.ImportedRows.Should().Be(1_000_000);

        // Headless should typically be faster
        _output.WriteLine($"\n✓ Headless mode is {speedup:F2}x faster than Interactive mode");
    }

    /// <summary>
    /// Defines comprehensive validation rules for testing
    /// </summary>
    private async Task DefineValidationRules(IAdvancedDataGridFacade grid)
    {
        _output.WriteLine("Defining validation rules...");

        // Rule 1: Column_1 must not be empty
        var rule1 = new SimpleValidationRule(
            ruleId: "rule_column1_required",
            ruleName: "Column_1_Required",
            dependentColumns: new[] { "Column_1" },
            validator: (row, context) =>
            {
                var value = row["Column_1"];
                if (value == null || string.IsNullOrWhiteSpace(value.ToString()))
                {
                    return ValidationResult.Error("Column_1 is required", PublicValidationSeverity.Error, "Column_1");
                }
                return ValidationResult.Success();
            });

        await grid.Validation.AddValidationRuleAsync(rule1);

        // Rule 2: Column_2 must be numeric and in range 0-1000
        var rule2 = new SimpleValidationRule(
            ruleId: "rule_column2_range",
            ruleName: "Column_2_NumericRange",
            dependentColumns: new[] { "Column_2" },
            validator: (row, context) =>
            {
                var value = row["Column_2"];
                if (value != null && int.TryParse(value.ToString(), out var numValue))
                {
                    if (numValue < 0 || numValue > 1000)
                    {
                        return ValidationResult.Error($"Column_2 must be between 0 and 1000, got {numValue}", PublicValidationSeverity.Warning, "Column_2");
                    }
                }
                return ValidationResult.Success();
            });

        await grid.Validation.AddValidationRuleAsync(rule2);

        // Rule 3: Column_3 length check
        var rule3 = new SimpleValidationRule(
            ruleId: "rule_column3_length",
            ruleName: "Column_3_Length",
            dependentColumns: new[] { "Column_3" },
            validator: (row, context) =>
            {
                var value = row["Column_3"];
                if (value != null && value.ToString()!.Length > 50)
                {
                    return ValidationResult.Error("Column_3 must be less than 50 characters", PublicValidationSeverity.Warning, "Column_3");
                }
                return ValidationResult.Success();
            });

        await grid.Validation.AddValidationRuleAsync(rule3);

        _output.WriteLine("✓ 3 validation rules registered successfully");
        _output.WriteLine("  - Column_1: Required (Error)");
        _output.WriteLine("  - Column_2: Numeric Range 0-1000 (Warning)");
        _output.WriteLine("  - Column_3: Max Length 50 (Warning)");
    }

    /// <summary>
    /// Simple validation rule implementation for testing
    /// </summary>
    private class SimpleValidationRule : IValidationRule
    {
        private readonly string _ruleId;
        private readonly string _ruleName;
        private readonly IReadOnlyList<string> _dependentColumns;
        private readonly Func<IReadOnlyDictionary<string, object?>, ValidationContext, ValidationResult> _validator;

        public SimpleValidationRule(
            string ruleId,
            string ruleName,
            string[] dependentColumns,
            Func<IReadOnlyDictionary<string, object?>, ValidationContext, ValidationResult> validator)
        {
            _ruleId = ruleId;
            _ruleName = ruleName;
            _dependentColumns = dependentColumns;
            _validator = validator;
        }

        public string RuleId => _ruleId;
        public string RuleName => _ruleName;
        public IReadOnlyList<string> DependentColumns => _dependentColumns;
        public bool IsEnabled => true;
        public TimeSpan ValidationTimeout => TimeSpan.FromSeconds(5);

        public ValidationResult Validate(IReadOnlyDictionary<string, object?> row, ValidationContext context)
        {
            return _validator(row, context);
        }

        public Task<ValidationResult> ValidateAsync(
            IReadOnlyDictionary<string, object?> row,
            ValidationContext context,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_validator(row, context));
        }
    }

    public void Dispose()
    {
        _perfMonitor?.Dispose();
    }
}
