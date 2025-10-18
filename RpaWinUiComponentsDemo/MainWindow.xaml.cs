using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.UIControls;

namespace RpaWinUiComponents.Demo;

/// <summary>
/// Simple demo application for testing AdvancedWinUiDataGrid
/// Updated to use current public API (2025) with UI component
/// </summary>
public sealed partial class MainWindow : Window
{
    private IAdvancedDataGridFacade? _gridFacade;
    private AdvancedDataGridControl? _gridControl;
    private bool _isInitialized = false;
    private readonly System.Text.StringBuilder _logOutput = new();
    private AdvancedDataGridOptions? _currentOptions;

    public MainWindow()
    {
        this.InitializeComponent();
        AddLogMessage("=== Demo Application Started ===");
        AddLogMessage("Click 'Initialize Grid' to begin");
    }

    #region Initialization

    private async void InitButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            AddLogMessage("");
            AddLogMessage("=== INITIALIZING GRID ===");

            // Use App's LoggerFactory which logs to file
            var loggerFactory = App.LoggerFactory ?? LoggerFactory.Create(builder =>
            {
                builder.AddConsole();
                builder.SetMinimumLevel(LogLevel.Debug);
            });

            AddLogMessage("Using App's LoggerFactory for component logging to file");
            if (!string.IsNullOrEmpty(App.CurrentLogFilePath))
            {
                AddLogMessage($"Log file: {App.CurrentLogFilePath}");
            }

            // Create grid options with DispatcherQueue for UI operations
            var options = new AdvancedDataGridOptions
            {
                OperationMode = PublicDataGridOperationMode.Interactive,
                EnableParallelProcessing = true,
                EnableLinqOptimizations = true,
                EnableCaching = true,
                BatchSize = 1000,
                MinimumLogLevel = LogLevel.Debug, // Changed to Debug for detailed logging
                LoggerFactory = loggerFactory,     // CRITICAL: Pass logger factory to component
                DispatcherQueue = this.DispatcherQueue,

                // ENABLE SPECIAL COLUMNS for testing
                EnableRowNumberColumn = true,
                EnableCheckboxColumn = true,
                EnableValidationAlertsColumn = true,
                EnableDeleteRowColumn = true,
                ValidationAlertsColumnMinWidth = 150.0,

                // AUTOMATIC VALIDATION: Validates on import, paste, edit, row operations
                ValidationAutomationMode = ValidationAutomationMode.Automatic,
                EnableBatchValidation = true,      // Validate after import/paste/smart operations
                EnableRealTimeValidation = true    // Validate on cell/row edits
            };

            // Store options for later use in UI refresh
            _currentOptions = options;

            AddLogMessage("Creating DataGrid facade...");
            _gridFacade = AdvancedDataGridFacadeFactory.CreateStandalone(options, loggerFactory, this.DispatcherQueue);

            // CRITICAL: Get UI control from facade (Interactive mode)
            // Facade manages ViewModel internally and InternalUIUpdateHandler auto-updates UI
            // Demo should NOT create own ViewModel or subscribe to data refresh manually
            AddLogMessage("Getting UI control from facade (component-managed ViewModel)...");
            _gridControl = _gridFacade.GetUIControl();

            if (_gridControl == null)
            {
                AddLogMessage("✗ Failed to get UI control from facade");
                return;
            }

            // Wire up special column events (optional - for application-level tracking)
            // NOTE: In Interactive mode, delete and auto-expand are handled automatically by InternalUIOperationHandler
            // Application code only needs to subscribe to these events for tracking/logging purposes
            _gridControl.RowSelectionChanged += OnRowSelectionChanged;

            // Add UI control to container
            GridContainer.Child = _gridControl;

            if (_gridFacade != null && _gridControl != null)
            {
                _isInitialized = true;
                AddLogMessage("✓ Grid created successfully with component-managed ViewModel!");
                AddLogMessage("✓ InternalUIUpdateHandler will auto-update UI (NO manual subscription needed)");

                // Define validation rules after grid initialization
                AddLogMessage("");
                await DefineValidationRulesAsync();

                AddLogMessage("");
                AddLogMessage("Available operations:");
                AddLogMessage("- Import Dictionary: Import sample data and display in grid");
                AddLogMessage("- Export Dictionary: Export current data");
                AddLogMessage("- Add Row: Add new row to grid");
                AddLogMessage("- Validate All: Validate all rows with defined rules");
                AddLogMessage("- Get Statistics: Show row/column count");
                AddLogMessage("");
                AddLogMessage("NOTE: UI will auto-refresh after data operations (Interactive mode)");
                AddLogMessage("NOTE: Validation rules are active - errors will appear in ValidationAlerts column");
            }
            else
            {
                AddLogMessage("✗ Failed to create grid facade or UI control");
            }

            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            AddLogMessage($"✗ Exception: {ex.Message}");
            AddLogMessage($"  Stack: {ex.StackTrace}");
        }
    }

    private void InitWithValidationButton_Click(object sender, RoutedEventArgs e)
    {
        AddLogMessage("Init with validation not implemented in this demo");
    }

    #endregion

    #region Import Operations

    private async void ImportDictionaryButton_Click(object sender, RoutedEventArgs e)
    {
        if (!_isInitialized || _gridFacade == null || _gridControl == null)
        {
            AddLogMessage("⚠ Initialize grid first!");
            return;
        }

        try
        {
            AddLogMessage("");
            AddLogMessage("=== IMPORTING DATA ===");

            // Generate test data (100 rows for testing validation and smart delete)
            var testData = GenerateTestData(100, 5);

            AddLogMessage($"Importing {testData.Count} rows...");

            // Create import command
            var command = ImportDataCommand.FromDictionaries(testData);

            // Execute import via facade (using new modular API)
            var result = await _gridFacade.IO.ImportAsync(command, default);

            if (result.IsSuccess)
            {
                AddLogMessage($"✓ Import successful: {result.ImportedRows} rows imported to facade");
                AddLogMessage($"  Duration: {result.ImportTime.TotalMilliseconds:F0}ms");
                AddLogMessage("⏳ Waiting for automatic UI refresh...");
            }
            else
            {
                AddLogMessage($"✗ Import failed: {string.Join(", ", result.ErrorMessages)}");
            }
        }
        catch (Exception ex)
        {
            AddLogMessage($"✗ Exception: {ex.Message}");
            AddLogMessage($"  Stack: {ex.StackTrace}");
        }
    }

    private void ImportDataTableButton_Click(object sender, RoutedEventArgs e)
    {
        AddLogMessage("DataTable import not implemented in this demo");
    }

    #endregion

    #region Export Operations

    private async void ExportDictionaryButton_Click(object sender, RoutedEventArgs e)
    {
        if (!_isInitialized || _gridFacade == null || _gridControl == null)
        {
            AddLogMessage("⚠ Initialize grid first!");
            return;
        }

        try
        {
            AddLogMessage("");
            AddLogMessage("=== EXPORTING DATA ===");

            // Create export command
            var command = ExportDataCommand.ToDictionary();

            // Execute export (using new modular API)
            var result = await _gridFacade.IO.ExportAsync(command, default);

            if (result.IsSuccess)
            {
                AddLogMessage($"✓ Export successful: {result.ExportedRows} rows exported");
                AddLogMessage($"  Duration: {result.ExportTime.TotalMilliseconds:F0}ms");

                // Show sample data
                if (result.ExportedData is List<Dictionary<string, object?>> data && data.Count > 0)
                {
                    AddLogMessage($"  First row: {string.Join(", ", data[0].Take(3).Select(kvp => $"{kvp.Key}={kvp.Value}"))}...");
                }
            }
            else
            {
                AddLogMessage($"✗ Export failed: {string.Join(", ", result.ErrorMessages)}");
            }
        }
        catch (Exception ex)
        {
            AddLogMessage($"✗ Exception: {ex.Message}");
            AddLogMessage($"  Stack: {ex.StackTrace}");
        }
    }

    private void ExportDataTableButton_Click(object sender, RoutedEventArgs e)
    {
        AddLogMessage("DataTable export not implemented in this demo");
    }

    #endregion

    #region Validation Operations

    private async void ValidateAllButton_Click(object sender, RoutedEventArgs e)
    {
        if (!_isInitialized || _gridFacade == null || _gridControl == null)
        {
            AddLogMessage("⚠ Initialize grid first!");
            return;
        }

        try
        {
            AddLogMessage("");
            AddLogMessage("=== VALIDATING ALL ROWS ===");

            // Trigger validation for all rows with detailed statistics
            var result = await _gridFacade.Validation.ValidateAllWithStatisticsAsync(
                onlyFiltered: false,
                onlyChecked: false,
                cancellationToken: CancellationToken.None);

            AddLogMessage($"✓ Validation completed successfully!");
            AddLogMessage($"  Total rows validated: {result.TotalRows}");
            AddLogMessage($"  Valid rows: {result.ValidRows}");
            AddLogMessage($"  Invalid rows: {result.TotalRows - result.ValidRows}");
            AddLogMessage($"  Total errors: {result.TotalErrors}");
            AddLogMessage($"  Duration: {result.Duration.TotalMilliseconds:F0}ms");

            if (result.TotalRows - result.ValidRows > 0)
            {
                AddLogMessage("");
                AddLogMessage($"⚠ Found {result.TotalRows - result.ValidRows} rows with validation errors");
                AddLogMessage("Check the ValidationAlerts column (🔔) for details");

                // Show errors by severity if available
                if (result.ErrorsBySeverity.Count > 0)
                {
                    AddLogMessage("Errors by severity:");
                    foreach (var kvp in result.ErrorsBySeverity)
                    {
                        AddLogMessage($"  - {kvp.Key}: {kvp.Value}");
                    }
                }
            }
            else
            {
                AddLogMessage("");
                AddLogMessage("✓ All rows are valid!");
            }

            // CRITICAL FIX: Apply validation errors to UI ViewModels to show red borders
            AddLogMessage("🎨 Applying validation errors to UI (red borders, validation alerts)...");

            // Get validation errors from backend and apply to UI
            var validationErrors = await _gridFacade.Validation.GetValidationErrorsAsync();
            if (validationErrors != null && validationErrors.Count > 0)
            {
                AddLogMessage($"  - Found {validationErrors.Count} validation errors in backend");
                _gridControl.ViewModel.ApplyValidationErrors(validationErrors);
                AddLogMessage($"✓ Validation errors applied to UI successfully!");
            }
            else
            {
                AddLogMessage("  - No validation errors to display, clearing UI");
                _gridControl.ViewModel.ClearValidationErrors();
            }
        }
        catch (Exception ex)
        {
            AddLogMessage($"✗ Exception during validation: {ex.Message}");
            AddLogMessage($"  Stack: {ex.StackTrace}");
        }
    }

    private void BatchValidationButton_Click(object sender, RoutedEventArgs e)
    {
        AddLogMessage("Batch validation not implemented in this demo");
    }

    private void ClearFiltersButton_Click(object sender, RoutedEventArgs e)
    {
        AddLogMessage("Clear filters not implemented in this demo");
    }

    private void RealTimeValidationButton_Click(object sender, RoutedEventArgs e)
    {
        AddLogMessage("Real-time validation not implemented in this demo");
    }

    private void OnSaveValidationButton_Click(object sender, RoutedEventArgs e)
    {
        AddLogMessage("On-save validation not implemented in this demo");
    }

    private void OnFocusValidationButton_Click(object sender, RoutedEventArgs e)
    {
        AddLogMessage("On-focus validation not implemented in this demo");
    }

    private void ManualValidationButton_Click(object sender, RoutedEventArgs e)
    {
        AddLogMessage("Manual validation not implemented in this demo");
    }

    #endregion

    #region UI Operations

    private async void RefreshUIButton_Click(object sender, RoutedEventArgs e)
    {
        if (!_isInitialized || _gridFacade == null || _gridControl == null)
        {
            AddLogMessage("⚠ Initialize grid first!");
            return;
        }

        try
        {
            AddLogMessage("");
            AddLogMessage("=== MANUAL UI REFRESH ==");
            AddLogMessage("Requesting manual UI refresh...");

            // Trigger manual refresh via Notifications module
            await _gridFacade.Notifications.RefreshUIAsync("ManualRefresh", 0);

            AddLogMessage("✓ Manual refresh requested successfully!");
        }
        catch (Exception ex)
        {
            AddLogMessage($"✗ Exception: {ex.Message}");
            AddLogMessage($"  Stack: {ex.StackTrace}");
        }
    }

    private void UpdateValidationUIButton_Click(object sender, RoutedEventArgs e)
    {
        AddLogMessage("Update validation UI not implemented in this demo");
    }

    private void InvalidateLayoutButton_Click(object sender, RoutedEventArgs e)
    {
        AddLogMessage("Invalidate layout not implemented in this demo");
    }

    #endregion

    #region Search Operations

    private void SearchButton_Click(object sender, RoutedEventArgs e)
    {
        var searchText = SearchTextBox?.Text ?? "";
        if (string.IsNullOrWhiteSpace(searchText))
        {
            AddLogMessage("⚠ Enter search text!");
            return;
        }

        AddLogMessage($"Search for '{searchText}' not implemented in this demo");
    }

    #endregion

    #region Row Management

    private async void AddRowButton_Click(object sender, RoutedEventArgs e)
    {
        if (!_isInitialized || _gridFacade == null || _gridControl == null)
        {
            AddLogMessage("⚠ Initialize grid first!");
            return;
        }

        try
        {
            AddLogMessage("Adding new row...");

            var newRow = new Dictionary<string, object?>
            {
                ["Column_1"] = "New Value 1",
                ["Column_2"] = "New Value 2",
                ["Column_3"] = "New Value 3",
                ["Column_4"] = "New Value 4",
                ["Column_5"] = "New Value 5"
            };

            // Add row using new modular API
            var result = await _gridFacade.Rows.AddRowAsync(newRow);

            if (result.IsSuccess)
            {
                AddLogMessage($"✓ Row added at index {result.Value} via facade");
                AddLogMessage("⏳ Waiting for automatic UI refresh...");
            }
            else
            {
                AddLogMessage($"✗ Failed to add row");
            }
        }
        catch (Exception ex)
        {
            AddLogMessage($"✗ Exception: {ex.Message}");
            AddLogMessage($"  Stack: {ex.StackTrace}");
        }
    }

    private void DeleteRowButton_Click(object sender, RoutedEventArgs e)
    {
        AddLogMessage("Delete row not implemented in this demo");
    }

    private void SmartDeleteButton_Click(object sender, RoutedEventArgs e)
    {
        AddLogMessage("Smart delete not implemented in this demo");
    }

    private void CompactRowsButton_Click(object sender, RoutedEventArgs e)
    {
        AddLogMessage("Compact rows not implemented in this demo");
    }

    private void PasteDataButton_Click(object sender, RoutedEventArgs e)
    {
        AddLogMessage("Paste data not implemented in this demo");
    }

    #endregion

    #region Special Column Event Handlers

    /// <summary>
    /// Handles row selection changes from the checkbox special column.
    /// Logs selection state for tracking purposes.
    /// NOTE: Delete and auto-expand are handled automatically by InternalUIOperationHandler in Interactive mode.
    /// </summary>
    private void OnRowSelectionChanged(object? sender, (int rowIndex, bool isSelected) e)
    {
        if (!_isInitialized || _gridFacade == null || _gridControl == null)
        {
            return;
        }

        try
        {
            AddLogMessage($"Row {e.rowIndex} selection changed: {(e.isSelected ? "✓ Selected" : "○ Deselected")}");
        }
        catch (Exception ex)
        {
            AddLogMessage($"✗ Exception during selection change: {ex.Message}");
        }
    }

    #endregion

    #region Color/Theme Operations

    private void ApplyDarkThemeButton_Click(object sender, RoutedEventArgs e)
    {
        AddLogMessage("Dark theme not implemented in this demo");
    }

    private void ResetColorsButton_Click(object sender, RoutedEventArgs e)
    {
        AddLogMessage("Reset colors not implemented in this demo");
    }

    private void TestSelectiveColorsButton_Click(object sender, RoutedEventArgs e)
    {
        AddLogMessage("Selective colors not implemented in this demo");
    }

    private void TestBorderOnlyButton_Click(object sender, RoutedEventArgs e)
    {
        AddLogMessage("Border only not implemented in this demo");
    }

    private void TestValidationOnlyButton_Click(object sender, RoutedEventArgs e)
    {
        AddLogMessage("Validation only not implemented in this demo");
    }

    #endregion

    #region Data Operations

    private void ClearDataButton_Click(object sender, RoutedEventArgs e)
    {
        AddLogMessage("Clear data not implemented in this demo");
    }

    #endregion

    #region Statistics

    private void GetStatsButton_Click(object sender, RoutedEventArgs e)
    {
        if (!_isInitialized || _gridFacade == null || _gridControl == null)
        {
            AddLogMessage("⚠ Initialize grid first!");
            return;
        }

        try
        {
            AddLogMessage("");
            AddLogMessage("=== STATISTICS ===");

            // Use new modular API
            var rowCount = _gridFacade.Rows.GetRowCount();
            var colCount = _gridFacade.Columns.GetColumnCount();

            AddLogMessage($"Rows (Facade): {rowCount}");
            AddLogMessage($"Columns (Facade): {colCount}");
            AddLogMessage($"Rows (UI): {_gridControl.ViewModel.Rows.Count}");
            AddLogMessage($"Columns (UI): {_gridControl.ViewModel.ColumnHeaders.Count}");
            AddLogMessage($"Initialized: {_isInitialized}");
        }
        catch (Exception ex)
        {
            AddLogMessage($"✗ Exception: {ex.Message}");
            AddLogMessage($"  Stack: {ex.StackTrace}");
        }
    }

    #endregion

    #region Helper Methods

    // NOTE: Empty row management is now handled automatically by SmartOperations.AutoExpandEmptyRow
    // No need for custom EnsureEmptyRowAtEnd logic in demo app

    private List<Dictionary<string, object?>> GenerateTestData(int rowCount, int columnCount)
    {
        var result = new List<Dictionary<string, object?>>();
        var random = new Random();

        for (int i = 0; i < rowCount; i++)
        {
            var row = new Dictionary<string, object?>();

            // Generate data with intentional validation errors for testing
            for (int j = 1; j <= columnCount; j++)
            {
                if (j == 1)
                {
                    // Column_1: Required - occasionally make it empty (10% chance)
                    if (random.Next(100) < 10)
                    {
                        row["Column_1"] = ""; // Will fail required validation
                    }
                    else
                    {
                        row["Column_1"] = $"Row{i + 1}_Col{j}";
                    }
                }
                else if (j == 2)
                {
                    // Column_2: Numeric range 1-100 - occasionally out of range (20% chance)
                    if (random.Next(100) < 20)
                    {
                        var invalidValue = random.Next(2) == 0 ? random.Next(-10, 1) : random.Next(101, 200);
                        row["Column_2"] = invalidValue.ToString(); // Will fail range validation
                    }
                    else
                    {
                        row["Column_2"] = random.Next(1, 101).ToString(); // Valid range 1-100
                    }
                }
                else if (j == 3)
                {
                    // Column_3: Max length 20 - occasionally too long (15% chance)
                    if (random.Next(100) < 15)
                    {
                        row["Column_3"] = $"This_is_a_very_long_text_that_exceeds_20_characters_Row{i + 1}"; // Will fail length validation
                    }
                    else
                    {
                        row["Column_3"] = $"Row{i + 1}_Col{j}"; // Valid length
                    }
                }
                else if (j == 4)
                {
                    // Column_4: Must start with "Valid" - occasionally invalid (25% chance)
                    if (random.Next(100) < 25)
                    {
                        row["Column_4"] = $"Invalid_Row{i + 1}"; // Will fail pattern validation
                    }
                    else
                    {
                        row["Column_4"] = $"Valid_Row{i + 1}_Col{j}"; // Valid pattern
                    }
                }
                else
                {
                    row[$"Column_{j}"] = $"Row{i + 1}_Col{j}";
                }
            }
            result.Add(row);
        }

        return result;
    }

    private void AddLogMessage(string message)
    {
        var timestamp = DateTime.Now.ToString("HH:mm:ss");
        var logLine = $"[{timestamp}] {message}";

        _logOutput.AppendLine(logLine);

        this.DispatcherQueue.TryEnqueue(() =>
        {
            if (LogOutput != null)
            {
                LogOutput.Text = _logOutput.ToString();
                LogScrollViewer?.ChangeView(null, LogScrollViewer.ScrollableHeight, null);
            }
        });
    }

    #endregion

    #region Validation Rules

    /// <summary>
    /// Defines validation rules for the demo application.
    /// Demonstrates Required, Range, Length, and Pattern validation.
    /// </summary>
    private async Task DefineValidationRulesAsync()
    {
        if (_gridFacade == null)
        {
            AddLogMessage("⚠ Grid not initialized!");
            return;
        }

        try
        {
            AddLogMessage("Defining validation rules...");

            // Rule 1: Column_1 must not be empty (Required)
            var rule1 = new SimpleValidationRule(
                ruleId: "rule_column1_required",
                ruleName: "Column_1_Required",
                dependentColumns: new[] { "Column_1" },
                validator: (row, context) =>
                {
                    var value = row["Column_1"];
                    if (value == null || string.IsNullOrWhiteSpace(value.ToString()))
                    {
                        return ValidationResult.Error("Column_1 is required", PublicValidationSeverity.Error, "Column_1");
                    }
                    return ValidationResult.Success();
                });

            await _gridFacade.Validation.AddValidationRuleAsync(rule1);

            // Rule 2: Column_2 must be numeric and in range 1-100 (Range)
            var rule2 = new SimpleValidationRule(
                ruleId: "rule_column2_range",
                ruleName: "Column_2_NumericRange",
                dependentColumns: new[] { "Column_2" },
                validator: (row, context) =>
                {
                    var value = row["Column_2"];
                    if (value != null)
                    {
                        var strValue = value.ToString();
                        if (!string.IsNullOrEmpty(strValue))
                        {
                            if (int.TryParse(strValue, out var numValue))
                            {
                                if (numValue < 1 || numValue > 100)
                                {
                                    return ValidationResult.Error($"Column_2 must be between 1 and 100, got {numValue}", PublicValidationSeverity.Warning, "Column_2");
                                }
                            }
                            else
                            {
                                return ValidationResult.Error("Column_2 must be a number", PublicValidationSeverity.Error, "Column_2");
                            }
                        }
                    }
                    return ValidationResult.Success();
                });

            await _gridFacade.Validation.AddValidationRuleAsync(rule2);

            // Rule 3: Column_3 length check (max 20 characters)
            var rule3 = new SimpleValidationRule(
                ruleId: "rule_column3_length",
                ruleName: "Column_3_MaxLength",
                dependentColumns: new[] { "Column_3" },
                validator: (row, context) =>
                {
                    var value = row["Column_3"];
                    if (value != null && value.ToString()!.Length > 20)
                    {
                        return ValidationResult.Error("Column_3 must be less than 20 characters", PublicValidationSeverity.Warning, "Column_3");
                    }
                    return ValidationResult.Success();
                });

            await _gridFacade.Validation.AddValidationRuleAsync(rule3);

            // Rule 4: Column_4 must start with "Valid" (Pattern)
            var rule4 = new SimpleValidationRule(
                ruleId: "rule_column4_pattern",
                ruleName: "Column_4_Pattern",
                dependentColumns: new[] { "Column_4" },
                validator: (row, context) =>
                {
                    var value = row["Column_4"];
                    if (value != null)
                    {
                        var strValue = value.ToString();
                        if (!string.IsNullOrEmpty(strValue) && !strValue.StartsWith("Valid"))
                        {
                            return ValidationResult.Error("Column_4 must start with 'Valid'", PublicValidationSeverity.Info, "Column_4");
                        }
                    }
                    return ValidationResult.Success();
                });

            await _gridFacade.Validation.AddValidationRuleAsync(rule4);

            AddLogMessage("✓ 4 validation rules registered successfully:");
            AddLogMessage("  - Column_1: Required (Error)");
            AddLogMessage("  - Column_2: Numeric Range 1-100 (Warning)");
            AddLogMessage("  - Column_3: Max Length 20 (Warning)");
            AddLogMessage("  - Column_4: Must start with 'Valid' (Info)");
        }
        catch (Exception ex)
        {
            AddLogMessage($"✗ Failed to define validation rules: {ex.Message}");
        }
    }

    /// <summary>
    /// Simple validation rule implementation for demo purposes.
    /// Implements IValidationRule interface with custom validation logic.
    /// </summary>
    private class SimpleValidationRule : IValidationRule
    {
        private readonly string _ruleId;
        private readonly string _ruleName;
        private readonly IReadOnlyList<string> _dependentColumns;
        private readonly Func<IReadOnlyDictionary<string, object?>, ValidationContext, ValidationResult> _validator;

        public SimpleValidationRule(
            string ruleId,
            string ruleName,
            string[] dependentColumns,
            Func<IReadOnlyDictionary<string, object?>, ValidationContext, ValidationResult> validator)
        {
            _ruleId = ruleId;
            _ruleName = ruleName;
            _dependentColumns = dependentColumns;
            _validator = validator;
        }

        public string RuleId => _ruleId;
        public string RuleName => _ruleName;
        public IReadOnlyList<string> DependentColumns => _dependentColumns;
        public bool IsEnabled => true;
        public TimeSpan ValidationTimeout => TimeSpan.FromSeconds(5);

        public ValidationResult Validate(IReadOnlyDictionary<string, object?> row, ValidationContext context)
        {
            return _validator(row, context);
        }

        public Task<ValidationResult> ValidateAsync(
            IReadOnlyDictionary<string, object?> row,
            ValidationContext context,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_validator(row, context));
        }
    }

    #endregion
}
