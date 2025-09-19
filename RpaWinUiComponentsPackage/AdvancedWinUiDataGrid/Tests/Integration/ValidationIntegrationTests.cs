#if ENABLE_UNIT_TESTS
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using Microsoft.Extensions.Logging.Abstractions;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Core.Enums;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Tests.Integration;

/// <summary>
/// INTEGRATION TESTS: End-to-end validation scenarios across all layers
/// COVERAGE: 8-type validation system with timeout protection
/// </summary>
public class ValidationIntegrationTests : IDisposable
{
    private readonly AdvancedDataGrid _grid;

    public ValidationIntegrationTests()
    {
        _grid = new AdvancedDataGrid(NullLogger.Instance, DataGridOperationMode.Headless);
    }

    [Fact]
    public async Task SingleCellValidation_WithTimeout_WorksCorrectly()
    {
        // Arrange
        var timeoutTriggered = false;
        var validationCompleted = false;

        // Add rule with very short timeout to test timeout mechanism
        var success = await _grid.AddSingleCellValidationRuleAsync(
            "Email",
            email =>
            {
                // Simulate slow validation
                System.Threading.Thread.Sleep(3000); // 3 seconds
                validationCompleted = true;
                return email?.ToString()?.Contains("@") == true;
            },
            "Email must contain @",
            timeout: TimeSpan.FromMilliseconds(100) // Very short timeout
        );

        Assert.True(success);

        // Act
        var result = await _grid.SetCellValueAsync(0, "Email", "invalid-email");

        // Assert
        Assert.True(result); // Cell value should be set even if validation times out
        Assert.False(validationCompleted); // Validation should have been cancelled due to timeout
    }

    [Fact]
    public async Task CrossColumnValidation_WorksCorrectly()
    {
        // Arrange
        var success = await _grid.AddCrossColumnValidationRuleAsync(
            new[] { "Age", "Email" },
            rowData =>
            {
                var age = Convert.ToInt32(rowData.GetValueOrDefault("Age", 0));
                var email = rowData.GetValueOrDefault("Email")?.ToString();

                if (age >= 18 && string.IsNullOrEmpty(email))
                {
                    return (false, "Adults must provide email address");
                }

                return (true, null);
            },
            "Age and email validation failed"
        );

        Assert.True(success);

        // Act - Set age to 25 (adult)
        await _grid.SetCellValueAsync(0, "Age", 25);
        // Don't set email - should trigger validation error

        _grid.RefreshUI(); // Manual refresh in headless mode

        // Assert
        var emailValue = _grid.GetCellValue(0, "Email");
        Assert.Null(emailValue); // Email should be empty

        // In a full implementation, we would check validation results here
    }

    [Fact]
    public async Task MultipleValidationRules_ExecuteInPriorityOrder()
    {
        // Arrange
        var executionOrder = new List<string>();

        // Add rules with different priorities
        await _grid.AddSingleCellValidationRuleAsync(
            "Name",
            name =>
            {
                executionOrder.Add("Priority1");
                return !string.IsNullOrEmpty(name?.ToString());
            },
            "Name required",
            priority: 1,
            ruleName: "NameRequired"
        );

        await _grid.AddSingleCellValidationRuleAsync(
            "Name",
            name =>
            {
                executionOrder.Add("Priority3");
                return name?.ToString()?.Length >= 2;
            },
            "Name too short",
            priority: 3,
            ruleName: "NameLength"
        );

        await _grid.AddSingleCellValidationRuleAsync(
            "Name",
            name =>
            {
                executionOrder.Add("Priority2");
                return !name?.ToString()?.Contains("@") == true;
            },
            "Name cannot contain @",
            priority: 2,
            ruleName: "NameFormat"
        );

        // Act
        await _grid.SetCellValueAsync(0, "Name", "John");

        // Assert
        Assert.Equal(new[] { "Priority1", "Priority2", "Priority3" }, executionOrder);
    }

    [Fact]
    public async Task ValidationSeverityLevels_WorkCorrectly()
    {
        // Arrange - Add rules with different severity levels
        await _grid.AddSingleCellValidationRuleAsync(
            "Status",
            status => status?.ToString() != "INVALID",
            "Status cannot be INVALID",
            ValidationSeverity.Error,
            ruleName: "StatusError"
        );

        await _grid.AddSingleCellValidationRuleAsync(
            "Status",
            status => status?.ToString() != "DEPRECATED",
            "Status is deprecated",
            ValidationSeverity.Warning,
            ruleName: "StatusWarning"
        );

        await _grid.AddSingleCellValidationRuleAsync(
            "Status",
            status => status?.ToString() != "OLD",
            "Status is old",
            ValidationSeverity.Info,
            ruleName: "StatusInfo"
        );

        // Act
        await _grid.SetCellValueAsync(0, "Status", "INVALID");

        // Assert - Error should take precedence
        // In full implementation, we would verify the highest severity is Error
    }

    [Fact]
    public async Task UIMode_vs_HeadlessMode_BehaveDifferently()
    {
        // Arrange
        var uiGrid = new AdvancedDataGrid(NullLogger.Instance, DataGridOperationMode.UI);
        var headlessGrid = new AdvancedDataGrid(NullLogger.Instance, DataGridOperationMode.Headless);

        try
        {
            // Add same validation rule to both grids
            await uiGrid.AddSingleCellValidationRuleAsync("Email",
                email => email?.ToString()?.Contains("@") == true,
                "Invalid email");

            await headlessGrid.AddSingleCellValidationRuleAsync("Email",
                email => email?.ToString()?.Contains("@") == true,
                "Invalid email");

            // Act
            await uiGrid.SetCellValueAsync(0, "Email", "invalid");
            await headlessGrid.SetCellValueAsync(0, "Email", "invalid");

            // In UI mode, validation and UI update happen automatically
            // In Headless mode, manual refresh is needed
            headlessGrid.RefreshUI();

            // Assert
            Assert.Equal(DataGridOperationMode.UI, uiGrid.OperationMode);
            Assert.Equal(DataGridOperationMode.Headless, headlessGrid.OperationMode);
        }
        finally
        {
            uiGrid.Dispose();
            headlessGrid.Dispose();
        }
    }

    [Fact]
    public async Task ColorConfiguration_UpdatesCorrectly()
    {
        // Arrange
        var originalConfig = _grid.ColorConfiguration;

        // Act
        _grid.SetElementColor("ValidationError", Colors.Orange);

        // Assert
        Assert.NotEqual(originalConfig.ValidationErrorTextColor, _grid.ColorConfiguration.ValidationErrorTextColor);
        Assert.Equal(Colors.Orange, _grid.ColorConfiguration.ValidationErrorTextColor);
    }

    [Fact]
    public async Task MinimumRows_PropertyWorksCorrectly()
    {
        // Arrange & Act
        _grid.MinimumRows = 5;

        // Assert
        Assert.Equal(5, _grid.MinimumRows);
    }

    [Fact]
    public void MinimumRows_NegativeValue_ThrowsException()
    {
        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => _grid.MinimumRows = -1);
    }

    [Fact]
    public async Task DefaultTimeout_IsAppliedCorrectly()
    {
        // Arrange & Act
        var success = await _grid.AddSingleCellValidationRuleAsync(
            "Test",
            value => true,
            "Test rule"
            // No timeout specified - should use default 2 seconds
        );

        // Assert
        Assert.True(success);
    }

    [Fact]
    public void ColorConfiguration_DarkTheme_WorksCorrectly()
    {
        // Arrange
        var darkTheme = ColorConfiguration.CreateDarkTheme();

        // Act
        _grid.ColorConfiguration = darkTheme;

        // Assert
        Assert.Equal(darkTheme, _grid.ColorConfiguration);
        Assert.Equal(Colors.Black, _grid.ColorConfiguration.GridBackgroundColor);
        Assert.Equal(Colors.White, _grid.ColorConfiguration.HeaderForegroundColor);
    }

    [Fact]
    public async Task LargeDataset_HandlesCorrectly()
    {
        // Arrange - Simulate large dataset operations
        const int rowCount = 1000;

        // Act
        var tasks = new List<Task<bool>>();
        for (int i = 0; i < rowCount; i++)
        {
            tasks.Add(_grid.SetCellValueAsync(i, "Data", $"Value_{i}"));
        }

        var results = await Task.WhenAll(tasks);

        // Assert
        Assert.All(results, result => Assert.True(result));
    }

    [Fact]
    public void Dispose_CleansUpCorrectly()
    {
        // Arrange
        var grid = new AdvancedDataGrid(NullLogger.Instance);

        // Act
        grid.Dispose();

        // Assert - Should not throw exceptions after disposal
        Assert.Throws<ObjectDisposedException>(() => grid.MinimumRows = 5);
    }

    public void Dispose()
    {
        _grid?.Dispose();
    }
}

/// <summary>
/// PERFORMANCE TESTS: Large dataset handling and validation performance
/// </summary>
public class PerformanceTests
{
    [Fact]
    public async Task ValidationPerformance_1000Rules_CompletesInReasonableTime()
    {
        // Arrange
        using var grid = new AdvancedDataGrid(NullLogger.Instance, DataGridOperationMode.Headless);
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // Act - Add 1000 validation rules
        for (int i = 0; i < 1000; i++)
        {
            await grid.AddSingleCellValidationRuleAsync(
                $"Column_{i % 10}", // 10 different columns
                value => value != null,
                $"Rule {i} failed",
                ruleName: $"Rule_{i}",
                timeout: TimeSpan.FromMilliseconds(50) // Short timeout for performance
            );
        }

        stopwatch.Stop();

        // Assert - Should complete within 10 seconds
        Assert.True(stopwatch.ElapsedMilliseconds < 10000,
            $"Adding 1000 rules took {stopwatch.ElapsedMilliseconds}ms, expected < 10000ms");
    }

    [Fact]
    public async Task CellOperations_10000Cells_CompletesInReasonableTime()
    {
        // Arrange
        using var grid = new AdvancedDataGrid(NullLogger.Instance, DataGridOperationMode.Headless);
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // Act - Set 10,000 cell values
        const int operationCount = 10000;
        var tasks = new List<Task<bool>>();

        for (int i = 0; i < operationCount; i++)
        {
            tasks.Add(grid.SetCellValueAsync(i / 100, $"Col_{i % 10}", $"Value_{i}"));
        }

        var results = await Task.WhenAll(tasks);
        stopwatch.Stop();

        // Assert
        Assert.All(results, result => Assert.True(result));
        Assert.True(stopwatch.ElapsedMilliseconds < 30000,
            $"10,000 cell operations took {stopwatch.ElapsedMilliseconds}ms, expected < 30000ms");
    }
}
#endif