using Microsoft.Extensions.Logging.Abstractions;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid;
//using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Api.Models;

namespace RpaWinUiComponentsPackage.Tests.Functional;

/// <summary>
/// Functional tests - verify all features work correctly
/// </summary>
public class FunctionalTests
{
    public async Task<List<TestResult>> RunAllTests()
    {
        var results = new List<TestResult>();

        results.Add(await TestColumnManagement());
        results.Add(await TestRowOperations());
        results.Add(await TestSortingFunctionality());
        results.Add(await TestFilteringFunctionality());
        results.Add(await TestSearchFunctionality());
        results.Add(await TestSelectionFunctionality());
        results.Add(await TestValidationFunctionality());
        results.Add(await TestCellEditing());

        return results;
    }

    private async Task<TestResult> TestColumnManagement()
    {
        try
        {
            var facade = CreateTestFacade();

            // Add column
            var col = new PublicColumnDefinition { Name = "Test", Header = "Test", DataType = typeof(string), IsVisible = true };
            facade.AddColumn(col);

            // Get column
            var retrieved = facade.GetColumn("Test");
            if (retrieved == null)
                throw new Exception("Failed to retrieve added column");

            // Success
            return new TestResult
            {
                Category = "Functional",
                Name = "ColumnManagement",
                Success = true,
                Details = "Add and retrieve column successful"
            };
        }
        catch (Exception ex)
        {
            return new TestResult { Category = "Functional", Name = "ColumnManagement", Success = false, Error = ex.Message };
        }
    }

    private async Task<TestResult> TestRowOperations()
    {
        try
        {
            var facade = CreateTestFacade();

            facade.AddColumn(new PublicColumnDefinition { Name = "ID", Header = "ID", DataType = typeof(int), IsVisible = true });
            facade.AddColumn(new PublicColumnDefinition { Name = "Name", Header = "Name", DataType = typeof(string), IsVisible = true });

            // Add rows
            await facade.AddRowAsync(new Dictionary<string, object?> { ["ID"] = 1, ["Name"] = "Row1" });
            await facade.AddRowAsync(new Dictionary<string, object?> { ["ID"] = 2, ["Name"] = "Row2" });

            var data = facade.GetCurrentData();
            if (data.Count != 2)
                throw new Exception($"Expected 2 rows, got {data.Count}");

            // Clear rows
            await facade.ClearAllRowsAsync();

            data = facade.GetCurrentData();
            if (data.Count != 0)
                throw new Exception($"Expected 0 rows after clear, got {data.Count}");

            return new TestResult { Category = "Functional", Name = "RowOperations", Success = true, Details = "Add and clear rows successful" };
        }
        catch (Exception ex)
        {
            return new TestResult { Category = "Functional", Name = "RowOperations", Success = false, Error = ex.Message };
        }
    }

    private async Task<TestResult> TestSortingFunctionality()
    {
        try
        {
            var facade = CreateTestFacade();

            facade.AddColumn(new PublicColumnDefinition { Name = "Value", Header = "Value", DataType = typeof(int), IsSortable = true, IsVisible = true });

            await facade.AddRowAsync(new Dictionary<string, object?> { ["Value"] = 3 });
            await facade.AddRowAsync(new Dictionary<string, object?> { ["Value"] = 1 });
            await facade.AddRowAsync(new Dictionary<string, object?> { ["Value"] = 2 });

            // Sort ascending
            await facade.SortByColumnAsync("Value", PublicSortDirection.Ascending);

            var data = facade.GetCurrentData();
            if ((int)data[0]["Value"]! != 1 || (int)data[1]["Value"]! != 2 || (int)data[2]["Value"]! != 3)
                throw new Exception("Ascending sort failed");

            // Sort descending
            await facade.SortByColumnAsync("Value", PublicSortDirection.Descending);

            data = facade.GetCurrentData();
            if ((int)data[0]["Value"]! != 3 || (int)data[1]["Value"]! != 2 || (int)data[2]["Value"]! != 1)
                throw new Exception("Descending sort failed");

            return new TestResult { Category = "Functional", Name = "SortingFunctionality", Success = true, Details = "Sort ascending and descending successful" };
        }
        catch (Exception ex)
        {
            return new TestResult { Category = "Functional", Name = "SortingFunctionality", Success = false, Error = ex.Message };
        }
    }

    private async Task<TestResult> TestFilteringFunctionality()
    {
        try
        {
            var facade = CreateTestFacade();

            facade.AddColumn(new PublicColumnDefinition { Name = "Value", Header = "Value", DataType = typeof(int), IsFilterable = true, IsVisible = true });

            await facade.AddRowAsync(new Dictionary<string, object?> { ["Value"] = 10 });
            await facade.AddRowAsync(new Dictionary<string, object?> { ["Value"] = 20 });
            await facade.AddRowAsync(new Dictionary<string, object?> { ["Value"] = 30 });

            // Filter > 15
            await facade.ApplyFilterAsync("Value", PublicFilterOperator.GreaterThan, 15);

            var data = facade.GetCurrentData();
            if (data.Count != 2)
                throw new Exception($"Expected 2 filtered rows, got {data.Count}");

            // Clear filter
            await facade.ClearFilterAsync();

            data = facade.GetCurrentData();
            if (data.Count != 3)
                throw new Exception($"Expected 3 rows after clearing filter, got {data.Count}");

            return new TestResult { Category = "Functional", Name = "FilteringFunctionality", Success = true, Details = "Filter and clear filter successful" };
        }
        catch (Exception ex)
        {
            return new TestResult { Category = "Functional", Name = "FilteringFunctionality", Success = false, Error = ex.Message };
        }
    }

    private async Task<TestResult> TestSearchFunctionality()
    {
        try
        {
            var facade = CreateTestFacade();

            facade.AddColumn(new PublicColumnDefinition { Name = "Name", Header = "Name", DataType = typeof(string), IsVisible = true });

            await facade.AddRowAsync(new Dictionary<string, object?> { ["Name"] = "Apple" });
            await facade.AddRowAsync(new Dictionary<string, object?> { ["Name"] = "Banana" });
            await facade.AddRowAsync(new Dictionary<string, object?> { ["Name"] = "Cherry" });

            // Search for "Ban"
            var command = new SearchDataCommand(
                facade.GetCurrentData(),
                "Ban",
                TargetColumns: new[] { "Name" }
            );
            var result = await facade.SearchAsync(command);

            if (!result.IsSuccess)
                throw new Exception("Search failed");

            if (result.TotalMatchesFound != 1)
                throw new Exception($"Expected 1 search result, got {result.TotalMatchesFound}");

            return new TestResult { Category = "Functional", Name = "SearchFunctionality", Success = true, Details = "Search successful" };
        }
        catch (Exception ex)
        {
            return new TestResult { Category = "Functional", Name = "SearchFunctionality", Success = false, Error = ex.Message };
        }
    }

    private async Task<TestResult> TestSelectionFunctionality()
    {
        try
        {
            var facade = CreateTestFacade();

            facade.AddColumn(new PublicColumnDefinition { Name = "ID", Header = "ID", DataType = typeof(int), IsVisible = true });

            await facade.AddRowAsync(new Dictionary<string, object?> { ["ID"] = 1 });
            await facade.AddRowAsync(new Dictionary<string, object?> { ["ID"] = 2 });

            // Select row
            await facade.SelectRowAsync(0);

            // Clear selection
            await facade.ClearSelectionAsync();

            return new TestResult { Category = "Functional", Name = "SelectionFunctionality", Success = true, Details = "Selection and clear selection successful" };
        }
        catch (Exception ex)
        {
            return new TestResult { Category = "Functional", Name = "SelectionFunctionality", Success = false, Error = ex.Message };
        }
    }

    private async Task<TestResult> TestValidationFunctionality()
    {
        try
        {
            var facade = CreateTestFacade(enableValidation: true);

            facade.AddColumn(new PublicColumnDefinition
            {
                Name = "Email",
                Header = "Email",
                DataType = typeof(string),
                IsVisible = true,
                ValidationRules = new List<PublicValidationRule>
                {
                    new() { RuleType = PublicValidationRuleType.Regex, RegexPattern = @"^[\w\.\-]+@[\w\.\-]+\.\w+$", ErrorMessage = "Invalid email" }
                }
            });

            // Add valid email
            await facade.AddRowAsync(new Dictionary<string, object?> { ["Email"] = "test@example.com" });

            return new TestResult { Category = "Functional", Name = "ValidationFunctionality", Success = true, Details = "Validation rules applied successfully" };
        }
        catch (Exception ex)
        {
            return new TestResult { Category = "Functional", Name = "ValidationFunctionality", Success = false, Error = ex.Message };
        }
    }

    private async Task<TestResult> TestCellEditing()
    {
        try
        {
            var facade = CreateTestFacade();

            facade.AddColumn(new PublicColumnDefinition { Name = "Value", Header = "Value", DataType = typeof(int), IsVisible = true });

            await facade.AddRowAsync(new Dictionary<string, object?> { ["Value"] = 100 });

            // Update cell
            await facade.UpdateCellAsync(0, "Value", 200);

            var data = facade.GetCurrentData();
            if ((int)data[0]["Value"]! != 200)
                throw new Exception("Cell update failed");

            return new TestResult { Category = "Functional", Name = "CellEditing", Success = true, Details = "Cell update successful" };
        }
        catch (Exception ex)
        {
            return new TestResult { Category = "Functional", Name = "CellEditing", Success = false, Error = ex.Message };
        }
    }

    private IAdvancedDataGridFacade CreateTestFacade(bool enableValidation = false)
    {
        var options = new AdvancedDataGridOptions
        {
            BatchSize = 1000,
            EnableParallelProcessing = false,
            EnableRealTimeValidation = enableValidation,
            LoggerFactory = NullLoggerFactory.Instance,
            OperationMode = PublicDataGridOperationMode.Headless
        };

        options.EnabledFeatures.Clear();
        options.EnabledFeatures.Add(GridFeature.Sort);
        options.EnabledFeatures.Add(GridFeature.Filter);
        options.EnabledFeatures.Add(GridFeature.Search);
        options.EnabledFeatures.Add(GridFeature.RowColumnOperations);
        options.EnabledFeatures.Add(GridFeature.Selection);
        if (enableValidation)
            options.EnabledFeatures.Add(GridFeature.Validation);

        return AdvancedDataGridFacadeFactory.CreateStandalone(options, NullLoggerFactory.Instance, null);
    }
}
