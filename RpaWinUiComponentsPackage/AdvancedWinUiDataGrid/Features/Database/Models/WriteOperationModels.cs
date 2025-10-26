using System.Collections.Generic;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Database.Models;

/// <summary>
/// Enum representing the type of write operation
/// </summary>
internal enum WriteOperationType
{
    /// <summary>Insert a single row</summary>
    InsertRow,

    /// <summary>Update a single row</summary>
    UpdateRow,

    /// <summary>Delete a single row (soft delete)</summary>
    DeleteRow,

    /// <summary>Bulk insert multiple rows</summary>
    BulkInsert,

    /// <summary>Bulk update multiple rows</summary>
    BulkUpdate,

    /// <summary>Bulk delete multiple rows</summary>
    BulkDelete,

    /// <summary>Update validation state for a row</summary>
    UpdateValidationState,

    /// <summary>Vacuum database (optimize storage)</summary>
    Vacuum,

    /// <summary>Flush writer queue (wait for all pending ops)</summary>
    Flush
}

/// <summary>
/// Base class for all write operations
/// </summary>
internal abstract class WriteOperation
{
    /// <summary>Type of write operation</summary>
    public abstract WriteOperationType OperationType { get; }

    /// <summary>Timestamp when operation was queued</summary>
    public DateTime QueuedAt { get; init; } = DateTime.UtcNow;

    /// <summary>Optional operation ID for tracking</summary>
    public string? OperationId { get; init; }
}

/// <summary>
/// Write operation for inserting a single row
/// </summary>
internal sealed class InsertRowWriteOp : WriteOperation
{
    public override WriteOperationType OperationType => WriteOperationType.InsertRow;

    /// <summary>Row ID</summary>
    public required string RowId { get; init; }

    /// <summary>Row data as JSON</summary>
    public required string DataJson { get; init; }

    /// <summary>Created timestamp (Unix milliseconds)</summary>
    public required long CreatedAt { get; init; }

    /// <summary>Modified timestamp (Unix milliseconds)</summary>
    public required long ModifiedAt { get; init; }

    /// <summary>Validation state JSON (optional)</summary>
    public string? ValidationStateJson { get; init; }
}

/// <summary>
/// Write operation for updating a single row
/// </summary>
internal sealed class UpdateRowWriteOp : WriteOperation
{
    public override WriteOperationType OperationType => WriteOperationType.UpdateRow;

    /// <summary>Row ID</summary>
    public required string RowId { get; init; }

    /// <summary>Updated row data as JSON</summary>
    public required string DataJson { get; init; }

    /// <summary>Modified timestamp (Unix milliseconds)</summary>
    public required long ModifiedAt { get; init; }

    /// <summary>Validation state JSON (optional)</summary>
    public string? ValidationStateJson { get; init; }
}

/// <summary>
/// Write operation for deleting a single row (soft delete)
/// </summary>
internal sealed class DeleteRowWriteOp : WriteOperation
{
    public override WriteOperationType OperationType => WriteOperationType.DeleteRow;

    /// <summary>Row ID to delete</summary>
    public required string RowId { get; init; }

    /// <summary>Modified timestamp (Unix milliseconds)</summary>
    public required long ModifiedAt { get; init; }
}

/// <summary>
/// Write operation for bulk inserting multiple rows
/// </summary>
internal sealed class BulkInsertWriteOp : WriteOperation
{
    public override WriteOperationType OperationType => WriteOperationType.BulkInsert;

    /// <summary>List of rows to insert</summary>
    public required IReadOnlyList<RowInsertData> Rows { get; init; }
}

/// <summary>
/// Data for a single row in bulk insert operation
/// </summary>
internal sealed class RowInsertData
{
    public required string RowId { get; init; }
    public required string DataJson { get; init; }
    public required long CreatedAt { get; init; }
    public required long ModifiedAt { get; init; }
    public string? ValidationStateJson { get; init; }
}

/// <summary>
/// Write operation for bulk updating multiple rows
/// </summary>
internal sealed class BulkUpdateWriteOp : WriteOperation
{
    public override WriteOperationType OperationType => WriteOperationType.BulkUpdate;

    /// <summary>List of rows to update</summary>
    public required IReadOnlyList<RowUpdateData> Rows { get; init; }
}

/// <summary>
/// Data for a single row in bulk update operation
/// </summary>
internal sealed class RowUpdateData
{
    public required string RowId { get; init; }
    public required string DataJson { get; init; }
    public required long ModifiedAt { get; init; }
    public string? ValidationStateJson { get; init; }
}

/// <summary>
/// Write operation for bulk deleting multiple rows
/// </summary>
internal sealed class BulkDeleteWriteOp : WriteOperation
{
    public override WriteOperationType OperationType => WriteOperationType.BulkDelete;

    /// <summary>List of row IDs to delete</summary>
    public required IReadOnlyList<string> RowIds { get; init; }

    /// <summary>Modified timestamp (Unix milliseconds)</summary>
    public required long ModifiedAt { get; init; }
}

/// <summary>
/// Write operation for updating validation state of a row
/// </summary>
internal sealed class UpdateValidationStateWriteOp : WriteOperation
{
    public override WriteOperationType OperationType => WriteOperationType.UpdateValidationState;

    /// <summary>Row ID</summary>
    public required string RowId { get; init; }

    /// <summary>Validation state JSON</summary>
    public required string ValidationStateJson { get; init; }

    /// <summary>Modified timestamp (Unix milliseconds)</summary>
    public required long ModifiedAt { get; init; }
}

/// <summary>
/// Write operation for vacuuming the database (optimization)
/// </summary>
internal sealed class VacuumWriteOp : WriteOperation
{
    public override WriteOperationType OperationType => WriteOperationType.Vacuum;
}

/// <summary>
/// SENIOR FIX: Flush writer queue operation - wait for all pending operations to complete.
/// Uses TaskCompletionSource to signal completion after all previous ops are processed.
/// CRITICAL: Prevents race condition in ReplaceAllRowsAsync.
/// </summary>
internal sealed class FlushWriteOp : WriteOperation
{
    public override WriteOperationType OperationType => WriteOperationType.Flush;

    /// <summary>TaskCompletionSource to signal flush completion</summary>
    public required TaskCompletionSource<bool> CompletionSource { get; init; }
}
