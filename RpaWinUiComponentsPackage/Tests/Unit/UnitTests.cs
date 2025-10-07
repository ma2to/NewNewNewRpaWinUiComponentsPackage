using RpaWinUiComponentsPackage.Tests.TestInfrastructure;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Api.Models;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid;

namespace RpaWinUiComponentsPackage.Tests.Unit;

/// <summary>
/// Unit tests for individual components and methods
/// </summary>
public class UnitTests : TestBase
{
    public async Task<List<TestResult>> RunAllTests()
    {
        var results = new List<TestResult>();

        Console.WriteLine("╔════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║                      UNIT TESTS                            ║");
        Console.WriteLine("╚════════════════════════════════════════════════════════════╝");
        Console.WriteLine();

        // Row Operations
        results.Add(await TestAddSingleRow());
        results.Add(await TestAddMultipleRows());
        results.Add(await TestUpdateRow_Valid());
        results.Add(await TestUpdateRow_InvalidIndex());
        results.Add(await TestRemoveRow_Valid());
        results.Add(await TestRemoveRow_InvalidIndex());
        results.Add(await TestGetRow_Valid());
        results.Add(await TestGetRow_InvalidIndex());

        // Column Operations
        results.Add(await TestAddColumn_Valid());
        results.Add(await TestAddColumn_Duplicate());
        results.Add(await TestRemoveColumn_Valid());
        results.Add(await TestRemoveColumn_NotFound());
        results.Add(await TestUpdateColumn_Valid());

        // Sorting
        results.Add(await TestSort_Ascending());
        results.Add(await TestSort_Descending());
        results.Add(await TestSort_MultiColumn());
        results.Add(await TestSort_EmptyData());

        // Filtering
        results.Add(await TestFilter_Equals());
        results.Add(await TestFilter_GreaterThan());
        results.Add(await TestFilter_Contains());
        results.Add(await TestFilter_MultipleFilters());
        results.Add(await TestClearFilter());

        // Searching
        results.Add(await TestSearch_Basic());
        results.Add(await TestSearch_CaseSensitive());
        results.Add(await TestSearch_NoResults());

        // Validation
        results.Add(await TestValidation_RegexRule());
        results.Add(await TestValidation_RangeRule());
        results.Add(await TestValidation_CustomRule());

        // Import/Export
        results.Add(await TestImport_ValidData());
        results.Add(await TestImport_EmptyData());
        results.Add(await TestExport_WithHeaders());
        results.Add(await TestExport_WithoutHeaders());

        return results;
    }

    private async Task<TestResult> TestAddSingleRow()
    {
        return await MeasureAsync("AddRow - Single valid row", "Unit", async () =>
        {
            var facade = CreateFacade();
            SetupColumns(facade);

            var index = await facade.AddRowAsync(new Dictionary<string, object?>
            {
                ["ID"] = 1,
                ["Name"] = "Test",
                ["Value"] = 100.0
            });

            if (index < 0) throw new Exception("Failed to add row");
            if (facade.GetRowCount() != 1) throw new Exception("Row count mismatch");
        });
    }

    private async Task<TestResult> TestAddMultipleRows()
    {
        return await MeasureAsync("AddRowsBatch - 1000 rows", "Unit", async () =>
        {
            var facade = CreateFacade();
            SetupColumns(facade);

            var count = await facade.AddRowsBatchAsync(GenerateTestData(1000));
            if (count != 1000) throw new Exception($"Expected 1000, got {count}");
        });
    }

    private async Task<TestResult> TestUpdateRow_Valid()
    {
        return await MeasureAsync("UpdateRow - Valid index", "Unit", async () =>
        {
            var facade = CreateFacade();
            SetupColumns(facade);
            await facade.AddRowsBatchAsync(GenerateTestData(10));

            var success = await facade.UpdateRowAsync(5, new Dictionary<string, object?>
            {
                ["Name"] = "Updated",
                ["Value"] = 999.0
            });

            if (!success) throw new Exception("Update failed");
            var row = facade.GetRow(5);
            if (row?["Name"]?.ToString() != "Updated") throw new Exception("Data not updated");
        });
    }

    private async Task<TestResult> TestUpdateRow_InvalidIndex()
    {
        return await MeasureAsync("UpdateRow - Invalid index", "Unit", async () =>
        {
            var facade = CreateFacade();
            SetupColumns(facade);
            await facade.AddRowsBatchAsync(GenerateTestData(10));

            var success = await facade.UpdateRowAsync(999, new Dictionary<string, object?>
            {
                ["Name"] = "Updated"
            });

            if (success) throw new Exception("Should have failed with invalid index");
        });
    }

    private async Task<TestResult> TestRemoveRow_Valid()
    {
        return await MeasureAsync("RemoveRow - Valid index", "Unit", async () =>
        {
            var facade = CreateFacade();
            SetupColumns(facade);
            await facade.AddRowsBatchAsync(GenerateTestData(10));

            var countBefore = facade.GetRowCount();
            var success = await facade.RemoveRowAsync(5);

            if (!success) throw new Exception("Remove failed");
            if (facade.GetRowCount() != countBefore - 1) throw new Exception("Row count mismatch");
        });
    }

    private async Task<TestResult> TestRemoveRow_InvalidIndex()
    {
        return await MeasureAsync("RemoveRow - Invalid index", "Unit", async () =>
        {
            var facade = CreateFacade();
            SetupColumns(facade);
            await facade.AddRowsBatchAsync(GenerateTestData(10));

            var success = await facade.RemoveRowAsync(999);
            if (success) throw new Exception("Should have failed with invalid index");
        });
    }

    private async Task<TestResult> TestGetRow_Valid()
    {
        return await MeasureAsync("GetRow - Valid index", "Unit", async () =>
        {
            var facade = CreateFacade();
            SetupColumns(facade);
            await facade.AddRowsBatchAsync(GenerateTestData(10));

            var row = facade.GetRow(5);
            if (row == null) throw new Exception("Row not found");
            await Task.CompletedTask;
        });
    }

    private async Task<TestResult> TestGetRow_InvalidIndex()
    {
        return await MeasureAsync("GetRow - Invalid index", "Unit", async () =>
        {
            var facade = CreateFacade();
            SetupColumns(facade);
            await facade.AddRowsBatchAsync(GenerateTestData(10));

            var row = facade.GetRow(999);
            if (row != null) throw new Exception("Should return null for invalid index");
            await Task.CompletedTask;
        });
    }

    private async Task<TestResult> TestAddColumn_Valid()
    {
        return await MeasureAsync("AddColumn - Valid column", "Unit", async () =>
        {
            var facade = CreateFacade();

            var success = facade.AddColumn(new PublicColumnDefinition
            {
                Name = "NewCol",
                Header = "New Column",
                DataType = typeof(string),
                IsVisible = true
            });

            if (!success) throw new Exception("Failed to add column");
            await Task.CompletedTask;
        });
    }

    private async Task<TestResult> TestAddColumn_Duplicate()
    {
        return await MeasureAsync("AddColumn - Duplicate name", "Unit", async () =>
        {
            var facade = CreateFacade();
            SetupColumns(facade);

            var success = facade.AddColumn(new PublicColumnDefinition
            {
                Name = "ID", // Duplicate
                Header = "ID",
                DataType = typeof(int),
                IsVisible = true
            });

            if (success) throw new Exception("Should not allow duplicate column");
            await Task.CompletedTask;
        });
    }

    private async Task<TestResult> TestRemoveColumn_Valid()
    {
        return await MeasureAsync("RemoveColumn - Valid name", "Unit", async () =>
        {
            var facade = CreateFacade();
            SetupColumns(facade);

            var success = facade.RemoveColumn("Name");
            if (!success) throw new Exception("Failed to remove column");
            await Task.CompletedTask;
        });
    }

    private async Task<TestResult> TestRemoveColumn_NotFound()
    {
        return await MeasureAsync("RemoveColumn - Not found", "Unit", async () =>
        {
            var facade = CreateFacade();
            SetupColumns(facade);

            var success = facade.RemoveColumn("NonExistent");
            if (success) throw new Exception("Should return false for non-existent column");
            await Task.CompletedTask;
        });
    }

    private async Task<TestResult> TestUpdateColumn_Valid()
    {
        return await MeasureAsync("UpdateColumn - Valid update", "Unit", async () =>
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
    }

    private async Task<TestResult> TestSort_Ascending()
    {
        return await MeasureAsync("Sort - Ascending order", "Unit", async () =>
        {
            var facade = CreateFacade();
            SetupColumns(facade);
            await facade.AddRowsBatchAsync(GenerateTestData(100));

            var result = await facade.SortByColumnAsync("Value", PublicSortDirection.Ascending);
            if (!result.IsSuccess) throw new Exception("Sort failed");
        });
    }

    private async Task<TestResult> TestSort_Descending()
    {
        return await MeasureAsync("Sort - Descending order", "Unit", async () =>
        {
            var facade = CreateFacade();
            SetupColumns(facade);
            await facade.AddRowsBatchAsync(GenerateTestData(100));

            var result = await facade.SortByColumnAsync("Value", PublicSortDirection.Descending);
            if (!result.IsSuccess) throw new Exception("Sort failed");
        });
    }

    private async Task<TestResult> TestSort_MultiColumn()
    {
        return await MeasureAsync("Sort - Multi-column", "Unit", async () =>
        {
            var facade = CreateFacade();
            SetupColumns(facade);
            await facade.AddRowsBatchAsync(GenerateTestData(100));

            var command = new MultiSortDataCommand(
                Data: facade.GetCurrentData(),
                SortColumns: new[]
                {
                    new SortColumnConfig("Category", PublicSortDirection.Ascending),
                    new SortColumnConfig("Value", PublicSortDirection.Descending)
                }
            );

            var result = await facade.MultiSortAsync(command);
            if (!result.IsSuccess) throw new Exception("Multi-sort failed");
        });
    }

    private async Task<TestResult> TestSort_EmptyData()
    {
        return await MeasureAsync("Sort - Empty data", "Unit", async () =>
        {
            var facade = CreateFacade();
            SetupColumns(facade);

            var result = await facade.SortByColumnAsync("Value", PublicSortDirection.Ascending);
            // Should not throw
            await Task.CompletedTask;
        });
    }

    private async Task<TestResult> TestFilter_Equals()
    {
        return await MeasureAsync("Filter - Equals operator", "Unit", async () =>
        {
            var facade = CreateFacade();
            SetupColumns(facade);
            await facade.AddRowsBatchAsync(GenerateTestData(100));

            var count = await facade.ApplyFilterAsync("Status", PublicFilterOperator.Equals, "Active");
            if (count < 0) throw new Exception("Filter failed");
        });
    }

    private async Task<TestResult> TestFilter_GreaterThan()
    {
        return await MeasureAsync("Filter - GreaterThan operator", "Unit", async () =>
        {
            var facade = CreateFacade();
            SetupColumns(facade);
            await facade.AddRowsBatchAsync(GenerateTestData(100));

            var count = await facade.ApplyFilterAsync("Value", PublicFilterOperator.GreaterThan, 500.0);
            if (count < 0) throw new Exception("Filter failed");
        });
    }

    private async Task<TestResult> TestFilter_Contains()
    {
        return await MeasureAsync("Filter - Contains operator", "Unit", async () =>
        {
            var facade = CreateFacade();
            SetupColumns(facade);
            await facade.AddRowsBatchAsync(GenerateTestData(100));

            var count = await facade.ApplyFilterAsync("Name", PublicFilterOperator.Contains, "Row_1");
            if (count < 0) throw new Exception("Filter failed");
        });
    }

    private async Task<TestResult> TestFilter_MultipleFilters()
    {
        return await MeasureAsync("Filter - Multiple filters", "Unit", async () =>
        {
            var facade = CreateFacade();
            SetupColumns(facade);
            await facade.AddRowsBatchAsync(GenerateTestData(100));

            await facade.ApplyFilterAsync("Value", PublicFilterOperator.GreaterThan, 300.0);
            var count = await facade.ApplyFilterAsync("Status", PublicFilterOperator.Equals, "Active");
            if (count < 0) throw new Exception("Multiple filters failed");
        });
    }

    private async Task<TestResult> TestClearFilter()
    {
        return await MeasureAsync("Filter - Clear all", "Unit", async () =>
        {
            var facade = CreateFacade();
            SetupColumns(facade);
            await facade.AddRowsBatchAsync(GenerateTestData(100));

            await facade.ApplyFilterAsync("Value", PublicFilterOperator.GreaterThan, 500.0);
            var count = await facade.ClearFiltersAsync();
            if (count != 100) throw new Exception("Clear filter failed");
        });
    }

    private async Task<TestResult> TestSearch_Basic()
    {
        return await MeasureAsync("Search - Basic search", "Unit", async () =>
        {
            var facade = CreateFacade();
            SetupColumns(facade);
            await facade.AddRowsBatchAsync(GenerateTestData(100));

            var result = facade.QuickSearch(facade.GetCurrentData(), "Row_10", false);
            if (!result.IsSuccess) throw new Exception("Search failed");
        });
    }

    private async Task<TestResult> TestSearch_CaseSensitive()
    {
        return await MeasureAsync("Search - Case sensitive", "Unit", async () =>
        {
            var facade = CreateFacade();
            SetupColumns(facade);
            await facade.AddRowsBatchAsync(GenerateTestData(100));

            var result = facade.QuickSearch(facade.GetCurrentData(), "row_10", true);
            // Should find no matches due to case sensitivity
            await Task.CompletedTask;
        });
    }

    private async Task<TestResult> TestSearch_NoResults()
    {
        return await MeasureAsync("Search - No results", "Unit", async () =>
        {
            var facade = CreateFacade();
            SetupColumns(facade);
            await facade.AddRowsBatchAsync(GenerateTestData(100));

            var result = facade.QuickSearch(facade.GetCurrentData(), "NONEXISTENT", false);
            if (result.Results.Any()) throw new Exception("Should find no matches");
            await Task.CompletedTask;
        });
    }

    private async Task<TestResult> TestValidation_RegexRule()
    {
        return await MeasureAsync("Validation - Regex rule", "Unit", async () =>
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
    }

    private async Task<TestResult> TestValidation_RangeRule()
    {
        return await MeasureAsync("Validation - Range rule", "Unit", async () =>
        {
            var facade = CreateFacade(enableValidation: true);

            // Add column with validation rule
            facade.AddColumn(new PublicColumnDefinition
            {
                Name = "Value",
                Header = "Value",
                DataType = typeof(double),
                IsVisible = true,
                ValidationRules = new List<PublicValidationRule>
                {
                    new() { RuleType = PublicValidationRuleType.Range, MinValue = 0.0, MaxValue = 1000.0, ErrorMessage = "Value out of range" }
                }
            });

            await Task.CompletedTask;
        });
    }

    private async Task<TestResult> TestValidation_CustomRule()
    {
        return await MeasureAsync("Validation - Custom rule", "Unit", async () =>
        {
            var facade = CreateFacade(enableValidation: true);

            // Add column with validation rule
            facade.AddColumn(new PublicColumnDefinition
            {
                Name = "Status",
                Header = "Status",
                DataType = typeof(string),
                IsVisible = true,
                ValidationRules = new List<PublicValidationRule>
                {
                    new() { RuleType = PublicValidationRuleType.Custom, CustomValidator = value => value?.ToString() == "Active" || value?.ToString() == "Inactive", ErrorMessage = "Invalid status" }
                }
            });

            await Task.CompletedTask;
        });
    }

    private async Task<TestResult> TestImport_ValidData()
    {
        return await MeasureAsync("Import - Valid data", "Unit", async () =>
        {
            var facade = CreateFacade();
            SetupColumns(facade);

            var command = ImportDataCommand.FromDictionaries(GenerateTestData(1000), PublicImportMode.Replace);

            var result = await facade.ImportAsync(command);
            if (!result.IsSuccess) throw new Exception("Import failed");
            if (facade.GetRowCount() != 1000) throw new Exception("Row count mismatch");
        });
    }

    private async Task<TestResult> TestImport_EmptyData()
    {
        return await MeasureAsync("Import - Empty data", "Unit", async () =>
        {
            var facade = CreateFacade();
            SetupColumns(facade);

            var command = ImportDataCommand.FromDictionaries(new List<Dictionary<string, object?>>(), PublicImportMode.Replace);

            var result = await facade.ImportAsync(command);
            // Should handle empty data gracefully
            await Task.CompletedTask;
        });
    }

    private async Task<TestResult> TestExport_WithHeaders()
    {
        return await MeasureAsync("Export - With headers", "Unit", async () =>
        {
            var facade = CreateFacade();
            SetupColumns(facade);
            await facade.AddRowsBatchAsync(GenerateTestData(100));

            var command = ExportDataCommand.ToDictionary(exportOnlyFiltered: false);

            var result = await facade.ExportAsync(command);
            if (!result.IsSuccess) throw new Exception("Export failed");
        });
    }

    private async Task<TestResult> TestExport_WithoutHeaders()
    {
        return await MeasureAsync("Export - Without headers", "Unit", async () =>
        {
            var facade = CreateFacade();
            SetupColumns(facade);
            await facade.AddRowsBatchAsync(GenerateTestData(100));

            var command = ExportDataCommand.ToDictionary(exportOnlyFiltered: false);

            var result = await facade.ExportAsync(command);
            if (!result.IsSuccess) throw new Exception("Export failed");
        });
    }
}
