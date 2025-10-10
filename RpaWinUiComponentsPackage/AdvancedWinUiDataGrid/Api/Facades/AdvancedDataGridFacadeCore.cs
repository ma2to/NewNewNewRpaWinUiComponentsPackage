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
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Common;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Api.Mappings;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Common.Models;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Configuration;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Infrastructure.Persistence.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Infrastructure.Logging.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Infrastructure.Logging.NullPattern;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid;

/// <summary>
/// Public implementation of IAdvancedDataGridFacade
/// Orchestrates all component operations via internal services
/// CORE: Constructor, disposal, and common helper methods
/// </summary>
public sealed partial class AdvancedDataGridFacade : IAdvancedDataGridFacade
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
    /// AdvancedDataGridFacade constructor
    /// Initializes dependencies and obtains operation logger via DI
    /// </summary>
    public AdvancedDataGridFacade(
        IServiceProvider serviceProvider,
        AdvancedDataGridOptions options)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = serviceProvider.GetRequiredService<ILogger<AdvancedDataGridFacade>>();
        _dispatcher = serviceProvider.GetService<DispatcherQueue>();

        // Obtain operation logger via DI, or use null pattern
        var operationLogger = serviceProvider.GetService<IOperationLogger<AdvancedDataGridFacade>>();
        _operationLogger = operationLogger ?? NullOperationLogger<AdvancedDataGridFacade>.Instance;

        // Obtain UI notification service (available if DispatcherQueue provided)
        _uiNotificationService = serviceProvider.GetService<UIAdapters.WinUI.UiNotificationService>();

        // Obtain GridViewModelAdapter (available if DispatcherQueue provided)
        _gridViewModelAdapter = serviceProvider.GetService<UIAdapters.WinUI.GridViewModelAdapter>();

        // Obtain ThemeService (always available)
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

    #region Disposal

    /// <summary>
    /// Disposes the facade and all its resources
    /// </summary>
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
}
