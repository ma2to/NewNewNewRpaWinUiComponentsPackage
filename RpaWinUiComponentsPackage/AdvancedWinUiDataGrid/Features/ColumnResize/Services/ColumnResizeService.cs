using Microsoft.Extensions.Logging;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.ColumnResize.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.ColumnResize.Models;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Column.Interfaces;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.ColumnResize.Services;

/// <summary>
/// Internal implementation of column resize service
/// Thread-safe with debounce support for performance
/// </summary>
internal sealed class ColumnResizeService : IColumnResizeService
{
    private readonly ILogger<ColumnResizeService> _logger;
    private readonly IColumnService _columnService;
    private readonly AdvancedDataGridOptions _options;
    private readonly object _resizeLock = new();
    private ResizeState? _currentResize;
    private readonly TimeSpan _debounceInterval = TimeSpan.FromMilliseconds(16); // ~60fps

    /// <summary>
    /// Constructor for ColumnResizeService
    /// </summary>
    public ColumnResizeService(
        ILogger<ColumnResizeService> logger,
        IColumnService columnService,
        AdvancedDataGridOptions options)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _columnService = columnService ?? throw new ArgumentNullException(nameof(columnService));
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    /// <summary>
    /// Resizes a column to a specific width with constraint enforcement
    /// </summary>
    public double ResizeColumn(int columnIndex, double newWidth)
    {
        lock (_resizeLock)
        {
            _logger.LogDebug("Resizing column {ColumnIndex} to width {NewWidth}", columnIndex, newWidth);

            // Enforce min/max constraints
            var constrainedWidth = EnforceWidthConstraints(newWidth);

            // Get column definition
            var columns = _columnService.GetColumnDefinitions();
            if (columnIndex < 0 || columnIndex >= columns.Count)
            {
                _logger.LogWarning("Invalid column index {ColumnIndex} for resize", columnIndex);
                return 0;
            }

            var column = columns[columnIndex];

            // Create updated column definition with new width
            var updatedColumn = column.Clone();
            updatedColumn.Width = constrainedWidth;

            // Update column
            _columnService.UpdateColumn(updatedColumn);

            _logger.LogInformation("Column {ColumnIndex} ({ColumnName}) resized to {ActualWidth}px",
                columnIndex, column.Name, constrainedWidth);

            return constrainedWidth;
        }
    }

    /// <summary>
    /// Starts a column resize operation
    /// </summary>
    public void StartColumnResize(int columnIndex, double clientX)
    {
        lock (_resizeLock)
        {
            _logger.LogDebug("Starting column resize for column {ColumnIndex} at clientX {ClientX}",
                columnIndex, clientX);

            var columns = _columnService.GetColumnDefinitions();
            if (columnIndex < 0 || columnIndex >= columns.Count)
            {
                _logger.LogWarning("Invalid column index {ColumnIndex} for resize start", columnIndex);
                return;
            }

            var column = columns[columnIndex];
            var currentWidth = column.Width;

            _currentResize = new ResizeState
            {
                ColumnIndex = columnIndex,
                StartClientX = clientX,
                StartWidth = currentWidth,
                CurrentWidth = currentWidth,
                IsActive = true,
                LastUpdateTime = DateTime.UtcNow
            };

            _logger.LogInformation("Column resize started for column {ColumnIndex} ({ColumnName}), initial width: {Width}px",
                columnIndex, column.Name, currentWidth);
        }
    }

    /// <summary>
    /// Updates column width during drag with debouncing
    /// </summary>
    public void UpdateColumnResize(double clientX)
    {
        lock (_resizeLock)
        {
            if (_currentResize == null || !_currentResize.IsActive)
            {
                _logger.LogWarning("UpdateColumnResize called without active resize operation");
                return;
            }

            // Debounce check
            var now = DateTime.UtcNow;
            var timeSinceLastUpdate = now - _currentResize.LastUpdateTime;
            if (timeSinceLastUpdate < _debounceInterval)
            {
                return; // Skip update for performance
            }

            // Calculate new width based on mouse movement
            var deltaX = clientX - _currentResize.StartClientX;
            var newWidth = _currentResize.StartWidth + deltaX;

            // Enforce constraints
            var constrainedWidth = EnforceWidthConstraints(newWidth);

            // Update if width changed
            if (Math.Abs(constrainedWidth - _currentResize.CurrentWidth) > 0.1)
            {
                _currentResize.CurrentWidth = constrainedWidth;
                _currentResize.LastUpdateTime = now;

                // Apply the width change
                ResizeColumn(_currentResize.ColumnIndex, constrainedWidth);

                _logger.LogDebug("Column resize updated: deltaX={DeltaX}, newWidth={NewWidth}",
                    deltaX, constrainedWidth);
            }
        }
    }

    /// <summary>
    /// Ends the column resize operation
    /// </summary>
    public void EndColumnResize()
    {
        lock (_resizeLock)
        {
            if (_currentResize == null || !_currentResize.IsActive)
            {
                _logger.LogWarning("EndColumnResize called without active resize operation");
                return;
            }

            var columnIndex = _currentResize.ColumnIndex;
            var finalWidth = _currentResize.CurrentWidth;

            _logger.LogInformation("Column resize ended for column {ColumnIndex}, final width: {FinalWidth}px",
                columnIndex, finalWidth);

            _currentResize = null;
        }
    }

    /// <summary>
    /// Gets the current width of a column
    /// </summary>
    public double GetColumnWidth(int columnIndex)
    {
        lock (_resizeLock)
        {
            var columns = _columnService.GetColumnDefinitions();
            if (columnIndex < 0 || columnIndex >= columns.Count)
            {
                _logger.LogWarning("Invalid column index {ColumnIndex} for GetColumnWidth", columnIndex);
                return 0;
            }

            return columns[columnIndex].Width;
        }
    }

    /// <summary>
    /// Checks if a resize operation is currently active
    /// </summary>
    public bool IsResizing()
    {
        lock (_resizeLock)
        {
            return _currentResize != null && _currentResize.IsActive;
        }
    }

    /// <summary>
    /// Enforces min/max width constraints
    /// </summary>
    private double EnforceWidthConstraints(double width)
    {
        if (width < _options.MinimumColumnWidth)
        {
            _logger.LogDebug("Width {Width} below minimum {MinWidth}, clamping",
                width, _options.MinimumColumnWidth);
            return _options.MinimumColumnWidth;
        }

        if (width > _options.MaximumColumnWidth)
        {
            _logger.LogDebug("Width {Width} above maximum {MaxWidth}, clamping",
                width, _options.MaximumColumnWidth);
            return _options.MaximumColumnWidth;
        }

        return width;
    }

    public async Task AutoFitAllColumnsAsync(CancellationToken cancellationToken = default)
    {
        await Task.Run(() =>
        {
            lock (_resizeLock)
            {
                _logger.LogInformation("Auto-fitting all columns");
                var columns = _columnService.GetColumnDefinitions();

                for (int i = 0; i < columns.Count; i++)
                {
                    var column = columns[i];
                    var optimalWidth = CalculateOptimalWidth(column);
                    ResizeColumn(i, optimalWidth);
                }

                _logger.LogInformation("Auto-fit completed for {Count} columns", columns.Count);
            }
        }, cancellationToken);
    }

    public async Task AutoFitColumnAsync(string columnName, CancellationToken cancellationToken = default)
    {
        await Task.Run(() =>
        {
            lock (_resizeLock)
            {
                _logger.LogInformation("Auto-fitting column '{ColumnName}'", columnName);
                var columns = _columnService.GetColumnDefinitions();
                var columnIndex = -1;
                for (int i = 0; i < columns.Count; i++)
                {
                    if (columns[i].Name == columnName)
                    {
                        columnIndex = i;
                        break;
                    }
                }

                if (columnIndex == -1)
                {
                    throw new InvalidOperationException($"Column '{columnName}' not found");
                }

                var column = columns[columnIndex];
                var optimalWidth = CalculateOptimalWidth(column);
                ResizeColumn(columnIndex, optimalWidth);

                _logger.LogInformation("Auto-fit completed for column '{ColumnName}' with width {Width}px",
                    columnName, optimalWidth);
            }
        }, cancellationToken);
    }

    public async Task ResizeColumnAsync(string columnName, double width, CancellationToken cancellationToken = default)
    {
        await Task.Run(() =>
        {
            lock (_resizeLock)
            {
                _logger.LogInformation("Resizing column '{ColumnName}' to {Width}px", columnName, width);
                var columns = _columnService.GetColumnDefinitions();
                var columnIndex = -1;
                for (int i = 0; i < columns.Count; i++)
                {
                    if (columns[i].Name == columnName)
                    {
                        columnIndex = i;
                        break;
                    }
                }

                if (columnIndex == -1)
                {
                    throw new InvalidOperationException($"Column '{columnName}' not found");
                }

                ResizeColumn(columnIndex, width);
                _logger.LogInformation("Resize completed for column '{ColumnName}'", columnName);
            }
        }, cancellationToken);
    }

    private double CalculateOptimalWidth(dynamic column)
    {
        // Simplified optimal width calculation
        // In real implementation, this would measure content width
        var headerLength = column.Name?.Length ?? 0;
        var estimatedWidth = Math.Max(100, headerLength * 10 + 40);
        return EnforceWidthConstraints(estimatedWidth);
    }
}
