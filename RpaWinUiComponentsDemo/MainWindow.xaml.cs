using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
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
    private IDisposable? _dataRefreshSubscription;

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

            // Create logger factory
            var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddConsole();
                builder.SetMinimumLevel(LogLevel.Warning);
            });

            // Create grid options with DispatcherQueue for UI operations
            var options = new AdvancedDataGridOptions
            {
                OperationMode = PublicDataGridOperationMode.Interactive,
                EnableParallelProcessing = true,
                EnableLinqOptimizations = true,
                EnableCaching = true,
                BatchSize = 1000,
                MinimumLogLevel = LogLevel.Warning,
                DispatcherQueue = this.DispatcherQueue,

                // ENABLE SPECIAL COLUMNS for testing
                EnableRowNumberColumn = true,
                EnableCheckboxColumn = true,
                EnableValidationAlertsColumn = true,
                EnableDeleteRowColumn = true,
                ValidationAlertsColumnMinWidth = 150.0
            };

            AddLogMessage("Creating DataGrid facade and UI control...");
            _gridFacade = AdvancedDataGridFacadeFactory.CreateStandalone(options, loggerFactory, this.DispatcherQueue);

            // Create UI control
            _gridControl = new AdvancedDataGridControl(loggerFactory.CreateLogger<AdvancedDataGridControl>());

            // Add UI control to container
            GridContainer.Child = _gridControl;

            if (_gridFacade != null && _gridControl != null)
            {
                _isInitialized = true;
                AddLogMessage("✓ Grid created successfully with UI component!");

                // Subscribe to automatic UI refresh notifications (Interactive mode)
                AddLogMessage("Setting up automatic UI refresh subscription...");
                _dataRefreshSubscription = _gridFacade.Notifications.SubscribeToDataRefresh(eventArgs =>
                {
                    AddLogMessage($"🔄 Auto-refresh triggered: {eventArgs.OperationType}, {eventArgs.AffectedRows} rows");

                    // Automatically refresh UI control with special columns support
                    var currentData = _gridFacade.Rows.GetAllRows();
                    var headers = currentData.FirstOrDefault()?.Keys.ToList() ?? new List<string>();

                    _gridControl.LoadData(currentData, headers, options);

                    AddLogMessage("✓ UI automatically refreshed with special columns!");
                });
                AddLogMessage("✓ Auto-refresh subscription active (Interactive mode)");

                AddLogMessage("");
                AddLogMessage("Available operations:");
                AddLogMessage("- Import Dictionary: Import sample data and display in grid");
                AddLogMessage("- Export Dictionary: Export current data");
                AddLogMessage("- Add Row: Add new row to grid");
                AddLogMessage("- Get Statistics: Show row/column count");
                AddLogMessage("");
                AddLogMessage("NOTE: UI will auto-refresh after data operations (Interactive mode)");
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

            // Generate test data
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

    private void ValidateAllButton_Click(object sender, RoutedEventArgs e)
    {
        AddLogMessage("Validation not implemented in this demo");
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

    private List<Dictionary<string, object?>> GenerateTestData(int rowCount, int columnCount)
    {
        var result = new List<Dictionary<string, object?>>();

        for (int i = 0; i < rowCount; i++)
        {
            var row = new Dictionary<string, object?>();
            for (int j = 1; j <= columnCount; j++)
            {
                row[$"Column_{j}"] = $"Row{i + 1}_Col{j}";
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
}
