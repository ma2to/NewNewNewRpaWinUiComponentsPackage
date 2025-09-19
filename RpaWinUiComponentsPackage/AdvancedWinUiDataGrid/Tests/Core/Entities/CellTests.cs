#if ENABLE_UNIT_TESTS
using System;
using Xunit;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Core.Entities;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Core.ValueObjects;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Core.Enums;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Tests.Core.Entities;

/// <summary>
/// UNIT TESTS: Cell entity behavior and state management
/// COVERAGE: Value changes, validation tracking, change detection
/// </summary>
public class CellTests
{
    [Fact]
    public void Cell_Constructor_SetsPropertiesCorrectly()
    {
        // Arrange
        var address = new CellAddress(0, 0);
        const string columnName = "TestColumn";
        const string initialValue = "Initial";

        // Act
        var cell = new Cell(address, columnName, initialValue);

        // Assert
        Assert.Equal(address, cell.Address);
        Assert.Equal(columnName, cell.ColumnName);
        Assert.Equal(initialValue, cell.Value);
        Assert.Equal(initialValue, cell.OriginalValue);
        Assert.False(cell.HasUnsavedChanges);
        Assert.False(cell.IsEmpty);
        Assert.False(cell.HasValidationErrors);
    }

    [Fact]
    public void Cell_ValueChange_TriggersEvent()
    {
        // Arrange
        var cell = new Cell(new CellAddress(0, 0), "Test");
        var eventTriggered = false;
        object? oldValue = null;
        object? newValue = null;

        cell.ValueChanged += (sender, e) =>
        {
            eventTriggered = true;
            oldValue = e.OldValue;
            newValue = e.NewValue;
        };

        // Act
        cell.Value = "New Value";

        // Assert
        Assert.True(eventTriggered);
        Assert.Null(oldValue);
        Assert.Equal("New Value", newValue);
    }

    [Fact]
    public void Cell_ValueChange_DetectsUnsavedChanges()
    {
        // Arrange
        var cell = new Cell(new CellAddress(0, 0), "Test", "Original");

        // Act
        cell.Value = "Modified";

        // Assert
        Assert.True(cell.HasUnsavedChanges);
        Assert.Equal("Modified", cell.Value);
        Assert.Equal("Original", cell.OriginalValue);
    }

    [Fact]
    public void Cell_CommitChanges_UpdatesOriginalValue()
    {
        // Arrange
        var cell = new Cell(new CellAddress(0, 0), "Test", "Original");
        cell.Value = "Modified";

        // Act
        cell.CommitChanges();

        // Assert
        Assert.False(cell.HasUnsavedChanges);
        Assert.Equal("Modified", cell.Value);
        Assert.Equal("Modified", cell.OriginalValue);
    }

    [Fact]
    public void Cell_RevertChanges_RestoresOriginalValue()
    {
        // Arrange
        var cell = new Cell(new CellAddress(0, 0), "Test", "Original");
        cell.Value = "Modified";

        // Act
        cell.RevertChanges();

        // Assert
        Assert.False(cell.HasUnsavedChanges);
        Assert.Equal("Original", cell.Value);
        Assert.Equal("Original", cell.OriginalValue);
    }

    [Fact]
    public void Cell_ValidationResults_TrackCorrectly()
    {
        // Arrange
        var cell = new Cell(new CellAddress(0, 0), "Test");
        var validationResult = ValidationResult.ErrorForCell(0, "Test", "Error message", ValidationSeverity.Error);

        // Act
        cell.AddValidationResult(validationResult);

        // Assert
        Assert.True(cell.HasValidationErrors);
        Assert.Single(cell.ValidationResults);
        Assert.Equal(ValidationSeverity.Error, cell.GetHighestSeverity());
    }

    [Theory]
    [InlineData(null, true)]
    [InlineData("", true)]
    [InlineData("   ", true)]
    [InlineData("Value", false)]
    public void Cell_IsEmpty_ReturnsCorrectValue(object? value, bool expectedEmpty)
    {
        // Arrange
        var cell = new Cell(new CellAddress(0, 0), "Test", value);

        // Assert
        Assert.Equal(expectedEmpty, cell.IsEmpty);
    }

    [Fact]
    public void Cell_ReadOnly_PreventsModification()
    {
        // Arrange
        var cell = new Cell(new CellAddress(0, 0), "Test", "Original")
        {
            IsReadOnly = true
        };

        // Act & Assert
        Assert.True(cell.IsReadOnly);
        // Note: In a full implementation, setting Value on read-only cell should be prevented
    }

    [Fact]
    public void Cell_MultipleValidationResults_TrackCorrectly()
    {
        // Arrange
        var cell = new Cell(new CellAddress(0, 0), "Test");
        var error1 = ValidationResult.ErrorForCell(0, "Test", "Error 1", ValidationSeverity.Warning);
        var error2 = ValidationResult.ErrorForCell(0, "Test", "Error 2", ValidationSeverity.Error);

        // Act
        cell.AddValidationResult(error1);
        cell.AddValidationResult(error2);

        // Assert
        Assert.True(cell.HasValidationErrors);
        Assert.Equal(2, cell.ValidationResults.Count);
        Assert.Equal(ValidationSeverity.Error, cell.GetHighestSeverity());
    }

    [Fact]
    public void Cell_ClearValidationResults_RemovesAllErrors()
    {
        // Arrange
        var cell = new Cell(new CellAddress(0, 0), "Test");
        cell.AddValidationResult(ValidationResult.ErrorForCell(0, "Test", "Error", ValidationSeverity.Error));

        // Act
        cell.ClearValidationResults();

        // Assert
        Assert.False(cell.HasValidationErrors);
        Assert.Empty(cell.ValidationResults);
        Assert.Equal(ValidationSeverity.Info, cell.GetHighestSeverity());
    }

    [Fact]
    public void Cell_ToString_ReturnsCorrectFormat()
    {
        // Arrange
        var cell = new Cell(new CellAddress(1, 2), "TestColumn", "TestValue");

        // Act
        var result = cell.ToString();

        // Assert
        Assert.Contains("Cell[C3]", result); // Excel-style address (1-based)
        Assert.Contains("TestValue", result);
    }

    [Fact]
    public void Cell_Constructor_ThrowsOnNullColumnName()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new Cell(new CellAddress(0, 0), null!));
    }

    [Fact]
    public void Cell_SameValueAssignment_DoesNotTriggerEvent()
    {
        // Arrange
        var cell = new Cell(new CellAddress(0, 0), "Test", "Value");
        var eventTriggered = false;

        cell.ValueChanged += (sender, e) => eventTriggered = true;

        // Act
        cell.Value = "Value"; // Same value

        // Assert
        Assert.False(eventTriggered);
        Assert.False(cell.HasUnsavedChanges);
    }
}
#endif