using System;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Core.ValueObjects;

/// <summary>
/// DOMAIN: Immutable value object representing a cell's position in the grid
/// ENTERPRISE: Type-safe cell addressing for navigation and operations
/// </summary>
internal readonly record struct CellAddress : IComparable<CellAddress>
{
    public int RowIndex { get; }
    public int ColumnIndex { get; }

    /// <summary>
    /// BACKWARD COMPATIBILITY: Alias for RowIndex to maintain compatibility with existing code
    /// ENTERPRISE: Provides seamless migration path for legacy implementations
    /// </summary>
    public int Row => RowIndex;

    public CellAddress(int rowIndex, int columnIndex)
    {
        if (rowIndex < 0) throw new ArgumentOutOfRangeException(nameof(rowIndex), "Row index cannot be negative");
        if (columnIndex < 0) throw new ArgumentOutOfRangeException(nameof(columnIndex), "Column index cannot be negative");

        RowIndex = rowIndex;
        ColumnIndex = columnIndex;
    }

    /// <summary>Check if this cell is adjacent to another cell</summary>
    public bool IsAdjacentTo(CellAddress other)
    {
        var rowDiff = Math.Abs(RowIndex - other.RowIndex);
        var colDiff = Math.Abs(ColumnIndex - other.ColumnIndex);
        return (rowDiff == 1 && colDiff == 0) || (rowDiff == 0 && colDiff == 1);
    }

    /// <summary>Get cell address offset by specified amounts</summary>
    public CellAddress Offset(int rowOffset, int columnOffset)
    {
        var newRow = RowIndex + rowOffset;
        var newCol = ColumnIndex + columnOffset;

        if (newRow < 0 || newCol < 0)
            throw new ArgumentOutOfRangeException("Offset results in negative indices");

        return new CellAddress(newRow, newCol);
    }

    /// <summary>Get next cell in row (right)</summary>
    public CellAddress NextInRow() => Offset(0, 1);

    /// <summary>Get previous cell in row (left)</summary>
    public CellAddress PreviousInRow() => ColumnIndex > 0 ? Offset(0, -1) : this;

    /// <summary>Get cell below</summary>
    public CellAddress Below() => Offset(1, 0);

    /// <summary>Get cell above</summary>
    public CellAddress Above() => RowIndex > 0 ? Offset(-1, 0) : this;

    public int CompareTo(CellAddress other)
    {
        var rowComparison = RowIndex.CompareTo(other.RowIndex);
        return rowComparison != 0 ? rowComparison : ColumnIndex.CompareTo(other.ColumnIndex);
    }

    public static bool operator >(CellAddress left, CellAddress right) => left.CompareTo(right) > 0;
    public static bool operator <(CellAddress left, CellAddress right) => left.CompareTo(right) < 0;
    public static bool operator >=(CellAddress left, CellAddress right) => left.CompareTo(right) >= 0;
    public static bool operator <=(CellAddress left, CellAddress right) => left.CompareTo(right) <= 0;

    /// <summary>Excel-style cell address (A1, B2, etc.)</summary>
    public string ToExcelAddress()
    {
        var columnName = "";
        var tempCol = ColumnIndex + 1; // Convert to 1-based

        while (tempCol > 0)
        {
            tempCol--;
            columnName = (char)('A' + tempCol % 26) + columnName;
            tempCol /= 26;
        }

        return $"{columnName}{RowIndex + 1}";
    }

    public override string ToString() => $"[{RowIndex}, {ColumnIndex}]";
}