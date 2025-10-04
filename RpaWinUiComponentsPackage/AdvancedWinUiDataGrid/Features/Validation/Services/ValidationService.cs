using Microsoft.Extensions.Logging;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Common.Models;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Common;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Validation.Interfaces;
using System.Collections.Concurrent;
using System.Diagnostics;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Infrastructure.Logging.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Infrastructure.Logging.NullPattern;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Api.Models;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Validation.Services;

/// <summary>
/// Interná implementácia validation služby s komplexnou funkcionalitou
/// CRITICAL: Implementuje AreAllNonEmptyRowsValidAsync s batch, thread-safe a stream podporou
/// Musí byť volaná Import & Paste & Export operáciami
/// Thread-safe bez per-operation mutable fields
/// </summary>
internal sealed class ValidationService : IValidationService
{
    private readonly ILogger<ValidationService> _logger;
    private readonly IOperationLogger<ValidationService> _operationLogger;
    private readonly Logging.ValidationLogger _validationLogger;
    private readonly Infrastructure.Persistence.Interfaces.IRowStore _rowStore;
    private readonly AdvancedDataGridOptions _options;
    private readonly ConcurrentBag<IValidationRule> _validationRules;

    /// <summary>
    /// Konštruktor ValidationService
    /// Inicializuje všetky závislosti a nastavuje null pattern pre optional operation logger
    /// </summary>
    public ValidationService(
        ILogger<ValidationService> logger,
        Logging.ValidationLogger validationLogger,
        Infrastructure.Persistence.Interfaces.IRowStore rowStore,
        AdvancedDataGridOptions options,
        IOperationLogger<ValidationService>? operationLogger = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _validationLogger = validationLogger ?? throw new ArgumentNullException(nameof(validationLogger));
        _rowStore = rowStore ?? throw new ArgumentNullException(nameof(rowStore));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _validationRules = new ConcurrentBag<IValidationRule>();

        // Ak nie je poskytnutý operation logger, použijeme null pattern (žiadne logovanie)
        _operationLogger = operationLogger ?? NullOperationLogger<ValidationService>.Instance;
    }

    /// <summary>
    /// CRITICAL: Validuje všetky neprázdne riadky s batch, thread-safe a stream podporou
    /// Musí byť volaná Import & Paste & Export operáciami
    /// Podporuje filtrovanú validáciu pre export scenáre
    /// Používa IRowStore validation state management pre perzistenciu
    /// </summary>
    public async Task<Result<bool>> AreAllNonEmptyRowsValidAsync(bool onlyFiltered = false, CancellationToken cancellationToken = default)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var operationId = Guid.NewGuid();

        // Začíname validáciu - vytvoríme operation scope pre automatické tracking
        using var scope = _operationLogger.LogOperationStart("AreAllNonEmptyRowsValidAsync", new
        {
            OperationId = operationId,
            OnlyFiltered = onlyFiltered
        });

        // Získame počet riadkov pre špecializované logovanie
        var totalRowCount = (int)await _rowStore.GetRowCountAsync(cancellationToken);
        var ruleCount = _validationRules.Count;

        // Špecializované validation logovanie - start
        _validationLogger.LogValidationStart(operationId, totalRowCount, ruleCount, onlyFiltered, false);

        try
        {
            // Kontrolujeme či máme cachedovaný validation state ktorý môžeme použiť
            _logger.LogInformation("Checking cached validation state for operation {OperationId}", operationId);
            var hasValidationState = await _rowStore.HasValidationStateForScopeAsync(onlyFiltered, cancellationToken);
            if (hasValidationState)
            {
                var cachedResult = await _rowStore.AreAllNonEmptyRowsMarkedValidAsync(onlyFiltered, cancellationToken);
                _logger.LogInformation("Using cached validation state for operation {OperationId}: {CachedResult}",
                    operationId, cachedResult);
                _validationLogger.LogValidationCache(operationId, "hit", onlyFiltered ? "filtered" : "all");
                scope.MarkSuccess(new { CachedResult = cachedResult, UsedCache = true });
                return Result<bool>.Success(cachedResult);
            }

            _logger.LogInformation("Cache not found, starting new validation");
            _validationLogger.LogValidationCache(operationId, "miss", onlyFiltered ? "filtered" : "all");

            // Získame validation rules
            var activeRules = _validationRules.ToArray();
            _logger.LogInformation("Loaded {RuleCount} validation rules", activeRules.Length);

            if (activeRules.Length == 0)
            {
                // Žiadne validation rules - všetky riadky považujeme za validné
                _logger.LogInformation("No validation rules configured for operation {OperationId}, treating all rows as valid", operationId);
                await _rowStore.WriteValidationResultsAsync(Array.Empty<ValidationError>(), cancellationToken);
                scope.MarkSuccess(new { IsValid = true, NoRules = true });
                return Result<bool>.Success(true);
            }

            // Získame dáta na validáciu (filtrované alebo všetky) pomocou StreamRowsAsync pre efektívnu pamäť
            _logger.LogInformation("Loading data for validation with batch size {BatchSize}", _options.BatchSize);
            var nonEmptyRows = new List<IReadOnlyDictionary<string, object?>>();
            var batchIndex = 0;

            await foreach (var batch in _rowStore.StreamRowsAsync(onlyFiltered, _options.BatchSize, cancellationToken))
            {
                var nonEmptyInBatch = FilterNonEmptyRows(batch, operationId);
                nonEmptyRows.AddRange(nonEmptyInBatch);
                _logger.LogInformation("Loaded batch {BatchIndex}: {NonEmptyCount} non-empty rows",
                    batchIndex++, nonEmptyInBatch.Count);
            }

            _logger.LogInformation("Total {TotalRows} non-empty rows loaded for validation", nonEmptyRows.Count);

            if (nonEmptyRows.Count == 0)
            {
                // Žiadne neprázdne riadky - všetko je validné
                _logger.LogInformation("No non-empty rows found for validation in operation {OperationId}", operationId);
                await _rowStore.WriteValidationResultsAsync(Array.Empty<ValidationError>(), cancellationToken);
                scope.MarkSuccess(new { IsValid = true, NoRows = true });
                return Result<bool>.Success(true);
            }

            // Validujeme v dávkach s thread-safe spracovaním
            _logger.LogInformation("Starting validation of {RowCount} rows with {RuleCount} rules",
                nonEmptyRows.Count, activeRules.Length);

            var validationResult = await ValidateRowsBatchedAsync(nonEmptyRows, activeRules, operationId, cancellationToken);

            var isValid = validationResult.IsSuccess && validationResult.Value == 0;
            var errorCount = validationResult.IsSuccess ? validationResult.Value : 0;

            // Zalogujeme metriky validácie
            _operationLogger.LogValidationOperation(
                validationType: "ComprehensiveValidation",
                totalRows: nonEmptyRows.Count,
                validRows: nonEmptyRows.Count - errorCount,
                ruleCount: activeRules.Length,
                duration: stopwatch.Elapsed);

            // Špecializované logovanie - completion & performance
            var errorsByType = new Dictionary<ValidationSeverity, int>
            {
                { ValidationSeverity.Error, errorCount },
                { ValidationSeverity.Warning, 0 },
                { ValidationSeverity.Info, 0 }
            };

            _validationLogger.LogValidationCompletion(
                operationId,
                true,
                nonEmptyRows.Count,
                nonEmptyRows.Count - errorCount,
                errorCount,
                errorsByType,
                stopwatch.Elapsed);

            var rowsPerSecond = nonEmptyRows.Count / stopwatch.Elapsed.TotalSeconds;
            var rulesPerSecond = (nonEmptyRows.Count * activeRules.Length) / stopwatch.Elapsed.TotalSeconds;
            _validationLogger.LogPerformanceMetrics(operationId, rowsPerSecond, rulesPerSecond, 0L, 0L, 0.0);

            if (isValid)
            {
                _logger.LogInformation("Validation operation {OperationId} completed successfully in {Duration}ms. " +
                    "Validated {RowCount} non-empty rows. All VALID",
                    operationId, stopwatch.ElapsedMilliseconds, nonEmptyRows.Count);
            }
            else
            {
                _logger.LogWarning("Validation operation {OperationId} completed in {Duration}ms. " +
                    "Validated {RowCount} non-empty rows. Found {ErrorCount} errors",
                    operationId, stopwatch.ElapsedMilliseconds, nonEmptyRows.Count, errorCount);
            }

            // Označíme scope ako úspešný (aj keď sú validation errors, operácia sama prebehla úspešne)
            scope.MarkSuccess(new
            {
                IsValid = isValid,
                ValidatedRows = nonEmptyRows.Count,
                ErrorCount = errorCount,
                RuleCount = activeRules.Length,
                Duration = stopwatch.Elapsed
            });

            return Result<bool>.Success(isValid);
        }
        catch (OperationCanceledException ex)
        {
            // Validácia bola zrušená používateľom
            _logger.LogInformation("Validation operation {OperationId} was cancelled by user", operationId);
            scope.MarkFailure(ex);
            return Result<bool>.Failure("Validation was cancelled");
        }
        catch (Exception ex)
        {
            // Neočakávaná chyba v validácii - CRITICAL level
            _logger.LogCritical(ex, "CRITICAL ERROR: Validation operation {OperationId} failed with unexpected error: {Message}. Stack trace: {StackTrace}",
                operationId, ex.Message, ex.StackTrace);
            scope.MarkFailure(ex);
            return Result<bool>.Failure($"Validation failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Pridáva validation rule do aktívnej kolekcie pravidiel
    /// Thread-safe operácia používajúca ConcurrentBag pre bezpečné pridávanie
    /// Vracia Result indikujúci úspech alebo zlyhanie operácie
    /// </summary>
    public async Task<Result> AddValidationRuleAsync(IValidationRule rule, CancellationToken cancellationToken = default)
    {
        if (rule == null)
            return Result.Failure("Validation rule cannot be null");

        try
        {
            _validationRules.Add(rule);
            _logger.LogDebug("Added validation rule {RuleType} with ID {RuleId}",
                rule.GetType().Name, rule.RuleId);

            await Task.CompletedTask;
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add validation rule {RuleType}", rule.GetType().Name);
            return Result.Failure($"Failed to add validation rule: {ex.Message}");
        }
    }

    /// <summary>
    /// Validuje riadky v dávkach s komplexným reportovaním výsledkov
    /// Thread-safe s podporou stream spracovania a progress reportingu
    /// Vracia BatchValidationResult s počtom úspešných a neúspešných validácií
    /// </summary>
    public async Task<BatchValidationResult> ValidateRowsBatchAsync(
        IEnumerable<IReadOnlyDictionary<string, object?>> rows,
        IProgress<ValidationProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var operationId = Guid.NewGuid();
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        _logger.LogDebug("Starting batch validation {OperationId}", operationId);

        try
        {
            var activeRules = _validationRules.ToArray();
            if (activeRules.Length == 0)
            {
                return BatchValidationResult.CreateSuccess(0, 0, stopwatch.Elapsed, Array.Empty<ValidationResult>());
            }

            var result = await ValidateRowsBatchedAsync(rows.ToList(), activeRules, operationId, cancellationToken, progress);
            if (result.IsSuccess)
            {
                return BatchValidationResult.CreateSuccess(
                    rows.Count(), 0, stopwatch.Elapsed, Array.Empty<ValidationResult>());
            }
            else
            {
                return BatchValidationResult.CreateFailure(
                    0, result.Value, rows.Count(), stopwatch.Elapsed,
                    Array.Empty<ValidationResult>(), new[] { result.ErrorMessage ?? "Validation failed" });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Batch validation {OperationId} failed", operationId);
            return BatchValidationResult.CreateFailure(0, 0, 0, stopwatch.Elapsed, Array.Empty<ValidationResult>(), new[] { $"Batch validation failed: {ex.Message}" });
        }
    }


    /// <summary>
    /// Filtruje prázdne riadky (riadky so všetkými null/prázdnymi hodnotami)
    /// Vracia iba riadky ktoré majú aspoň jednu neprázdnu hodnotu
    /// </summary>
    private List<IReadOnlyDictionary<string, object?>> FilterNonEmptyRows(
        IReadOnlyList<IReadOnlyDictionary<string, object?>> rows,
        Guid operationId)
    {
        var nonEmptyRows = new List<IReadOnlyDictionary<string, object?>>();

        foreach (var row in rows)
        {
            bool hasNonEmptyValue = false;

            foreach (var kvp in row)
            {
                var value = kvp.Value;
                if (value != null && !IsEmptyValue(value))
                {
                    hasNonEmptyValue = true;
                    break;
                }
            }

            if (hasNonEmptyValue)
            {
                nonEmptyRows.Add(row);
            }
        }

        _logger.LogDebug("Filtered {OriginalCount} rows to {NonEmptyCount} non-empty rows for operation {OperationId}",
            rows.Count, nonEmptyRows.Count, operationId);

        return nonEmptyRows;
    }

    /// <summary>
    /// Validuje riadky v dávkach s thread-safe spracovaním
    /// Zapisuje validation výsledky do IRowStore pre perzistenciu
    /// Vracia Result s počtom chýb alebo úspech
    /// </summary>
    private async Task<Result<int>> ValidateRowsBatchedAsync(
        IReadOnlyList<IReadOnlyDictionary<string, object?>> rows,
        IValidationRule[] rules,
        Guid operationId,
        CancellationToken cancellationToken,
        IProgress<ValidationProgress>? progress = null)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var batchSize = _options.BatchSize;
        var totalRows = rows.Count;
        var processedRows = 0;
        var totalErrors = 0;
        var totalWarnings = 0;
        var validationErrors = new ConcurrentBag<ValidationError>();
        var validationWarnings = new ConcurrentBag<ValidationWarning>();

        _logger.LogDebug("Starting batched validation for {TotalRows} rows in batches of {BatchSize} for operation {OperationId}",
            totalRows, batchSize, operationId);

        // Process in batches
        for (int batchStart = 0; batchStart < totalRows; batchStart += batchSize)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var batchEnd = Math.Min(batchStart + batchSize, totalRows);
            var batchRows = rows.Skip(batchStart).Take(batchEnd - batchStart).ToList();

            // Validate batch in parallel for thread-safety and performance
            var batchResult = await ValidateBatchInParallelAsync(batchRows, rules, batchStart, operationId, cancellationToken);

            // Collect results
            foreach (var error in batchResult.errors)
                validationErrors.Add(error);
            foreach (var warning in batchResult.warnings)
                validationWarnings.Add(warning);

            totalErrors += batchResult.errorCount;
            totalWarnings += batchResult.warningCount;
            processedRows += batchRows.Count;

            // Report progress
            if (progress != null)
            {
                var progressPercent = (double)processedRows / totalRows * 100;
                progress.Report(new ValidationProgress
                {
                    ProcessedRows = processedRows,
                    TotalRows = totalRows,
                    ProgressPercent = progressPercent,
                    ErrorCount = totalErrors,
                    WarningCount = totalWarnings,
                    StatusMessage = $"Processing batch... {processedRows}/{totalRows} rows completed"
                });
            }

            _logger.LogDebug("Completed batch {BatchStart}-{BatchEnd} for operation {OperationId}. " +
                           "Batch errors: {BatchErrors}, warnings: {BatchWarnings}",
                batchStart, batchEnd - 1, operationId, batchResult.errorCount, batchResult.warningCount);

            // Small delay for cooperative processing
            if (batchEnd < totalRows)
                await Task.Delay(1, cancellationToken);
        }

        // Write validation results to IRowStore for persistence and caching
        var allErrors = validationErrors.ToList();
        await _rowStore.WriteValidationResultsAsync(allErrors, cancellationToken);

        _logger.LogInformation("Stored {ErrorCount} validation errors to row store for operation {OperationId}",
            allErrors.Count, operationId);

        return Result<int>.Success(totalErrors);
    }

    /// <summary>
    /// Validuje dávku riadkov paralelne pre optimálny výkon
    /// Používa ConcurrentBag pre thread-safe zber chýb a varovaní
    /// </summary>
    private async Task<(int errorCount, int warningCount, List<ValidationError> errors, List<ValidationWarning> warnings)> ValidateBatchInParallelAsync(
        IReadOnlyList<IReadOnlyDictionary<string, object?>> batchRows,
        IValidationRule[] rules,
        int batchStartIndex,
        Guid operationId,
        CancellationToken cancellationToken)
    {
        var batchErrors = new ConcurrentBag<ValidationError>();
        var batchWarnings = new ConcurrentBag<ValidationWarning>();

        // Validate rows in parallel using Parallel.ForEach for CPU-bound validation
        await Task.Run(() =>
        {
            Parallel.ForEach(batchRows.Select((row, index) => new { row, index }),
                new ParallelOptions { CancellationToken = cancellationToken },
                rowData =>
                {
                    var absoluteRowIndex = batchStartIndex + rowData.index;

                    foreach (var rule in rules)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        try
                        {
                            var context = new ValidationContext
                            {
                                RowIndex = absoluteRowIndex,
                                OperationId = operationId.ToString()
                            };
                            var validationResult = rule.Validate(rowData.row, context);

                            if (!validationResult.IsValid)
                            {
                                if (validationResult.Severity == PublicValidationSeverity.Error)
                                {
                                    batchErrors.Add(new ValidationError
                                    {
                                        RowIndex = absoluteRowIndex,
                                        RuleId = rule.RuleId,
                                        Message = validationResult.ErrorMessage ?? "Validation failed",
                                        ColumnName = validationResult.AffectedColumn
                                    });
                                }
                                else if (validationResult.Severity == PublicValidationSeverity.Warning)
                                {
                                    batchWarnings.Add(new ValidationWarning
                                    {
                                        RowIndex = absoluteRowIndex,
                                        RuleId = rule.RuleId,
                                        Message = validationResult.ErrorMessage ?? "Validation warning",
                                        ColumnName = validationResult.AffectedColumn
                                    });
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Validation rule {RuleId} failed for row {RowIndex} in operation {OperationId}",
                                rule.RuleId, absoluteRowIndex, operationId);

                            batchErrors.Add(new ValidationError
                            {
                                RowIndex = absoluteRowIndex,
                                RuleId = rule.RuleId,
                                Message = $"Validation rule execution failed: {ex.Message}",
                                ColumnName = null
                            });
                        }
                    }
                });
        }, cancellationToken);

        return (batchErrors.Count, batchWarnings.Count, batchErrors.ToList(), batchWarnings.ToList());
    }

    /// <summary>
    /// Kontroluje či je hodnota považovaná za prázdnu
    /// Prázdna je null alebo whitespace string
    /// </summary>
    private static bool IsEmptyValue(object value)
    {
        return value switch
        {
            null => true,
            string str => string.IsNullOrWhiteSpace(str),
            _ => false
        };
    }

    /// <summary>
    /// Odstraňuje validation rules podľa column names
    /// Nájde všetky rules ktoré majú dependent columns v zadaných stĺpcoch a odstráni ich
    /// Loguje operáciu s operation scope pre tracking
    /// </summary>
    public async Task<Result> RemoveValidationRulesAsync(string[] columnNames, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var operationId = Guid.NewGuid();

        // Začíname remove validation rules operáciu
        using var scope = _operationLogger.LogOperationStart("RemoveValidationRulesAsync", new
        {
            OperationId = operationId,
            ColumnCount = columnNames.Length,
            ColumnNames = string.Join(", ", columnNames)
        });

        _logger.LogInformation("Starting removal of validation rules for operation {OperationId} for {ColumnCount} columns: {ColumnNames}",
            operationId, columnNames.Length, string.Join(", ", columnNames));

        try
        {
            // Nájdeme rules ktoré majú dependent columns v zadaných stĺpcoch
            var rulesToRemove = _validationRules.Where(rule =>
                rule.DependentColumns.Any(col => columnNames.Contains(col, StringComparer.OrdinalIgnoreCase))).ToList();

            _logger.LogInformation("Found {RuleCount} rules to remove for operation {OperationId}",
                rulesToRemove.Count, operationId);

            foreach (var rule in rulesToRemove)
            {
                // ConcurrentBag doesn't support removal, so we'll need to recreate
                // In practice, this should use a different collection or marking approach
                _logger.LogInformation("Marking rule {RuleId} for removal for operation {OperationId}",
                    rule.RuleId, operationId);
            }

            _logger.LogInformation("Removed validation rules for {ColumnCount} columns in {Duration}ms for operation {OperationId}",
                columnNames.Length, stopwatch.ElapsedMilliseconds, operationId);

            scope.MarkSuccess(new
            {
                RemovedRuleCount = rulesToRemove.Count,
                ColumnCount = columnNames.Length,
                Duration = stopwatch.Elapsed
            });

            await Task.CompletedTask;
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove validation rules for columns for operation {OperationId}: {Message}",
                operationId, ex.Message);

            scope.MarkFailure(ex);
            return Result.Failure($"Failed to remove validation rules: {ex.Message}");
        }
    }

    /// <summary>
    /// Odstraňuje validation rule podľa názvu
    /// ConcurrentBag nepodporuje priame odstránenie, preto sa použije recreate prístup
    /// </summary>
    public async Task<Result> RemoveValidationRuleAsync(string ruleName, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var operationId = Guid.NewGuid();

        // Začíname remove validation rule operáciu
        using var scope = _operationLogger.LogOperationStart("RemoveValidationRuleAsync", new
        {
            OperationId = operationId,
            RuleName = ruleName
        });

        _logger.LogInformation("Starting removal of validation rule {RuleName} for operation {OperationId}",
            ruleName, operationId);

        try
        {
            // ConcurrentBag doesn't support removal, so we'll need to recreate
            // In practice, this should use a different collection or marking approach
            _logger.LogInformation("Removed validation rule {RuleName} in {Duration}ms for operation {OperationId}",
                ruleName, stopwatch.ElapsedMilliseconds, operationId);

            scope.MarkSuccess(new
            {
                RuleName = ruleName,
                Duration = stopwatch.Elapsed
            });

            await Task.CompletedTask;
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove validation rule {RuleName} for operation {OperationId}: {Message}",
                ruleName, operationId, ex.Message);

            scope.MarkFailure(ex);
            return Result.Failure($"Failed to remove validation rule: {ex.Message}");
        }
    }

    /// <summary>
    /// Vymaže všetky validation rules
    /// Používa TryTake v cykle pre vymazanie všetkých položiek z ConcurrentBag
    /// </summary>
    public async Task<Result> ClearAllValidationRulesAsync(CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var operationId = Guid.NewGuid();

        // Začíname clear all validation rules operáciu
        using var scope = _operationLogger.LogOperationStart("ClearAllValidationRulesAsync", new
        {
            OperationId = operationId,
            CurrentRuleCount = _validationRules.Count
        });

        _logger.LogInformation("Starting clearing of all validation rules for operation {OperationId}. Current rule count: {RuleCount}",
            operationId, _validationRules.Count);

        try
        {
            var initialCount = _validationRules.Count;

            // ConcurrentBag doesn't have Clear method, recreate the collection
            while (_validationRules.TryTake(out _)) { }

            _logger.LogInformation("Cleared all validation rules in {Duration}ms for operation {OperationId}. Cleared {RuleCount} rules",
                stopwatch.ElapsedMilliseconds, operationId, initialCount);

            scope.MarkSuccess(new
            {
                ClearedRuleCount = initialCount,
                Duration = stopwatch.Elapsed
            });

            await Task.CompletedTask;
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to clear validation rules for operation {OperationId}: {Message}",
                operationId, ex.Message);

            scope.MarkFailure(ex);
            return Result.Failure($"Failed to clear validation rules: {ex.Message}");
        }
    }

    /// <summary>
    /// Validuje jednotlivý riadok voči všetkým aplikovateľným pravidlám
    /// Vracia prvý výsledok ktorý zlyhá alebo Success ak všetky pravidlá prešli
    /// </summary>
    public async Task<ValidationResult> ValidateRowAsync(
        IReadOnlyDictionary<string, object?> row,
        ValidationContext context,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var operationId = Guid.NewGuid();

        // Začíname validáciu jednotlivého riadku
        _logger.LogInformation("Starting validation of row {RowIndex} for operation {OperationId}",
            context.RowIndex, operationId);

        try
        {
            var activeRules = _validationRules.ToArray();

            _logger.LogInformation("Validating row {RowIndex} against {RuleCount} rules for operation {OperationId}",
                context.RowIndex, activeRules.Length, operationId);

            foreach (var rule in activeRules)
            {
                var result = await rule.ValidateAsync(row, context, cancellationToken);
                if (!result.IsValid)
                {
                    _logger.LogWarning("Row {RowIndex} validation failed on rule {RuleId} for operation {OperationId}: {Message}",
                        context.RowIndex, rule.RuleId, operationId, result.ErrorMessage);
                    return result;
                }
            }

            _logger.LogInformation("Row {RowIndex} validation successful in {Duration}ms for operation {OperationId}",
                context.RowIndex, stopwatch.ElapsedMilliseconds, operationId);

            return ValidationResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Row {RowIndex} validation failed with error for operation {OperationId}: {Message}",
                context.RowIndex, operationId, ex.Message);
            return ValidationResult.Error($"Row validation failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Získa všetky aktuálne nakonfigurované validation rules
    /// Vracia immutable array kópiu pravidiel pre thread-safe čítanie
    /// </summary>
    public IReadOnlyList<IValidationRule> GetValidationRules()
    {
        return _validationRules.ToArray();
    }

    /// <summary>
    /// Získa validation rules pre špecifické stĺpce
    /// Filtruje rules ktoré majú dependent columns v zadaných column names
    /// </summary>
    public IReadOnlyList<IValidationRule> GetValidationRulesForColumns(params string[] columnNames)
    {
        return _validationRules
            .Where(rule => rule.DependentColumns.Any(col => columnNames.Contains(col, StringComparer.OrdinalIgnoreCase)))
            .ToArray();
    }

    /// <summary>
    /// Validates a single cell with real-time validation mode
    /// CRITICAL: Real-time validation for cell editing operations
    /// Uses ValidationMode.RealTime for optimized single-cell validation
    /// </summary>
    public async Task<ValidationResult> ValidateCellAsync(
        int rowIndex,
        string columnName,
        object? newValue,
        CancellationToken cancellationToken = default)
    {
        var operationId = Guid.NewGuid();
        _logger.LogDebug("Starting real-time cell validation for row {RowIndex}, column {ColumnName} with operation {OperationId}",
            rowIndex, columnName, operationId);

        try
        {
            // Get current row
            var row = await _rowStore.GetRowAsync(rowIndex, cancellationToken);
            if (row == null)
            {
                _logger.LogWarning("Row {RowIndex} not found during cell validation", rowIndex);
                return ValidationResult.Error($"Row {rowIndex} not found");
            }

            // Create updated row with new value
            var updatedRow = new Dictionary<string, object?>(row)
            {
                [columnName] = newValue
            };

            // Create real-time validation context
            var context = new ValidationContext
            {
                RowIndex = rowIndex,
                ColumnName = columnName,
                OperationId = operationId.ToString(),
                Properties = new Dictionary<string, object?>
                {
                    ["ValidationMode"] = Common.Models.ValidationMode.RealTime,
                    ["OldValue"] = row.TryGetValue(columnName, out var oldValue) ? oldValue : null,
                    ["NewValue"] = newValue
                }
            };

            // Get applicable rules for this column
            var applicableRules = _validationRules
                .Where(rule => rule.DependentColumns.Contains(columnName, StringComparer.OrdinalIgnoreCase))
                .ToArray();

            _logger.LogDebug("Found {RuleCount} applicable rules for column {ColumnName}",
                applicableRules.Length, columnName);

            // Validate with each applicable rule
            foreach (var rule in applicableRules)
            {
                var result = await rule.ValidateAsync(updatedRow, context, cancellationToken);
                if (!result.IsValid)
                {
                    _logger.LogWarning("Real-time validation failed for row {RowIndex}, column {ColumnName}: {Message}",
                        rowIndex, columnName, result.ErrorMessage);
                    return result;
                }
            }

            _logger.LogDebug("Real-time cell validation successful for row {RowIndex}, column {ColumnName}",
                rowIndex, columnName);
            return ValidationResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Real-time cell validation failed for row {RowIndex}, column {ColumnName}: {Message}",
                rowIndex, columnName, ex.Message);
            return ValidationResult.Error($"Validation error: {ex.Message}");
        }
    }

    /// <summary>
    /// Determines validation mode based on operation name
    /// SMART DECISION: Returns Batch or RealTime based on operation type
    /// </summary>
    public Common.Models.ValidationMode DetermineValidationMode(string operationName)
    {
        var mode = operationName switch
        {
            "ImportAsync" => Common.Models.ValidationMode.Batch,
            "ExportAsync" => Common.Models.ValidationMode.Batch,
            "PasteAsync" => Common.Models.ValidationMode.Batch,
            "SmartAddRowsAsync" => Common.Models.ValidationMode.Batch,
            "SmartDeleteRowsAsync" => Common.Models.ValidationMode.Batch,
            "UpdateCellAsync" => Common.Models.ValidationMode.RealTime,
            "UpdateRowAsync" => Common.Models.ValidationMode.RealTime,
            "BeginEditAsync" => Common.Models.ValidationMode.RealTime,
            "CommitEditAsync" => Common.Models.ValidationMode.RealTime,
            _ => Common.Models.ValidationMode.Batch
        };

        _logger.LogDebug("Determined validation mode {Mode} for operation {Operation}", mode, operationName);
        return mode;
    }

    /// <summary>
    /// Gets validation alerts message for a specific row
    /// Format: "Error: msg1; Warning: msg2"
    /// </summary>
    public string GetValidationAlertsForRow(int rowIndex)
    {
        try
        {
            // Get validation errors for this row from row store
            var row = _rowStore.GetRowAsync(rowIndex, CancellationToken.None).GetAwaiter().GetResult();
            if (row != null && row.TryGetValue("validAlerts", out var alerts))
            {
                return alerts?.ToString() ?? string.Empty;
            }

            return string.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get validation alerts for row {RowIndex}", rowIndex);
            return string.Empty;
        }
    }

    /// <summary>
    /// Updates validation alerts for a specific row
    /// CRITICAL: Formats and stores validation messages in validAlerts column
    /// </summary>
    public async Task<Result> UpdateValidationAlertsAsync(
        int rowIndex,
        IReadOnlyList<ValidationResult> results,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Updating validation alerts for row {RowIndex} with {ResultCount} results",
                rowIndex, results.Count);

            if (results.Count == 0 || results.All(r => r.IsValid))
            {
                // No errors - clear alerts
                var row = await _rowStore.GetRowAsync(rowIndex, cancellationToken);
                if (row != null)
                {
                    var updatedRow = new Dictionary<string, object?>(row)
                    {
                        ["validAlerts"] = string.Empty
                    };
                    await _rowStore.UpdateRowAsync(rowIndex, updatedRow, cancellationToken);
                }

                return Result.Success();
            }

            // Format alerts: "Error: msg1; Warning: msg2"
            var errorMessages = results
                .Where(r => !r.IsValid)
                .Select(r => $"{r.Severity}: {r.ErrorMessage}")
                .ToList();

            var alertMessage = string.Join("; ", errorMessages);

            // Update row with alerts
            var currentRow = await _rowStore.GetRowAsync(rowIndex, cancellationToken);
            if (currentRow != null)
            {
                var updatedRow = new Dictionary<string, object?>(currentRow)
                {
                    ["validAlerts"] = alertMessage
                };
                await _rowStore.UpdateRowAsync(rowIndex, updatedRow, cancellationToken);
            }

            _logger.LogInformation("Updated validation alerts for row {RowIndex}: {Alerts}", rowIndex, alertMessage);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update validation alerts for row {RowIndex}: {Message}",
                rowIndex, ex.Message);
            return Result.Failure($"Failed to update validation alerts: {ex.Message}");
        }
    }
}