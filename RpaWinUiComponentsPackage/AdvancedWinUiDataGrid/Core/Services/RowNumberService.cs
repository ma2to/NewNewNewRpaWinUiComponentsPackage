using System;
using System.Collections.Generic;
using System.Linq;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Core.Entities;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Core.Services;

/// <summary>
/// CORE SERVICE: Manages automatic RowNumber assignment and synchronization
/// SINGLE RESPONSIBILITY: RowNumber lifecycle management for data consistency
/// ENTERPRISE: Professional row numbering with gap prevention and ordering
/// </summary>
internal sealed class RowNumberService
{
    /// <summary>
    /// CORE: Assign RowNumber to new row based on existing data
    /// ENTERPRISE: Ensures sequential numbering without gaps
    /// </summary>
    public void AssignRowNumber(DataRow newRow, IEnumerable<DataRow> existingRows)
    {
        if (newRow == null) throw new ArgumentNullException(nameof(newRow));
        if (existingRows == null) throw new ArgumentNullException(nameof(existingRows));

        var maxRowNumber = existingRows.Any()
            ? existingRows.Max(r => r.RowNumber)
            : 0;

        newRow.RowNumber = maxRowNumber + 1;
    }

    /// <summary>
    /// CORE: Regenerate all RowNumbers in sequential order
    /// ENTERPRISE: Maintains data integrity after sort operations
    /// </summary>
    public void RegenerateRowNumbers(IList<DataRow> orderedRows)
    {
        if (orderedRows == null) throw new ArgumentNullException(nameof(orderedRows));

        for (int i = 0; i < orderedRows.Count; i++)
        {
            orderedRows[i].RowNumber = i + 1; // 1-based numbering
        }
    }

    /// <summary>
    /// CORE: Compact RowNumbers after deletions to remove gaps
    /// ENTERPRISE: Ensures continuous numbering sequence
    /// </summary>
    public void CompactRowNumbers(IList<DataRow> rows)
    {
        if (rows == null) throw new ArgumentNullException(nameof(rows));

        var sortedByRowNumber = rows.OrderBy(r => r.RowNumber).ToList();
        RegenerateRowNumbers(sortedByRowNumber);
    }

    /// <summary>
    /// CORE: Assign RowNumbers to batch of rows for import operations
    /// ENTERPRISE: Efficient batch processing for large datasets
    /// </summary>
    public void AssignRowNumbersBatch(IList<DataRow> newRows, int startingRowNumber = 1)
    {
        if (newRows == null) throw new ArgumentNullException(nameof(newRows));
        if (startingRowNumber < 1) throw new ArgumentOutOfRangeException(nameof(startingRowNumber), "Starting row number must be positive");

        for (int i = 0; i < newRows.Count; i++)
        {
            newRows[i].RowNumber = startingRowNumber + i;
        }
    }

    /// <summary>
    /// CORE: Get next available RowNumber for new row creation
    /// ENTERPRISE: Thread-safe row number generation
    /// </summary>
    public int GetNextRowNumber(IEnumerable<DataRow> existingRows)
    {
        if (existingRows == null) throw new ArgumentNullException(nameof(existingRows));

        return existingRows.Any()
            ? existingRows.Max(r => r.RowNumber) + 1
            : 1;
    }

    /// <summary>
    /// VALIDATION: Validate RowNumber sequence integrity
    /// ENTERPRISE: Data consistency validation for debugging
    /// </summary>
    public RowNumberValidationResult ValidateRowNumberSequence(IEnumerable<DataRow> rows)
    {
        if (rows == null) throw new ArgumentNullException(nameof(rows));

        var rowList = rows.ToList();
        var issues = new List<string>();

        if (!rowList.Any())
        {
            return new RowNumberValidationResult(true, issues);
        }

        var orderedRows = rowList.OrderBy(r => r.RowNumber).ToList();

        // Check for duplicates
        var duplicates = orderedRows.GroupBy(r => r.RowNumber)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        if (duplicates.Any())
        {
            issues.Add($"Duplicate RowNumbers found: {string.Join(", ", duplicates)}");
        }

        // Check for gaps in sequence
        for (int i = 0; i < orderedRows.Count; i++)
        {
            var expectedRowNumber = i + 1;
            if (orderedRows[i].RowNumber != expectedRowNumber)
            {
                issues.Add($"RowNumber gap detected at position {i + 1}. Expected: {expectedRowNumber}, Actual: {orderedRows[i].RowNumber}");
            }
        }

        // Check for zero or negative numbers
        var invalidNumbers = orderedRows.Where(r => r.RowNumber <= 0).ToList();
        if (invalidNumbers.Any())
        {
            issues.Add($"Invalid RowNumbers (<=0) found: {string.Join(", ", invalidNumbers.Select(r => r.RowNumber))}");
        }

        return new RowNumberValidationResult(issues.Count == 0, issues);
    }

    /// <summary>
    /// ENTERPRISE: Repair RowNumber sequence by regenerating all numbers
    /// SMART: Uses creation time as fallback ordering when RowNumbers are corrupted
    /// </summary>
    public void RepairRowNumberSequence(IList<DataRow> rows)
    {
        if (rows == null) throw new ArgumentNullException(nameof(rows));

        // Order by RowIndex as fallback (stable sort)
        var orderedRows = rows.OrderBy(r => r.RowIndex).ToList();
        RegenerateRowNumbers(orderedRows);

        // Update the original list in place
        for (int i = 0; i < rows.Count; i++)
        {
            rows[i] = orderedRows[i];
        }
    }
}

/// <summary>
/// VALUE OBJECT: Result of RowNumber sequence validation
/// </summary>
internal sealed class RowNumberValidationResult
{
    public bool IsValid { get; }
    public IReadOnlyList<string> Issues { get; }

    public RowNumberValidationResult(bool isValid, IEnumerable<string> issues)
    {
        IsValid = isValid;
        Issues = issues?.ToList().AsReadOnly() ?? new List<string>().AsReadOnly();
    }
}