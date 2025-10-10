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
/// Partial class containing Search Operations
/// </summary>
public sealed partial class AdvancedDataGridFacade : IAdvancedDataGridFacade
{
    #region Search Operations

    /// <summary>
    /// Executes basic search using command pattern
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

            // Map public command to internal
            var internalCommand = command.ToInternal();

            // Execute search
            var internalResult = await searchService.SearchAsync(internalCommand, cancellationToken);

            // Map result to public
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
            return new SearchDataResult(false, Array.Empty<PublicSearchMatch>(), 0, 0, stopwatch.Elapsed, PublicSearchMode.Contains, false, new[] { $"Search failed: {ex.Message}" });
        }
    }

    /// <summary>
    /// Executes advanced search with regex, fuzzy matching and smart ranking
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

            // Map public command to internal
            var internalCommand = command.ToInternal();

            // Execute advanced search
            var internalResult = await searchService.AdvancedSearchAsync(internalCommand, cancellationToken);

            // Map result to public
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
            return new SearchDataResult(false, Array.Empty<PublicSearchMatch>(), 0, 0, stopwatch.Elapsed, command.SearchCriteria.Mode, false, new[] { $"Advanced search failed: {ex.Message}" });
        }
    }

    /// <summary>
    /// Executes smart search with automatic optimization
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

            // Map public command to internal
            var internalCommand = command.ToInternal();

            // Execute smart search
            var internalResult = await searchService.SmartSearchAsync(internalCommand, cancellationToken);

            // Map result to public
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
            return new SearchDataResult(false, Array.Empty<PublicSearchMatch>(), 0, 0, stopwatch.Elapsed, PublicSearchMode.Contains, false, new[] { $"Smart search failed: {ex.Message}" });
        }
    }

    /// <summary>
    /// Quick synchronous search for immediate results
    /// </summary>
    public SearchDataResult QuickSearch(IEnumerable<IReadOnlyDictionary<string, object?>> data, string searchText, bool caseSensitive = false)
    {
        ThrowIfDisposed();

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            using var scope = ServiceRegistration.CreateOperationScope(_serviceProvider);
            var searchService = scope.ServiceProvider.GetRequiredService<Features.Search.Interfaces.ISearchService>();

            // Execute quick search
            var internalResult = searchService.QuickSearch(data, searchText, caseSensitive);

            // Map result to public
            var result = internalResult.ToPublic();

            _logger.LogInformation("QuickSearch completed in {Duration}ms for text '{SearchText}': found {MatchCount} matches",
                stopwatch.ElapsedMilliseconds, searchText, result.TotalMatchesFound);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "QuickSearch failed for text '{SearchText}'", searchText);
            return new SearchDataResult(false, Array.Empty<PublicSearchMatch>(), 0, 0, stopwatch.Elapsed, PublicSearchMode.Contains, false, new[] { $"QuickSearch failed: {ex.Message}" });
        }
    }

    /// <summary>
    /// Validates search criteria
    /// </summary>
    public async Task<PublicResult> ValidateSearchCriteriaAsync(PublicAdvancedSearchCriteria searchCriteria)
    {
        ThrowIfDisposed();

        try
        {
            using var scope = ServiceRegistration.CreateOperationScope(_serviceProvider);
            var searchService = scope.ServiceProvider.GetRequiredService<Features.Search.Interfaces.ISearchService>();

            // Map public criteria to internal
            var internalCriteria = searchCriteria.ToInternal();

            // Validation
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
    /// Gets list of searchable columns
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
}

