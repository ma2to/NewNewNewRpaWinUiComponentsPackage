namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid;

/// <summary>
/// Cell operation types
/// </summary>
public enum PublicCellOperationType
{
    Update,
    Clear,
    Validate
}

/// <summary>
/// Batch row operation types
/// </summary>
public enum PublicBatchRowOperationType
{
    Insert,
    Delete,
    Update,
    Move
}

/// <summary>
/// Column operation types
/// </summary>
public enum PublicColumnOperationType
{
    Add,
    Remove,
    Resize,
    Reorder,
    Rename
}

/// <summary>
/// Single cell operation configuration
/// </summary>
/// <param name="RowIndex">Row index</param>
/// <param name="ColumnIndex">Column index</param>
/// <param name="Value">Cell value</param>
/// <param name="OperationType">Operation type</param>
public record CellOperationConfig(
    int RowIndex,
    int ColumnIndex,
    object? Value = null,
    PublicCellOperationType OperationType = PublicCellOperationType.Update
);

/// <summary>
/// Command for batch cell updates
/// </summary>
/// <param name="Operations">List of cell operations</param>
/// <param name="ValidateBeforeCommit">Validate before committing changes</param>
public record BatchUpdateCellsDataCommand(
    IReadOnlyList<CellOperationConfig> Operations,
    bool ValidateBeforeCommit = true
)
{
    public BatchUpdateCellsDataCommand() : this(Array.Empty<CellOperationConfig>(), true) { }
}

/// <summary>
/// Single row operation configuration
/// </summary>
/// <param name="RowIndex">Row index</param>
/// <param name="RowData">Row data dictionary</param>
/// <param name="OperationType">Operation type</param>
public record RowOperationConfig(
    int RowIndex,
    IReadOnlyDictionary<string, object?>? RowData = null,
    PublicBatchRowOperationType OperationType = PublicBatchRowOperationType.Insert
);

/// <summary>
/// Command for batch row operations
/// </summary>
/// <param name="Operations">List of row operations</param>
public record BatchRowOperationsDataCommand(
    IReadOnlyList<RowOperationConfig> Operations
)
{
    public BatchRowOperationsDataCommand() : this(Array.Empty<RowOperationConfig>()) { }
}

/// <summary>
/// Single column operation configuration
/// </summary>
/// <param name="ColumnName">Column name</param>
/// <param name="Width">New column width</param>
/// <param name="NewPosition">New column position</param>
/// <param name="NewName">New column name</param>
/// <param name="OperationType">Operation type</param>
public record ColumnOperationConfig(
    string ColumnName,
    double? Width = null,
    int? NewPosition = null,
    string? NewName = null,
    PublicColumnOperationType OperationType = PublicColumnOperationType.Resize
);

/// <summary>
/// Command for batch column operations
/// </summary>
/// <param name="Operations">List of column operations</param>
public record BatchColumnOperationsDataCommand(
    IReadOnlyList<ColumnOperationConfig> Operations
)
{
    public BatchColumnOperationsDataCommand() : this(Array.Empty<ColumnOperationConfig>()) { }
}

/// <summary>
/// Result of batch operation
/// </summary>
/// <param name="IsSuccess">Whether operation succeeded</param>
/// <param name="AffectedItems">Number of items affected</param>
/// <param name="Errors">List of errors if any</param>
/// <param name="Duration">Operation duration</param>
public record BatchOperationResult(
    bool IsSuccess,
    int AffectedItems,
    IReadOnlyList<string> Errors,
    TimeSpan Duration
)
{
    public BatchOperationResult() : this(false, 0, Array.Empty<string>(), TimeSpan.Zero) { }
}
