using BenchmarkDotNet.Attributes;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Api.Models;
using RpaWinUiComponentsPackage.PerformanceBenchmarks.Helpers;
using System.Data;

namespace RpaWinUiComponentsPackage.PerformanceBenchmarks.Benchmarks;

/// <summary>
/// Benchmarks for Import/Export operations with Dictionary and DataTable
/// Tests performance of data transformation operations
/// </summary>
[MemoryDiagnoser]
[ThreadingDiagnoser]
public class ImportExportBenchmarks
{
    private IAdvancedDataGridFacade _facade = null!;
    private List<Dictionary<string, object?>> _dictionaryData = null!;
    private DataTable _dataTableData = null!;

    [Params(1000, 10_000, 100_000)]
    public int RowCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _facade = BenchmarkHelper.CreateFacade(GridFeature.Import, GridFeature.Export, GridFeature.RowColumnOperations);

        _facade.AddColumn(BenchmarkHelper.CreateColumn("ID", typeof(int)));
        _facade.AddColumn(BenchmarkHelper.CreateColumn("Name", typeof(string)));
        _facade.AddColumn(BenchmarkHelper.CreateColumn("Email", typeof(string)));
        _facade.AddColumn(BenchmarkHelper.CreateColumn("Age", typeof(int)));
        _facade.AddColumn(BenchmarkHelper.CreateColumn("Salary", typeof(decimal)));

        // Generate Dictionary data
        _dictionaryData = new List<Dictionary<string, object?>>();
        for (int i = 0; i < RowCount; i++)
        {
            _dictionaryData.Add(new Dictionary<string, object?>
            {
                ["ID"] = i,
                ["Name"] = $"User{i}",
                ["Email"] = $"user{i}@example.com",
                ["Age"] = 20 + (i % 50),
                ["Salary"] = 30000m + (i % 70000)
            });
        }

        // Generate DataTable data
        _dataTableData = new DataTable();
        _dataTableData.Columns.Add("ID", typeof(int));
        _dataTableData.Columns.Add("Name", typeof(string));
        _dataTableData.Columns.Add("Email", typeof(string));
        _dataTableData.Columns.Add("Age", typeof(int));
        _dataTableData.Columns.Add("Salary", typeof(decimal));

        for (int i = 0; i < RowCount; i++)
        {
            _dataTableData.Rows.Add(
                i,
                $"User{i}",
                $"user{i}@example.com",
                20 + (i % 50),
                30000m + (i % 70000)
            );
        }
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        if (_facade is IDisposable disposable)
            disposable.Dispose();
    }

    [Benchmark]
    public async Task ImportDictionaryToGrid()
    {
        // Add rows one by one (simulates dictionary import)
        foreach (var row in _dictionaryData)
        {
            await _facade.AddRowAsync(row);
        }
    }

    [Benchmark]
    public async Task ExportGridToDataTable()
    {
        // First populate grid
        foreach (var row in _dictionaryData.Take(Math.Min(1000, RowCount)))
        {
            await _facade.AddRowAsync(row);
        }

        // Then get data as DataTable via GetCurrentDataAsDataTableAsync
        var result = await _facade.GetCurrentDataAsDataTableAsync();

        if (result == null || result.Rows.Count == 0)
            throw new Exception("Export returned no data");
    }

    [Benchmark]
    public void TransformDataTableToDictionaries()
    {
        // Benchmark pure data transformation without facade
        var dictionaries = new List<Dictionary<string, object?>>();

        foreach (DataRow row in _dataTableData.Rows)
        {
            var dict = new Dictionary<string, object?>();
            foreach (DataColumn col in _dataTableData.Columns)
            {
                dict[col.ColumnName] = row[col];
            }
            dictionaries.Add(dict);
        }

        if (dictionaries.Count != RowCount)
            throw new Exception("Transformation failed");
    }

    [Benchmark]
    public void TransformDictionariesToDataTable()
    {
        // Benchmark pure data transformation without facade
        var dt = new DataTable();

        if (_dictionaryData.Count > 0)
        {
            var firstRow = _dictionaryData[0];
            foreach (var key in firstRow.Keys)
            {
                var value = firstRow[key];
                var columnType = value?.GetType() ?? typeof(object);
                dt.Columns.Add(key, columnType);
            }

            foreach (var dict in _dictionaryData)
            {
                var row = dt.NewRow();
                foreach (var kvp in dict)
                {
                    row[kvp.Key] = kvp.Value ?? DBNull.Value;
                }
                dt.Rows.Add(row);
            }
        }

        if (dt.Rows.Count != RowCount)
            throw new Exception("Transformation failed");
    }
}
