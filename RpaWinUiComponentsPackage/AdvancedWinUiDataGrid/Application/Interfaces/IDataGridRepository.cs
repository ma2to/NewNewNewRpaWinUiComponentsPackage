using System.Collections.Generic;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Core.Entities;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Core.ValueObjects;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Application.Interfaces;

/// <summary>
/// APPLICATION: Repository interface for data grid storage operations
/// SINGLE RESPONSIBILITY: Data persistence and retrieval abstraction
/// </summary>
internal interface IDataGridRepository
{
    /// <summary>Get all rows in the grid</summary>
    Result<IReadOnlyList<DataRow>> GetAllRows();

    /// <summary>Get specific row by index</summary>
    Result<DataRow?> GetRow(int rowIndex);

    /// <summary>Add new row</summary>
    Result AddRow(DataRow row);

    /// <summary>Update existing row</summary>
    Result UpdateRow(DataRow row);

    /// <summary>Remove row by index</summary>
    Result RemoveRow(int rowIndex);

    /// <summary>Get all columns in the grid</summary>
    Result<IReadOnlyList<DataColumn>> GetAllColumns();

    /// <summary>Get specific column by name</summary>
    Result<DataColumn?> GetColumn(string columnName);

    /// <summary>Add new column</summary>
    Result AddColumn(DataColumn column);

    /// <summary>Update existing column</summary>
    Result UpdateColumn(DataColumn column);

    /// <summary>Remove column by name</summary>
    Result RemoveColumn(string columnName);

    /// <summary>Get cell by coordinates</summary>
    Result<Cell?> GetCell(int rowIndex, string columnName);

    /// <summary>Update cell value</summary>
    Result UpdateCell(int rowIndex, string columnName, object? value);

    /// <summary>Get total row count</summary>
    Result<int> GetRowCount();

    /// <summary>Get total column count</summary>
    Result<int> GetColumnCount();

    /// <summary>Clear all data</summary>
    Result ClearAllData();

    /// <summary>Commit all pending changes</summary>
    Result CommitChanges();

    /// <summary>Revert all pending changes</summary>
    Result RevertChanges();
}