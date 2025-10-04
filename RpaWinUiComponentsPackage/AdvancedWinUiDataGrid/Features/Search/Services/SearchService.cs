using System.Diagnostics;
using Microsoft.Extensions.Logging;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Common.Models;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Core.Utilities;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Common;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Core.ValueObjects;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Search.Commands;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Search.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Search.Models;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Infrastructure.Logging.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Infrastructure.Logging.NullPattern;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Search.Services;

/// <summary>
/// Interná implementácia search služby s LINQ optimalizáciami
/// Thread-safe s podporou parallel processing, regex, fuzzy matching
/// </summary>
internal sealed class SearchService : ISearchService
{
    private const int ParallelProcessingThreshold = 1000;
    private readonly ILogger<SearchService> _logger;
    private readonly IOperationLogger<SearchService> _operationLogger;

    public SearchService(
        ILogger<SearchService> logger,
        IOperationLogger<SearchService>? operationLogger = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _operationLogger = operationLogger ?? NullOperationLogger<SearchService>.Instance;
    }

    /// <summary>
    /// Vykoná základné vyhľadávanie s LINQ optimization
    /// </summary>
    public async Task<SearchResultCollection> SearchAsync(SearchCommand command, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var operationId = Guid.NewGuid();

        using var scope = _operationLogger.LogOperationStart("SearchAsync", new
        {
            OperationId = operationId,
            SearchText = command.SearchText,
            TargetColumns = command.TargetColumns?.Length ?? 0,
            CaseSensitive = command.CaseSensitive
        });

        _logger.LogInformation("Starting search operation {OperationId}: text='{SearchText}', columns={ColumnCount}, caseSensitive={CaseSensitive}",
            operationId, command.SearchText, command.TargetColumns?.Length ?? 0, command.CaseSensitive);

        try
        {
            // Validácia search textu
            if (string.IsNullOrWhiteSpace(command.SearchText))
            {
                var error = "Search text cannot be empty";
                _logger.LogWarning("Search validation failed for operation {OperationId}: {Error}", operationId, error);
                scope.MarkFailure(new ArgumentException(error));
                return SearchResultCollection.CreateFailure(new[] { error }, stopwatch.Elapsed);
            }

            // Konverzia na list pre performance
            var dataList = command.Data.ToList();
            _logger.LogInformation("Processing {RowCount} rows for search operation {OperationId}", dataList.Count, operationId);

            // Získame target columns
            var searchColumns = command.TargetColumns ?? GetSearchableColumns(dataList).ToArray();
            if (searchColumns.Length == 0)
            {
                var error = "No searchable columns found";
                _logger.LogWarning("Search failed for operation {OperationId}: {Error}", operationId, error);
                scope.MarkFailure(new InvalidOperationException(error));
                return SearchResultCollection.CreateFailure(new[] { error }, stopwatch.Elapsed);
            }

            // Výber search stratégie
            var useParallel = command.EnableParallelProcessing && dataList.Count > ParallelProcessingThreshold;

            // LINQ search execution
            var results = new List<SearchResult>();
            var processedRows = 0;

            if (useParallel)
            {
                _logger.LogInformation("Using parallel search for {RowCount} rows", dataList.Count);

                var parallelResults = dataList
                    .AsParallel()
                    .WithCancellation(cancellationToken)
                    .SelectMany((row, rowIndex) =>
                    {
                        var rowResults = new List<SearchResult>();
                        foreach (var columnName in searchColumns)
                        {
                            if (row.TryGetValue(columnName, out var value))
                            {
                                var text = value?.ToString() ?? string.Empty;
                                if (SearchFilterAlgorithms.IsMatch(text, command.SearchText, false, command.CaseSensitive))
                                {
                                    rowResults.Add(SearchResult.Create(rowIndex, columnName, value, text));
                                }
                            }
                        }

                        if (command.ProgressReporter != null)
                        {
                            var processed = Interlocked.Increment(ref processedRows);
                            command.ProgressReporter.Report(new SearchProgress(
                                processed, dataList.Count, stopwatch.Elapsed, "Searching", results.Count, null));
                        }

                        return rowResults;
                    })
                    .ToList();

                results.AddRange(parallelResults);
            }
            else
            {
                _logger.LogInformation("Using sequential search for {RowCount} rows", dataList.Count);

                for (var rowIndex = 0; rowIndex < dataList.Count; rowIndex++)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var row = dataList[rowIndex];
                    foreach (var columnName in searchColumns)
                    {
                        if (row.TryGetValue(columnName, out var value))
                        {
                            var text = value?.ToString() ?? string.Empty;
                            if (SearchFilterAlgorithms.IsMatch(text, command.SearchText, false, command.CaseSensitive))
                            {
                                results.Add(SearchResult.Create(rowIndex, columnName, value, text));
                            }
                        }
                    }

                    if (command.ProgressReporter != null && rowIndex % 100 == 0)
                    {
                        command.ProgressReporter.Report(new SearchProgress(
                            rowIndex + 1, dataList.Count, stopwatch.Elapsed, "Searching", results.Count, null));
                    }
                }
            }

            stopwatch.Stop();

            _logger.LogInformation("Search operation {OperationId} completed in {Duration}ms: found {MatchCount} matches in {RowCount} rows",
                operationId, stopwatch.ElapsedMilliseconds, results.Count, dataList.Count);

            scope.MarkSuccess(new
            {
                MatchCount = results.Count,
                RowsSearched = dataList.Count,
                Duration = stopwatch.Elapsed,
                UsedParallel = useParallel
            });

            return SearchResultCollection.CreateSuccess(
                results,
                dataList.Count,
                searchColumns.Length,
                stopwatch.Elapsed,
                SearchMode.Contains,
                usedParallel: useParallel,
                usedRanking: false);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Search operation {OperationId} was cancelled", operationId);
            scope.MarkFailure(new OperationCanceledException("Search cancelled"));
            return SearchResultCollection.CreateFailure(new[] { "Search operation was cancelled" }, stopwatch.Elapsed);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Search operation {OperationId} failed: {Message}", operationId, ex.Message);
            scope.MarkFailure(ex);
            return SearchResultCollection.CreateFailure(new[] { $"Search failed: {ex.Message}" }, stopwatch.Elapsed);
        }
    }

    /// <summary>
    /// Vykoná pokročilé vyhľadávanie s regex, fuzzy matching a smart ranking
    /// </summary>
    public async Task<SearchResultCollection> AdvancedSearchAsync(AdvancedSearchCommand command, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var operationId = Guid.NewGuid();

        using var scope = _operationLogger.LogOperationStart("AdvancedSearchAsync", new
        {
            OperationId = operationId,
            SearchText = command.SearchCriteria.SearchText,
            Mode = command.SearchCriteria.Mode,
            UseRegex = command.SearchCriteria.UseRegex,
            UseFuzzy = command.SearchCriteria.Mode == SearchMode.Fuzzy
        });

        _logger.LogInformation("Starting advanced search operation {OperationId}: text='{SearchText}', mode={Mode}, regex={UseRegex}",
            operationId, command.SearchCriteria.SearchText, command.SearchCriteria.Mode, command.SearchCriteria.UseRegex);

        try
        {
            // Validácia
            var validationResult = await ValidateSearchCriteriaAsync(command.SearchCriteria, cancellationToken);
            if (!validationResult.IsSuccess)
            {
                _logger.LogWarning("Advanced search validation failed for operation {OperationId}: {Error}",
                    operationId, validationResult.ErrorMessage);
                scope.MarkFailure(new InvalidOperationException(validationResult.ErrorMessage));
                return SearchResultCollection.CreateFailure(new[] { validationResult.ErrorMessage ?? "Validation failed" }, stopwatch.Elapsed);
            }

            var dataList = command.Data.ToList();
            var searchColumns = command.SearchCriteria.TargetColumns ?? GetSearchableColumns(dataList).ToArray();

            if (searchColumns.Length == 0)
            {
                var error = "No searchable columns found";
                scope.MarkFailure(new InvalidOperationException(error));
                return SearchResultCollection.CreateFailure(new[] { error }, stopwatch.Elapsed);
            }

            var useParallel = command.EnableParallelProcessing && dataList.Count > ParallelProcessingThreshold;
            var results = new List<SearchResult>();

            // LINQ search execution podľa mode
            switch (command.SearchCriteria.Mode)
            {
                case SearchMode.Regex:
                    results = await SearchWithRegexAsync(dataList, searchColumns, command, useParallel, cancellationToken);
                    break;

                case SearchMode.Fuzzy:
                    results = await SearchWithFuzzyAsync(dataList, searchColumns, command, useParallel, cancellationToken);
                    break;

                case SearchMode.Exact:
                    results = await SearchExactAsync(dataList, searchColumns, command, useParallel, cancellationToken);
                    break;

                case SearchMode.StartsWith:
                    results = await SearchStartsWithAsync(dataList, searchColumns, command, useParallel, cancellationToken);
                    break;

                case SearchMode.EndsWith:
                    results = await SearchEndsWithAsync(dataList, searchColumns, command, useParallel, cancellationToken);
                    break;

                default: // Contains
                    results = await SearchContainsAsync(dataList, searchColumns, command, useParallel, cancellationToken);
                    break;
            }

            // Apply ranking if enabled
            if (command.UseSmartRanking && command.SearchCriteria.Ranking != SearchRanking.None)
            {
                results = ApplyRanking(results, command.SearchCriteria.Ranking);
            }

            // Apply max matches limit
            if (command.SearchCriteria.MaxMatches.HasValue && results.Count > command.SearchCriteria.MaxMatches.Value)
            {
                _logger.LogInformation("Limiting results from {FoundCount} to {MaxCount} matches",
                    results.Count, command.SearchCriteria.MaxMatches.Value);
                results = results.Take(command.SearchCriteria.MaxMatches.Value).ToList();
            }

            stopwatch.Stop();

            _logger.LogInformation("Advanced search operation {OperationId} completed in {Duration}ms: found {MatchCount} matches",
                operationId, stopwatch.ElapsedMilliseconds, results.Count);

            scope.MarkSuccess(new
            {
                MatchCount = results.Count,
                RowsSearched = dataList.Count,
                Duration = stopwatch.Elapsed,
                UsedParallel = useParallel,
                UsedRanking = command.UseSmartRanking
            });

            return SearchResultCollection.CreateSuccess(
                results,
                dataList.Count,
                searchColumns.Length,
                stopwatch.Elapsed,
                command.SearchCriteria.Mode,
                usedParallel: useParallel,
                usedRanking: command.UseSmartRanking);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Advanced search operation {OperationId} was cancelled", operationId);
            scope.MarkFailure(new OperationCanceledException("Search cancelled"));
            return SearchResultCollection.CreateFailure(new[] { "Advanced search operation was cancelled" }, stopwatch.Elapsed);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Advanced search operation {OperationId} failed: {Message}", operationId, ex.Message);
            scope.MarkFailure(ex);
            return SearchResultCollection.CreateFailure(new[] { $"Advanced search failed: {ex.Message}" }, stopwatch.Elapsed);
        }
    }

    /// <summary>
    /// Vykoná smart search s automatickou optimalizáciou
    /// </summary>
    public async Task<SearchResultCollection> SmartSearchAsync(SmartSearchCommand command, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var operationId = Guid.NewGuid();

        _logger.LogInformation("Starting smart search operation {OperationId}: text='{SearchText}', autoOptimize={AutoOptimize}",
            operationId, command.SearchText, command.AutoOptimize);

        try
        {
            // Automaticky vyber najlepšiu stratégiu
            var dataList = command.Data.ToList();
            var searchColumns = command.TargetColumns ?? GetSearchableColumns(dataList).ToArray();

            // Analyzuj search text a odporuč mode
            var recommendedModes = GetRecommendedSearchModes(dataList, command.SearchText);
            var selectedMode = recommendedModes.FirstOrDefault();

            _logger.LogInformation("Smart search selected mode: {SelectedMode} for '{SearchText}'",
                selectedMode, command.SearchText);

            // Vytvor advanced search criteria s optimálnymi nastaveniami
            var criteria = new AdvancedSearchCriteria
            {
                SearchText = command.SearchText,
                TargetColumns = searchColumns,
                Mode = selectedMode,
                CaseSensitive = command.CaseSensitive,
                Scope = command.Scope,
                UseRegex = selectedMode == SearchMode.Regex,
                Ranking = SearchRanking.Relevance,
                ShowProgress = true
            };

            var advancedCommand = new AdvancedSearchCommand
            {
                Data = dataList,
                SearchCriteria = criteria,
                EnableParallelProcessing = dataList.Count > ParallelProcessingThreshold,
                UseSmartRanking = true,
                CancellationToken = cancellationToken,
                ProgressReporter = command.ProgressReporter
            };

            return await AdvancedSearchAsync(advancedCommand, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Smart search operation {OperationId} failed: {Message}", operationId, ex.Message);
            return SearchResultCollection.CreateFailure(new[] { $"Smart search failed: {ex.Message}" }, stopwatch.Elapsed);
        }
    }

    /// <summary>
    /// Quick search pre okamžité výsledky (synchronous)
    /// </summary>
    public SearchResultCollection QuickSearch(
        IEnumerable<IReadOnlyDictionary<string, object?>> data,
        string searchText,
        bool caseSensitive = false)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var command = SearchCommand.Create(data, searchText);
            command = command with { CaseSensitive = caseSensitive, EnableParallelProcessing = false };

            var result = SearchAsync(command, CancellationToken.None).GetAwaiter().GetResult();

            _logger.LogInformation("QuickSearch completed in {Duration}ms for text '{SearchText}'",
                stopwatch.ElapsedMilliseconds, searchText);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "QuickSearch failed for text '{SearchText}'", searchText);
            return SearchResultCollection.CreateFailure(new[] { $"QuickSearch failed: {ex.Message}" }, stopwatch.Elapsed);
        }
    }

    /// <summary>
    /// Validuje search kritériá
    /// </summary>
    public async Task<Result> ValidateSearchCriteriaAsync(
        AdvancedSearchCriteria searchCriteria,
        CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask; // Pre async kompatibilitu

        if (string.IsNullOrWhiteSpace(searchCriteria.SearchText))
            return Result.Failure("Search text cannot be empty");

        if (searchCriteria.UseRegex || searchCriteria.Mode == SearchMode.Regex)
        {
            try
            {
                _ = new System.Text.RegularExpressions.Regex(searchCriteria.SearchText);
            }
            catch (Exception ex)
            {
                return Result.Failure($"Invalid regex pattern: {ex.Message}");
            }
        }

        if (searchCriteria.FuzzyThreshold.HasValue)
        {
            if (searchCriteria.FuzzyThreshold.Value < 0.0 || searchCriteria.FuzzyThreshold.Value > 1.0)
                return Result.Failure("Fuzzy threshold must be between 0.0 and 1.0");
        }

        return Result.Success();
    }

    /// <summary>
    /// Získa zoznam stĺpcov, v ktorých možno vyhľadávať
    /// </summary>
    public IReadOnlyList<string> GetSearchableColumns(IEnumerable<IReadOnlyDictionary<string, object?>> data)
    {
        var firstRow = data.FirstOrDefault();
        if (firstRow == null)
            return Array.Empty<string>();

        return firstRow.Keys.ToList().AsReadOnly();
    }

    /// <summary>
    /// Odporúči vhodné search modes pre dáta
    /// </summary>
    public IReadOnlyList<SearchMode> GetRecommendedSearchModes(
        IEnumerable<IReadOnlyDictionary<string, object?>> data,
        string searchText)
    {
        var modes = new List<SearchMode>();

        // Detekcia regex patternov
        if (searchText.Contains(".*") || searchText.Contains("\\d") || searchText.Contains("[") || searchText.Contains("^") || searchText.Contains("$"))
        {
            modes.Add(SearchMode.Regex);
        }

        // Detekcia exact match (quoted string)
        if (searchText.StartsWith("\"") && searchText.EndsWith("\""))
        {
            modes.Add(SearchMode.Exact);
        }

        // Detekcia wildcard patterns
        if (searchText.EndsWith("*") && !searchText.StartsWith("*"))
        {
            modes.Add(SearchMode.StartsWith);
        }
        else if (searchText.StartsWith("*") && !searchText.EndsWith("*"))
        {
            modes.Add(SearchMode.EndsWith);
        }

        // Default modes
        if (modes.Count == 0)
        {
            modes.Add(SearchMode.Contains);
            modes.Add(SearchMode.Fuzzy);
        }

        return modes;
    }

    #region Private Helper Methods

    private async Task<List<SearchResult>> SearchWithRegexAsync(
        List<IReadOnlyDictionary<string, object?>> dataList,
        string[] searchColumns,
        AdvancedSearchCommand command,
        bool useParallel,
        CancellationToken cancellationToken)
    {
        var results = new List<SearchResult>();

        if (useParallel)
        {
            var parallelResults = dataList
                .AsParallel()
                .WithCancellation(cancellationToken)
                .SelectMany((row, rowIndex) =>
                {
                    var rowResults = new List<SearchResult>();
                    foreach (var columnName in searchColumns)
                    {
                        if (row.TryGetValue(columnName, out var value))
                        {
                            var text = value?.ToString() ?? string.Empty;
                            if (SearchFilterAlgorithms.IsRegexMatch(text, command.SearchCriteria.SearchText, command.SearchCriteria.CaseSensitive))
                            {
                                var relevanceScore = SearchFilterAlgorithms.CalculateRelevanceScore(text, command.SearchCriteria.SearchText, command.SearchCriteria.CaseSensitive);
                                rowResults.Add(SearchResult.CreateEnhanced(
                                    rowIndex, columnName, value, text, false, 1.0, relevanceScore, SearchMode.Regex, command.SearchCriteria.HighlightMatches));
                            }
                        }
                    }
                    return rowResults;
                })
                .ToList();

            results.AddRange(parallelResults);
        }
        else
        {
            for (var rowIndex = 0; rowIndex < dataList.Count; rowIndex++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var row = dataList[rowIndex];
                foreach (var columnName in searchColumns)
                {
                    if (row.TryGetValue(columnName, out var value))
                    {
                        var text = value?.ToString() ?? string.Empty;
                        if (SearchFilterAlgorithms.IsRegexMatch(text, command.SearchCriteria.SearchText, command.SearchCriteria.CaseSensitive))
                        {
                            var relevanceScore = SearchFilterAlgorithms.CalculateRelevanceScore(text, command.SearchCriteria.SearchText, command.SearchCriteria.CaseSensitive);
                            results.Add(SearchResult.CreateEnhanced(
                                rowIndex, columnName, value, text, false, 1.0, relevanceScore, SearchMode.Regex, command.SearchCriteria.HighlightMatches));
                        }
                    }
                }
            }
        }

        return await Task.FromResult(results);
    }

    private async Task<List<SearchResult>> SearchWithFuzzyAsync(
        List<IReadOnlyDictionary<string, object?>> dataList,
        string[] searchColumns,
        AdvancedSearchCommand command,
        bool useParallel,
        CancellationToken cancellationToken)
    {
        var threshold = command.SearchCriteria.FuzzyThreshold ?? 0.8;
        var results = new List<SearchResult>();

        if (useParallel)
        {
            var parallelResults = dataList
                .AsParallel()
                .WithCancellation(cancellationToken)
                .SelectMany((row, rowIndex) =>
                {
                    var rowResults = new List<SearchResult>();
                    foreach (var columnName in searchColumns)
                    {
                        if (row.TryGetValue(columnName, out var value))
                        {
                            var text = value?.ToString() ?? string.Empty;
                            var matchScore = SearchFilterAlgorithms.CalculateFuzzyMatchScore(text, command.SearchCriteria.SearchText);
                            if (matchScore >= threshold)
                            {
                                rowResults.Add(SearchResult.CreateEnhanced(
                                    rowIndex, columnName, value, text, false, matchScore, matchScore, SearchMode.Fuzzy, command.SearchCriteria.HighlightMatches));
                            }
                        }
                    }
                    return rowResults;
                })
                .ToList();

            results.AddRange(parallelResults);
        }
        else
        {
            for (var rowIndex = 0; rowIndex < dataList.Count; rowIndex++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var row = dataList[rowIndex];
                foreach (var columnName in searchColumns)
                {
                    if (row.TryGetValue(columnName, out var value))
                    {
                        var text = value?.ToString() ?? string.Empty;
                        var matchScore = SearchFilterAlgorithms.CalculateFuzzyMatchScore(text, command.SearchCriteria.SearchText);
                        if (matchScore >= threshold)
                        {
                            results.Add(SearchResult.CreateEnhanced(
                                rowIndex, columnName, value, text, false, matchScore, matchScore, SearchMode.Fuzzy, command.SearchCriteria.HighlightMatches));
                        }
                    }
                }
            }
        }

        return await Task.FromResult(results);
    }

    private async Task<List<SearchResult>> SearchExactAsync(
        List<IReadOnlyDictionary<string, object?>> dataList,
        string[] searchColumns,
        AdvancedSearchCommand command,
        bool useParallel,
        CancellationToken cancellationToken)
    {
        var comparison = command.SearchCriteria.CaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
        var results = new List<SearchResult>();

        if (useParallel)
        {
            var parallelResults = dataList
                .AsParallel()
                .WithCancellation(cancellationToken)
                .SelectMany((row, rowIndex) =>
                {
                    var rowResults = new List<SearchResult>();
                    foreach (var columnName in searchColumns)
                    {
                        if (row.TryGetValue(columnName, out var value))
                        {
                            var text = value?.ToString() ?? string.Empty;
                            if (text.Equals(command.SearchCriteria.SearchText, comparison))
                            {
                                rowResults.Add(SearchResult.CreateEnhanced(
                                    rowIndex, columnName, value, text, true, 1.0, 1.0, SearchMode.Exact, command.SearchCriteria.HighlightMatches));
                            }
                        }
                    }
                    return rowResults;
                })
                .ToList();

            results.AddRange(parallelResults);
        }
        else
        {
            for (var rowIndex = 0; rowIndex < dataList.Count; rowIndex++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var row = dataList[rowIndex];
                foreach (var columnName in searchColumns)
                {
                    if (row.TryGetValue(columnName, out var value))
                    {
                        var text = value?.ToString() ?? string.Empty;
                        if (text.Equals(command.SearchCriteria.SearchText, comparison))
                        {
                            results.Add(SearchResult.CreateEnhanced(
                                rowIndex, columnName, value, text, true, 1.0, 1.0, SearchMode.Exact, command.SearchCriteria.HighlightMatches));
                        }
                    }
                }
            }
        }

        return await Task.FromResult(results);
    }

    private async Task<List<SearchResult>> SearchStartsWithAsync(
        List<IReadOnlyDictionary<string, object?>> dataList,
        string[] searchColumns,
        AdvancedSearchCommand command,
        bool useParallel,
        CancellationToken cancellationToken)
    {
        var comparison = command.SearchCriteria.CaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
        var results = new List<SearchResult>();

        if (useParallel)
        {
            var parallelResults = dataList
                .AsParallel()
                .WithCancellation(cancellationToken)
                .SelectMany((row, rowIndex) =>
                {
                    var rowResults = new List<SearchResult>();
                    foreach (var columnName in searchColumns)
                    {
                        if (row.TryGetValue(columnName, out var value))
                        {
                            var text = value?.ToString() ?? string.Empty;
                            if (text.StartsWith(command.SearchCriteria.SearchText, comparison))
                            {
                                rowResults.Add(SearchResult.CreateEnhanced(
                                    rowIndex, columnName, value, text, false, 0.9, 0.9, SearchMode.StartsWith, command.SearchCriteria.HighlightMatches));
                            }
                        }
                    }
                    return rowResults;
                })
                .ToList();

            results.AddRange(parallelResults);
        }
        else
        {
            for (var rowIndex = 0; rowIndex < dataList.Count; rowIndex++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var row = dataList[rowIndex];
                foreach (var columnName in searchColumns)
                {
                    if (row.TryGetValue(columnName, out var value))
                    {
                        var text = value?.ToString() ?? string.Empty;
                        if (text.StartsWith(command.SearchCriteria.SearchText, comparison))
                        {
                            results.Add(SearchResult.CreateEnhanced(
                                rowIndex, columnName, value, text, false, 0.9, 0.9, SearchMode.StartsWith, command.SearchCriteria.HighlightMatches));
                        }
                    }
                }
            }
        }

        return await Task.FromResult(results);
    }

    private async Task<List<SearchResult>> SearchEndsWithAsync(
        List<IReadOnlyDictionary<string, object?>> dataList,
        string[] searchColumns,
        AdvancedSearchCommand command,
        bool useParallel,
        CancellationToken cancellationToken)
    {
        var comparison = command.SearchCriteria.CaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
        var results = new List<SearchResult>();

        if (useParallel)
        {
            var parallelResults = dataList
                .AsParallel()
                .WithCancellation(cancellationToken)
                .SelectMany((row, rowIndex) =>
                {
                    var rowResults = new List<SearchResult>();
                    foreach (var columnName in searchColumns)
                    {
                        if (row.TryGetValue(columnName, out var value))
                        {
                            var text = value?.ToString() ?? string.Empty;
                            if (text.EndsWith(command.SearchCriteria.SearchText, comparison))
                            {
                                rowResults.Add(SearchResult.CreateEnhanced(
                                    rowIndex, columnName, value, text, false, 0.85, 0.85, SearchMode.EndsWith, command.SearchCriteria.HighlightMatches));
                            }
                        }
                    }
                    return rowResults;
                })
                .ToList();

            results.AddRange(parallelResults);
        }
        else
        {
            for (var rowIndex = 0; rowIndex < dataList.Count; rowIndex++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var row = dataList[rowIndex];
                foreach (var columnName in searchColumns)
                {
                    if (row.TryGetValue(columnName, out var value))
                    {
                        var text = value?.ToString() ?? string.Empty;
                        if (text.EndsWith(command.SearchCriteria.SearchText, comparison))
                        {
                            results.Add(SearchResult.CreateEnhanced(
                                rowIndex, columnName, value, text, false, 0.85, 0.85, SearchMode.EndsWith, command.SearchCriteria.HighlightMatches));
                        }
                    }
                }
            }
        }

        return await Task.FromResult(results);
    }

    private async Task<List<SearchResult>> SearchContainsAsync(
        List<IReadOnlyDictionary<string, object?>> dataList,
        string[] searchColumns,
        AdvancedSearchCommand command,
        bool useParallel,
        CancellationToken cancellationToken)
    {
        var comparison = command.SearchCriteria.CaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
        var results = new List<SearchResult>();

        if (useParallel)
        {
            var parallelResults = dataList
                .AsParallel()
                .WithCancellation(cancellationToken)
                .SelectMany((row, rowIndex) =>
                {
                    var rowResults = new List<SearchResult>();
                    foreach (var columnName in searchColumns)
                    {
                        if (row.TryGetValue(columnName, out var value))
                        {
                            var text = value?.ToString() ?? string.Empty;
                            if (text.Contains(command.SearchCriteria.SearchText, comparison))
                            {
                                var relevanceScore = SearchFilterAlgorithms.CalculateRelevanceScore(text, command.SearchCriteria.SearchText, command.SearchCriteria.CaseSensitive);
                                rowResults.Add(SearchResult.CreateEnhanced(
                                    rowIndex, columnName, value, text, false, 0.8, relevanceScore, SearchMode.Contains, command.SearchCriteria.HighlightMatches));
                            }
                        }
                    }
                    return rowResults;
                })
                .ToList();

            results.AddRange(parallelResults);
        }
        else
        {
            for (var rowIndex = 0; rowIndex < dataList.Count; rowIndex++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var row = dataList[rowIndex];
                foreach (var columnName in searchColumns)
                {
                    if (row.TryGetValue(columnName, out var value))
                    {
                        var text = value?.ToString() ?? string.Empty;
                        if (text.Contains(command.SearchCriteria.SearchText, comparison))
                        {
                            var relevanceScore = SearchFilterAlgorithms.CalculateRelevanceScore(text, command.SearchCriteria.SearchText, command.SearchCriteria.CaseSensitive);
                            results.Add(SearchResult.CreateEnhanced(
                                rowIndex, columnName, value, text, false, 0.8, relevanceScore, SearchMode.Contains, command.SearchCriteria.HighlightMatches));
                        }
                    }
                }
            }
        }

        return await Task.FromResult(results);
    }

    private List<SearchResult> ApplyRanking(List<SearchResult> results, SearchRanking ranking)
    {
        return ranking switch
        {
            SearchRanking.Relevance => results.OrderByDescending(r => r.RelevanceScore).ThenByDescending(r => r.MatchScore).ToList(),
            SearchRanking.Position => results.OrderBy(r => r.RowIndex).ThenBy(r => r.ColumnName).ToList(),
            SearchRanking.Frequency => results.GroupBy(r => r.MatchedText).OrderByDescending(g => g.Count()).SelectMany(g => g).ToList(),
            _ => results
        };
    }

    #endregion
}
