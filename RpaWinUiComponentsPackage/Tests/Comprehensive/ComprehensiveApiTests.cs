using RpaWinUiComponentsPackage.Tests.TestInfrastructure;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Api.Models;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Common.Models;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid;

namespace RpaWinUiComponentsPackage.Tests.Comprehensive;

/// <summary>
/// Comprehensive API tests covering all public methods with performance metrics
/// </summary>
public class ComprehensiveApiTests : TestBase
{
    private readonly List<TestResult> _results = new();

    public async Task<List<TestResult>> RunAllTests()
    {
        Console.WriteLine("╔════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║     COMPREHENSIVE API TESTS - ALL PUBLIC METHODS          ║");
        Console.WriteLine("╚════════════════════════════════════════════════════════════╝");
        Console.WriteLine();

        // Row Operations (CRUD)
        await TestAddRowAsync();
        await TestAddRowsBatchAsync();
        await TestUpdateRowAsync();
        await TestRemoveRowAsync();
        await TestGetRow();
        await TestClearAllRowsAsync();

        // Import/Export
        await TestImportAsync();
        await TestExportAsync();

        // Column Operations
        await TestAddColumn();
        await TestRemoveColumn();
        await TestUpdateColumn();
        await TestGetColumn();

        // Sorting
        await TestSortAsync();
        await TestMultiSortAsync();
        await TestQuickSort();
        await TestSortByColumnAsync();
        await TestClearSortingAsync();

        // Filtering
        await TestApplyFilterAsync();
        await TestClearFiltersAsync();

        // Searching
        await TestSearchAsync();
        await TestAdvancedSearchAsync();
        await TestQuickSearch();

        // Validation
        await TestAddValidationRuleAsync();
        await TestValidateAllAsync();
        await TestValidateAllWithStatisticsAsync();
        await TestRemoveValidationRulesAsync();

        // Selection
        await TestSelectRowAsync();
        await TestClearSelectionAsync();
        await TestSelectCell();

        // Data Access
        await TestGetCurrentData();
        await TestGetCurrentDataAsDataTableAsync();
        await TestGetRowCount();
        await TestGetVisibleRowCount();

        // Column Resize
        await TestResizeColumn();
        await TestGetColumnWidth();

        // MVVM
        await TestAdaptToRowViewModel();
        await TestAdaptToColumnViewModel();

        PrintSummary();
        return _results;
    }

    private async Task TestAddRowAsync()
    {
        var result = await MeasureAsync("AddRowAsync", "Row Operations", async () =>
        {
            var facade = CreateFacade();
            SetupColumns(facade);

            var rowData = new Dictionary<string, object?>
            {
                ["ID"] = 1,
                ["Name"] = "Test Row",
                ["Value"] = 100.0
            };

            var index = await facade.AddRowAsync(rowData);
            if (index < 0) throw new Exception("Failed to add row");
        });

        _results.Add(result);
        PrintResult(result);
    }

    private async Task TestAddRowsBatchAsync()
    {
        var result = await MeasureAsync("AddRowsBatchAsync (10K rows)", "Row Operations", async () =>
        {
            var facade = CreateFacade();
            SetupColumns(facade);

            var rows = GenerateTestData(10000);
            var count = await facade.AddRowsBatchAsync(rows);
            if (count != 10000) throw new Exception($"Expected 10000 rows, got {count}");
        }, 10000);

        _results.Add(result);
        PrintResult(result);
    }

    private async Task TestUpdateRowAsync()
    {
        var result = await MeasureAsync("UpdateRowAsync", "Row Operations", async () =>
        {
            var facade = CreateFacade();
            SetupColumns(facade);
            await facade.AddRowsBatchAsync(GenerateTestData(100));

            var success = await facade.UpdateRowAsync(50, new Dictionary<string, object?>
            {
                ["ID"] = 50,
                ["Name"] = "Updated",
                ["Value"] = 999.0
            });

            if (!success) throw new Exception("Failed to update row");
        });

        _results.Add(result);
        PrintResult(result);
    }

    private async Task TestRemoveRowAsync()
    {
        var result = await MeasureAsync("RemoveRowAsync", "Row Operations", async () =>
        {
            var facade = CreateFacade();
            SetupColumns(facade);
            await facade.AddRowsBatchAsync(GenerateTestData(100));

            var success = await facade.RemoveRowAsync(50);
            if (!success) throw new Exception("Failed to remove row");
        });

        _results.Add(result);
        PrintResult(result);
    }

    private async Task TestGetRow()
    {
        var result = await MeasureAsync("GetRow", "Row Operations", async () =>
        {
            var facade = CreateFacade();
            SetupColumns(facade);
            await facade.AddRowsBatchAsync(GenerateTestData(1000));

            var row = facade.GetRow(500);
            if (row == null) throw new Exception("Row not found");
            await Task.CompletedTask;
        });

        _results.Add(result);
        PrintResult(result);
    }

    private async Task TestClearAllRowsAsync()
    {
        var result = await MeasureAsync("ClearAllRowsAsync", "Row Operations", async () =>
        {
            var facade = CreateFacade();
            SetupColumns(facade);
            await facade.AddRowsBatchAsync(GenerateTestData(10000));

            await facade.ClearAllRowsAsync();
            if (facade.GetRowCount() != 0) throw new Exception("Rows not cleared");
        });

        _results.Add(result);
        PrintResult(result);
    }

    private async Task TestImportAsync()
    {
        var result = await MeasureAsync("ImportAsync (50K rows)", "Import/Export", async () =>
        {
            var facade = CreateFacade();
            SetupColumns(facade);

            var command = ImportDataCommand.FromDictionaries(GenerateTestData(50000), PublicImportMode.Replace);

            var importResult = await facade.ImportAsync(command);
            if (!importResult.IsSuccess) throw new Exception(importResult.ErrorMessages.FirstOrDefault() ?? "Import failed");
        }, 50000);

        _results.Add(result);
        PrintResult(result);
    }

    private async Task TestExportAsync()
    {
        var result = await MeasureAsync("ExportAsync (10K rows)", "Import/Export", async () =>
        {
            var facade = CreateFacade();
            SetupColumns(facade);
            await facade.AddRowsBatchAsync(GenerateTestData(10000));

            var command = ExportDataCommand.ToDictionary(exportOnlyFiltered: false);

            var exportResult = await facade.ExportAsync(command);
            if (!exportResult.IsSuccess) throw new Exception(exportResult.ErrorMessages.FirstOrDefault() ?? "Export failed");
        });

        _results.Add(result);
        PrintResult(result);
    }

    private async Task TestAddColumn()
    {
        var result = await MeasureAsync("AddColumn", "Column Operations", async () =>
        {
            var facade = CreateFacade();

            var success = facade.AddColumn(new PublicColumnDefinition
            {
                Name = "TestCol",
                Header = "Test Column",
                DataType = typeof(string),
                IsVisible = true
            });

            if (!success) throw new Exception("Failed to add column");
            await Task.CompletedTask;
        });

        _results.Add(result);
        PrintResult(result);
    }

    private async Task TestRemoveColumn()
    {
        var result = await MeasureAsync("RemoveColumn", "Column Operations", async () =>
        {
            var facade = CreateFacade();
            SetupColumns(facade);

            var success = facade.RemoveColumn("Name");
            if (!success) throw new Exception("Failed to remove column");
            await Task.CompletedTask;
        });

        _results.Add(result);
        PrintResult(result);
    }

    private async Task TestUpdateColumn()
    {
        var result = await MeasureAsync("UpdateColumn", "Column Operations", async () =>
        {
            var facade = CreateFacade();
            SetupColumns(facade);

            var success = facade.UpdateColumn(new PublicColumnDefinition
            {
                Name = "Name",
                Header = "Updated Name",
                DataType = typeof(string),
                IsVisible = true
            });

            if (!success) throw new Exception("Failed to update column");
            await Task.CompletedTask;
        });

        _results.Add(result);
        PrintResult(result);
    }

    private async Task TestGetColumn()
    {
        var result = await MeasureAsync("GetColumn", "Column Operations", async () =>
        {
            var facade = CreateFacade();
            SetupColumns(facade);

            var col = facade.GetColumn("Name");
            if (col == null) throw new Exception("Column not found");
            await Task.CompletedTask;
        });

        _results.Add(result);
        PrintResult(result);
    }

    private async Task TestSortAsync()
    {
        var result = await MeasureAsync("SortAsync (10K rows)", "Sorting", async () =>
        {
            var facade = CreateFacade();
            SetupColumns(facade);
            await facade.AddRowsBatchAsync(GenerateTestData(10000));

            var command = new SortDataCommand
            {
                Data = facade.GetCurrentData(),
                ColumnName = "Value",
                Direction = PublicSortDirection.Ascending
            };

            var sortResult = await facade.SortAsync(command);
            if (!sortResult.IsSuccess) throw new Exception(sortResult.ErrorMessages?.FirstOrDefault() ?? "Sort failed");
        });

        _results.Add(result);
        PrintResult(result);
    }

    private async Task TestMultiSortAsync()
    {
        var result = await MeasureAsync("MultiSortAsync (10K rows)", "Sorting", async () =>
        {
            var facade = CreateFacade();
            SetupColumns(facade);
            await facade.AddRowsBatchAsync(GenerateTestData(10000));

            var command = new MultiSortDataCommand(
                Data: facade.GetCurrentData(),
                SortColumns: new[]
                {
                    new SortColumnConfig("Category", PublicSortDirection.Ascending),
                    new SortColumnConfig("Value", PublicSortDirection.Descending)
                }
            );

            var sortResult = await facade.MultiSortAsync(command);
            if (!sortResult.IsSuccess) throw new Exception(sortResult.ErrorMessages?.FirstOrDefault() ?? "Sort failed");
        });

        _results.Add(result);
        PrintResult(result);
    }

    private async Task TestQuickSort()
    {
        var result = await MeasureAsync("QuickSort (10K rows)", "Sorting", async () =>
        {
            var facade = CreateFacade();
            var data = GenerateTestData(10000);

            var sortResult = facade.QuickSort(data, "Value", PublicSortDirection.Ascending);
            if (!sortResult.IsSuccess) throw new Exception(sortResult.ErrorMessages?.FirstOrDefault() ?? "Sort failed");
            await Task.CompletedTask;
        });

        _results.Add(result);
        PrintResult(result);
    }

    private async Task TestSortByColumnAsync()
    {
        var result = await MeasureAsync("SortByColumnAsync (10K rows)", "Sorting", async () =>
        {
            var facade = CreateFacade();
            SetupColumns(facade);
            await facade.AddRowsBatchAsync(GenerateTestData(10000));

            var sortResult = await facade.SortByColumnAsync("Value", PublicSortDirection.Ascending);
            if (!sortResult.IsSuccess) throw new Exception(sortResult.ErrorMessage ?? "Sort failed");
        });

        _results.Add(result);
        PrintResult(result);
    }

    private async Task TestClearSortingAsync()
    {
        var result = await MeasureAsync("ClearSortingAsync", "Sorting", async () =>
        {
            var facade = CreateFacade();
            SetupColumns(facade);
            await facade.AddRowsBatchAsync(GenerateTestData(10000));
            await facade.SortByColumnAsync("Value", PublicSortDirection.Ascending);

            var clearResult = await facade.ClearSortingAsync();
            if (!clearResult.IsSuccess) throw new Exception(clearResult.ErrorMessage ?? "Sort failed");
        });

        _results.Add(result);
        PrintResult(result);
    }

    private async Task TestApplyFilterAsync()
    {
        var result = await MeasureAsync("ApplyFilterAsync (10K rows)", "Filtering", async () =>
        {
            var facade = CreateFacade();
            SetupColumns(facade);
            await facade.AddRowsBatchAsync(GenerateTestData(10000));

            var count = await facade.ApplyFilterAsync("Value", PublicFilterOperator.GreaterThan, 500.0);
            if (count < 0) throw new Exception("Filter failed");
        });

        _results.Add(result);
        PrintResult(result);
    }

    private async Task TestClearFiltersAsync()
    {
        var result = await MeasureAsync("ClearFiltersAsync", "Filtering", async () =>
        {
            var facade = CreateFacade();
            SetupColumns(facade);
            await facade.AddRowsBatchAsync(GenerateTestData(10000));
            await facade.ApplyFilterAsync("Value", PublicFilterOperator.GreaterThan, 500.0);

            var count = await facade.ClearFiltersAsync();
            if (count != 10000) throw new Exception("Clear filter failed");
        });

        _results.Add(result);
        PrintResult(result);
    }

    private async Task TestSearchAsync()
    {
        var result = await MeasureAsync("SearchAsync (10K rows)", "Searching", async () =>
        {
            var facade = CreateFacade();
            SetupColumns(facade);
            await facade.AddRowsBatchAsync(GenerateTestData(10000));

            var command = new SearchDataCommand
            {
                Data = facade.GetCurrentData(),
                SearchText = "Row_100",
                CaseSensitive = false
            };

            var searchResult = await facade.SearchAsync(command);
            if (!searchResult.IsSuccess) throw new Exception(searchResult.ErrorMessages?.FirstOrDefault() ?? "Search failed");
        });

        _results.Add(result);
        PrintResult(result);
    }

    private async Task TestAdvancedSearchAsync()
    {
        var result = await MeasureAsync("AdvancedSearchAsync (10K rows)", "Searching", async () =>
        {
            var facade = CreateFacade();
            SetupColumns(facade);
            await facade.AddRowsBatchAsync(GenerateTestData(10000));

            var command = new AdvancedSearchDataCommand
            {
                Data = facade.GetCurrentData(),
                SearchCriteria = new PublicAdvancedSearchCriteria
                {
                    SearchText = "Row",
                    CaseSensitive = false,
                    UseRegex = false,
                    TargetColumns = new[] { "Name" }
                }
            };

            var searchResult = await facade.AdvancedSearchAsync(command);
            if (!searchResult.IsSuccess) throw new Exception(searchResult.ErrorMessages?.FirstOrDefault() ?? "Search failed");
        });

        _results.Add(result);
        PrintResult(result);
    }

    private async Task TestQuickSearch()
    {
        var result = await MeasureAsync("QuickSearch (10K rows)", "Searching", async () =>
        {
            var facade = CreateFacade();
            var data = GenerateTestData(10000);

            var searchResult = facade.QuickSearch(data, "Row_100", false);
            if (!searchResult.IsSuccess) throw new Exception(searchResult.ErrorMessages?.FirstOrDefault() ?? "Search failed");
            await Task.CompletedTask;
        });

        _results.Add(result);
        PrintResult(result);
    }

    private async Task TestAddValidationRuleAsync()
    {
        var result = await MeasureAsync("AddValidationRuleAsync", "Validation", async () =>
        {
            var facade = CreateFacade(enableValidation: true);

            // Add column with validation rule
            facade.AddColumn(new PublicColumnDefinition
            {
                Name = "Name",
                Header = "Name",
                DataType = typeof(string),
                IsVisible = true,
                ValidationRules = new List<PublicValidationRule>
                {
                    new() { RuleType = PublicValidationRuleType.Regex, RegexPattern = "^Row_\\d+$", ErrorMessage = "Invalid format" }
                }
            });

            await Task.CompletedTask;
        });

        _results.Add(result);
        PrintResult(result);
    }

    private async Task TestValidateAllAsync()
    {
        var result = await MeasureAsync("ValidateAllAsync (1K rows)", "Validation", async () =>
        {
            var facade = CreateFacade(enableValidation: true);

            // Add column with validation rule
            facade.AddColumn(new PublicColumnDefinition
            {
                Name = "Name",
                Header = "Name",
                DataType = typeof(string),
                IsVisible = true,
                ValidationRules = new List<PublicValidationRule>
                {
                    new() { RuleType = PublicValidationRuleType.Regex, RegexPattern = "^Row_\\d+$", ErrorMessage = "Invalid format" }
                }
            });

            await facade.AddRowsBatchAsync(GenerateTestData(1000));

            var validateResult = await facade.ValidateAllAsync();
            if (!validateResult.IsSuccess) throw new Exception(validateResult.ErrorMessage ?? "Validation failed");
        });

        _results.Add(result);
        PrintResult(result);
    }

    private async Task TestValidateAllWithStatisticsAsync()
    {
        var result = await MeasureAsync("ValidateAllWithStatisticsAsync (1K rows)", "Validation", async () =>
        {
            var facade = CreateFacade(enableValidation: true);

            // Add column with validation rule
            facade.AddColumn(new PublicColumnDefinition
            {
                Name = "Name",
                Header = "Name",
                DataType = typeof(string),
                IsVisible = true,
                ValidationRules = new List<PublicValidationRule>
                {
                    new() { RuleType = PublicValidationRuleType.Regex, RegexPattern = "^Row_\\d+$", ErrorMessage = "Invalid format" }
                }
            });

            await facade.AddRowsBatchAsync(GenerateTestData(1000));

            var validateResult = await facade.ValidateAllWithStatisticsAsync();
            if (!validateResult.IsValid) throw new Exception("Validation with statistics failed");
        });

        _results.Add(result);
        PrintResult(result);
    }

    private async Task TestRemoveValidationRulesAsync()
    {
        var result = await MeasureAsync("RemoveValidationRulesAsync", "Validation", async () =>
        {
            var facade = CreateFacade(enableValidation: true);

            // Add column with validation rule
            facade.AddColumn(new PublicColumnDefinition
            {
                Name = "Name",
                Header = "Name",
                DataType = typeof(string),
                IsVisible = true,
                ValidationRules = new List<PublicValidationRule>
                {
                    new() { RuleType = PublicValidationRuleType.Regex, RegexPattern = "^Row_\\d+$", ErrorMessage = "Invalid format" }
                }
            });

            var removeResult = await facade.RemoveValidationRulesAsync("Name");
            if (!removeResult.IsSuccess) throw new Exception(removeResult.ErrorMessage ?? "Remove validation rules failed");
        });

        _results.Add(result);
        PrintResult(result);
    }

    private async Task TestSelectRowAsync()
    {
        var result = await MeasureAsync("SelectRowAsync", "Selection", async () =>
        {
            var facade = CreateFacade();
            SetupColumns(facade);
            await facade.AddRowsBatchAsync(GenerateTestData(1000));

            await facade.SelectRowAsync(500);
        });

        _results.Add(result);
        PrintResult(result);
    }

    private async Task TestClearSelectionAsync()
    {
        var result = await MeasureAsync("ClearSelectionAsync", "Selection", async () =>
        {
            var facade = CreateFacade();
            SetupColumns(facade);
            await facade.AddRowsBatchAsync(GenerateTestData(1000));
            await facade.SelectRowAsync(500);

            await facade.ClearSelectionAsync();
        });

        _results.Add(result);
        PrintResult(result);
    }

    private async Task TestSelectCell()
    {
        var result = await MeasureAsync("SelectCell", "Selection", async () =>
        {
            var facade = CreateFacade();
            SetupColumns(facade);
            await facade.AddRowsBatchAsync(GenerateTestData(1000));

            facade.SelectCell(500, 2);
            await Task.CompletedTask;
        });

        _results.Add(result);
        PrintResult(result);
    }

    private async Task TestGetCurrentData()
    {
        var result = await MeasureAsync("GetCurrentData (10K rows)", "Data Access", async () =>
        {
            var facade = CreateFacade();
            SetupColumns(facade);
            await facade.AddRowsBatchAsync(GenerateTestData(10000));

            var data = facade.GetCurrentData();
            if (data.Count != 10000) throw new Exception($"Expected 10000 rows, got {data.Count}");
        });

        _results.Add(result);
        PrintResult(result);
    }

    private async Task TestGetCurrentDataAsDataTableAsync()
    {
        var result = await MeasureAsync("GetCurrentDataAsDataTableAsync (10K rows)", "Data Access", async () =>
        {
            var facade = CreateFacade();
            SetupColumns(facade);
            await facade.AddRowsBatchAsync(GenerateTestData(10000));

            var dt = await facade.GetCurrentDataAsDataTableAsync();
            if (dt.Rows.Count != 10000) throw new Exception($"Expected 10000 rows, got {dt.Rows.Count}");
        });

        _results.Add(result);
        PrintResult(result);
    }

    private async Task TestGetRowCount()
    {
        var result = await MeasureAsync("GetRowCount", "Data Access", async () =>
        {
            var facade = CreateFacade();
            SetupColumns(facade);
            await facade.AddRowsBatchAsync(GenerateTestData(10000));

            var count = facade.GetRowCount();
            if (count != 10000) throw new Exception($"Expected 10000, got {count}");
        });

        _results.Add(result);
        PrintResult(result);
    }

    private async Task TestGetVisibleRowCount()
    {
        var result = await MeasureAsync("GetVisibleRowCount", "Data Access", async () =>
        {
            var facade = CreateFacade();
            SetupColumns(facade);
            await facade.AddRowsBatchAsync(GenerateTestData(10000));

            var count = facade.GetVisibleRowCount();
            if (count != 10000) throw new Exception($"Expected 10000, got {count}");
        });

        _results.Add(result);
        PrintResult(result);
    }

    private async Task TestResizeColumn()
    {
        var result = await MeasureAsync("ResizeColumn", "Column Resize", async () =>
        {
            var facade = CreateFacade();
            SetupColumns(facade);

            var width = facade.ResizeColumn(0, 200.0);
            if (width != 200.0) throw new Exception($"Expected width 200, got {width}");
            await Task.CompletedTask;
        });

        _results.Add(result);
        PrintResult(result);
    }

    private async Task TestGetColumnWidth()
    {
        var result = await MeasureAsync("GetColumnWidth", "Column Resize", async () =>
        {
            var facade = CreateFacade();
            SetupColumns(facade);
            facade.ResizeColumn(0, 200.0);

            var width = facade.GetColumnWidth(0);
            if (width != 200.0) throw new Exception($"Expected width 200, got {width}");
            await Task.CompletedTask;
        });

        _results.Add(result);
        PrintResult(result);
    }

    private async Task TestAdaptToRowViewModel()
    {
        var result = await MeasureAsync("AdaptToRowViewModel", "MVVM", async () =>
        {
            var facade = CreateFacade();
            SetupColumns(facade);

            var rowData = new Dictionary<string, object?>
            {
                ["ID"] = 1,
                ["Name"] = "Test",
                ["Value"] = 100.0
            };

            var vm = facade.AdaptToRowViewModel(rowData, 0);
            if (vm == null) throw new Exception("Failed to adapt");
            await Task.CompletedTask;
        });

        _results.Add(result);
        PrintResult(result);
    }

    private async Task TestAdaptToColumnViewModel()
    {
        var result = await MeasureAsync("AdaptToColumnViewModel", "MVVM", async () =>
        {
            var facade = CreateFacade();

            var colDef = new PublicColumnDefinition
            {
                Name = "Test",
                Header = "Test",
                DataType = typeof(string),
                IsVisible = true
            };

            var vm = facade.AdaptToColumnViewModel(colDef);
            if (vm == null) throw new Exception("Failed to adapt");
            await Task.CompletedTask;
        });

        _results.Add(result);
        PrintResult(result);
    }

    private void PrintResult(TestResult result)
    {
        var status = result.Success ? "✓ PASS" : "✗ FAIL";
        var color = result.Success ? ConsoleColor.Green : ConsoleColor.Red;

        Console.ForegroundColor = color;
        Console.Write($"[{status}] ");
        Console.ResetColor();

        Console.Write($"{result.Category,-15} | {result.TestName,-40} | ");
        Console.Write($"{result.Metrics.Duration.TotalMilliseconds,8:F2}ms | ");
        Console.Write($"{result.Metrics.MemoryUsedMB,8:F2}MB");

        if (result.Metrics.ThroughputPerSecond > 0)
        {
            Console.Write($" | {result.Metrics.ThroughputPerSecond,10:N0} ops/s");
        }

        Console.WriteLine();

        if (!result.Success && result.ErrorMessage != null)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"    Error: {result.ErrorMessage}");
            Console.ResetColor();
        }
    }

    private void PrintSummary()
    {
        Console.WriteLine();
        Console.WriteLine("╔════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║                     TEST SUMMARY                           ║");
        Console.WriteLine("╚════════════════════════════════════════════════════════════╝");
        Console.WriteLine();

        var totalTests = _results.Count;
        var passedTests = _results.Count(r => r.Success);
        var failedTests = _results.Count(r => !r.Success);
        var totalDuration = TimeSpan.FromMilliseconds(_results.Sum(r => r.Metrics.Duration.TotalMilliseconds));
        var totalMemory = _results.Sum(r => r.Metrics.MemoryUsedMB);

        Console.WriteLine($"Total Tests:    {totalTests}");
        Console.WriteLine($"Passed:         {passedTests} ({(double)passedTests / totalTests * 100:F1}%)");
        Console.WriteLine($"Failed:         {failedTests} ({(double)failedTests / totalTests * 100:F1}%)");
        Console.WriteLine($"Total Duration: {totalDuration.TotalSeconds:F2}s");
        Console.WriteLine($"Total Memory:   {totalMemory:F2}MB");
        Console.WriteLine();

        // Category breakdown
        var byCategory = _results.GroupBy(r => r.Category);
        Console.WriteLine("Tests by Category:");
        foreach (var category in byCategory)
        {
            var catPassed = category.Count(r => r.Success);
            var catTotal = category.Count();
            Console.WriteLine($"  {category.Key,-20}: {catPassed}/{catTotal} passed");
        }
        Console.WriteLine();

        // Top 5 slowest tests
        Console.WriteLine("Top 5 Slowest Tests:");
        var slowest = _results.OrderByDescending(r => r.Metrics.Duration).Take(5);
        foreach (var test in slowest)
        {
            Console.WriteLine($"  {test.TestName,-40}: {test.Metrics.Duration.TotalMilliseconds,8:F2}ms");
        }
        Console.WriteLine();

        // Top 5 memory intensive tests
        Console.WriteLine("Top 5 Memory Intensive Tests:");
        var memoryIntensive = _results.OrderByDescending(r => r.Metrics.MemoryUsedMB).Take(5);
        foreach (var test in memoryIntensive)
        {
            Console.WriteLine($"  {test.TestName,-40}: {test.Metrics.MemoryUsedMB,8:F2}MB");
        }
        Console.WriteLine();
    }
}
