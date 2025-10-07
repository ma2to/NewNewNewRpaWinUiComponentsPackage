using RpaWinUiComponentsPackage.Tests.TestInfrastructure;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Api.Models;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid;

namespace RpaWinUiComponentsPackage.Tests.Stress;

/// <summary>
/// Stress tests - extreme load scenarios
/// </summary>
public class StressTests : TestBase
{
    public async Task<List<TestResult>> RunAllTests()
    {
        var results = new List<TestResult>();

        Console.WriteLine("╔════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║                    STRESS TESTS                            ║");
        Console.WriteLine("╚════════════════════════════════════════════════════════════╝");
        Console.WriteLine();

        results.Add(await TestExtremeLargeDataset());
        results.Add(await TestRapidCRUDOperations());
        results.Add(await TestConcurrentOperations());
        results.Add(await TestMassiveSortingLoad());
        results.Add(await TestExtremeFiltering());
        results.Add(await TestContinuousImportExport());
        results.Add(await TestMemoryPressure());

        return results;
    }

    private async Task<TestResult> TestExtremeLargeDataset()
    {
        var result = await MeasureAsync("Extreme Large Dataset - 1M rows", "Stress", async () =>
        {
            var facade = CreateFacade(batchSize: 50000);
            SetupColumns(facade, 20); // 20 columns

            var data = GenerateTestData(1_000_000, 20);
            var count = await facade.AddRowsBatchAsync(data);

            if (count != 1_000_000) throw new Exception($"Expected 1M rows, got {count}");

            // Successfully handled 1M rows × 20 columns
        }, 1_000_000);

        return result;
    }

    private async Task<TestResult> TestRapidCRUDOperations()
    {
        var result = await MeasureAsync("Rapid CRUD - 10K operations", "Stress", async () =>
        {
            var facade = CreateFacade();
            SetupColumns(facade);
            await facade.AddRowsBatchAsync(GenerateTestData(10000));

            var random = new Random(42);
            int operations = 10000;

            for (int i = 0; i < operations; i++)
            {
                var op = i % 4;
                switch (op)
                {
                    case 0: // Add
                        await facade.AddRowAsync(new Dictionary<string, object?>
                        {
                            ["ID"] = i,
                            ["Name"] = $"New_{i}",
                            ["Value"] = random.NextDouble() * 1000
                        });
                        break;
                    case 1: // Update
                        await facade.UpdateRowAsync(random.Next(0, 1000), new Dictionary<string, object?>
                        {
                            ["Value"] = random.NextDouble() * 1000
                        });
                        break;
                    case 2: // Remove
                        if (facade.GetRowCount() > 100)
                            await facade.RemoveRowAsync(random.Next(0, facade.GetRowCount()));
                        break;
                    case 3: // Get
                        var _ = facade.GetRow(random.Next(0, facade.GetRowCount()));
                        break;
                }
            }

            // Completed operations mixed CRUD operations
        }, 10000);

        return result;
    }

    private async Task<TestResult> TestConcurrentOperations()
    {
        var result = await MeasureAsync("Concurrent Operations - 100 threads", "Stress", async () =>
        {
            var facade = CreateFacade(enableParallel: true);
            SetupColumns(facade);
            await facade.AddRowsBatchAsync(GenerateTestData(10000));

            var tasks = new List<Task>();
            var random = new Random(42);

            for (int i = 0; i < 100; i++)
            {
                var taskId = i;
                tasks.Add(Task.Run(async () =>
                {
                    for (int j = 0; j < 100; j++)
                    {
                        var op = (taskId + j) % 5;
                        switch (op)
                        {
                            case 0:
                                await facade.AddRowAsync(new Dictionary<string, object?>
                                {
                                    ["ID"] = taskId * 1000 + j,
                                    ["Name"] = $"Concurrent_{taskId}_{j}",
                                    ["Value"] = random.NextDouble() * 1000
                                });
                                break;
                            case 1:
                                await facade.SortByColumnAsync("Value", PublicSortDirection.Ascending);
                                break;
                            case 2:
                                await facade.ApplyFilterAsync("Value", PublicFilterOperator.GreaterThan, 500.0);
                                break;
                            case 3:
                                var _ = facade.GetCurrentData();
                                break;
                            case 4:
                                await facade.SelectRowAsync(random.Next(0, 100));
                                break;
                        }
                    }
                }));
            }

            await Task.WhenAll(tasks);
            // 100 threads × 100 operations each = 10,000 concurrent ops
        });

        return result;
    }

    private async Task<TestResult> TestMassiveSortingLoad()
    {
        var result = await MeasureAsync("Massive Sorting - 100K rows × 100 sorts", "Stress", async () =>
        {
            var facade = CreateFacade();
            SetupColumns(facade);
            await facade.AddRowsBatchAsync(GenerateTestData(100000));

            for (int i = 0; i < 100; i++)
            {
                var direction = i % 2 == 0 ? PublicSortDirection.Ascending : PublicSortDirection.Descending;
                var column = new[] { "ID", "Name", "Value", "Status", "Category" }[i % 5];
                await facade.SortByColumnAsync(column, direction);
            }

            // 100 sort operations on 100K rows
        }, 100);

        return result;
    }

    private async Task<TestResult> TestExtremeFiltering()
    {
        var result = await MeasureAsync("Extreme Filtering - 100K rows × 1K filters", "Stress", async () =>
        {
            var facade = CreateFacade();
            SetupColumns(facade);
            await facade.AddRowsBatchAsync(GenerateTestData(100000));

            var random = new Random(42);
            for (int i = 0; i < 1000; i++)
            {
                var value = random.NextDouble() * 1000;
                var op = (PublicFilterOperator)(i % 8);
                await facade.ApplyFilterAsync("Value", op, value);
                await facade.ClearFiltersAsync();
            }

            // 1000 filter cycles on 100K rows
        }, 1000);

        return result;
    }

    private async Task<TestResult> TestContinuousImportExport()
    {
        var result = await MeasureAsync("Continuous Import/Export - 100 cycles", "Stress", async () =>
        {
            var facade = CreateFacade();
            SetupColumns(facade);

            for (int i = 0; i < 100; i++)
            {
                // Import
                var importCmd = ImportDataCommand.FromDictionaries(GenerateTestData(1000), PublicImportMode.Replace);
                await facade.ImportAsync(importCmd);

                // Export
                var exportCmd = ExportDataCommand.ToDictionary(exportOnlyFiltered: false);
                await facade.ExportAsync(exportCmd);
            }

            // 100 import/export cycles completed
        }, 100);

        return result;
    }

    private async Task<TestResult> TestMemoryPressure()
    {
        var result = await MeasureAsync("Memory Pressure - 10 facades × 50K rows", "Stress", async () =>
        {
            var facades = new List<IAdvancedDataGridFacade>();

            for (int i = 0; i < 10; i++)
            {
                var facade = CreateFacade();
                SetupColumns(facade);
                await facade.AddRowsBatchAsync(GenerateTestData(50000));
                facades.Add(facade);
            }

            var totalRows = facades.Sum(f => f.GetRowCount());
            // 10 concurrent facades with totalRows total rows

            // Cleanup
            foreach (var facade in facades)
            {
                await facade.ClearAllRowsAsync();
            }
        });

        return result;
    }
}
