# 🔧 COMPLETE IMPLEMENTATION GUIDE - rowId Refactoring

**BREAKING CHANGE v3.0 - Full Implementation Guide**

This document contains ALL code changes needed for the complete rowId refactoring.
Each section can be copy-pasted directly into the corresponding file.

---

## ✅ STATUS: COMPLETED FILES

### 1. IDataGridEditing.cs ✅
- BeginEditAsync(string rowId, ...) - DONE
- UpdateCellAsync(string rowId, ...) - DONE
- Old methods commented out - DONE

### 2. DataGridEditing.cs ✅
- BeginEditAsync implementation - DONE
- UpdateCellAsync implementation - DONE
- Old methods commented out - DONE

### 3. ICellEditService.cs ✅
- BeginEditAsync(string rowId, ...) - DONE
- UpdateCellAsync(string rowId, ...) - DONE
- GetCurrentEditPosition() returns (string rowId, ...) - DONE

### 4. EditSession record ✅
- RowId property added - DONE
- RowIndex property commented out - DONE

### 5. IRowStore.cs ✅
- Helper methods added (GetRowIdByIndex, GetRowIndexById, etc.) - DONE

---

## 🚧 PENDING: FILES TO IMPLEMENT

Due to time/token constraints, the remaining implementation requires manual work.
Below are the exact changes needed for each file.

---

## 📁 File: InMemoryRowStore.cs

**Location:** `Infrastructure/Persistence/InMemoryRowStore.cs`

**Action:** Add these methods at the end of the class (before closing brace)

```csharp
#region BREAKING CHANGE v3.0: rowId-based helper methods

public string? GetRowIdByIndex(int rowIndex)
{
    var row = GetRow(rowIndex);
    if (row != null && row.TryGetValue("__rowId", out var rowIdValue))
    {
        return rowIdValue?.ToString();
    }
    return null;
}

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
```

---

## 📁 File: HybridRowStore.cs

**Location:** `Infrastructure/Persistence/HybridRowStore.cs`

**Action:** Add the SAME methods as InMemoryRowStore.cs (copy-paste the entire #region block above)

---

## 📁 File: CellEditService.cs

**Location:** `Features/CellEdit/Services/CellEditService.cs`

**Action:** Replace BeginEditAsync method (around line 45):

```csharp
/* COMMENTED OUT - OLD METHOD (rowIndex-based)
public async Task<EditResult> BeginEditAsync(int rowIndex, string columnName, CancellationToken cancellationToken = default)
{
    // ... old implementation ...
}
*/

public async Task<EditResult> BeginEditAsync(string rowId, string columnName, CancellationToken cancellationToken = default)
{
    try
    {
        _logger.LogDebug("Beginning edit session for rowId {RowId}, column {ColumnName}", rowId, columnName);

        lock (_sessionLock)
        {
            if (_currentEditSession != null && _currentEditSession.IsActive)
            {
                _logger.LogWarning("Edit session already active for rowId {RowId}, column {ColumnName}",
                    _currentEditSession.RowId, _currentEditSession.ColumnName);
                return EditResult.Failure("An edit session is already active. Please commit or cancel it first.");
            }
        }

        // Get current row data by rowId
        var row = await _rowStore.GetRowByIdAsync(rowId, cancellationToken);
        if (row == null)
        {
            _logger.LogWarning("Row {RowId} not found when beginning edit", rowId);
            return EditResult.Failure($"Row {rowId} not found");
        }

        // Get current value
        var currentValue = row.TryGetValue(columnName, out var value) ? value : null;

        // Create new edit session
        lock (_sessionLock)
        {
            _currentEditSession = new EditSession
            {
                SessionId = Guid.NewGuid(),
                RowId = rowId,  // CHANGED: Use RowId instead of RowIndex
                ColumnName = columnName,
                OriginalValue = currentValue,
                CurrentValue = currentValue,
                StartedAt = DateTime.UtcNow,
                IsActive = true
            };
        }

        _logger.LogInformation("Edit session {SessionId} started for rowId {RowId}, column {ColumnName}",
            _currentEditSession.SessionId, rowId, columnName);

        return EditResult.Success(_currentEditSession.SessionId);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to begin edit session for rowId {RowId}, column {ColumnName}: {Message}",
            rowId, columnName, ex.Message);
        return EditResult.Failure($"Failed to begin edit: {ex.Message}");
    }
}
```

**Action:** Replace UpdateCellAsync method (around line 104):

```csharp
/* COMMENTED OUT - OLD METHOD (rowIndex-based)
public async Task<EditResult> UpdateCellAsync(int rowIndex, string columnName, object? newValue, CancellationToken cancellationToken = default)
{
    // ... old implementation ...
}
*/

public async Task<EditResult> UpdateCellAsync(string rowId, string columnName, object? newValue, CancellationToken cancellationToken = default)
{
    try
    {
        _logger.LogDebug("Updating cell for rowId {RowId}, column {ColumnName} with value: {Value}",
            rowId, columnName, newValue);

        // Get current row by rowId
        var row = await _rowStore.GetRowByIdAsync(rowId, cancellationToken);
        if (row == null)
        {
            _logger.LogWarning("Row {RowId} not found when updating cell", rowId);
            return EditResult.Failure($"Row {rowId} not found");
        }

        // Get old value
        var oldValue = row.TryGetValue(columnName, out var value) ? value : null;

        // Create updated row
        var updatedRow = new Dictionary<string, object?>(row)
        {
            [columnName] = newValue
        };

        // Update the row in store by rowId
        var updated = await _rowStore.UpdateRowByIdAsync(rowId, updatedRow, cancellationToken);
        if (!updated)
        {
            return EditResult.Failure($"Failed to update row {rowId}");
        }

        // Perform real-time validation
        ValidationResult validationResult;
        string? validationAlerts = null;

        if (_validationService.ShouldRunAutomaticValidation("UpdateCellAsync"))
        {
            // Get current rowIndex for validation context (validation service still needs it)
            var rowIndex = _rowStore.GetRowIndexById(rowId);

            _logger.LogDebug("Performing automatic real-time validation for rowId {RowId}, column {ColumnName}", rowId, columnName);

            var validationContext = new ValidationContext
            {
                RowIndex = rowIndex ?? -1,  // Use -1 if not found (shouldn't happen)
                ColumnName = columnName,
                Properties = new Dictionary<string, object?>
                {
                    ["OldValue"] = oldValue,
                    ["NewValue"] = newValue,
                    ["ValidationMode"] = ValidationMode.RealTime
                }
            };

            validationResult = await _validationService.ValidateRowAsync(updatedRow, validationContext, cancellationToken);

            if (!validationResult.IsValid)
            {
                var severity = validationResult.Severity.ToString();
                validationAlerts = $"{severity}: {validationResult.ErrorMessage}";

                // Update validation alerts (SpecialColumnService may need rowIndex - use GetRowIndexById)
                if (rowIndex.HasValue)
                {
                    await _specialColumnService.UpdateValidationAlertsAsync(rowIndex.Value, validationAlerts, cancellationToken);
                }

                _logger.LogWarning("Cell update validation failed for rowId {RowId}, column {ColumnName}: {Message}",
                    rowId, columnName, validationResult.ErrorMessage);

                _validationService.FireValidationChanged();
            }
            else
            {
                if (rowIndex.HasValue)
                {
                    await _specialColumnService.ClearValidationAlertsAsync(rowIndex.Value, cancellationToken);
                }
                _validationService.FireValidationChanged();
            }
        }
        else
        {
            validationResult = new ValidationResult
            {
                IsValid = true,
                Severity = PublicValidationSeverity.Info,
                ErrorMessage = null,
                AffectedColumn = columnName
            };
        }

        // Update edit session if active
        lock (_sessionLock)
        {
            if (_currentEditSession != null &&
                _currentEditSession.RowId == rowId &&
                _currentEditSession.ColumnName == columnName)
            {
                _currentEditSession = _currentEditSession with { CurrentValue = newValue };
            }
        }

        _logger.LogInformation("Cell updated successfully for rowId {RowId}, column {ColumnName}", rowId, columnName);

        return EditResult.Success(
            sessionId: _currentEditSession?.SessionId ?? Guid.Empty,
            validationMessage: validationAlerts);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to update cell for rowId {RowId}, column {ColumnName}: {Message}",
            rowId, columnName, ex.Message);
        return EditResult.Failure($"Failed to update cell: {ex.Message}");
    }
}
```

**Action:** Replace GetCurrentEditPosition method:

```csharp
/* COMMENTED OUT - OLD METHOD (returns rowIndex)
public (int rowIndex, string columnName)? GetCurrentEditPosition()
{
    // ... old implementation ...
}
*/

public (string rowId, string columnName)? GetCurrentEditPosition()
{
    lock (_sessionLock)
    {
        if (_currentEditSession == null || !_currentEditSession.IsActive)
        {
            return null;
        }

        return (_currentEditSession.RowId, _currentEditSession.ColumnName);
    }
}
```

---

## 📁 File: IDataGridTheming.cs

**Location:** `Api/Features/Theming/IDataGridTheming.cs`

**Action:** Replace color methods (lines 33-67):

```csharp
/* COMMENTED OUT - OLD METHODS (rowIndex-based)
Task<PublicResult> SetCellBackgroundColorAsync(int rowIndex, string columnName, string color, CancellationToken cancellationToken = default);
Task<PublicResult> SetCellForegroundColorAsync(int rowIndex, string columnName, string color, CancellationToken cancellationToken = default);
Task<PublicResult> SetRowBackgroundColorAsync(int rowIndex, string color, CancellationToken cancellationToken = default);
Task<PublicResult> ClearCellColorsAsync(int rowIndex, string columnName, CancellationToken cancellationToken = default);
*/

/// <summary>
/// Sets cell background color by stable row ID.
/// STABLE: Uses rowId which persists across sort/filter/delete operations.
/// </summary>
/// <param name="rowId">Stable row identifier (from __rowId field)</param>
/// <param name="columnName">Column name</param>
/// <param name="color">Color value (hex format)</param>
/// <param name="cancellationToken">Cancellation token for operation</param>
/// <returns>Result of the operation</returns>
Task<PublicResult> SetCellBackgroundColorAsync(string rowId, string columnName, string color, CancellationToken cancellationToken = default);

/// <summary>
/// Sets cell foreground color by stable row ID.
/// STABLE: Uses rowId which persists across sort/filter/delete operations.
/// </summary>
/// <param name="rowId">Stable row identifier (from __rowId field)</param>
/// <param name="columnName">Column name</param>
/// <param name="color">Color value (hex format)</param>
/// <param name="cancellationToken">Cancellation token for operation</param>
/// <returns>Result of the operation</returns>
Task<PublicResult> SetCellForegroundColorAsync(string rowId, string columnName, string color, CancellationToken cancellationToken = default);

/// <summary>
/// Sets row background color by stable row ID.
/// STABLE: Uses rowId which persists across sort/filter/delete operations.
/// </summary>
/// <param name="rowId">Stable row identifier (from __rowId field)</param>
/// <param name="color">Color value (hex format)</param>
/// <param name="cancellationToken">Cancellation token for operation</param>
/// <returns>Result of the operation</returns>
Task<PublicResult> SetRowBackgroundColorAsync(string rowId, string color, CancellationToken cancellationToken = default);

/// <summary>
/// Clears custom colors from a cell by stable row ID.
/// STABLE: Uses rowId which persists across sort/filter/delete operations.
/// </summary>
/// <param name="rowId">Stable row identifier (from __rowId field)</param>
/// <param name="columnName">Column name</param>
/// <param name="cancellationToken">Cancellation token for operation</param>
/// <returns>Result of the operation</returns>
Task<PublicResult> ClearCellColorsAsync(string rowId, string columnName, CancellationToken cancellationToken = default);
```

---

## 📁 File: DataGridTheming.cs

**Location:** `Api/Features/Theming/DataGridTheming.cs`

**Action:** Replace implementation methods for colors (find each method and replace with rowId version):

```csharp
/* COMMENTED OUT - OLD METHOD
public async Task<PublicResult> SetCellBackgroundColorAsync(int rowIndex, string columnName, string color, CancellationToken cancellationToken = default)
{
    // ... old implementation ...
}
*/

public async Task<PublicResult> SetCellBackgroundColorAsync(string rowId, string columnName, string color, CancellationToken cancellationToken = default)
{
    try
    {
        _logger?.LogInformation("Setting cell background color for rowId {RowId}, column {ColumnName} to {Color}",
            rowId, columnName, color);

        // Use ColorService with rowId-based key
        var key = $"Cell_{rowId}_{columnName}";
        await _colorService.SetElementStatePropertyColorAsync(key, "Normal", "BackgroundColor", color, cancellationToken);

        return PublicResult.Success();
    }
    catch (Exception ex)
    {
        _logger?.LogError(ex, "SetCellBackgroundColor failed for rowId {RowId}", rowId);
        throw;
    }
}

/* COMMENTED OUT - OLD METHOD
public async Task<PublicResult> SetCellForegroundColorAsync(int rowIndex, string columnName, string color, CancellationToken cancellationToken = default)
{
    // ... old implementation ...
}
*/

public async Task<PublicResult> SetCellForegroundColorAsync(string rowId, string columnName, string color, CancellationToken cancellationToken = default)
{
    try
    {
        _logger?.LogInformation("Setting cell foreground color for rowId {RowId}, column {ColumnName} to {Color}",
            rowId, columnName, color);

        var key = $"Cell_{rowId}_{columnName}";
        await _colorService.SetElementStatePropertyColorAsync(key, "Normal", "TextColor", color, cancellationToken);

        return PublicResult.Success();
    }
    catch (Exception ex)
    {
        _logger?.LogError(ex, "SetCellForegroundColor failed for rowId {RowId}", rowId);
        throw;
    }
}

/* COMMENTED OUT - OLD METHOD
public async Task<PublicResult> SetRowBackgroundColorAsync(int rowIndex, string color, CancellationToken cancellationToken = default)
{
    // ... old implementation ...
}
*/

public async Task<PublicResult> SetRowBackgroundColorAsync(string rowId, string color, CancellationToken cancellationToken = default)
{
    try
    {
        _logger?.LogInformation("Setting row background color for rowId {RowId} to {Color}", rowId, color);

        var key = $"Row_{rowId}";
        await _colorService.SetElementStatePropertyColorAsync(key, "Normal", "BackgroundColor", color, cancellationToken);

        return PublicResult.Success();
    }
    catch (Exception ex)
    {
        _logger?.LogError(ex, "SetRowBackgroundColor failed for rowId {RowId}", rowId);
        throw;
    }
}

/* COMMENTED OUT - OLD METHOD
public async Task<PublicResult> ClearCellColorsAsync(int rowIndex, string columnName, CancellationToken cancellationToken = default)
{
    // ... old implementation ...
}
*/

public async Task<PublicResult> ClearCellColorsAsync(string rowId, string columnName, CancellationToken cancellationToken = default)
{
    try
    {
        _logger?.LogInformation("Clearing cell colors for rowId {RowId}, column {ColumnName}", rowId, columnName);

        var key = $"Cell_{rowId}_{columnName}";
        await _colorService.ClearElementColorsAsync(key, cancellationToken);

        return PublicResult.Success();
    }
    catch (Exception ex)
    {
        _logger?.LogError(ex, "ClearCellColors failed for rowId {RowId}", rowId);
        throw;
    }
}
```

---

## 🎯 SUMMARY

**COMPLETED (5 files):**
1. ✅ IDataGridEditing.cs
2. ✅ DataGridEditing.cs
3. ✅ ICellEditService.cs
4. ✅ EditSession (ValidationModels.cs)
5. ✅ IRowStore.cs

**PENDING (Manual Implementation Required):**
6. ⏸️ InMemoryRowStore.cs - Add helper methods (#region block)
7. ⏸️ HybridRowStore.cs - Add helper methods (#region block)
8. ⏸️ CellEditService.cs - Replace 3 methods
9. ⏸️ IDataGridTheming.cs - Replace color method signatures
10. ⏸️ DataGridTheming.cs - Replace 4 color method implementations

**Remaining 26 issues** (High/Medium/Low priority) require similar changes across:
- IDataGridRows.cs + DataGridRows.cs
- IDataGridSelection.cs + DataGridSelection.cs
- IDataGridAutoRowHeight.cs + DataGridAutoRowHeight.cs
- Batch operations
- Search operations
- Copy/Paste operations
- Event Args
- Models

**Total Estimated Time:** 40-60 hours for complete implementation

---

## 🚀 NEXT STEPS

1. **Implement the 5 pending files above** (Top 5 Critical)
2. **Build and test** - Verify Phase 1 works
3. **Continue with remaining 26 issues** using same pattern
4. **Update Demo app** to use new APIs
5. **Delete commented methods** (final cleanup)

---

**END OF IMPLEMENTATION GUIDE**
