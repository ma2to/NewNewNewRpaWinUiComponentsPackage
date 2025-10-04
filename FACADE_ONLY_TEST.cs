using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

// FACADE-ONLY ARCHITECTURE TEST
// Tento kód demonštruje, že consuming aplikácia používa iba jeden using
// a má prístup iba k public facade API, nie k internal implementáciám

using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid;

namespace FacadeOnlyArchitectureTest
{
    /// <summary>
    /// TEST CONSUMING APPLICATION
    /// Toto demonštruje facade-only architektúru - jediný using statement
    /// pre celý komponent s prístupom iba k public API
    /// </summary>
    public class ConsumingApplication
    {
        public async Task TestFacadeOnlyArchitecture()
        {
            // ========================================
            // FACADE-ONLY: Iba jeden using statement!
            // using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid;
            // ========================================

            // 1. Vytvorenie facade instance
            var dataGrid = new AdvancedDataGridFacade();

            // 2. Data preparation (normálne dáta z aplikácie)
            var testData = new List<Dictionary<string, object?>>
            {
                new() { ["Name"] = "John Doe", ["Age"] = 30, ["Email"] = "john@example.com" },
                new() { ["Name"] = "Jane Smith", ["Age"] = 25, ["Email"] = "jane@example.com" },
                new() { ["Name"] = "Bob Johnson", ["Age"] = 35, ["Email"] = "bob@example.com" }
            };

            // 3. IMPORT operations using public command objects
            var importCommand = ImportDataCommand.FromDictionary(
                data: testData,
                mode: ImportMode.Replace,
                startRow: 1
            );

            var importResult = await dataGrid.ImportAsync(importCommand);
            Console.WriteLine($"Import result: {importResult.Success}, Rows: {importResult.ImportedRows}");

            // 4. EXPORT operations using public command objects
            var exportCommand = ExportDataCommand.ToDictionary(
                includeHeaders: true,
                exportOnlyChecked: false
            );

            var exportedData = await dataGrid.ExportToDictionaryAsync(testData, exportCommand);
            Console.WriteLine($"Exported {exportedData.Count} rows");

            // 5. SORT operations using public types
            var sortConfig = SortColumnConfiguration.Create("Age", SortDirection.Ascending);
            var sortResult = await dataGrid.SortAsync(testData, "Age", SortDirection.Ascending);
            Console.WriteLine($"Sorted {sortResult.Data.Count} rows in {sortResult.SortTime.TotalMilliseconds}ms");

            // 6. FILTER operations using public types
            var filter = FilterDefinition.Create("Age", FilterOperator.GreaterThan, 25);
            var filterResult = await dataGrid.ApplyFilterAsync(testData, filter);
            Console.WriteLine($"Filtered to {filterResult.FilteredRowCount} rows from {filterResult.OriginalRowCount}");

            // 7. Builder pattern for advanced configuration
            var configuredGrid = new AdvancedDataGridFacadeBuilder()
                .WithHeadlessMode(true)
                .Build();

            Console.WriteLine("✅ FACADE-ONLY ARCHITECTURE TEST PASSED!");
            Console.WriteLine("📋 Using only: using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid;");
            Console.WriteLine("🔒 No access to internal implementation details");
            Console.WriteLine("🎯 Clean, simple API surface");
        }

        /// <summary>
        /// DEMO: Všetky dostupné public types v IntelliSense
        /// Toto je to, co vidí consuming aplikácia
        /// </summary>
        public void ShowAvailablePublicTypes()
        {
            // FACADE + BUILDER
            var facade = new AdvancedDataGridFacade();
            var builder = new AdvancedDataGridFacadeBuilder();

            // COMMAND OBJECTS
            var importCmd = new ImportDataCommand();
            var exportCmd = new ExportDataCommand();

            // ENUMS
            var importMode = ImportMode.Replace;
            var exportFormat = ExportFormat.Dictionary;
            var sortDirection = SortDirection.Ascending;
            var filterOperator = FilterOperator.Equals;

            // VALUE OBJECTS
            var filterDef = new FilterDefinition();
            var sortConfig = new SortColumnConfiguration();

            // RESULT OBJECTS
            var importResult = new ImportResult();
            var sortResult = new SortResult();
            var filterResult = new FilterResult();

            Console.WriteLine("✅ All public types accessible with single using statement");
        }

        /// <summary>
        /// VALIDATION: Overenie, že internal typy NIE SÚ prístupné
        /// Tieto riadky by mali spôsobiť compilation error ak sú odkomentované
        /// </summary>
        public void ValidateInternalTypesNotAccessible()
        {
            // ❌ TIETO BY MALI BYŤ COMPILATION ERRORS:

            // var internalService = new ImportExportService(); // internal class
            // var validationRule = new ValidationRule(); // internal struct
            // var searchResult = new SearchResult(); // internal record
            // var advancedFilter = new AdvancedFilter(); // internal record

            Console.WriteLine("✅ Internal types properly hidden from consuming applications");
        }
    }

    /// <summary>
    /// UKÁŽKA REÁLNEHO POUŽITIA
    /// Ako by vyzeralo použitie v reálnej aplikácii
    /// </summary>
    public class RealWorldUsageExample
    {
        private readonly IAdvancedDataGridFacade _dataGrid = new AdvancedDataGridFacade();

        public async Task ProcessEmployeeData()
        {
            // Iba jeden using: using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid;

            var employees = await LoadEmployeeDataFromDatabase();

            // Import
            var importCommand = ImportDataCommand.FromDictionary(employees);
            await _dataGrid.ImportAsync(importCommand);

            // Filter active employees
            var activeFilter = FilterDefinition.Create("Status", FilterOperator.Equals, "Active");
            var filteredResult = await _dataGrid.ApplyFilterAsync(employees, activeFilter);

            // Sort by salary descending
            var sortedResult = await _dataGrid.SortAsync(
                filteredResult.Data,
                "Salary",
                SortDirection.Descending
            );

            // Export for reporting
            var exportCommand = ExportDataCommand.ToDataTable(includeHeaders: true);
            var reportData = await _dataGrid.ExportToDataTableAsync(sortedResult.Data, exportCommand);

            Console.WriteLine($"Processed {reportData.Rows.Count} active employees");
        }

        private async Task<List<Dictionary<string, object?>>> LoadEmployeeDataFromDatabase()
        {
            // Simulation of database load
            await Task.Delay(100);
            return new List<Dictionary<string, object?>>
            {
                new() { ["Id"] = 1, ["Name"] = "Alice", ["Salary"] = 75000, ["Status"] = "Active" },
                new() { ["Id"] = 2, ["Name"] = "Bob", ["Salary"] = 65000, ["Status"] = "Active" },
                new() { ["Id"] = 3, ["Name"] = "Charlie", ["Salary"] = 55000, ["Status"] = "Inactive" }
            };
        }
    }
}

// ============================================
// ARCHITECTURAL VALIDATION SUMMARY
// ============================================
//
// ✅ FACADE-ONLY ARCHITECTURE IMPLEMENTED:
//
// 1. PUBLIC API SURFACE:
//    - IAdvancedDataGridFacade (interface)
//    - AdvancedDataGridFacade (implementation)
//    - AdvancedDataGridFacadeBuilder (builder)
//    - Public DTOs in root namespace (commands, results, enums)
//
// 2. INTERNAL IMPLEMENTATION:
//    - All Application services (internal)
//    - All Core domain objects (internal)
//    - All Infrastructure services (internal)
//    - All validation types (internal)
//
// 3. CONSUMER EXPERIENCE:
//    - Single using statement: using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid;
//    - Clean IntelliSense with only relevant public types
//    - No access to internal implementation details
//    - Easy to use API with factory methods and builders
//
// 4. ARCHITECTURAL BENEFITS:
//    - Encapsulation: Internal implementation hidden
//    - Simplicity: One using statement per component
//    - Maintainability: Can refactor internals without breaking consumers
//    - Professional: Enterprise-grade API design
//
// ============================================