// ========== ADD THESE METHODS TO InMemoryRowStore.cs AND HybridRowStore.cs ==========
// Location: At the end of the class, before closing brace

#region BREAKING CHANGE v3.0: rowId-based helper methods

/// <summary>
/// Gets the stable rowId for a row at the specified index.
/// HELPER: Converts volatile rowIndex to stable rowId.
/// </summary>
public string? GetRowIdByIndex(int rowIndex)
{
    var row = GetRow(rowIndex);
    if (row != null && row.TryGetValue("__rowId", out var rowIdValue))
    {
        return rowIdValue?.ToString();
    }
    return null;
}

/// <summary>
/// Gets the current rowIndex for a row with the specified rowId.
/// HELPER: Converts stable rowId to volatile rowIndex (current position in view).
/// </summary>
public int? GetRowIndexById(string rowId)
{
    var allRows = GetAllRows();
    for (int i = 0; i < allRows.Count; i++)
    {
        if (allRows[i].TryGetValue("__rowId", out var rowIdValue))
        {
            if (rowIdValue?.ToString() == rowId)
            {
                return i;
            }
        }
    }
    return null;
}

/// <summary>
/// Gets a row by stable rowId.
/// STABLE: Uses rowId which persists across sort/filter/delete operations.
/// </summary>
public IReadOnlyDictionary<string, object?>? GetRowById(string rowId)
{
    var allRows = GetAllRows();
    foreach (var row in allRows)
    {
        if (row.TryGetValue("__rowId", out var rowIdValue))
        {
            if (rowIdValue?.ToString() == rowId)
            {
                return row;
            }
        }
    }
    return null;
}

/// <summary>
/// Gets a row by stable rowId (async version).
/// STABLE: Uses rowId which persists across sort/filter/delete operations.
/// </summary>
public async Task<IReadOnlyDictionary<string, object?>?> GetRowByIdAsync(string rowId, CancellationToken cancellationToken = default)
{
    var allRows = await GetAllRowsAsync(cancellationToken);
    foreach (var row in allRows)
    {
        if (row.TryGetValue("__rowId", out var rowIdValue))
        {
            if (rowIdValue?.ToString() == rowId)
            {
                return row;
            }
        }
    }
    return null;
}

/// <summary>
/// Updates a row by stable rowId.
/// STABLE: Uses rowId which persists across sort/filter/delete operations.
/// </summary>
public async Task<bool> UpdateRowByIdAsync(string rowId, IReadOnlyDictionary<string, object?> updatedRow, CancellationToken cancellationToken = default)
{
    var rowIndex = GetRowIndexById(rowId);
    if (rowIndex == null)
    {
        return false;
    }

    await UpdateRowAsync(rowIndex.Value, updatedRow, cancellationToken);
    return true;
}

/// <summary>
/// Removes a row by stable rowId.
/// STABLE: Uses rowId which persists across sort/filter/delete operations.
/// </summary>
public async Task<bool> RemoveRowByIdAsync(string rowId, CancellationToken cancellationToken = default)
{
    var rowIndex = GetRowIndexById(rowId);
    if (rowIndex == null)
    {
        return false;
    }

    await RemoveRowAsync(rowIndex.Value, cancellationToken);
    return true;
}

/// <summary>
/// Checks if a row exists by stable rowId.
/// STABLE: Uses rowId which persists across sort/filter/delete operations.
/// </summary>
public bool RowExistsById(string rowId)
{
    var allRows = GetAllRows();
    foreach (var row in allRows)
    {
        if (row.TryGetValue("__rowId", out var rowIdValue))
        {
            if (rowIdValue?.ToString() == rowId)
            {
                return true;
            }
        }
    }
    return false;
}

#endregion
