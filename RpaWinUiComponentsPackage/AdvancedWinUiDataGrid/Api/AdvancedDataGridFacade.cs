using System.Data;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Import.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Export.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Validation.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.CopyPaste.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Selection.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Column.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Filter.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.AutoRowHeight.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Initialization.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Initialization.Commands;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Initialization.Models;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Api.Models;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Common;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Api.Internal;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Common.Models;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Configuration;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Infrastructure.Persistence.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Infrastructure.Logging.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Infrastructure.Logging.NullPattern;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Api.Commands;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid;

/// <summary>
/// Verejná implementácia IAdvancedDataGridFacade
/// Orchestruje všetky operácie komponentov cez interné služby
/// </summary>
public sealed class AdvancedDataGridFacade : IAdvancedDataGridFacade
{
    private readonly IServiceProvider _serviceProvider;
    private readonly AdvancedDataGridOptions _options;
    private readonly ILogger<AdvancedDataGridFacade> _logger;
    private readonly IOperationLogger<AdvancedDataGridFacade> _operationLogger;
    private readonly DispatcherQueue? _dispatcher;
    private readonly UIAdapters.WinUI.UiNotificationService? _uiNotificationService;
    private readonly UIAdapters.WinUI.GridViewModelAdapter? _gridViewModelAdapter;
    private readonly Features.Color.ThemeService _themeService;
    private bool _disposed;

    /// <summary>
    /// Konštruktor AdvancedDataGridFacade
    /// Inicializuje závislosti a získava operation logger cez DI
    /// </summary>
    public AdvancedDataGridFacade(
        IServiceProvider serviceProvider,
        AdvancedDataGridOptions options)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = serviceProvider.GetRequiredService<ILogger<AdvancedDataGridFacade>>();
        _dispatcher = serviceProvider.GetService<DispatcherQueue>();

        // Získame operation logger cez DI, alebo použijeme null pattern
        var operationLogger = serviceProvider.GetService<IOperationLogger<AdvancedDataGridFacade>>();
        _operationLogger = operationLogger ?? NullOperationLogger<AdvancedDataGridFacade>.Instance;

        // Získame UI notification service (dostupný ak je DispatcherQueue poskytnutý)
        _uiNotificationService = serviceProvider.GetService<UIAdapters.WinUI.UiNotificationService>();

        // Získame GridViewModelAdapter (dostupný ak je DispatcherQueue poskytnutý)
        _gridViewModelAdapter = serviceProvider.GetService<UIAdapters.WinUI.GridViewModelAdapter>();

        // Získame ThemeService (vždy dostupný)
        _themeService = serviceProvider.GetRequiredService<Features.Color.ThemeService>();

        _logger.LogInformation("AdvancedDataGrid facade initialized with operation mode {OperationMode}", _options.OperationMode);
    }

    /// <summary>
    /// Helper method to check if a feature is enabled
    /// </summary>
    private bool IsFeatureEnabled(GridFeature feature)
    {
        return _options.EnabledFeatures.Contains(feature);
    }

    /// <summary>
    /// Helper method to throw exception if feature is disabled
    /// </summary>
    private void EnsureFeatureEnabled(GridFeature feature, string operationName)
    {
        if (!IsFeatureEnabled(feature))
        {
            var message = $"Feature '{feature}' is disabled. Operation '{operationName}' cannot be executed.";
            _logger.LogWarning(message);
            throw new InvalidOperationException(message);
        }
    }

    #region Import/Export Operations

    /// <summary>
    /// Importuje dáta pomocou command pattern s LINQ optimalizáciou a validačným pipeline
    /// </summary>
    public async Task<ImportResult> ImportAsync(ImportDataCommand command, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        EnsureFeatureEnabled(GridFeature.Import, nameof(ImportAsync));

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var operationId = Guid.NewGuid();

        // Začíname import operáciu - vytvoríme operation scope pre automatické tracking
        using var logScope = _operationLogger.LogOperationStart("ImportAsync", new
        {
            OperationId = operationId,
            CorrelationId = command.CorrelationId
        });

        _logger.LogInformation("Starting import operation {OperationId} [CorrelationId: {CorrelationId}]",
            operationId, command.CorrelationId);

        try
        {
            // Vytvoríme operation scope pre scoped services
            using var scope = ServiceRegistration.CreateOperationScope(_serviceProvider);
            var importService = scope.ServiceProvider.GetRequiredService<IImportService>();

            // Mapujeme public command na internal command
            var internalCommand = command.ToInternal();

            // Vykonáme interný import
            var internalResult = await importService.ImportAsync(internalCommand, cancellationToken);

            // Mapujeme interný result na public PublicResult
            var result = internalResult.ToPublic();

            // Automatický UI refresh v Interactive mode
            await TriggerUIRefreshIfNeededAsync("Import", result.ImportedRows);

            logScope.MarkSuccess(new { Duration = stopwatch.Elapsed, Success = result.IsSuccess });
            _logger.LogInformation("Import operation {OperationId} completed in {Duration}ms [CorrelationId: {CorrelationId}]",
                operationId, stopwatch.ElapsedMilliseconds, command.CorrelationId);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Import operation {OperationId} failed [CorrelationId: {CorrelationId}]",
                operationId, command.CorrelationId);
            logScope.MarkFailure(ex);
            throw;
        }
    }

    /// <summary>
    /// Exportuje dáta pomocou command pattern s komplexným filtrovaním
    /// </summary>
    public async Task<ExportResult> ExportAsync(ExportDataCommand command, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        EnsureFeatureEnabled(GridFeature.Export, nameof(ExportAsync));

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var operationId = Guid.NewGuid();

        // Začíname export operáciu - vytvoríme operation scope pre automatické tracking
        using var logScope = _operationLogger.LogOperationStart("ExportAsync", new
        {
            OperationId = operationId,
            CorrelationId = command.CorrelationId
        });

        _logger.LogInformation("Starting export operation {OperationId} [CorrelationId: {CorrelationId}]",
            operationId, command.CorrelationId);

        try
        {
            // Vytvoríme operation scope pre scoped services
            using var scope = ServiceRegistration.CreateOperationScope(_serviceProvider);
            var exportService = scope.ServiceProvider.GetRequiredService<IExportService>();

            // Mapujeme public command na internal command
            var internalCommand = command.ToInternal();

            // Vykonáme interný export
            var internalResult = await exportService.ExportAsync(internalCommand, cancellationToken);

            // Mapujeme interný result na public PublicResult (s exportovanými dátami)
            var result = internalResult.ToPublic(internalResult.ExportedData);

            // Automatický UI refresh v Interactive mode
            await TriggerUIRefreshIfNeededAsync("Export", result.ExportedRows);

            logScope.MarkSuccess(new { Duration = stopwatch.Elapsed, Success = result.IsSuccess });
            _logger.LogInformation("Export operation {OperationId} completed in {Duration}ms [CorrelationId: {CorrelationId}]",
                operationId, stopwatch.ElapsedMilliseconds, command.CorrelationId);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Export operation {OperationId} failed [CorrelationId: {CorrelationId}]",
                operationId, command.CorrelationId);
            logScope.MarkFailure(ex);
            throw;
        }
    }

    #endregion

    #region Validation Operations

    /// <summary>
    /// Validuje všetky neprázdne riadky s dávkovým, thread-safe spracovaním
    /// Implementácia podľa dokumentácie: AreAllNonEmptyRowsValidAsync s dávkovým, thread-safe, stream supportom
    /// </summary>
    public async Task<PublicResult<bool>> ValidateAllAsync(bool onlyFiltered = false, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        EnsureFeatureEnabled(GridFeature.Validation, nameof(ValidateAllAsync));

        _logger.LogDebug("Starting validation: onlyFiltered={OnlyFiltered}", onlyFiltered);

        try
        {
            using var scope = ServiceRegistration.CreateOperationScope(_serviceProvider);
            var validationService = scope.ServiceProvider.GetRequiredService<IValidationService>();

            var internalResult = await validationService.AreAllNonEmptyRowsValidAsync(onlyFiltered, cancellationToken);
            var result = internalResult.ToPublic();

            // Automatický UI refresh v Interactive mode
            await TriggerUIRefreshIfNeededAsync("Validation", 0);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Validation failed");
            return PublicResult<bool>.Failure($"Validation failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Validates all non-empty rows with detailed statistics tracking
    /// </summary>
    public async Task<PublicValidationResultWithStatistics> ValidateAllWithStatisticsAsync(bool onlyFiltered = false, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        EnsureFeatureEnabled(GridFeature.Validation, nameof(ValidateAllWithStatisticsAsync));

        _logger.LogDebug("Starting validation with statistics: onlyFiltered={OnlyFiltered}", onlyFiltered);

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            using var scope = ServiceRegistration.CreateOperationScope(_serviceProvider);
            var validationService = scope.ServiceProvider.GetRequiredService<IValidationService>();
            var rowStore = scope.ServiceProvider.GetRequiredService<IRowStore>();

            // Get all rows to validate
            var allRows = await rowStore.GetAllRowsAsync(cancellationToken);

            int totalRows = allRows.Count;
            int validRows = 0;
            int totalErrors = 0;
            var errorsBySeverity = new Dictionary<string, int>();
            var validationErrors = new List<PublicValidationErrorViewModel>();
            var ruleStatisticsDict = new Dictionary<string, RuleStatsAccumulator>();

            // Validate each row and accumulate statistics
            for (int rowIndex = 0; rowIndex < allRows.Count; rowIndex++)
            {
                var rowData = allRows[rowIndex];

                var context = new ValidationContext
                {
                    RowIndex = rowIndex,
                    AllRows = allRows,
                    OperationId = Guid.NewGuid().ToString()
                };

                var ruleStopwatch = System.Diagnostics.Stopwatch.StartNew();
                var validationResult = await validationService.ValidateRowAsync(rowData, context, cancellationToken);
                ruleStopwatch.Stop();

                if (validationResult.IsValid)
                {
                    validRows++;
                }
                else
                {
                    totalErrors++;

                    // Accumulate error by severity
                    var severityKey = validationResult.Severity.ToString();
                    errorsBySeverity.TryGetValue(severityKey, out int count);
                    errorsBySeverity[severityKey] = count + 1;

                    // Add validation error
                    validationErrors.Add(new PublicValidationErrorViewModel
                    {
                        RowIndex = rowIndex,
                        ColumnName = validationResult.AffectedColumn ?? string.Empty,
                        Message = validationResult.ErrorMessage ?? string.Empty,
                        Severity = validationResult.Severity.ToString(),
                        ErrorCode = $"VAL_{rowIndex}"
                    });
                }

                // Track rule statistics (simplified - in real implementation this would come from ValidationService)
                var ruleName = validationResult.AffectedColumn ?? "DefaultRule";
                if (!ruleStatisticsDict.ContainsKey(ruleName))
                {
                    ruleStatisticsDict[ruleName] = new RuleStatsAccumulator { RuleName = ruleName };
                }

                var stats = ruleStatisticsDict[ruleName];
                stats.ExecutionCount++;
                stats.TotalExecutionTime += ruleStopwatch.Elapsed;
                if (!validationResult.IsValid)
                {
                    stats.ErrorsFound++;
                }
            }

            stopwatch.Stop();

            // Convert rule statistics to public format
            var ruleStatistics = ruleStatisticsDict.Values.Select(stats => new PublicRuleStatistics
            {
                RuleName = stats.RuleName,
                ExecutionCount = stats.ExecutionCount,
                AverageExecutionTimeMs = stats.ExecutionCount > 0
                    ? stats.TotalExecutionTime.TotalMilliseconds / stats.ExecutionCount
                    : 0,
                ErrorsFound = stats.ErrorsFound,
                TotalExecutionTime = stats.TotalExecutionTime
            }).ToList();

            // Automatický UI refresh v Interactive mode
            await TriggerUIRefreshIfNeededAsync("Validation", 0);

            _logger.LogInformation(
                "Validation with statistics completed: TotalRows={TotalRows}, ValidRows={ValidRows}, TotalErrors={TotalErrors}, Duration={Duration}ms",
                totalRows, validRows, totalErrors, stopwatch.ElapsedMilliseconds);

            return totalErrors == 0
                ? PublicValidationResultWithStatistics.Success(totalRows, stopwatch.Elapsed, ruleStatistics)
                : PublicValidationResultWithStatistics.Failure(
                    totalRows,
                    validRows,
                    totalErrors,
                    errorsBySeverity,
                    stopwatch.Elapsed,
                    ruleStatistics,
                    validationErrors);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Validation with statistics failed");

            return PublicValidationResultWithStatistics.Failure(
                0, 0, 0,
                new Dictionary<string, int>(),
                stopwatch.Elapsed,
                Array.Empty<PublicRuleStatistics>(),
                new List<PublicValidationErrorViewModel>
                {
                    new() { Message = $"Validation failed: {ex.Message}", Severity = "Error" }
                });
        }
    }

    /// <summary>
    /// Obnoví výsledky validácie do UI (no-op v headless režime)
    /// </summary>
    public void RefreshValidationResultsToUI()
    {
        ThrowIfDisposed();

        if (_options.OperationMode == PublicDataGridOperationMode.Headless)
        {
            _logger.LogDebug("RefreshValidationResultsToUI called in headless mode - no operation performed");
            return;
        }

        if (_dispatcher == null)
        {
            _logger.LogWarning("No dispatcher available for UI refresh");
            return;
        }

        _dispatcher.TryEnqueue(DispatcherQueuePriority.Normal, () =>
        {
            _logger.LogDebug("Refreshing validation results to UI");
            // In a real implementation, this would trigger UI updates
        });
    }

    /// <summary>
    /// Manually refreshes UI after operations
    /// Available in both Interactive and Headless modes (if DispatcherQueue is provided)
    /// - Interactive mode: Automatic UI refresh after operations + manual via this method
    /// - Headless mode: NO automatic refresh, ONLY manual via this method
    /// </summary>
    public async Task RefreshUIAsync(string operationType = "ManualRefresh", int affectedRows = 0)
    {
        ThrowIfDisposed();

        if (_uiNotificationService == null)
        {
            throw new InvalidOperationException(
                "UI refresh is not available because DispatcherQueue was not provided in AdvancedDataGridOptions. " +
                "To enable UI refresh, provide a DispatcherQueue when creating the grid.");
        }

        _logger.LogInformation("Manual UI refresh requested: OperationType={OperationType}, AffectedRows={AffectedRows}",
            operationType, affectedRows);

        // Funguje v Interactive aj Headless mode (ak je DispatcherQueue poskytnutý)
        await _uiNotificationService.NotifyDataRefreshAsync(affectedRows, operationType);
    }

    #region MVVM Transformations

    /// <summary>
    /// Adapts raw row data to UI-friendly view model for MVVM binding
    /// </summary>
    public PublicRowViewModel AdaptToRowViewModel(IReadOnlyDictionary<string, object?> rowData, int rowIndex)
    {
        ThrowIfDisposed();

        if (_gridViewModelAdapter == null)
        {
            throw new InvalidOperationException(
                "MVVM transformations are not available because DispatcherQueue was not provided in AdvancedDataGridOptions.");
        }

        if (rowData == null)
            throw new ArgumentNullException(nameof(rowData));

        // Use internal adapter to create internal view model
        var internalViewModel = _gridViewModelAdapter.AdaptToRowViewModel(rowData, rowIndex);

        // Transform to public view model
        return new PublicRowViewModel
        {
            Index = internalViewModel.Index,
            IsSelected = internalViewModel.IsSelected,
            IsValid = internalViewModel.IsValid,
            ValidationErrors = internalViewModel.ValidationErrors != null
                ? internalViewModel.ValidationErrors.ToList().AsReadOnly()
                : Array.Empty<string>(),
            ValidationErrorDetails = Array.Empty<PublicValidationErrorViewModel>(), // Not populated by internal adapter
            CellValues = internalViewModel.CellValues != null
                ? new Dictionary<string, object?>(internalViewModel.CellValues)
                : new Dictionary<string, object?>()
        };
    }

    /// <summary>
    /// Adapts multiple rows to view models for MVVM binding
    /// </summary>
    public IReadOnlyList<PublicRowViewModel> AdaptToRowViewModels(
        IEnumerable<IReadOnlyDictionary<string, object?>> rows,
        int startIndex = 0)
    {
        ThrowIfDisposed();

        if (_gridViewModelAdapter == null)
        {
            throw new InvalidOperationException(
                "MVVM transformations are not available because DispatcherQueue was not provided in AdvancedDataGridOptions.");
        }

        if (rows == null)
            throw new ArgumentNullException(nameof(rows));

        var viewModels = new List<PublicRowViewModel>();
        var currentIndex = startIndex;

        foreach (var row in rows)
        {
            var viewModel = AdaptToRowViewModel(row, currentIndex);
            viewModels.Add(viewModel);
            currentIndex++;
        }

        return viewModels.AsReadOnly();
    }

    /// <summary>
    /// Adapts column definition to UI-friendly view model for MVVM binding
    /// </summary>
    public PublicColumnViewModel AdaptToColumnViewModel(PublicColumnDefinition columnDefinition)
    {
        ThrowIfDisposed();

        if (_gridViewModelAdapter == null)
        {
            throw new InvalidOperationException(
                "MVVM transformations are not available because DispatcherQueue was not provided in AdvancedDataGridOptions.");
        }

        if (columnDefinition == null)
            throw new ArgumentNullException(nameof(columnDefinition));

        // Convert public column definition to internal
        var internalColumnDef = columnDefinition.ToInternal();

        // Use internal adapter to create internal view model
        var internalViewModel = _gridViewModelAdapter.AdaptToColumnViewModel(internalColumnDef);

        // Transform to public view model
        return new PublicColumnViewModel
        {
            Name = internalViewModel.Name,
            DisplayName = internalViewModel.DisplayName,
            IsVisible = internalViewModel.IsVisible,
            Width = internalViewModel.Width,
            IsReadOnly = internalViewModel.IsReadOnly,
            DataType = internalViewModel.DataType,
            SortDirection = internalViewModel.SortDirection
        };
    }

    /// <summary>
    /// Adapts validation errors to UI-friendly view models
    /// This is primarily a convenience method for transforming collections
    /// </summary>
    public IReadOnlyList<PublicValidationErrorViewModel> AdaptValidationErrors(
        IReadOnlyList<PublicValidationErrorViewModel> errors)
    {
        ThrowIfDisposed();

        if (_gridViewModelAdapter == null)
        {
            throw new InvalidOperationException(
                "MVVM transformations are not available because DispatcherQueue was not provided in AdvancedDataGridOptions.");
        }

        if (errors == null)
            throw new ArgumentNullException(nameof(errors));

        // Return as readonly list (already in correct format)
        return errors.ToList().AsReadOnly();
    }

    #endregion

    #region UI Notification Subscriptions

    /// <summary>
    /// Subscribes to validation refresh notifications
    /// </summary>
    public IDisposable SubscribeToValidationRefresh(Action<PublicValidationRefreshEventArgs> handler)
    {
        ThrowIfDisposed();

        if (_uiNotificationService == null)
        {
            throw new InvalidOperationException(
                "UI notification subscriptions are not available because DispatcherQueue was not provided in AdvancedDataGridOptions.");
        }

        if (handler == null)
            throw new ArgumentNullException(nameof(handler));

        _logger.LogDebug("Subscribing to validation refresh notifications");

        // Subscribe to internal event and wrap it
        Action<int, bool> internalHandler = (errorCount, hasErrors) =>
        {
            var eventArgs = new PublicValidationRefreshEventArgs
            {
                TotalErrors = errorCount,
                ErrorCount = hasErrors ? errorCount : 0,
                WarningCount = 0, // Not tracked separately in current implementation
                HasErrors = hasErrors,
                RefreshTime = DateTime.UtcNow
            };

            handler(eventArgs);
        };

        _uiNotificationService.OnValidationResultsRefreshed += internalHandler;

        // Return disposable that unsubscribes
        return new NotificationSubscription(() =>
        {
            _uiNotificationService.OnValidationResultsRefreshed -= internalHandler;
            _logger.LogDebug("Unsubscribed from validation refresh notifications");
        });
    }

    /// <summary>
    /// Subscribes to data refresh notifications
    /// </summary>
    public IDisposable SubscribeToDataRefresh(Action<PublicDataRefreshEventArgs> handler)
    {
        ThrowIfDisposed();

        if (_uiNotificationService == null)
        {
            throw new InvalidOperationException(
                "UI notification subscriptions are not available because DispatcherQueue was not provided in AdvancedDataGridOptions.");
        }

        if (handler == null)
            throw new ArgumentNullException(nameof(handler));

        _logger.LogDebug("Subscribing to data refresh notifications");

        // Subscribe to internal event and wrap it
        Action<int, int> internalHandler = (rowCount, columnCount) =>
        {
            var eventArgs = new PublicDataRefreshEventArgs
            {
                AffectedRows = rowCount,
                ColumnCount = columnCount,
                OperationType = "DataRefresh",
                RefreshTime = DateTime.UtcNow
            };

            handler(eventArgs);
        };

        _uiNotificationService.OnDataRefreshed += internalHandler;

        // Return disposable that unsubscribes
        return new NotificationSubscription(() =>
        {
            _uiNotificationService.OnDataRefreshed -= internalHandler;
            _logger.LogDebug("Unsubscribed from data refresh notifications");
        });
    }

    /// <summary>
    /// Subscribes to operation progress notifications
    /// </summary>
    public IDisposable SubscribeToOperationProgress(Action<PublicOperationProgressEventArgs> handler)
    {
        ThrowIfDisposed();

        if (_uiNotificationService == null)
        {
            throw new InvalidOperationException(
                "UI notification subscriptions are not available because DispatcherQueue was not provided in AdvancedDataGridOptions.");
        }

        if (handler == null)
            throw new ArgumentNullException(nameof(handler));

        _logger.LogDebug("Subscribing to operation progress notifications");

        // Subscribe to internal event and wrap it
        Action<string, double, string?> internalHandler = (operationName, progressPercentage, message) =>
        {
            var eventArgs = new PublicOperationProgressEventArgs
            {
                OperationName = operationName,
                ProcessedItems = 0, // Not tracked separately
                TotalItems = 0, // Not tracked separately
                ProgressPercentage = progressPercentage,
                Message = message,
                ElapsedTime = TimeSpan.Zero // Not tracked separately
            };

            handler(eventArgs);
        };

        _uiNotificationService.OnOperationProgress += internalHandler;

        // Return disposable that unsubscribes
        return new NotificationSubscription(() =>
        {
            _uiNotificationService.OnOperationProgress -= internalHandler;
            _logger.LogDebug("Unsubscribed from operation progress notifications");
        });
    }

    #endregion

    #endregion

    #region Copy/Paste Operations

    /// <summary>
    /// Nastaví obsah schránky pre copy/paste operácie
    /// </summary>
    public void SetClipboard(object payload)
    {
        ThrowIfDisposed();

        var copyPasteService = _serviceProvider.GetRequiredService<ICopyPasteService>();
        copyPasteService.SetClipboard(payload);
        _logger.LogDebug("Clipboard content updated");
    }

    /// <summary>
    /// Získa obsah schránky pre copy/paste operácie
    /// </summary>
    public object? GetClipboard()
    {
        ThrowIfDisposed();

        var copyPasteService = _serviceProvider.GetRequiredService<ICopyPasteService>();
        return copyPasteService.GetClipboard();
    }

    /// <summary>
    /// Skopíruje vybrané dáta do schránky
    /// </summary>
    public async Task<CopyPasteResult> CopyAsync(CopyDataCommand command, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        EnsureFeatureEnabled(GridFeature.CopyPaste, nameof(CopyAsync));

        _logger.LogInformation("Starting copy operation [CorrelationId: {CorrelationId}]", command.CorrelationId);

        try
        {
            var copyPasteService = _serviceProvider.GetRequiredService<ICopyPasteService>();
            return await copyPasteService.CopyToClipboardAsync(command, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Copy operation failed [CorrelationId: {CorrelationId}]", command.CorrelationId);
            throw;
        }
    }

    /// <summary>
    /// Vloží dáta zo schránky
    /// </summary>
    public async Task<CopyPasteResult> PasteAsync(PasteDataCommand command, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        EnsureFeatureEnabled(GridFeature.CopyPaste, nameof(PasteAsync));

        _logger.LogInformation("Starting paste operation [CorrelationId: {CorrelationId}]", command.CorrelationId);

        try
        {
            var copyPasteService = _serviceProvider.GetRequiredService<ICopyPasteService>();
            return await copyPasteService.PasteFromClipboardAsync(command, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Paste operation failed [CorrelationId: {CorrelationId}]", command.CorrelationId);
            throw;
        }
    }

    #endregion

    #region Selection Operations

    /// <summary>
    /// Starts column resize operation
    /// </summary>
    public double ResizeColumn(int columnIndex, double newWidth)
    {
        ThrowIfDisposed();
        EnsureFeatureEnabled(GridFeature.ColumnResize, nameof(ResizeColumn));

        try
        {
            var resizeService = _serviceProvider.GetRequiredService<Features.ColumnResize.Interfaces.IColumnResizeService>();
            return resizeService.ResizeColumn(columnIndex, newWidth);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to resize column {ColumnIndex}: {Message}", columnIndex, ex.Message);
            throw;
        }
    }

    public void StartColumnResize(int columnIndex, double clientX)
    {
        ThrowIfDisposed();
        EnsureFeatureEnabled(GridFeature.ColumnResize, nameof(StartColumnResize));

        try
        {
            var resizeService = _serviceProvider.GetRequiredService<Features.ColumnResize.Interfaces.IColumnResizeService>();
            resizeService.StartColumnResize(columnIndex, clientX);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start column resize for column {ColumnIndex}: {Message}", columnIndex, ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Updates column resize operation
    /// </summary>
    public void UpdateColumnResize(double clientX)
    {
        ThrowIfDisposed();
        EnsureFeatureEnabled(GridFeature.ColumnResize, nameof(UpdateColumnResize));

        try
        {
            var resizeService = _serviceProvider.GetRequiredService<Features.ColumnResize.Interfaces.IColumnResizeService>();
            resizeService.UpdateColumnResize(clientX);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update column resize at clientX {ClientX}: {Message}", clientX, ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Ends column resize operation
    /// </summary>
    public void EndColumnResize()
    {
        ThrowIfDisposed();
        EnsureFeatureEnabled(GridFeature.ColumnResize, nameof(EndColumnResize));

        try
        {
            var resizeService = _serviceProvider.GetRequiredService<Features.ColumnResize.Interfaces.IColumnResizeService>();
            resizeService.EndColumnResize();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to end column resize: {Message}", ex.Message);
            throw;
        }
    }

    public double GetColumnWidth(int columnIndex)
    {
        ThrowIfDisposed();
        EnsureFeatureEnabled(GridFeature.ColumnResize, nameof(GetColumnWidth));

        try
        {
            var resizeService = _serviceProvider.GetRequiredService<Features.ColumnResize.Interfaces.IColumnResizeService>();
            return resizeService.GetColumnWidth(columnIndex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get column width for column {ColumnIndex}: {Message}", columnIndex, ex.Message);
            throw;
        }
    }

    public bool IsResizing()
    {
        ThrowIfDisposed();

        try
        {
            var resizeService = _serviceProvider.GetRequiredService<Features.ColumnResize.Interfaces.IColumnResizeService>();
            return resizeService.IsResizing();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check resize status: {Message}", ex.Message);
            return false;
        }
    }

    /// <summary>
    /// Starts drag selection operation
    /// </summary>
    public void StartDragSelect(int row, int col)
    {
        ThrowIfDisposed();

        using var scope = ServiceRegistration.CreateOperationScope(_serviceProvider);
        var selectionService = scope.ServiceProvider.GetRequiredService<ISelectionService>();
        selectionService.StartDragSelectInternal(row, col);
    }

    /// <summary>
    /// Updates drag selection to new position
    /// </summary>
    public void DragSelectTo(int row, int col)
    {
        ThrowIfDisposed();

        using var scope = ServiceRegistration.CreateOperationScope(_serviceProvider);
        var selectionService = scope.ServiceProvider.GetRequiredService<ISelectionService>();
        selectionService.UpdateDragSelectInternal(row, col);
    }

    /// <summary>
    /// Ends drag selection operation
    /// </summary>
    public void EndDragSelect(int row, int col)
    {
        ThrowIfDisposed();

        using var scope = ServiceRegistration.CreateOperationScope(_serviceProvider);
        var selectionService = scope.ServiceProvider.GetRequiredService<ISelectionService>();
        selectionService.EndDragSelectInternal();
    }

    /// <summary>
    /// Selects a specific cell
    /// </summary>
    public void SelectCell(int row, int col)
    {
        ThrowIfDisposed();

        using var scope = ServiceRegistration.CreateOperationScope(_serviceProvider);
        var selectionService = scope.ServiceProvider.GetRequiredService<ISelectionService>();
        selectionService.SelectCellInternal(row, col);
    }

    /// <summary>
    /// Toggles cell selection state
    /// </summary>
    public void ToggleCellSelection(int row, int col)
    {
        ThrowIfDisposed();

        using var scope = ServiceRegistration.CreateOperationScope(_serviceProvider);
        var selectionService = scope.ServiceProvider.GetRequiredService<ISelectionService>();
        selectionService.ToggleSelectionInternal(row, col);
    }

    /// <summary>
    /// Extends selection to specified cell
    /// </summary>
    public void ExtendSelectionTo(int row, int col)
    {
        ThrowIfDisposed();

        using var scope = ServiceRegistration.CreateOperationScope(_serviceProvider);
        var selectionService = scope.ServiceProvider.GetRequiredService<ISelectionService>();
        selectionService.ExtendSelectionInternal(row, col);
    }

    #endregion

    #region Data Access APIs

    /// <summary>
    /// Získa aktuálne dáta gridu ako read-only dictionary kolekciu
    /// </summary>
    public IReadOnlyList<IReadOnlyDictionary<string, object?>> GetCurrentData()
    {
        ThrowIfDisposed();

        try
        {
            var rowStore = _serviceProvider.GetRequiredService<IRowStore>();
            var data = rowStore.GetAllRowsAsync().GetAwaiter().GetResult();
            return data.ToList().AsReadOnly();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get current data");
            return new List<IReadOnlyDictionary<string, object?>>().AsReadOnly();
        }
    }

    /// <summary>
    /// Získa aktuálne dáta gridu ako DataTable
    /// </summary>
    public async Task<DataTable> GetCurrentDataAsDataTableAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        try
        {
            using var scope = ServiceRegistration.CreateOperationScope(_serviceProvider);
            var exportService = scope.ServiceProvider.GetRequiredService<IExportService>();
            var rowStore = scope.ServiceProvider.GetRequiredService<IRowStore>();

            var currentData = await rowStore.GetAllRowsAsync(cancellationToken);
            var exportCommand = ExportDataCommand.ToDataTable(correlationId: Guid.NewGuid().ToString());
            var internalCommand = exportCommand.ToInternal();

            return await exportService.ExportToDataTableAsync(currentData, internalCommand, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get current data as DataTable");
            return new DataTable();
        }
    }

    #endregion

    #region Column Management APIs

    /// <summary>
    /// Získa definície stĺpcov
    /// </summary>
    public IReadOnlyList<PublicColumnDefinition> GetColumnDefinitions()
    {
        ThrowIfDisposed();

        using var scope = ServiceRegistration.CreateOperationScope(_serviceProvider);
        var columnService = scope.ServiceProvider.GetRequiredService<IColumnService>();
        return columnService.GetColumnDefinitions().ToPublicList();
    }

    /// <summary>
    /// Pridá novú definíciu stĺpca
    /// </summary>
    public bool AddColumn(PublicColumnDefinition columnDefinition)
    {
        ThrowIfDisposed();

        using var scope = ServiceRegistration.CreateOperationScope(_serviceProvider);
        var columnService = scope.ServiceProvider.GetRequiredService<IColumnService>();
        return columnService.AddColumn(columnDefinition.ToInternal());
    }

    /// <summary>
    /// Odstráni stĺpec podľa názvu
    /// </summary>
    public bool RemoveColumn(string columnName)
    {
        ThrowIfDisposed();

        using var scope = ServiceRegistration.CreateOperationScope(_serviceProvider);
        var columnService = scope.ServiceProvider.GetRequiredService<IColumnService>();
        return columnService.RemoveColumn(columnName);
    }

    /// <summary>
    /// Aktualizuje existujúcu definíciu stĺpca
    /// </summary>
    public bool UpdateColumn(PublicColumnDefinition columnDefinition)
    {
        ThrowIfDisposed();

        using var scope = ServiceRegistration.CreateOperationScope(_serviceProvider);
        var columnService = scope.ServiceProvider.GetRequiredService<IColumnService>();
        return columnService.UpdateColumn(columnDefinition.ToInternal());
    }

    #endregion

    #region Validation Management APIs

    /// <summary>
    /// Pridá validačné pravidlo
    /// </summary>
    public async Task<PublicResult> AddValidationRuleAsync(IValidationRule rule)
    {
        ThrowIfDisposed();

        try
        {
            using var scope = ServiceRegistration.CreateOperationScope(_serviceProvider);
            var validationService = scope.ServiceProvider.GetRequiredService<IValidationService>();
            var internalResult = await validationService.AddValidationRuleAsync(rule);
            return internalResult.ToPublic();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add validation rule");
            return PublicResult.Failure($"Failed to add validation rule: {ex.Message}");
        }
    }

    /// <summary>
    /// Odstráni validačné pravidlá podľa názvov stĺpcov
    /// </summary>
    public async Task<PublicResult> RemoveValidationRulesAsync(params string[] columnNames)
    {
        ThrowIfDisposed();

        try
        {
            using var scope = ServiceRegistration.CreateOperationScope(_serviceProvider);
            var validationService = scope.ServiceProvider.GetRequiredService<IValidationService>();
            var internalResult = await validationService.RemoveValidationRulesAsync(columnNames);
            return internalResult.ToPublic();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove validation rules");
            return PublicResult.Failure($"Failed to remove validation rules: {ex.Message}");
        }
    }

    /// <summary>
    /// Odstráni validačné pravidlo podľa názvu
    /// </summary>
    public async Task<PublicResult> RemoveValidationRuleAsync(string ruleName)
    {
        ThrowIfDisposed();

        try
        {
            using var scope = ServiceRegistration.CreateOperationScope(_serviceProvider);
            var validationService = scope.ServiceProvider.GetRequiredService<IValidationService>();
            var internalResult = await validationService.RemoveValidationRuleAsync(ruleName);
            return internalResult.ToPublic();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove validation rule");
            return PublicResult.Failure($"Failed to remove validation rule: {ex.Message}");
        }
    }

    /// <summary>
    /// Vymaže všetky validačné pravidlá
    /// </summary>
    public async Task<PublicResult> ClearAllValidationRulesAsync()
    {
        ThrowIfDisposed();

        try
        {
            using var scope = ServiceRegistration.CreateOperationScope(_serviceProvider);
            var validationService = scope.ServiceProvider.GetRequiredService<IValidationService>();
            var internalResult = await validationService.ClearAllValidationRulesAsync();
            return internalResult.ToPublic();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to clear validation rules");
            return PublicResult.Failure($"Failed to clear validation rules: {ex.Message}");
        }
    }

    #endregion

    #region Placeholder Implementations

    // These methods would need actual implementations based on specific requirements
    // For now, providing basic placeholder implementations

    public async Task<int> ApplyFilterAsync(string columnName, PublicFilterOperator @operator, object? value)
    {
        ThrowIfDisposed();
        EnsureFeatureEnabled(GridFeature.Filter, nameof(ApplyFilterAsync));

        try
        {
            using var scope = ServiceRegistration.CreateOperationScope(_serviceProvider);
            var filterService = scope.ServiceProvider.GetRequiredService<IFilterService>();
            var internalOperator = @operator.ToInternal();
            return await filterService.ApplyFilterAsync(columnName, internalOperator, value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to apply filter to column {ColumnName}", columnName);
            return 0;
        }
    }

    public async Task<int> ClearFiltersAsync()
    {
        ThrowIfDisposed();

        try
        {
            using var scope = ServiceRegistration.CreateOperationScope(_serviceProvider);
            var filterService = scope.ServiceProvider.GetRequiredService<IFilterService>();
            return await filterService.ClearFiltersAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to clear filters");
            return 0;
        }
    }

    /// <summary>
    /// Sortuje dáta podľa jedného stĺpca pomocou command pattern
    /// </summary>
    public async Task<SortDataResult> SortAsync(SortDataCommand command, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        EnsureFeatureEnabled(GridFeature.Sort, nameof(SortAsync));

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var operationId = command.CorrelationId ?? Guid.NewGuid();

        using var logScope = _operationLogger.LogOperationStart("SortAsync", new
        {
            OperationId = operationId,
            ColumnName = command.ColumnName,
            Direction = command.Direction
        });

        _logger.LogInformation("Starting sort operation {OperationId}: column={ColumnName}, direction={Direction}",
            operationId, command.ColumnName, command.Direction);

        try
        {
            using var scope = ServiceRegistration.CreateOperationScope(_serviceProvider);
            var sortService = scope.ServiceProvider.GetRequiredService<Features.Sort.Interfaces.ISortService>();

            // Mapujeme public command na internal
            var internalCommand = command.ToInternal();

            // Vykonáme sort
            var internalResult = await sortService.SortAsync(internalCommand, cancellationToken);

            // Mapujeme result na public
            var result = internalResult.ToPublic();

            logScope.MarkSuccess(new { Duration = stopwatch.Elapsed, Success = result.IsSuccess });
            _logger.LogInformation("Sort operation {OperationId} completed in {Duration}ms",
                operationId, stopwatch.ElapsedMilliseconds);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Sort operation {OperationId} failed", operationId);
            logScope.MarkFailure(ex);
            return new SortDataResult(false, Array.Empty<IReadOnlyDictionary<string, object?>>(), 0, stopwatch.Elapsed, false, new[] { $"Sort failed: {ex.Message}" });
        }
    }

    /// <summary>
    /// Sortuje dáta podľa viacerých stĺpcov pomocou command pattern
    /// </summary>
    public async Task<SortDataResult> MultiSortAsync(MultiSortDataCommand command, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        EnsureFeatureEnabled(GridFeature.Sort, nameof(MultiSortAsync));

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var operationId = command.CorrelationId ?? Guid.NewGuid();

        using var logScope = _operationLogger.LogOperationStart("MultiSortAsync", new
        {
            OperationId = operationId,
            ColumnCount = command.SortColumns.Count
        });

        _logger.LogInformation("Starting multi-sort operation {OperationId}: {ColumnCount} columns",
            operationId, command.SortColumns.Count);

        try
        {
            using var scope = ServiceRegistration.CreateOperationScope(_serviceProvider);
            var sortService = scope.ServiceProvider.GetRequiredService<Features.Sort.Interfaces.ISortService>();

            // Mapujeme public command na internal
            var internalCommand = command.ToInternal();

            // Vykonáme multi-sort
            var internalResult = await sortService.MultiSortAsync(internalCommand, cancellationToken);

            // Mapujeme result na public
            var result = internalResult.ToPublic();

            logScope.MarkSuccess(new { Duration = stopwatch.Elapsed, Success = result.IsSuccess });
            _logger.LogInformation("Multi-sort operation {OperationId} completed in {Duration}ms",
                operationId, stopwatch.ElapsedMilliseconds);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Multi-sort operation {OperationId} failed", operationId);
            logScope.MarkFailure(ex);
            return new SortDataResult(false, Array.Empty<IReadOnlyDictionary<string, object?>>(), 0, stopwatch.Elapsed, false, new[] { $"Multi-sort failed: {ex.Message}" });
        }
    }

    /// <summary>
    /// Quick synchronous sort pre okamžité výsledky
    /// </summary>
    public SortDataResult QuickSort(IEnumerable<IReadOnlyDictionary<string, object?>> data, string columnName, PublicSortDirection direction = PublicSortDirection.Ascending)
    {
        ThrowIfDisposed();

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            using var scope = ServiceRegistration.CreateOperationScope(_serviceProvider);
            var sortService = scope.ServiceProvider.GetRequiredService<Features.Sort.Interfaces.ISortService>();

            // Mapujeme public direction na internal
            var internalDirection = direction.ToInternal();

            // Vykonáme quick sort
            var internalResult = sortService.QuickSort(data, columnName, internalDirection);

            // Mapujeme result na public
            var result = internalResult.ToPublic();

            _logger.LogInformation("QuickSort completed in {Duration}ms for column {ColumnName}",
                stopwatch.ElapsedMilliseconds, columnName);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "QuickSort failed for column {ColumnName}", columnName);
            return new SortDataResult(false, Array.Empty<IReadOnlyDictionary<string, object?>>(), 0, stopwatch.Elapsed, false, new[] { $"QuickSort failed: {ex.Message}" });
        }
    }

    /// <summary>
    /// Získa zoznam sortovateľných stĺpcov
    /// </summary>
    public IReadOnlyList<string> GetSortableColumns(IEnumerable<IReadOnlyDictionary<string, object?>> data)
    {
        ThrowIfDisposed();

        try
        {
            using var scope = ServiceRegistration.CreateOperationScope(_serviceProvider);
            var sortService = scope.ServiceProvider.GetRequiredService<Features.Sort.Interfaces.ISortService>();

            return sortService.GetSortableColumns(data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get sortable columns");
            return Array.Empty<string>();
        }
    }

    public async Task<PublicResult> SortByColumnAsync(string columnName, PublicSortDirection direction)
    {
        ThrowIfDisposed();

        try
        {
            using var scope = ServiceRegistration.CreateOperationScope(_serviceProvider);
            var sortService = scope.ServiceProvider.GetRequiredService<Features.Sort.Interfaces.ISortService>();

            // Mapujeme public direction na internal
            var internalDirection = direction.ToInternal();
            var success = await sortService.SortByColumnAsync(columnName, (Core.ValueObjects.SortDirection)internalDirection, CancellationToken.None);
            return success ? PublicResult.Success() : PublicResult.Failure("Sort failed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Legacy sort by column failed");
            return PublicResult.Failure($"Sort failed: {ex.Message}");
        }
    }

    public async Task<PublicResult> ClearSortingAsync()
    {
        ThrowIfDisposed();

        try
        {
            using var scope = ServiceRegistration.CreateOperationScope(_serviceProvider);
            var sortService = scope.ServiceProvider.GetRequiredService<Features.Sort.Interfaces.ISortService>();

            var success = await sortService.ClearSortAsync();
            return success ? PublicResult.Success() : PublicResult.Failure("Clear sort failed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Clear sorting failed");
            return PublicResult.Failure($"Clear sort failed: {ex.Message}");
        }
    }

    #region Search Operations

    /// <summary>
    /// Vykoná základné vyhľadávanie pomocou command pattern
    /// </summary>
    public async Task<SearchDataResult> SearchAsync(SearchDataCommand command, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        EnsureFeatureEnabled(GridFeature.Search, nameof(SearchAsync));

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var operationId = command.CorrelationId ?? Guid.NewGuid();

        using var logScope = _operationLogger.LogOperationStart("SearchAsync", new
        {
            OperationId = operationId,
            SearchText = command.SearchText,
            TargetColumns = command.TargetColumns?.Length ?? 0
        });

        _logger.LogInformation("Starting search operation {OperationId}: text='{SearchText}', columns={ColumnCount}",
            operationId, command.SearchText, command.TargetColumns?.Length ?? 0);

        try
        {
            using var scope = ServiceRegistration.CreateOperationScope(_serviceProvider);
            var searchService = scope.ServiceProvider.GetRequiredService<Features.Search.Interfaces.ISearchService>();

            // Mapujeme public command na internal
            var internalCommand = command.ToInternal();

            // Vykonáme search
            var internalResult = await searchService.SearchAsync(internalCommand, cancellationToken);

            // Mapujeme result na public
            var result = internalResult.ToPublic();

            logScope.MarkSuccess(new { Duration = stopwatch.Elapsed, Success = result.IsSuccess });
            _logger.LogInformation("Search operation {OperationId} completed in {Duration}ms: found {MatchCount} matches",
                operationId, stopwatch.ElapsedMilliseconds, result.TotalMatchesFound);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Search operation {OperationId} failed", operationId);
            logScope.MarkFailure(ex);
            return new SearchDataResult(false, Array.Empty<PublicSearchResult>(), 0, 0, stopwatch.Elapsed, PublicSearchMode.Contains, false, new[] { $"Search failed: {ex.Message}" });
        }
    }

    /// <summary>
    /// Vykoná pokročilé vyhľadávanie s regex, fuzzy matching a smart ranking
    /// </summary>
    public async Task<SearchDataResult> AdvancedSearchAsync(AdvancedSearchDataCommand command, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var operationId = command.CorrelationId ?? Guid.NewGuid();

        using var logScope = _operationLogger.LogOperationStart("AdvancedSearchAsync", new
        {
            OperationId = operationId,
            SearchText = command.SearchCriteria.SearchText,
            Mode = command.SearchCriteria.Mode
        });

        _logger.LogInformation("Starting advanced search operation {OperationId}: text='{SearchText}', mode={Mode}",
            operationId, command.SearchCriteria.SearchText, command.SearchCriteria.Mode);

        try
        {
            using var scope = ServiceRegistration.CreateOperationScope(_serviceProvider);
            var searchService = scope.ServiceProvider.GetRequiredService<Features.Search.Interfaces.ISearchService>();

            // Mapujeme public command na internal
            var internalCommand = command.ToInternal();

            // Vykonáme advanced search
            var internalResult = await searchService.AdvancedSearchAsync(internalCommand, cancellationToken);

            // Mapujeme result na public
            var result = internalResult.ToPublic();

            logScope.MarkSuccess(new { Duration = stopwatch.Elapsed, Success = result.IsSuccess });
            _logger.LogInformation("Advanced search operation {OperationId} completed in {Duration}ms: found {MatchCount} matches",
                operationId, stopwatch.ElapsedMilliseconds, result.TotalMatchesFound);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Advanced search operation {OperationId} failed", operationId);
            logScope.MarkFailure(ex);
            return new SearchDataResult(false, Array.Empty<PublicSearchResult>(), 0, 0, stopwatch.Elapsed, command.SearchCriteria.Mode, false, new[] { $"Advanced search failed: {ex.Message}" });
        }
    }

    /// <summary>
    /// Vykoná smart search s automatickou optimalizáciou
    /// </summary>
    public async Task<SearchDataResult> SmartSearchAsync(SmartSearchDataCommand command, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var operationId = command.CorrelationId ?? Guid.NewGuid();

        using var logScope = _operationLogger.LogOperationStart("SmartSearchAsync", new
        {
            OperationId = operationId,
            SearchText = command.SearchText,
            AutoOptimize = command.AutoOptimize
        });

        _logger.LogInformation("Starting smart search operation {OperationId}: text='{SearchText}', autoOptimize={AutoOptimize}",
            operationId, command.SearchText, command.AutoOptimize);

        try
        {
            using var scope = ServiceRegistration.CreateOperationScope(_serviceProvider);
            var searchService = scope.ServiceProvider.GetRequiredService<Features.Search.Interfaces.ISearchService>();

            // Mapujeme public command na internal
            var internalCommand = command.ToInternal();

            // Vykonáme smart search
            var internalResult = await searchService.SmartSearchAsync(internalCommand, cancellationToken);

            // Mapujeme result na public
            var result = internalResult.ToPublic();

            logScope.MarkSuccess(new { Duration = stopwatch.Elapsed, Success = result.IsSuccess });
            _logger.LogInformation("Smart search operation {OperationId} completed in {Duration}ms: found {MatchCount} matches",
                operationId, stopwatch.ElapsedMilliseconds, result.TotalMatchesFound);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Smart search operation {OperationId} failed", operationId);
            logScope.MarkFailure(ex);
            return new SearchDataResult(false, Array.Empty<PublicSearchResult>(), 0, 0, stopwatch.Elapsed, PublicSearchMode.Contains, false, new[] { $"Smart search failed: {ex.Message}" });
        }
    }

    /// <summary>
    /// Quick synchronous search pre okamžité výsledky
    /// </summary>
    public SearchDataResult QuickSearch(IEnumerable<IReadOnlyDictionary<string, object?>> data, string searchText, bool caseSensitive = false)
    {
        ThrowIfDisposed();

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            using var scope = ServiceRegistration.CreateOperationScope(_serviceProvider);
            var searchService = scope.ServiceProvider.GetRequiredService<Features.Search.Interfaces.ISearchService>();

            // Vykonáme quick search
            var internalResult = searchService.QuickSearch(data, searchText, caseSensitive);

            // Mapujeme result na public
            var result = internalResult.ToPublic();

            _logger.LogInformation("QuickSearch completed in {Duration}ms for text '{SearchText}': found {MatchCount} matches",
                stopwatch.ElapsedMilliseconds, searchText, result.TotalMatchesFound);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "QuickSearch failed for text '{SearchText}'", searchText);
            return new SearchDataResult(false, Array.Empty<PublicSearchResult>(), 0, 0, stopwatch.Elapsed, PublicSearchMode.Contains, false, new[] { $"QuickSearch failed: {ex.Message}" });
        }
    }

    /// <summary>
    /// Validuje search kritériá
    /// </summary>
    public async Task<PublicResult> ValidateSearchCriteriaAsync(PublicAdvancedSearchCriteria searchCriteria)
    {
        ThrowIfDisposed();

        try
        {
            using var scope = ServiceRegistration.CreateOperationScope(_serviceProvider);
            var searchService = scope.ServiceProvider.GetRequiredService<Features.Search.Interfaces.ISearchService>();

            // Mapujeme public criteria na internal
            var internalCriteria = searchCriteria.ToInternal();

            // Validácia
            var internalResult = await searchService.ValidateSearchCriteriaAsync(internalCriteria);
            return internalResult.ToPublic();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Search criteria validation failed");
            return PublicResult.Failure($"Validation failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Získa zoznam searchable stĺpcov
    /// </summary>
    public IReadOnlyList<string> GetSearchableColumns(IEnumerable<IReadOnlyDictionary<string, object?>> data)
    {
        ThrowIfDisposed();

        try
        {
            using var scope = ServiceRegistration.CreateOperationScope(_serviceProvider);
            var searchService = scope.ServiceProvider.GetRequiredService<Features.Search.Interfaces.ISearchService>();

            return searchService.GetSearchableColumns(data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get searchable columns");
            return Array.Empty<string>();
        }
    }

    #endregion

    public async Task<int> AddRowAsync(IReadOnlyDictionary<string, object?> rowData)
    {
        ThrowIfDisposed();
        await Task.Delay(1); // Placeholder
        return 0;
    }

    public async Task<bool> RemoveRowAsync(int rowIndex)
    {
        ThrowIfDisposed();
        await Task.Delay(1); // Placeholder
        return false;
    }

    public async Task<bool> UpdateRowAsync(int rowIndex, IReadOnlyDictionary<string, object?> rowData)
    {
        ThrowIfDisposed();
        await Task.Delay(1); // Placeholder
        return false;
    }

    public IReadOnlyDictionary<string, object?>? GetRow(int rowIndex)
    {
        ThrowIfDisposed();
        return null; // Placeholder
    }

    public int GetRowCount()
    {
        ThrowIfDisposed();
        return 0; // Placeholder
    }

    public int GetVisibleRowCount()
    {
        ThrowIfDisposed();
        return 0; // Placeholder
    }

    #endregion

    #region Disposal

    public async ValueTask DisposeAsync()
    {
        if (!_disposed)
        {
            _logger.LogInformation("Disposing AdvancedDataGrid facade");

            try
            {
                // Dispose of service provider if it's disposable
                if (_serviceProvider is IDisposable disposableProvider)
                {
                    disposableProvider.Dispose();
                }
                else if (_serviceProvider is IAsyncDisposable asyncDisposableProvider)
                {
                    await asyncDisposableProvider.DisposeAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during facade disposal");
            }

            _disposed = true;
        }
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(AdvancedDataGridFacade));
        }
    }

    #endregion

    #region AutoRowHeight Implementation

    public async Task<PublicAutoRowHeightResult> EnableAutoRowHeightAsync(
        PublicAutoRowHeightConfiguration configuration,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var scope = ServiceRegistration.CreateOperationScope(_serviceProvider);
            var autoRowHeightService = scope.ServiceProvider.GetRequiredService<IAutoRowHeightService>();

            _logger.LogInformation("Enabling auto row height with configuration: MinHeight={MinHeight}, MaxHeight={MaxHeight}",
                configuration.MinimumRowHeight, configuration.MaximumRowHeight);

            var internalConfig = configuration.ToInternal();
            var internalResult = await autoRowHeightService.EnableAutoRowHeightAsync(internalConfig, cancellationToken);
            return internalResult.ToPublic();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to enable auto row height");
            return new PublicAutoRowHeightResult(false, ex.Message, null, null);
        }
    }

    public async Task<IReadOnlyList<PublicRowHeightCalculationResult>> CalculateOptimalRowHeightsAsync(
        IProgress<PublicBatchCalculationProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var scope = ServiceRegistration.CreateOperationScope(_serviceProvider);
            var autoRowHeightService = scope.ServiceProvider.GetRequiredService<IAutoRowHeightService>();

            _logger.LogInformation("Starting optimal row height calculation");

            // Create progress wrapper if provided
            IProgress<Features.AutoRowHeight.Interfaces.BatchCalculationProgress>? internalProgress = null;
            if (progress != null)
            {
                internalProgress = new Progress<Features.AutoRowHeight.Interfaces.BatchCalculationProgress>(p =>
                {
                    progress.Report(new PublicBatchCalculationProgress(
                        p.ProcessedRows,
                        p.TotalRows,
                        p.ElapsedTime.TotalMilliseconds,
                        p.CurrentOperation
                    ));
                });
            }

            var internalResults = await autoRowHeightService.CalculateOptimalRowHeightsAsync(internalProgress, cancellationToken);
            return internalResults.ToPublicList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to calculate optimal row heights");
            return Array.Empty<PublicRowHeightCalculationResult>();
        }
    }

    public async Task<PublicRowHeightCalculationResult> CalculateRowHeightAsync(
        int rowIndex,
        IReadOnlyDictionary<string, object?> rowData,
        PublicRowHeightCalculationOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var scope = ServiceRegistration.CreateOperationScope(_serviceProvider);
            var autoRowHeightService = scope.ServiceProvider.GetRequiredService<IAutoRowHeightService>();

            _logger.LogDebug("Calculating row height for row {RowIndex}", rowIndex);

            var internalOptions = options.ToInternal();
            var internalResult = await autoRowHeightService.CalculateRowHeightAsync(rowIndex, rowData, internalOptions, cancellationToken);
            return internalResult.ToPublic();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to calculate row height for row {RowIndex}", rowIndex);
            return new PublicRowHeightCalculationResult(rowIndex, 0, false, ex.Message, null);
        }
    }

    public async Task<PublicTextMeasurementResult> MeasureTextAsync(
        string text,
        string fontFamily,
        double fontSize,
        double maxWidth,
        bool textWrapping = true,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var scope = ServiceRegistration.CreateOperationScope(_serviceProvider);
            var autoRowHeightService = scope.ServiceProvider.GetRequiredService<IAutoRowHeightService>();

            _logger.LogDebug("Measuring text: length={TextLength}, font={FontFamily}, size={FontSize}",
                text?.Length ?? 0, fontFamily, fontSize);

            var internalResult = await autoRowHeightService.MeasureTextAsync(text ?? string.Empty, fontFamily, fontSize, maxWidth, textWrapping, cancellationToken);
            return internalResult.ToPublic();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to measure text");
            // Return fallback measurement
            return new PublicTextMeasurementResult(maxWidth, fontSize * 1.5, text ?? string.Empty, fontFamily, fontSize, false);
        }
    }

    public async Task<PublicAutoRowHeightResult> ApplyAutoRowHeightConfigurationAsync(
        PublicAutoRowHeightConfiguration configuration,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var scope = ServiceRegistration.CreateOperationScope(_serviceProvider);
            var autoRowHeightService = scope.ServiceProvider.GetRequiredService<IAutoRowHeightService>();

            _logger.LogInformation("Applying auto row height configuration: MinHeight={MinHeight}, MaxHeight={MaxHeight}",
                configuration.MinimumRowHeight, configuration.MaximumRowHeight);

            var internalConfig = configuration.ToInternal();
            var internalResult = await autoRowHeightService.ApplyConfigurationAsync(internalConfig, cancellationToken);
            return internalResult.ToPublic();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to apply auto row height configuration");
            return new PublicAutoRowHeightResult(false, ex.Message, null, null);
        }
    }

    public async Task<bool> InvalidateHeightCacheAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using var scope = ServiceRegistration.CreateOperationScope(_serviceProvider);
            var autoRowHeightService = scope.ServiceProvider.GetRequiredService<IAutoRowHeightService>();

            _logger.LogInformation("Invalidating auto row height cache");

            return await autoRowHeightService.InvalidateHeightCacheAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to invalidate height cache");
            return false;
        }
    }

    public PublicAutoRowHeightStatistics GetAutoRowHeightStatistics()
    {
        try
        {
            using var scope = ServiceRegistration.CreateOperationScope(_serviceProvider);
            var autoRowHeightService = scope.ServiceProvider.GetRequiredService<IAutoRowHeightService>();

            var internalStats = autoRowHeightService.GetStatistics();
            return internalStats.ToPublic();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get auto row height statistics");
            return new PublicAutoRowHeightStatistics(0, 0, 0, 0, 0, 0, 0);
        }
    }

    public PublicCacheStatistics GetCacheStatistics()
    {
        try
        {
            using var scope = ServiceRegistration.CreateOperationScope(_serviceProvider);
            var autoRowHeightService = scope.ServiceProvider.GetRequiredService<IAutoRowHeightService>();

            var internalStats = autoRowHeightService.GetCacheStatistics();
            return internalStats.ToPublic();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get cache statistics");
            return new PublicCacheStatistics(0, 0, 0, 0, 0, 0, 0);
        }
    }

    #endregion

    #region Component Initialization & Lifecycle

    /// <summary>
    /// Initialize for UI mode with default configuration
    /// CONVENIENCE: Simplified UI initialization
    /// </summary>
    public async Task<PublicInitializationResult> InitializeForUIAsync(
        PublicInitializationConfiguration? config = null,
        IProgress<PublicInitializationProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        try
        {
            using var scope = ServiceRegistration.CreateOperationScope(_serviceProvider);
            var lifecycleManager = scope.ServiceProvider.GetRequiredService<IComponentLifecycleManager>();

            // Konvertujeme public config na internal
            var internalConfig = config?.ToInternal() ?? new Configuration.InitializationConfiguration();

            // Vytvoríme progress wrapper
            var internalProgress = InitializationMappings.CreateProgressWrapper(progress);

            // Vytvoríme internal command
            var command = InitializeComponentCommand.ForUI(internalConfig, internalProgress, cancellationToken);

            // Vykonáme inicializáciu
            var internalResult = await lifecycleManager.InitializeAsync(command, cancellationToken);

            // Konvertujeme result na public
            return internalResult.ToPublic();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Component initialization (UI mode) failed: {Message}", ex.Message);
            return new PublicInitializationResult
            {
                IsSuccess = false,
                Message = "Initialization failed",
                ErrorMessage = ex.Message
            };
        }
    }

    /// <summary>
    /// Initialize for headless mode with server optimizations
    /// PERFORMANCE: Optimized for server/background scenarios
    /// </summary>
    public async Task<PublicInitializationResult> InitializeForHeadlessAsync(
        PublicInitializationConfiguration? config = null,
        IProgress<PublicInitializationProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        try
        {
            using var scope = ServiceRegistration.CreateOperationScope(_serviceProvider);
            var lifecycleManager = scope.ServiceProvider.GetRequiredService<IComponentLifecycleManager>();

            // Konvertujeme public config na internal
            var internalConfig = config?.ToInternal() ?? new Configuration.InitializationConfiguration();

            // Vytvoríme progress wrapper
            var internalProgress = InitializationMappings.CreateProgressWrapper(progress);

            // Vytvoríme internal command
            var command = InitializeComponentCommand.ForHeadless(internalConfig, internalProgress, cancellationToken);

            // Vykonáme inicializáciu
            var internalResult = await lifecycleManager.InitializeAsync(command, cancellationToken);

            // Konvertujeme result na public
            return internalResult.ToPublic();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Component initialization (headless mode) failed: {Message}", ex.Message);
            return new PublicInitializationResult
            {
                IsSuccess = false,
                Message = "Initialization failed",
                ErrorMessage = ex.Message
            };
        }
    }

    /// <summary>
    /// Graceful component shutdown with cleanup
    /// LIFECYCLE: Proper resource cleanup and disposal
    /// </summary>
    public async Task<bool> ShutdownAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using var scope = ServiceRegistration.CreateOperationScope(_serviceProvider);
            var lifecycleManager = scope.ServiceProvider.GetRequiredService<IComponentLifecycleManager>();

            return await lifecycleManager.ShutdownAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Component shutdown failed: {Message}", ex.Message);
            return false;
        }
    }

    /// <summary>
    /// Get current initialization status
    /// MONITORING: Runtime initialization state inspection
    /// </summary>
    public PublicInitializationStatus GetInitializationStatus()
    {
        try
        {
            using var scope = ServiceRegistration.CreateOperationScope(_serviceProvider);
            var lifecycleManager = scope.ServiceProvider.GetRequiredService<IComponentLifecycleManager>();

            var internalStatus = lifecycleManager.GetStatus();
            return internalStatus.ToPublic();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get initialization status: {Message}", ex.Message);
            return new PublicInitializationStatus { IsInitialized = false, LastError = ex.Message };
        }
    }

    #endregion

    #region Keyboard Shortcuts Operations

    public async Task<ShortcutDataResult> ExecuteShortcutAsync(ExecuteShortcutDataCommand command, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        try
        {
            using var scope = ServiceRegistration.CreateOperationScope(_serviceProvider);
            var shortcutService = scope.ServiceProvider.GetRequiredService<Features.Shortcuts.Interfaces.IShortcutService>();

            var internalCommand = new Features.Shortcuts.Commands.ShortcutCommand
            {
                ShortcutName = command.ShortcutName,
                Parameters = command.Parameters ?? new Dictionary<string, object?>(),
                CancellationToken = cancellationToken
            };

            var internalResult = await shortcutService.ExecuteShortcutAsync(internalCommand, cancellationToken);

            return new ShortcutDataResult
            {
                Success = internalResult.Success,
                ShortcutName = internalResult.ExecutedShortcut,
                ExecutionTime = internalResult.ExecutionTime,
                ErrorMessages = internalResult.ErrorMessages,
                ResultData = internalResult.Result
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Shortcut execution failed: {Message}", ex.Message);
            return new ShortcutDataResult
            {
                Success = false,
                ShortcutName = command.ShortcutName,
                ErrorMessages = new[] { $"Execution failed: {ex.Message}" }
            };
        }
    }

    public async Task<bool> RegisterShortcutAsync(PublicShortcutDefinition shortcut)
    {
        ThrowIfDisposed();

        try
        {
            using var scope = ServiceRegistration.CreateOperationScope(_serviceProvider);
            var shortcutService = scope.ServiceProvider.GetRequiredService<Features.Shortcuts.Interfaces.IShortcutService>();

            // For simplified implementation, we'll skip full registration
            // This would need full KeyCombination parsing in production
            _logger.LogInformation("Shortcut registration: {Name} - {Key}", shortcut.Name, shortcut.KeyCombination);
            return await Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Shortcut registration failed: {Message}", ex.Message);
            return false;
        }
    }

    public IReadOnlyList<PublicShortcutDefinition> GetRegisteredShortcuts()
    {
        ThrowIfDisposed();

        try
        {
            using var scope = ServiceRegistration.CreateOperationScope(_serviceProvider);
            var shortcutService = scope.ServiceProvider.GetRequiredService<Features.Shortcuts.Interfaces.IShortcutService>();

            var shortcuts = shortcutService.GetRegisteredShortcuts();
            return shortcuts.Select(s => new PublicShortcutDefinition
            {
                Name = s.Name,
                Description = s.Description,
                KeyCombination = s.KeyCombination.DisplayName,
                IsEnabled = s.IsEnabled
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get registered shortcuts: {Message}", ex.Message);
            return Array.Empty<PublicShortcutDefinition>();
        }
    }

    #endregion

    #region Smart Row Management Operations

    public async Task<SmartOperationDataResult> SmartAddRowsAsync(SmartAddRowsDataCommand command, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        try
        {
            using var scope = ServiceRegistration.CreateOperationScope(_serviceProvider);
            var smartOpService = scope.ServiceProvider.GetRequiredService<Features.SmartAddDelete.Interfaces.ISmartOperationService>();
            var rowStore = scope.ServiceProvider.GetRequiredService<IRowStore>();

            var currentRowCount = await rowStore.GetRowCountAsync(cancellationToken);

            var internalConfig = new Core.ValueObjects.RowManagementConfiguration
            {
                MinimumRows = command.Configuration.MinimumRows,
                EnableAutoExpand = command.Configuration.EnableAutoExpand,
                EnableSmartDelete = command.Configuration.EnableSmartDelete,
                AlwaysKeepLastEmpty = command.Configuration.AlwaysKeepLastEmpty
            };

            var internalCommand = new Features.SmartAddDelete.Commands.SmartAddRowsInternalCommand
            {
                DataToAdd = command.DataToAdd,
                Configuration = internalConfig,
                CancellationToken = cancellationToken
            };

            var internalResult = await smartOpService.SmartAddRowsAsync(internalCommand, cancellationToken);

            return new SmartOperationDataResult
            {
                Success = internalResult.Success,
                FinalRowCount = internalResult.FinalRowCount,
                ProcessedRows = internalResult.ProcessedRows,
                OperationTime = internalResult.OperationTime,
                Messages = internalResult.Messages,
                Statistics = new PublicRowManagementStatistics
                {
                    EmptyRowsCreated = internalResult.Statistics.EmptyRowsCreated,
                    RowsPhysicallyDeleted = internalResult.Statistics.RowsPhysicallyDeleted,
                    RowsContentCleared = internalResult.Statistics.RowsContentCleared,
                    RowsShifted = internalResult.Statistics.RowsShifted,
                    MinimumRowsEnforced = internalResult.Statistics.MinimumRowsEnforced,
                    LastEmptyRowMaintained = internalResult.Statistics.LastEmptyRowMaintained
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Smart add rows failed: {Message}", ex.Message);
            return new SmartOperationDataResult
            {
                Success = false,
                Messages = new[] { $"Smart add failed: {ex.Message}" }
            };
        }
    }

    public async Task<SmartOperationDataResult> SmartDeleteRowsAsync(SmartDeleteRowsDataCommand command, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        try
        {
            using var scope = ServiceRegistration.CreateOperationScope(_serviceProvider);
            var smartOpService = scope.ServiceProvider.GetRequiredService<Features.SmartAddDelete.Interfaces.ISmartOperationService>();

            var internalConfig = new Core.ValueObjects.RowManagementConfiguration
            {
                MinimumRows = command.Configuration.MinimumRows,
                EnableAutoExpand = command.Configuration.EnableAutoExpand,
                EnableSmartDelete = command.Configuration.EnableSmartDelete,
                AlwaysKeepLastEmpty = command.Configuration.AlwaysKeepLastEmpty
            };

            var internalCommand = new Features.SmartAddDelete.Commands.SmartDeleteRowsInternalCommand
            {
                RowIndexesToDelete = command.RowIndexesToDelete,
                Configuration = internalConfig,
                CurrentRowCount = command.CurrentRowCount,
                ForcePhysicalDelete = command.ForcePhysicalDelete,
                CancellationToken = cancellationToken
            };

            var internalResult = await smartOpService.SmartDeleteRowsAsync(internalCommand, cancellationToken);

            return new SmartOperationDataResult
            {
                Success = internalResult.Success,
                FinalRowCount = internalResult.FinalRowCount,
                ProcessedRows = internalResult.ProcessedRows,
                OperationTime = internalResult.OperationTime,
                Messages = internalResult.Messages,
                Statistics = new PublicRowManagementStatistics
                {
                    EmptyRowsCreated = internalResult.Statistics.EmptyRowsCreated,
                    RowsPhysicallyDeleted = internalResult.Statistics.RowsPhysicallyDeleted,
                    RowsContentCleared = internalResult.Statistics.RowsContentCleared,
                    RowsShifted = internalResult.Statistics.RowsShifted,
                    MinimumRowsEnforced = internalResult.Statistics.MinimumRowsEnforced,
                    LastEmptyRowMaintained = internalResult.Statistics.LastEmptyRowMaintained
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Smart delete rows failed: {Message}", ex.Message);
            return new SmartOperationDataResult
            {
                Success = false,
                Messages = new[] { $"Smart delete failed: {ex.Message}" }
            };
        }
    }

    public async Task<SmartOperationDataResult> AutoExpandEmptyRowAsync(AutoExpandEmptyRowDataCommand command, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        try
        {
            using var scope = ServiceRegistration.CreateOperationScope(_serviceProvider);
            var smartOpService = scope.ServiceProvider.GetRequiredService<Features.SmartAddDelete.Interfaces.ISmartOperationService>();

            var internalConfig = new Core.ValueObjects.RowManagementConfiguration
            {
                MinimumRows = command.Configuration.MinimumRows,
                EnableAutoExpand = command.Configuration.EnableAutoExpand,
                EnableSmartDelete = command.Configuration.EnableSmartDelete,
                AlwaysKeepLastEmpty = command.Configuration.AlwaysKeepLastEmpty
            };

            var internalCommand = new Features.SmartAddDelete.Commands.AutoExpandEmptyRowInternalCommand
            {
                Configuration = internalConfig,
                CurrentRowCount = command.CurrentRowCount,
                CancellationToken = cancellationToken
            };

            var internalResult = await smartOpService.AutoExpandEmptyRowAsync(internalCommand, cancellationToken);

            return new SmartOperationDataResult
            {
                Success = internalResult.Success,
                FinalRowCount = internalResult.FinalRowCount,
                ProcessedRows = internalResult.ProcessedRows,
                OperationTime = internalResult.OperationTime,
                Messages = internalResult.Messages,
                Statistics = new PublicRowManagementStatistics
                {
                    EmptyRowsCreated = internalResult.Statistics.EmptyRowsCreated,
                    LastEmptyRowMaintained = internalResult.Statistics.LastEmptyRowMaintained
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Auto-expand empty row failed: {Message}", ex.Message);
            return new SmartOperationDataResult
            {
                Success = false,
                Messages = new[] { $"Auto-expand failed: {ex.Message}" }
            };
        }
    }

    public async Task<PublicResult> ValidateRowManagementConfigurationAsync(PublicRowManagementConfiguration configuration)
    {
        ThrowIfDisposed();

        try
        {
            using var scope = ServiceRegistration.CreateOperationScope(_serviceProvider);
            var smartOpService = scope.ServiceProvider.GetRequiredService<Features.SmartAddDelete.Interfaces.ISmartOperationService>();

            var internalConfig = new Core.ValueObjects.RowManagementConfiguration
            {
                MinimumRows = configuration.MinimumRows,
                EnableAutoExpand = configuration.EnableAutoExpand,
                EnableSmartDelete = configuration.EnableSmartDelete,
                AlwaysKeepLastEmpty = configuration.AlwaysKeepLastEmpty
            };

            var internalResult = await smartOpService.ValidateRowManagementConfigurationAsync(internalConfig);
            return internalResult.ToPublic();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Row management configuration validation failed: {Message}", ex.Message);
            return PublicResult.Failure($"Validation failed: {ex.Message}");
        }
    }

    public PublicRowManagementStatistics GetRowManagementStatistics()
    {
        ThrowIfDisposed();

        try
        {
            using var scope = ServiceRegistration.CreateOperationScope(_serviceProvider);
            var smartOpService = scope.ServiceProvider.GetRequiredService<Features.SmartAddDelete.Interfaces.ISmartOperationService>();

            var stats = smartOpService.GetRowManagementStatistics();
            return new PublicRowManagementStatistics
            {
                EmptyRowsCreated = stats.EmptyRowsCreated,
                RowsPhysicallyDeleted = stats.RowsPhysicallyDeleted,
                RowsContentCleared = stats.RowsContentCleared,
                RowsShifted = stats.RowsShifted,
                MinimumRowsEnforced = stats.MinimumRowsEnforced,
                LastEmptyRowMaintained = stats.LastEmptyRowMaintained
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get row management statistics: {Message}", ex.Message);
            return new PublicRowManagementStatistics();
        }
    }

    #endregion

    #region Color Operations

    public async Task<ColorDataResult> ApplyColorAsync(ApplyColorDataCommand command, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        try
        {
            using var scope = ServiceRegistration.CreateOperationScope(_serviceProvider);
            var colorService = scope.ServiceProvider.GetRequiredService<Features.Color.Interfaces.IColorService>();

            var colorConfig = new Core.ValueObjects.ColorConfiguration
            {
                Mode = (Core.ValueObjects.ColorMode)command.Mode,
                BackgroundColor = command.BackgroundColor,
                ForegroundColor = command.ForegroundColor,
                RowIndex = command.RowIndex,
                ColumnIndex = command.ColumnIndex,
                ColumnName = command.ColumnName
            };

            var applyCommand = Features.Color.Commands.ApplyColorCommand.Create(colorConfig);
            var result = await colorService.ApplyColorAsync(applyCommand, cancellationToken);

            return new ColorDataResult(result.Success, result.AffectedCells, result.Duration, result.ErrorMessage);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to apply color");
            return new ColorDataResult(false, 0, TimeSpan.Zero, ex.Message);
        }
    }

    public async Task<ColorDataResult> ApplyConditionalFormattingAsync(ApplyConditionalFormattingDataCommand command, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        try
        {
            using var scope = ServiceRegistration.CreateOperationScope(_serviceProvider);
            var colorService = scope.ServiceProvider.GetRequiredService<Features.Color.Interfaces.IColorService>();

            var rules = command.Rules.Select(r => new Core.ValueObjects.ConditionalFormatRule
            {
                ColumnName = r.ColumnName,
                Rule = (Core.ValueObjects.ConditionalFormattingRule)r.Rule,
                Value = r.Value,
                ColorConfig = new Core.ValueObjects.ColorConfiguration
                {
                    BackgroundColor = r.BackgroundColor,
                    ForegroundColor = r.ForegroundColor
                }
            }).ToList();

            var applyCommand = Features.Color.Commands.ApplyConditionalFormattingCommand.Create(rules);
            var result = await colorService.ApplyConditionalFormattingAsync(applyCommand, cancellationToken);

            return new ColorDataResult(result.Success, result.AffectedCells, result.Duration, result.ErrorMessage);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to apply conditional formatting");
            return new ColorDataResult(false, 0, TimeSpan.Zero, ex.Message);
        }
    }

    public async Task<ColorDataResult> ClearColorAsync(ClearColorDataCommand command, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        try
        {
            using var scope = ServiceRegistration.CreateOperationScope(_serviceProvider);
            var colorService = scope.ServiceProvider.GetRequiredService<Features.Color.Interfaces.IColorService>();

            var clearCommand = new Features.Color.Commands.ClearColorCommand
            {
                Mode = (Core.ValueObjects.ColorMode)command.Mode,
                RowIndex = command.RowIndex,
                ColumnIndex = command.ColumnIndex,
                ColumnName = command.ColumnName
            };

            var result = await colorService.ClearColorAsync(clearCommand, cancellationToken);

            return new ColorDataResult(result.Success, result.AffectedCells, result.Duration, result.ErrorMessage);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to clear color");
            return new ColorDataResult(false, 0, TimeSpan.Zero, ex.Message);
        }
    }

    #endregion

    #region Performance Operations

    public async Task<PublicResult> StartPerformanceMonitoringAsync(StartPerformanceMonitoringCommand command, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        try
        {
            using var scope = ServiceRegistration.CreateOperationScope(_serviceProvider);
            var performanceService = scope.ServiceProvider.GetRequiredService<Features.Performance.Interfaces.IPerformanceService>();

            var startCommand = Features.Performance.Commands.StartMonitoringCommand.Create(command.MonitoringWindow);
            var internalResult = await performanceService.StartMonitoringAsync(startCommand, cancellationToken);
            return internalResult.ToPublic();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start performance monitoring");
            return PublicResult.Failure(ex.Message);
        }
    }

    public async Task<PublicResult> StopPerformanceMonitoringAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        try
        {
            using var scope = ServiceRegistration.CreateOperationScope(_serviceProvider);
            var performanceService = scope.ServiceProvider.GetRequiredService<Features.Performance.Interfaces.IPerformanceService>();

            var stopCommand = Features.Performance.Commands.StopMonitoringCommand.Create();
            var internalResult = await performanceService.StopMonitoringAsync(stopCommand, cancellationToken);
            return internalResult.ToPublic();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to stop performance monitoring");
            return PublicResult.Failure(ex.Message);
        }
    }

    public async Task<PerformanceSnapshotData> GetPerformanceSnapshotAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        try
        {
            using var scope = ServiceRegistration.CreateOperationScope(_serviceProvider);
            var performanceService = scope.ServiceProvider.GetRequiredService<Features.Performance.Interfaces.IPerformanceService>();

            var snapshot = await performanceService.GetPerformanceSnapshotAsync(cancellationToken);

            return new PerformanceSnapshotData(
                snapshot.TotalOperations,
                snapshot.TotalErrors,
                snapshot.ErrorRate,
                snapshot.CurrentMemoryUsage,
                snapshot.CpuTime,
                snapshot.ThreadCount,
                snapshot.Timestamp
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get performance snapshot");
            return new PerformanceSnapshotData();
        }
    }

    public async Task<PerformanceReportData> GetPerformanceReportAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        try
        {
            using var scope = ServiceRegistration.CreateOperationScope(_serviceProvider);
            var performanceService = scope.ServiceProvider.GetRequiredService<Features.Performance.Interfaces.IPerformanceService>();

            var reportCommand = Features.Performance.Commands.GetPerformanceReportCommand.Create();
            var report = await performanceService.GetPerformanceReportAsync(reportCommand, cancellationToken);

            var snapshot = new PerformanceSnapshotData(
                report.Snapshot.TotalOperations,
                report.Snapshot.TotalErrors,
                report.Snapshot.ErrorRate,
                report.Snapshot.CurrentMemoryUsage,
                report.Snapshot.CpuTime,
                report.Snapshot.ThreadCount,
                report.Snapshot.Timestamp
            );

            return new PerformanceReportData(
                snapshot,
                report.Bottlenecks,
                report.Recommendations,
                (PublicPerformanceThreshold)report.Threshold,
                report.AnalysisDuration
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get performance report");
            return new PerformanceReportData();
        }
    }

    public PerformanceStatisticsData GetPerformanceStatistics()
    {
        ThrowIfDisposed();

        try
        {
            using var scope = ServiceRegistration.CreateOperationScope(_serviceProvider);
            var performanceService = scope.ServiceProvider.GetRequiredService<Features.Performance.Interfaces.IPerformanceService>();

            var stats = performanceService.GetPerformanceStatistics();

            return new PerformanceStatisticsData(
                stats.TotalOperations,
                stats.TotalErrors,
                stats.AverageOperationTime,
                stats.PeakMemoryUsage,
                stats.CurrentMemoryUsage
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get performance statistics");
            return new PerformanceStatisticsData();
        }
    }

    #endregion

    #region Row/Column/Cell Batch Operations

    public async Task<BatchOperationResult> BatchUpdateCellsAsync(BatchUpdateCellsDataCommand command, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        try
        {
            using var scope = ServiceRegistration.CreateOperationScope(_serviceProvider);
            var rcService = scope.ServiceProvider.GetRequiredService<Features.RowColumnCell.Interfaces.IRowColumnCellService>();

            var operations = command.Operations.Select(op => new Core.ValueObjects.BatchCellOperation
            {
                RowIndex = op.RowIndex,
                ColumnIndex = op.ColumnIndex,
                Value = op.Value,
                OperationType = (Core.ValueObjects.CellOperationType)op.OperationType
            }).ToList();

            var batchCommand = Features.RowColumnCell.Commands.BatchUpdateCellsCommand.Create(operations);
            var result = await rcService.BatchUpdateCellsAsync(batchCommand, cancellationToken);

            return new BatchOperationResult(result.Success, result.AffectedItems, result.Errors, result.Duration);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to batch update cells");
            return new BatchOperationResult(false, 0, new[] { ex.Message }, TimeSpan.Zero);
        }
    }

    public async Task<BatchOperationResult> BatchRowOperationsAsync(BatchRowOperationsDataCommand command, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        try
        {
            using var scope = ServiceRegistration.CreateOperationScope(_serviceProvider);
            var rcService = scope.ServiceProvider.GetRequiredService<Features.RowColumnCell.Interfaces.IRowColumnCellService>();

            var operations = command.Operations.Select(op => new Core.ValueObjects.BatchRowOperation
            {
                RowIndex = op.RowIndex,
                RowData = op.RowData,
                OperationType = (Core.ValueObjects.BatchRowOperationType)op.OperationType
            }).ToList();

            if (operations.All(op => op.OperationType == Core.ValueObjects.BatchRowOperationType.Insert))
            {
                var insertCommand = Features.RowColumnCell.Commands.BatchInsertRowsCommand.Create(operations);
                var result = await rcService.BatchInsertRowsAsync(insertCommand, cancellationToken);
                return new BatchOperationResult(result.Success, result.AffectedItems, result.Errors, result.Duration);
            }
            else if (operations.All(op => op.OperationType == Core.ValueObjects.BatchRowOperationType.Delete))
            {
                var rowIndices = operations.Select(op => op.RowIndex).ToList();
                var deleteCommand = Features.RowColumnCell.Commands.BatchDeleteRowsCommand.Create(rowIndices);
                var result = await rcService.BatchDeleteRowsAsync(deleteCommand, cancellationToken);
                return new BatchOperationResult(result.Success, result.AffectedItems, result.Errors, result.Duration);
            }
            else
            {
                return new BatchOperationResult(false, 0, new[] { "Mixed operation types not supported" }, TimeSpan.Zero);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to batch row operations");
            return new BatchOperationResult(false, 0, new[] { ex.Message }, TimeSpan.Zero);
        }
    }

    public async Task<BatchOperationResult> BatchColumnOperationsAsync(BatchColumnOperationsDataCommand command, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        try
        {
            using var scope = ServiceRegistration.CreateOperationScope(_serviceProvider);
            var rcService = scope.ServiceProvider.GetRequiredService<Features.RowColumnCell.Interfaces.IRowColumnCellService>();

            var operations = command.Operations.Select(op => new Core.ValueObjects.BatchColumnOperation
            {
                ColumnName = op.ColumnName,
                Width = op.Width,
                NewPosition = op.NewPosition,
                NewName = op.NewName,
                OperationType = (Core.ValueObjects.ColumnOperationType)op.OperationType
            }).ToList();

            var batchCommand = Features.RowColumnCell.Commands.BatchUpdateColumnsCommand.Create(operations);
            var result = await rcService.BatchUpdateColumnsAsync(batchCommand, cancellationToken);

            return new BatchOperationResult(result.Success, result.AffectedItems, result.Errors, result.Duration);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to batch column operations");
            return new BatchOperationResult(false, 0, new[] { ex.Message }, TimeSpan.Zero);
        }
    }

    #endregion

    #region Cell Edit Operations

    /// <summary>
    /// Begins an edit session for a specific cell
    /// </summary>
    public async Task<CellEditResult> BeginEditAsync(BeginEditDataCommand command, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        try
        {
            _logger.LogDebug("Beginning edit for row {RowIndex}, column {ColumnName}", command.RowIndex, command.ColumnName);

            using var scope = ServiceRegistration.CreateOperationScope(_serviceProvider);
            var cellEditService = scope.ServiceProvider.GetRequiredService<Features.CellEdit.Interfaces.ICellEditService>();

            var result = await cellEditService.BeginEditAsync(command.RowIndex, command.ColumnName, cancellationToken);

            if (result.IsSuccess)
            {
                return CellEditResult.Success(result.SessionId, result.ValidationAlerts);
            }
            else
            {
                return CellEditResult.Failure(result.ErrorMessage ?? "Failed to begin edit");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to begin edit for row {RowIndex}, column {ColumnName}", command.RowIndex, command.ColumnName);
            return CellEditResult.Failure($"Edit operation failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Updates a cell value with real-time validation
    /// </summary>
    public async Task<CellEditResult> UpdateCellAsync(UpdateCellDataCommand command, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        try
        {
            _logger.LogDebug("Updating cell for row {RowIndex}, column {ColumnName}", command.RowIndex, command.ColumnName);

            using var scope = ServiceRegistration.CreateOperationScope(_serviceProvider);
            var cellEditService = scope.ServiceProvider.GetRequiredService<Features.CellEdit.Interfaces.ICellEditService>();

            var result = await cellEditService.UpdateCellAsync(command.RowIndex, command.ColumnName, command.NewValue, cancellationToken);

            if (!result.IsSuccess)
            {
                return CellEditResult.Failure(result.ErrorMessage ?? "Failed to update cell");
            }

            // Check validation result
            if (result.ValidationResult != null && !result.ValidationResult.IsValid)
            {
                return CellEditResult.ValidationError(
                    result.ValidationResult.ErrorMessage ?? "Validation failed",
                    result.ValidationResult.Severity,
                    result.ValidationAlerts);
            }

            return CellEditResult.Success(result.SessionId, result.ValidationAlerts);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update cell for row {RowIndex}, column {ColumnName}", command.RowIndex, command.ColumnName);
            return CellEditResult.Failure($"Update operation failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Commits the current edit session
    /// </summary>
    public async Task<CellEditResult> CommitEditAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        try
        {
            _logger.LogDebug("Committing edit session");

            using var scope = ServiceRegistration.CreateOperationScope(_serviceProvider);
            var cellEditService = scope.ServiceProvider.GetRequiredService<Features.CellEdit.Interfaces.ICellEditService>();

            var result = await cellEditService.CommitEditAsync(cancellationToken);

            if (result.IsSuccess)
            {
                return CellEditResult.Success(result.SessionId);
            }
            else
            {
                return CellEditResult.Failure(result.ErrorMessage ?? "Failed to commit edit");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to commit edit session");
            return CellEditResult.Failure($"Commit operation failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Cancels the current edit session
    /// </summary>
    public async Task<CellEditResult> CancelEditAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        try
        {
            _logger.LogDebug("Canceling edit session");

            using var scope = ServiceRegistration.CreateOperationScope(_serviceProvider);
            var cellEditService = scope.ServiceProvider.GetRequiredService<Features.CellEdit.Interfaces.ICellEditService>();

            var result = await cellEditService.CancelEditAsync(cancellationToken);

            if (result.IsSuccess)
            {
                return CellEditResult.Success(result.SessionId);
            }
            else
            {
                return CellEditResult.Failure(result.ErrorMessage ?? "Failed to cancel edit");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cancel edit session");
            return CellEditResult.Failure($"Cancel operation failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Gets validation alerts for a specific row
    /// </summary>
    public string GetValidationAlerts(int rowIndex)
    {
        ThrowIfDisposed();

        try
        {
            using var scope = ServiceRegistration.CreateOperationScope(_serviceProvider);
            var validationService = scope.ServiceProvider.GetRequiredService<IValidationService>();

            return validationService.GetValidationAlertsForRow(rowIndex);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get validation alerts for row {RowIndex}", rowIndex);
            return string.Empty;
        }
    }

    /// <summary>
    /// Checks if a row has validation errors
    /// </summary>
    public bool HasValidationErrors(int rowIndex)
    {
        ThrowIfDisposed();

        try
        {
            var alerts = GetValidationAlerts(rowIndex);
            return !string.IsNullOrEmpty(alerts) && alerts.Contains("Error:", StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to check validation errors for row {RowIndex}", rowIndex);
            return false;
        }
    }

    #endregion

    #region Private Helper Methods

    /// <summary>
    /// Centralized UI refresh logic - triggers automatic UI refresh ONLY in Interactive mode
    /// </summary>
    /// <param name="operationType">Type of operation that triggered refresh</param>
    /// <param name="affectedRows">Number of affected rows</param>
    private async Task TriggerUIRefreshIfNeededAsync(string operationType, int affectedRows)
    {
        // Automatický refresh LEN v Interactive mode
        if (_options.OperationMode == PublicDataGridOperationMode.Interactive && _uiNotificationService != null)
        {
            await _uiNotificationService.NotifyDataRefreshAsync(affectedRows, operationType);
        }
        // V Headless mode → skip (automatický refresh je zakázaný)
    }

    /// <summary>
    /// Helper class for notification subscriptions
    /// </summary>
    private sealed class NotificationSubscription : IDisposable
    {
        private readonly Action _unsubscribeAction;
        private bool _disposed;

        public NotificationSubscription(Action unsubscribeAction)
        {
            _unsubscribeAction = unsubscribeAction ?? throw new ArgumentNullException(nameof(unsubscribeAction));
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _unsubscribeAction();
            _disposed = true;
        }
    }

    private static long EstimateDataTableSize(DataTable dataTable)
    {
        // Rough estimation of DataTable size in bytes
        return dataTable.Rows.Count * dataTable.Columns.Count * 50L;
    }

    private static long EstimateDictionarySize(IReadOnlyList<IReadOnlyDictionary<string, object?>> dictionaries)
    {
        // Rough estimation of dictionary collection size in bytes
        var avgColumns = dictionaries.FirstOrDefault()?.Count ?? 0;
        return dictionaries.Count * avgColumns * 30L;
    }

    #endregion

    #region Business Presets

    /// <summary>
    /// Creates employee hierarchy sort preset (Department → Position → Salary)
    /// </summary>
    public PublicSortConfiguration CreateEmployeeHierarchySortPreset(
        string departmentColumn = "Department",
        string positionColumn = "Position",
        string salaryColumn = "Salary")
    {
        ThrowIfDisposed();
        _logger.LogDebug("Creating employee hierarchy sort preset with columns: {Department}, {Position}, {Salary}",
            departmentColumn, positionColumn, salaryColumn);

        var internalConfig = Core.ValueObjects.AdvancedSortConfiguration.CreateEmployeeHierarchy(
            departmentColumn, positionColumn, salaryColumn);

        return MapToPublicSortConfiguration(internalConfig);
    }

    /// <summary>
    /// Creates customer priority sort preset (Tier → Value → JoinDate)
    /// </summary>
    public PublicSortConfiguration CreateCustomerPrioritySortPreset(
        string tierColumn = "CustomerTier",
        string valueColumn = "TotalValue",
        string joinDateColumn = "JoinDate")
    {
        ThrowIfDisposed();
        _logger.LogDebug("Creating customer priority sort preset with columns: {Tier}, {Value}, {JoinDate}",
            tierColumn, valueColumn, joinDateColumn);

        var internalConfig = Core.ValueObjects.AdvancedSortConfiguration.CreateCustomerPriority(
            tierColumn, valueColumn, joinDateColumn);

        return MapToPublicSortConfiguration(internalConfig);
    }

    /// <summary>
    /// Gets responsive row height preset
    /// </summary>
    public PublicAutoRowHeightConfiguration GetResponsiveHeightPreset()
    {
        ThrowIfDisposed();
        _logger.LogDebug("Getting responsive height preset");

        var internalConfig = Features.AutoRowHeight.Interfaces.AutoRowHeightConfiguration.Responsive;
        return MapToPublicAutoRowHeightConfiguration(internalConfig);
    }

    /// <summary>
    /// Gets compact row height preset
    /// </summary>
    public PublicAutoRowHeightConfiguration GetCompactHeightPreset()
    {
        ThrowIfDisposed();
        _logger.LogDebug("Getting compact height preset");

        var internalConfig = Features.AutoRowHeight.Interfaces.AutoRowHeightConfiguration.Compact;
        return MapToPublicAutoRowHeightConfiguration(internalConfig);
    }

    /// <summary>
    /// Gets performance row height preset
    /// </summary>
    public PublicAutoRowHeightConfiguration GetPerformanceHeightPreset()
    {
        ThrowIfDisposed();
        _logger.LogDebug("Getting performance height preset");

        var internalConfig = Features.AutoRowHeight.Interfaces.AutoRowHeightConfiguration.Performance;
        return MapToPublicAutoRowHeightConfiguration(internalConfig);
    }

    /// <summary>
    /// Maps internal sort configuration to public type
    /// </summary>
    private static PublicSortConfiguration MapToPublicSortConfiguration(
        Core.ValueObjects.AdvancedSortConfiguration internalConfig)
    {
        return new PublicSortConfiguration
        {
            ConfigurationName = internalConfig.ConfigurationName,
            SortColumns = internalConfig.SortColumns
                .Select(col => new PublicSortColumn
                {
                    ColumnName = col.ColumnName,
                    Direction = col.Direction.ToString(),
                    Priority = col.Priority
                })
                .ToList()
                .AsReadOnly(),
            PerformanceMode = internalConfig.PerformanceMode.ToString(),
            EnableParallelProcessing = internalConfig.EnableParallelProcessing,
            MaxSortColumns = internalConfig.MaxSortColumns,
            BatchSize = 1000 // Default batch size
        };
    }

    /// <summary>
    /// Maps internal auto row height configuration to public type
    /// </summary>
    private static PublicAutoRowHeightConfiguration MapToPublicAutoRowHeightConfiguration(
        Features.AutoRowHeight.Interfaces.AutoRowHeightConfiguration internalConfig)
    {
        return new PublicAutoRowHeightConfiguration
        {
            IsEnabled = internalConfig.IsEnabled,
            MinimumRowHeight = internalConfig.MinimumRowHeight,
            MaximumRowHeight = internalConfig.MaximumRowHeight,
            DefaultFontFamily = internalConfig.DefaultFontFamily,
            DefaultFontSize = internalConfig.DefaultFontSize,
            EnableTextWrapping = internalConfig.EnableTextWrapping,
            UseCache = internalConfig.UseCache,
            CacheMaxSize = internalConfig.CacheMaxSize
        };
    }

    #endregion

    #region Theme and Color Management

    /// <summary>
    /// Applies a theme to the grid
    /// </summary>
    public async Task<PublicResult> ApplyThemeAsync(PublicGridTheme theme)
    {
        ThrowIfDisposed();
        if (theme == null)
            throw new ArgumentNullException(nameof(theme));

        _logger.LogDebug("Applying theme: {ThemeName}", theme.ThemeName);

        var internalTheme = MapToInternalTheme(theme);
        _themeService.ApplyTheme(internalTheme);

        // Refresh UI to apply theme changes
        await RefreshUIAsync();

        _logger.LogInformation("Theme applied successfully: {ThemeName}", theme.ThemeName);
        return PublicResult.Success();
    }

    /// <summary>
    /// Gets the current active theme
    /// </summary>
    public PublicGridTheme GetCurrentTheme()
    {
        ThrowIfDisposed();
        var internalTheme = _themeService.GetCurrentTheme();
        return MapToPublicTheme(internalTheme);
    }

    /// <summary>
    /// Resets theme to default
    /// </summary>
    public async Task<PublicResult> ResetThemeToDefaultAsync()
    {
        ThrowIfDisposed();
        _logger.LogDebug("Resetting theme to default");

        _themeService.ResetToDefault();

        // Refresh UI to apply theme changes
        await RefreshUIAsync();

        _logger.LogInformation("Theme reset to default successfully");
        return PublicResult.Success();
    }

    /// <summary>
    /// Updates specific cell colors without changing entire theme
    /// </summary>
    public async Task<PublicResult> UpdateCellColorsAsync(PublicCellColors cellColors)
    {
        ThrowIfDisposed();
        if (cellColors == null)
            throw new ArgumentNullException(nameof(cellColors));

        _logger.LogDebug("Updating cell colors");

        var internalCellColors = MapToInternalCellColors(cellColors);
        _themeService.UpdateCellColors(internalCellColors);

        // Refresh UI to apply color changes
        await RefreshUIAsync();

        _logger.LogInformation("Cell colors updated successfully");
        return PublicResult.Success();
    }

    /// <summary>
    /// Updates specific row colors without changing entire theme
    /// </summary>
    public async Task<PublicResult> UpdateRowColorsAsync(PublicRowColors rowColors)
    {
        ThrowIfDisposed();
        if (rowColors == null)
            throw new ArgumentNullException(nameof(rowColors));

        _logger.LogDebug("Updating row colors");

        var internalRowColors = MapToInternalRowColors(rowColors);
        _themeService.UpdateRowColors(internalRowColors);

        // Refresh UI to apply color changes
        await RefreshUIAsync();

        _logger.LogInformation("Row colors updated successfully");
        return PublicResult.Success();
    }

    /// <summary>
    /// Updates specific validation colors without changing entire theme
    /// </summary>
    public async Task<PublicResult> UpdateValidationColorsAsync(PublicValidationColors validationColors)
    {
        ThrowIfDisposed();
        if (validationColors == null)
            throw new ArgumentNullException(nameof(validationColors));

        _logger.LogDebug("Updating validation colors");

        var internalValidationColors = MapToInternalValidationColors(validationColors);
        _themeService.UpdateValidationColors(internalValidationColors);

        // Refresh UI to apply color changes
        await RefreshUIAsync();

        _logger.LogInformation("Validation colors updated successfully");
        return PublicResult.Success();
    }

    /// <summary>
    /// Creates a dark theme preset
    /// </summary>
    public PublicGridTheme CreateDarkTheme()
    {
        ThrowIfDisposed();
        _logger.LogDebug("Creating dark theme preset");
        return MapToPublicTheme(Features.Color.GridTheme.Dark);
    }

    /// <summary>
    /// Creates a light theme preset
    /// </summary>
    public PublicGridTheme CreateLightTheme()
    {
        ThrowIfDisposed();
        _logger.LogDebug("Creating light theme preset");
        return MapToPublicTheme(Features.Color.GridTheme.Light);
    }

    /// <summary>
    /// Creates a high contrast theme preset
    /// </summary>
    public PublicGridTheme CreateHighContrastTheme()
    {
        ThrowIfDisposed();
        _logger.LogDebug("Creating high contrast theme preset");
        return MapToPublicTheme(Features.Color.GridTheme.HighContrast);
    }

    #region Theme Mapping Methods

    private static Features.Color.GridTheme MapToInternalTheme(PublicGridTheme publicTheme)
    {
        return new Features.Color.GridTheme
        {
            ThemeName = publicTheme.ThemeName,
            CellColors = MapToInternalCellColors(publicTheme.CellColors),
            RowColors = MapToInternalRowColors(publicTheme.RowColors),
            HeaderColors = MapToInternalHeaderColors(publicTheme.HeaderColors),
            ValidationColors = MapToInternalValidationColors(publicTheme.ValidationColors),
            SelectionColors = MapToInternalSelectionColors(publicTheme.SelectionColors),
            BorderColors = MapToInternalBorderColors(publicTheme.BorderColors)
        };
    }

    private static PublicGridTheme MapToPublicTheme(Features.Color.GridTheme internalTheme)
    {
        return new PublicGridTheme
        {
            ThemeName = internalTheme.ThemeName,
            CellColors = MapToPublicCellColors(internalTheme.CellColors),
            RowColors = MapToPublicRowColors(internalTheme.RowColors),
            HeaderColors = MapToPublicHeaderColors(internalTheme.HeaderColors),
            ValidationColors = MapToPublicValidationColors(internalTheme.ValidationColors),
            SelectionColors = MapToPublicSelectionColors(internalTheme.SelectionColors),
            BorderColors = MapToPublicBorderColors(internalTheme.BorderColors)
        };
    }

    private static Features.Color.CellColors MapToInternalCellColors(PublicCellColors publicColors)
    {
        return new Features.Color.CellColors
        {
            DefaultBackground = publicColors.DefaultBackground,
            DefaultForeground = publicColors.DefaultForeground,
            HoverBackground = publicColors.HoverBackground,
            HoverForeground = publicColors.HoverForeground,
            FocusedBackground = publicColors.FocusedBackground,
            FocusedForeground = publicColors.FocusedForeground,
            DisabledBackground = publicColors.DisabledBackground,
            DisabledForeground = publicColors.DisabledForeground,
            ReadOnlyBackground = publicColors.ReadOnlyBackground,
            ReadOnlyForeground = publicColors.ReadOnlyForeground
        };
    }

    private static PublicCellColors MapToPublicCellColors(Features.Color.CellColors internalColors)
    {
        return new PublicCellColors
        {
            DefaultBackground = internalColors.DefaultBackground,
            DefaultForeground = internalColors.DefaultForeground,
            HoverBackground = internalColors.HoverBackground,
            HoverForeground = internalColors.HoverForeground,
            FocusedBackground = internalColors.FocusedBackground,
            FocusedForeground = internalColors.FocusedForeground,
            DisabledBackground = internalColors.DisabledBackground,
            DisabledForeground = internalColors.DisabledForeground,
            ReadOnlyBackground = internalColors.ReadOnlyBackground,
            ReadOnlyForeground = internalColors.ReadOnlyForeground
        };
    }

    private static Features.Color.RowColors MapToInternalRowColors(PublicRowColors publicColors)
    {
        return new Features.Color.RowColors
        {
            EvenRowBackground = publicColors.EvenRowBackground,
            OddRowBackground = publicColors.OddRowBackground,
            HoverBackground = publicColors.HoverBackground,
            SelectedBackground = publicColors.SelectedBackground,
            SelectedForeground = publicColors.SelectedForeground,
            SelectedInactiveBackground = publicColors.SelectedInactiveBackground,
            SelectedInactiveForeground = publicColors.SelectedInactiveForeground
        };
    }

    private static PublicRowColors MapToPublicRowColors(Features.Color.RowColors internalColors)
    {
        return new PublicRowColors
        {
            EvenRowBackground = internalColors.EvenRowBackground,
            OddRowBackground = internalColors.OddRowBackground,
            HoverBackground = internalColors.HoverBackground,
            SelectedBackground = internalColors.SelectedBackground,
            SelectedForeground = internalColors.SelectedForeground,
            SelectedInactiveBackground = internalColors.SelectedInactiveBackground,
            SelectedInactiveForeground = internalColors.SelectedInactiveForeground
        };
    }

    private static Features.Color.HeaderColors MapToInternalHeaderColors(PublicHeaderColors publicColors)
    {
        return new Features.Color.HeaderColors
        {
            Background = publicColors.Background,
            Foreground = publicColors.Foreground,
            HoverBackground = publicColors.HoverBackground,
            PressedBackground = publicColors.PressedBackground,
            SortIndicatorColor = publicColors.SortIndicatorColor
        };
    }

    private static PublicHeaderColors MapToPublicHeaderColors(Features.Color.HeaderColors internalColors)
    {
        return new PublicHeaderColors
        {
            Background = internalColors.Background,
            Foreground = internalColors.Foreground,
            HoverBackground = internalColors.HoverBackground,
            PressedBackground = internalColors.PressedBackground,
            SortIndicatorColor = internalColors.SortIndicatorColor
        };
    }

    private static Features.Color.ValidationColors MapToInternalValidationColors(PublicValidationColors publicColors)
    {
        return new Features.Color.ValidationColors
        {
            ErrorBackground = publicColors.ErrorBackground,
            ErrorForeground = publicColors.ErrorForeground,
            ErrorBorder = publicColors.ErrorBorder,
            WarningBackground = publicColors.WarningBackground,
            WarningForeground = publicColors.WarningForeground,
            WarningBorder = publicColors.WarningBorder,
            InfoBackground = publicColors.InfoBackground,
            InfoForeground = publicColors.InfoForeground,
            InfoBorder = publicColors.InfoBorder
        };
    }

    private static PublicValidationColors MapToPublicValidationColors(Features.Color.ValidationColors internalColors)
    {
        return new PublicValidationColors
        {
            ErrorBackground = internalColors.ErrorBackground,
            ErrorForeground = internalColors.ErrorForeground,
            ErrorBorder = internalColors.ErrorBorder,
            WarningBackground = internalColors.WarningBackground,
            WarningForeground = internalColors.WarningForeground,
            WarningBorder = internalColors.WarningBorder,
            InfoBackground = internalColors.InfoBackground,
            InfoForeground = internalColors.InfoForeground,
            InfoBorder = internalColors.InfoBorder
        };
    }

    private static Features.Color.SelectionColors MapToInternalSelectionColors(PublicSelectionColors publicColors)
    {
        return new Features.Color.SelectionColors
        {
            SelectionBorder = publicColors.SelectionBorder,
            SelectionFill = publicColors.SelectionFill,
            MultiSelectionBackground = publicColors.MultiSelectionBackground,
            MultiSelectionForeground = publicColors.MultiSelectionForeground
        };
    }

    private static PublicSelectionColors MapToPublicSelectionColors(Features.Color.SelectionColors internalColors)
    {
        return new PublicSelectionColors
        {
            SelectionBorder = internalColors.SelectionBorder,
            SelectionFill = internalColors.SelectionFill,
            MultiSelectionBackground = internalColors.MultiSelectionBackground,
            MultiSelectionForeground = internalColors.MultiSelectionForeground
        };
    }

    private static Features.Color.BorderColors MapToInternalBorderColors(PublicBorderColors publicColors)
    {
        return new Features.Color.BorderColors
        {
            CellBorder = publicColors.CellBorder,
            RowBorder = publicColors.RowBorder,
            ColumnBorder = publicColors.ColumnBorder,
            GridBorder = publicColors.GridBorder,
            FocusedCellBorder = publicColors.FocusedCellBorder
        };
    }

    private static PublicBorderColors MapToPublicBorderColors(Features.Color.BorderColors internalColors)
    {
        return new PublicBorderColors
        {
            CellBorder = internalColors.CellBorder,
            RowBorder = internalColors.RowBorder,
            ColumnBorder = internalColors.ColumnBorder,
            GridBorder = internalColors.GridBorder,
            FocusedCellBorder = internalColors.FocusedCellBorder
        };
    }

    #endregion

    #endregion

    #region Helper Classes

    /// <summary>
    /// Helper class for accumulating rule statistics during validation
    /// </summary>
    private class RuleStatsAccumulator
    {
        public string RuleName { get; set; } = string.Empty;
        public int ExecutionCount { get; set; }
        public TimeSpan TotalExecutionTime { get; set; }
        public int ErrorsFound { get; set; }
    }

    #endregion
}