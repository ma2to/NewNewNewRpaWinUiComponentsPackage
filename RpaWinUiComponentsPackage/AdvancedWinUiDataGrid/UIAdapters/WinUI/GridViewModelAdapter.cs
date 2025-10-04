using Microsoft.Extensions.Logging;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Common.Models;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.UIAdapters.WinUI;

/// <summary>
/// Internal adapter that maps internal model to ViewModel for WinUI integration
/// Transforms data grid internal models to UI-friendly view models
/// </summary>
internal sealed class GridViewModelAdapter
{
    private readonly ILogger<GridViewModelAdapter> _logger;

    public GridViewModelAdapter(ILogger<GridViewModelAdapter> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Transform internal row data to view model
    /// </summary>
    /// <param name="rowData">Internal row data</param>
    /// <param name="rowIndex">Row index for UI display</param>
    /// <returns>UI-friendly row view model</returns>
    public RowViewModel AdaptToRowViewModel(IReadOnlyDictionary<string, object?> rowData, int rowIndex)
    {
        try
        {
            var viewModel = new RowViewModel
            {
                Index = rowIndex,
                IsSelected = false,
                IsValid = true,
                ValidationErrors = new List<string>(),
                CellValues = new Dictionary<string, object?>()
            };

            // Copy cell values
            foreach (var kvp in rowData)
            {
                viewModel.CellValues[kvp.Key] = kvp.Value;
            }

            _logger.LogTrace("Adapted row {RowIndex} with {CellCount} cells to view model", rowIndex, rowData.Count);

            return viewModel;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to adapt row {RowIndex} to view model", rowIndex);
            throw;
        }
    }

    /// <summary>
    /// Transform internal column definition to view model
    /// </summary>
    /// <param name="columnDef">Internal column definition</param>
    /// <returns>UI-friendly column view model</returns>
    public ColumnViewModel AdaptToColumnViewModel(ColumnDefinition columnDef)
    {
        try
        {
            var viewModel = new ColumnViewModel
            {
                Name = columnDef.Name,
                DisplayName = columnDef.DisplayName ?? columnDef.Name,
                IsVisible = columnDef.IsVisible,
                Width = columnDef.Width,
                IsReadOnly = columnDef.IsReadOnly,
                DataType = columnDef.DataType?.Name ?? "String",
                SortDirection = columnDef.SortDirection.ToString()
            };

            _logger.LogTrace("Adapted column {ColumnName} to view model", columnDef.Name);

            return viewModel;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to adapt column {ColumnName} to view model", columnDef.Name);
            throw;
        }
    }

    /// <summary>
    /// Transform validation errors to UI-friendly format
    /// </summary>
    /// <param name="validationErrors">Internal validation errors</param>
    /// <returns>UI-friendly validation view models</returns>
    public IReadOnlyList<ValidationErrorViewModel> AdaptValidationErrors(IReadOnlyList<ValidationError> validationErrors)
    {
        try
        {
            var viewModels = validationErrors.Select(error => new ValidationErrorViewModel
            {
                RowIndex = error.RowIndex,
                ColumnName = error.ColumnName ?? string.Empty,
                Message = error.Message,
                Severity = error.Severity.ToString(),
                ErrorCode = error.ErrorCode
            }).ToList();

            _logger.LogTrace("Adapted {ErrorCount} validation errors to view models", validationErrors.Count);

            return viewModels;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to adapt validation errors to view models");
            throw;
        }
    }
}

/// <summary>
/// UI-friendly row view model
/// </summary>
internal sealed class RowViewModel
{
    public int Index { get; set; }
    public bool IsSelected { get; set; }
    public bool IsValid { get; set; }
    public IList<string> ValidationErrors { get; set; } = new List<string>();
    public IDictionary<string, object?> CellValues { get; set; } = new Dictionary<string, object?>();
}

/// <summary>
/// UI-friendly column view model
/// </summary>
internal sealed class ColumnViewModel
{
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public bool IsVisible { get; set; } = true;
    public double Width { get; set; } = 100;
    public bool IsReadOnly { get; set; }
    public string DataType { get; set; } = "String";
    public string SortDirection { get; set; } = "None";
}

/// <summary>
/// UI-friendly validation error view model
/// </summary>
internal sealed class ValidationErrorViewModel
{
    public int RowIndex { get; set; }
    public string ColumnName { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public string ErrorCode { get; set; } = string.Empty;
}