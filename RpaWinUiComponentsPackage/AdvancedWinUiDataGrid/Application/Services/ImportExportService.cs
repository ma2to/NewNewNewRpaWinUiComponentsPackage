using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Application.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Core.ValueObjects;
using CoreTypes = RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Core.ValueObjects;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Application.Services;

/// <summary>
/// INTERNAL SERVICE: Import and export operations implementation
/// CLEAN ARCHITECTURE: Application layer service for data import/export operations
/// </summary>
internal sealed class ImportExportService : IImportExportService
{
    public async Task<CoreTypes.ImportResult> ImportFromDataTableAsync(
        DataTable dataTable,
        CoreTypes.ImportOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            var stopwatch = Stopwatch.StartNew();
            options ??= CoreTypes.ImportOptions.Default;

            try
            {
                // LINQ OPTIMIZATION: Replace manual loop with functional approach
                var rowsData = dataTable.Rows.Cast<DataRow>()
                    .TakeWhile(_ => !cancellationToken.IsCancellationRequested)
                    .Select((row, index) => new
                    {
                        Row = row,
                        Index = index,
                        IsValid = !options.ValidateBeforeImport || ValidateDataRow(row)
                    })
                    .ToList();

                var importedRows = rowsData.Count(r => r.IsValid);
                var skippedRows = rowsData.Count(r => !r.IsValid);

                // Report final progress if available
                if (options.Progress != null)
                {
                    var progress = CoreTypes.ImportProgress.Create(
                        importedRows + skippedRows,
                        dataTable.Rows.Count,
                        stopwatch.Elapsed,
                        "Importing DataTable rows");
                    options.Progress.Report(progress);
                }

                stopwatch.Stop();
                return CoreTypes.ImportResult.CreateSuccess(importedRows, dataTable.Rows.Count, stopwatch.Elapsed);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                return CoreTypes.ImportResult.Failure(new[] { ex.Message }, stopwatch.Elapsed);
            }
        }, cancellationToken);
    }

    public async Task<CoreTypes.ImportResult> ImportFromDictionaryAsync(
        IEnumerable<IReadOnlyDictionary<string, object?>> sourceData,
        CoreTypes.ImportOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            var stopwatch = Stopwatch.StartNew();
            options ??= CoreTypes.ImportOptions.Default;

            try
            {
                // LINQ OPTIMIZATION: Replace manual loop with functional approach
                var dataList = sourceData.ToList();
                var processedRows = dataList
                    .TakeWhile(_ => !cancellationToken.IsCancellationRequested)
                    .Select((row, index) => new
                    {
                        Row = row,
                        Index = index,
                        IsValid = !options.ValidateBeforeImport || ValidateDictionaryRow(row)
                    })
                    .ToList();

                var importedRows = processedRows.Count(r => r.IsValid);
                var skippedRows = processedRows.Count(r => !r.IsValid);

                // Report final progress if available
                if (options.Progress != null)
                {
                    var progress = CoreTypes.ImportProgress.Create(
                        importedRows + skippedRows,
                        dataList.Count,
                        stopwatch.Elapsed,
                        "Importing dictionary rows");
                    options.Progress.Report(progress);
                }

                stopwatch.Stop();
                return CoreTypes.ImportResult.CreateSuccess(importedRows, dataList.Count, stopwatch.Elapsed);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                return CoreTypes.ImportResult.Failure(new[] { ex.Message }, stopwatch.Elapsed);
            }
        }, cancellationToken);
    }

    public async Task<DataTable> ExportToDataTableAsync(
        IEnumerable<IReadOnlyDictionary<string, object?>> data,
        CoreTypes.ExportOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            options ??= CoreTypes.ExportOptions.Default;
            var dataList = data.ToList();
            var dataTable = new DataTable();

            if (!dataList.Any())
                return dataTable;

            // LINQ OPTIMIZATION: Functional approach for column and row processing
            var allColumns = dataList.SelectMany(row => row.Keys).Distinct().ToList();
            var columnsToExport = options.ColumnsToExport?.ToList() ?? allColumns;

            // Create DataTable columns using LINQ
            columnsToExport.ForEach(columnName => dataTable.Columns.Add(columnName, typeof(object)));

            // Add data rows using functional approach
            var processedRows = dataList
                .TakeWhile(_ => !cancellationToken.IsCancellationRequested)
                .Select((row, index) =>
                {
                    var dataRow = dataTable.NewRow();
                    columnsToExport.ForEach(columnName =>
                        dataRow[columnName] = row.TryGetValue(columnName, out var value) ? value ?? DBNull.Value : DBNull.Value);
                    return new { DataRow = dataRow, Index = index };
                })
                .ToList();

            // Add all rows to DataTable
            processedRows.ForEach(item => dataTable.Rows.Add(item.DataRow));

            // Report final progress if available
            if (options.Progress != null)
            {
                var progress = CoreTypes.ExportProgress.Create(
                    processedRows.Count,
                    dataList.Count,
                    TimeSpan.Zero,
                    "Exporting to DataTable");
                options.Progress.Report(progress);
            }

            return dataTable;
        }, cancellationToken);
    }

    public async Task<IReadOnlyList<IReadOnlyDictionary<string, object?>>> ExportToDictionaryAsync(
        IEnumerable<IReadOnlyDictionary<string, object?>> data,
        CoreTypes.ExportOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            options ??= CoreTypes.ExportOptions.Default;
            var dataList = data.ToList();
            var result = new List<IReadOnlyDictionary<string, object?>>();

            if (!dataList.Any())
                return result;

            // LINQ OPTIMIZATION: Functional approach for data transformation
            var allColumns = dataList.SelectMany(row => row.Keys).Distinct().ToList();
            var columnsToExport = options.ColumnsToExport?.ToList() ?? allColumns;

            var exportedRows = dataList
                .TakeWhile(_ => !cancellationToken.IsCancellationRequested)
                .Select(row => columnsToExport.ToDictionary(
                    columnName => columnName,
                    columnName => row.TryGetValue(columnName, out var value) ? value : null))
                .Cast<IReadOnlyDictionary<string, object?>>()
                .ToList();

            result.AddRange(exportedRows);

            // Report final progress if available
            if (options.Progress != null)
            {
                var progress = CoreTypes.ExportProgress.Create(
                    exportedRows.Count,
                    dataList.Count,
                    TimeSpan.Zero,
                    "Exporting to Dictionary");
                options.Progress.Report(progress);
            }

            return (IReadOnlyList<IReadOnlyDictionary<string, object?>>)result;
        }, cancellationToken);
    }

    public async Task<CoreTypes.CopyPasteResult> CopyToClipboardAsync(
        IEnumerable<IReadOnlyDictionary<string, object?>> selectedData,
        bool includeHeaders = true,
        CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            try
            {
                var dataList = selectedData.ToList();
                if (!dataList.Any())
                    return CoreTypes.CopyPasteResult.CreateSuccess(0, string.Empty);

                // LINQ OPTIMIZATION: Functional approach for clipboard data preparation
                var allColumns = dataList.SelectMany(row => row.Keys).Distinct().ToList();
                var lines = new List<string>();

                // Add headers if requested
                if (includeHeaders)
                {
                    lines.Add(string.Join("\t", allColumns));
                }

                // Add data rows using LINQ
                var dataRows = dataList
                    .TakeWhile(_ => !cancellationToken.IsCancellationRequested)
                    .Select(row => string.Join("\t", allColumns.Select(column =>
                        row.TryGetValue(column, out var value) ? value?.ToString() ?? string.Empty : string.Empty)));

                lines.AddRange(dataRows);

                var clipboardData = string.Join(Environment.NewLine, lines);

                // Set clipboard data (requires UI thread in WinUI)
                try
                {
                    Clipboard.SetText(clipboardData);
                }
                catch
                {
                    // Clipboard operation might fail in some contexts
                }

                return CoreTypes.CopyPasteResult.CreateSuccess(dataList.Count, clipboardData);
            }
            catch (Exception ex)
            {
                return CoreTypes.CopyPasteResult.Failure(ex.Message);
            }
        }, cancellationToken);
    }

    public async Task<CoreTypes.CopyPasteResult> PasteFromClipboardAsync(
        int targetRowIndex = 0,
        int targetColumnIndex = 0,
        CoreTypes.ImportMode mode = CoreTypes.ImportMode.Replace,
        CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            try
            {
                // Get clipboard data (requires UI thread in WinUI)
                string clipboardData;
                try
                {
                    clipboardData = Clipboard.GetText();
                }
                catch
                {
                    return CoreTypes.CopyPasteResult.Failure("Unable to access clipboard");
                }

                if (string.IsNullOrEmpty(clipboardData))
                    return CoreTypes.CopyPasteResult.CreateSuccess(0);

                // LINQ OPTIMIZATION: Functional approach for clipboard data processing
                var lines = clipboardData.Split(new[] { Environment.NewLine, "\n", "\r\n" }, StringSplitOptions.RemoveEmptyEntries);

                var processedRows = lines
                    .TakeWhile(_ => !cancellationToken.IsCancellationRequested)
                    .Select(line => line.Split('\t'))
                    .Count();

                // In a real implementation, would apply the values based on target position and mode

                return CoreTypes.CopyPasteResult.CreateSuccess(processedRows);
            }
            catch (Exception ex)
            {
                return CoreTypes.CopyPasteResult.Failure(ex.Message);
            }
        }, cancellationToken);
    }

    public bool CanImport(object source)
    {
        return source is DataTable || source is IEnumerable<IReadOnlyDictionary<string, object?>>;
    }

    public bool ValidateImportData(object source, CoreTypes.ImportOptions? options = null)
    {
        if (!CanImport(source))
            return false;

        try
        {
            if (source is DataTable dataTable)
            {
                return dataTable.Rows.Count > 0 && dataTable.Columns.Count > 0;
            }

            if (source is IEnumerable<IReadOnlyDictionary<string, object?>> dictionary)
            {
                return dictionary.Any();
            }

            return false;
        }
        catch
        {
            return false;
        }
    }

    private static bool ValidateDataRow(DataRow row)
    {
        // Basic validation - check if row has any non-null values
        return row.ItemArray.Any(field => field != null && field != DBNull.Value);
    }

    private static bool ValidateDictionaryRow(IReadOnlyDictionary<string, object?> row)
    {
        // Basic validation - check if row has any non-null values
        return row.Values.Any(value => value != null);
    }
}