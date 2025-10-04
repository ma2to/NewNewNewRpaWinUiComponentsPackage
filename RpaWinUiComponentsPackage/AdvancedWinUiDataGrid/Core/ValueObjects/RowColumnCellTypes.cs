using System;
using System.Collections.Generic;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Core.ValueObjects;

/// <summary>
/// Cell operation types
/// </summary>
internal enum CellOperationType
{
    Update,
    Clear,
    Validate
}

/// <summary>
/// Batch row operation types
/// </summary>
internal enum BatchRowOperationType
{
    Insert,
    Delete,
    Update,
    Move
}

/// <summary>
/// Column operation types
/// </summary>
internal enum ColumnOperationType
{
    Add,
    Remove,
    Resize,
    Reorder,
    Rename
}

/// <summary>
/// Batch cell operation configuration
/// </summary>
internal sealed record BatchCellOperation
{
    public required int RowIndex { get; init; }
    public required int ColumnIndex { get; init; }
    public object? Value { get; init; }
    public CellOperationType OperationType { get; init; } = CellOperationType.Update;

    public static BatchCellOperation Create(int rowIndex, int columnIndex, object? value) =>
        new()
        {
            RowIndex = rowIndex,
            ColumnIndex = columnIndex,
            Value = value,
            OperationType = CellOperationType.Update
        };
}

/// <summary>
/// Batch row operation configuration
/// </summary>
internal sealed record BatchRowOperation
{
    public required int RowIndex { get; init; }
    public IReadOnlyDictionary<string, object?>? RowData { get; init; }
    public BatchRowOperationType OperationType { get; init; } = BatchRowOperationType.Insert;

    public static BatchRowOperation CreateInsert(int rowIndex, IReadOnlyDictionary<string, object?> rowData) =>
        new()
        {
            RowIndex = rowIndex,
            RowData = rowData,
            OperationType = BatchRowOperationType.Insert
        };

    public static BatchRowOperation CreateDelete(int rowIndex) =>
        new()
        {
            RowIndex = rowIndex,
            OperationType = BatchRowOperationType.Delete
        };
}

/// <summary>
/// Batch column operation configuration
/// </summary>
internal sealed record BatchColumnOperation
{
    public required string ColumnName { get; init; }
    public double? Width { get; init; }
    public int? NewPosition { get; init; }
    public string? NewName { get; init; }
    public ColumnOperationType OperationType { get; init; } = ColumnOperationType.Resize;

    public static BatchColumnOperation CreateResize(string columnName, double width) =>
        new()
        {
            ColumnName = columnName,
            Width = width,
            OperationType = ColumnOperationType.Resize
        };

    public static BatchColumnOperation CreateReorder(string columnName, int newPosition) =>
        new()
        {
            ColumnName = columnName,
            NewPosition = newPosition,
            OperationType = ColumnOperationType.Reorder
        };
}

/// <summary>
/// Operation result with details
/// </summary>
internal sealed record OperationResult
{
    public bool Success { get; init; }
    public int AffectedItems { get; init; }
    public IReadOnlyList<string> Errors { get; init; } = Array.Empty<string>();
    public TimeSpan Duration { get; init; }

    public static OperationResult CreateSuccess(int affectedItems, TimeSpan duration) =>
        new()
        {
            Success = true,
            AffectedItems = affectedItems,
            Duration = duration
        };

    public static OperationResult CreateFailure(IReadOnlyList<string> errors) =>
        new()
        {
            Success = false,
            Errors = errors
        };
}
