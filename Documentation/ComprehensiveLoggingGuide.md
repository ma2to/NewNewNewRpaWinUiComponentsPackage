# Komplexný Logging Guide - Unified Debug & Release

## 🎯 Logging Philosophy

**CRITICAL: Jednotný logging pre DEBUG aj RELEASE**
- ❌ **NIE Debug level** - nepoužíva sa vôbec
- ✅ **Information** - Normálne operácie, úspechy, progress
- ✅ **Warning** - Problémy ktoré nejsú kritické ale mali by sa riešiť
- ✅ **Error** - Chyby ktoré zabránili dokončeniu operácie
- ✅ **Critical** - Fatálne chyby systému, data corruption, security issues

## 📋 Štruktúra Loggingu

### 1. Začiatok Operácie (Information)
```csharp
// Vytvoríme operation scope pre automatické tracking
using var scope = _operationLogger.LogOperationStart("OperationName", new
{
    OperationId = operationId,
    Parameter1 = value1,
    Parameter2 = value2,
    CorrelationId = correlationId
});

_logger.LogInformation("Začíname operáciu {OperationName} s parametrami: {@Parameters}",
    operationName, new { param1, param2 });
```

### 2. Validácia (Information/Warning)
```csharp
// Začiatok validácie
_logger.LogInformation("Validujeme vstupné dáta pre operáciu {OperationId}", operationId);

var validationResult = await ValidateAsync(data);
if (!validationResult.IsValid)
{
    // Validácia zlyhala - WARNING alebo ERROR podľa závažnosti
    _logger.LogWarning("Validácia zlyhala pre operáciu {OperationId}: {Errors}",
        operationId, string.Join(", ", validationResult.Errors));

    scope.MarkFailure(new ValidationException("Validation failed"));
    return FailureResult(validationResult.Errors);
}

_logger.LogInformation("Validácia úspešná pre operáciu {OperationId}", operationId);
```

### 3. Spracovanie Dát (Information)
```csharp
// Začiatok spracovania
_logger.LogInformation("Spracovávame {RowCount} riadkov pre operáciu {OperationId}",
    data.Count, operationId);

// Spracovanie v dávkach
var batchSize = _options.BatchSize;
for (int i = 0; i < data.Count; i += batchSize)
{
    var batch = data.Skip(i).Take(batchSize).ToList();

    _logger.LogInformation("Spracovávame dávku {BatchNumber}/{TotalBatches}: {BatchSize} riadkov",
        (i / batchSize) + 1, (data.Count + batchSize - 1) / batchSize, batch.Count);

    await ProcessBatchAsync(batch, cancellationToken);
}

_logger.LogInformation("Spracovanie dokončené: {RowCount} riadkov za {Duration}ms",
    data.Count, stopwatch.ElapsedMilliseconds);
```

### 4. Problémy a Chyby (Warning/Error)
```csharp
// Warning - nie je kritické ale malo by sa riešiť
if (someConditionThatIsNotIdeal)
{
    _logger.LogWarning("Nájdený potenciálny problém v operácii {OperationId}: {Problem}",
        operationId, problemDescription);
    scope.MarkWarning(problemDescription);
}

// Error - operácia nemôže pokračovať
if (!result.IsSuccess)
{
    _logger.LogError("Operácia {OperationId} zlyhala: {Error}",
        operationId, result.ErrorMessage);
    scope.MarkFailure(new InvalidOperationException(result.ErrorMessage));
    return FailureResult(result.ErrorMessage);
}
```

### 5. Metriky a Performance (Information)
```csharp
// Zalogujeme performance metriky
_operationLogger.LogPerformanceMetrics("DataProcessing", new
{
    RowsProcessed = processedRows.Count,
    Duration = stopwatch.Elapsed,
    MemoryUsed = GC.GetTotalMemory(false),
    CacheHitRate = cacheHits / (double)totalAccesses
});

_logger.LogInformation("Performance metriky pre operáciu {OperationId}: " +
    "Riadkov={RowCount}, Čas={Duration}ms, Pamäť={Memory}MB, Cache hit rate={CacheRate:P}",
    operationId, rowCount, durationMs, memoryMB, cacheHitRate);
```

### 6. Úspešné Dokončenie (Information)
```csharp
// Zalogujeme metriky operácie
_operationLogger.LogImportOperation(
    importType: "DataTable",
    totalRows: data.Count,
    importedRows: processedRows.Count,
    duration: stopwatch.Elapsed);

_logger.LogInformation("Operácia {OperationId} dokončená úspešne za {Duration}ms: " +
    "{ProcessedRows} riadkov spracovaných",
    operationId, stopwatch.ElapsedMilliseconds, processedRows.Count);

// Označíme scope ako úspešný
scope.MarkSuccess(new
{
    ProcessedRows = processedRows.Count,
    Duration = stopwatch.Elapsed,
    Success = true
});
```

### 7. Exception Handling (Error/Critical)
```csharp
catch (OperationCanceledException ex)
{
    // Operácia zrušená - nie je to chyba, len info
    _logger.LogInformation("Operácia {OperationId} bola zrušená používateľom", operationId);
    scope.MarkFailure(ex);
    return CancelledResult();
}
catch (ValidationException ex)
{
    // Validačná chyba - ERROR level
    _logger.LogError(ex, "Validačná chyba v operácii {OperationId}: {Message}",
        operationId, ex.Message);
    scope.MarkFailure(ex);
    return ValidationFailureResult(ex.Message);
}
catch (Exception ex)
{
    // Neočakávaná chyba - CRITICAL level
    _logger.LogCritical(ex, "KRITICKÁ CHYBA v operácii {OperationId}: {Message}. " +
        "Stack trace: {StackTrace}",
        operationId, ex.Message, ex.StackTrace);
    scope.MarkFailure(ex);
    return CriticalFailureResult(ex);
}
```

## 🔄 Kompletný Príklad - Import Service

```csharp
public async Task<InternalImportResult> ImportAsync(
    InternalImportDataCommand command,
    CancellationToken cancellationToken = default)
{
    var stopwatch = Stopwatch.StartNew();
    var operationId = Guid.NewGuid();

    // 1. Začiatok operácie s operation scope
    using var scope = _operationLogger.LogOperationStart("ImportAsync", new
    {
        OperationId = operationId,
        Mode = command.Mode,
        DataType = command.DataTableData != null ? "DataTable" : "Dictionary",
        CorrelationId = command.CorrelationId
    });

    _logger.LogInformation("🚀 Začíname import operáciu {OperationId} s režimom {ImportMode} " +
        "pre typ dát {DataType}",
        operationId, command.Mode,
        command.DictionaryData?.GetType().Name ?? command.DataTableData?.GetType().Name ?? "null");

    try
    {
        // 2. Validácia vstupných dát
        _logger.LogInformation("📋 Validujeme import konfiguráciu pre operáciu {OperationId}", operationId);

        var validationResult = await ValidateImportDataAsync(command, cancellationToken);
        if (!validationResult.IsValid)
        {
            // Validácia zlyhala - zalogujeme WARNING s detailami
            _logger.LogWarning("⚠️ Validácia importu zlyhala pre operáciu {OperationId}: {Errors}",
                operationId, string.Join(", ", validationResult.ValidationErrors));

            scope.MarkFailure(new InvalidOperationException(
                $"Import validation failed: {string.Join(", ", validationResult.ValidationErrors)}"));

            return InternalImportResult.Failure(validationResult.ValidationErrors,
                stopwatch.Elapsed, command.Mode, command.CorrelationId);
        }

        _logger.LogInformation("✅ Validácia úspešná, začíname spracovanie dát pre operáciu {OperationId}",
            operationId);

        // 3. Spracovanie dát v dávkach
        _logger.LogInformation("⚙️ Spracovávame import dáta podľa typu - PODPORUJEME len DataTable a Dictionary");

        var processedRows = await ProcessImportDataAsync(command, operationId, cancellationToken);

        _logger.LogInformation("✅ Spracované {RowCount} riadkov za {Duration}ms pre operáciu {OperationId}",
            processedRows.Count, stopwatch.ElapsedMilliseconds, operationId);

        // 4. Ukladanie dát
        _logger.LogInformation("💾 Ukladáme {RowCount} riadkov s režimom {Mode} pre operáciu {OperationId}",
            processedRows.Count, command.Mode, operationId);

        var storeResult = await StoreImportedDataAsync(processedRows, command.Mode,
            operationId, cancellationToken);

        if (!storeResult.IsSuccess)
        {
            // Ukladanie zlyhalo - ERROR level
            _logger.LogError("❌ Ukladanie importovaných dát zlyhalo pre operáciu {OperationId}: {Error}",
                operationId, storeResult.ErrorMessage);

            scope.MarkFailure(new InvalidOperationException($"Storage failed: {storeResult.ErrorMessage}"));

            return InternalImportResult.Failure(new[] { storeResult.ErrorMessage ?? "Storage failed" },
                stopwatch.Elapsed, command.Mode, command.CorrelationId);
        }

        _logger.LogInformation("✅ Dáta úspešne uložené, spúšťame post-import validáciu pre operáciu {OperationId}",
            operationId);

        // 5. Post-import validácia
        var postImportValidation = await _validationService.AreAllNonEmptyRowsValidAsync(false, cancellationToken);

        if (!postImportValidation.IsSuccess)
        {
            // Post-import validácia našla problémy - WARNING (nie je kritické, data sú uložené)
            _logger.LogWarning("⚠️ Post-import validácia našla problémy pre operáciu {OperationId}: {Error}",
                operationId, postImportValidation.ErrorMessage);

            scope.MarkWarning($"Post-import validation found issues: {postImportValidation.ErrorMessage}");
        }
        else
        {
            _logger.LogInformation("✅ Post-import validácia úspešná pre operáciu {OperationId}", operationId);
        }

        // 6. Metriky importu
        _operationLogger.LogImportOperation(
            importType: command.DataTableData != null ? "DataTable" : "Dictionary",
            totalRows: processedRows.Count,
            importedRows: processedRows.Count,
            duration: stopwatch.Elapsed);

        _logger.LogInformation("🎉 Import operácia {OperationId} dokončená úspešne za {Duration}ms, " +
            "importovaných {RowCount} riadkov",
            operationId, stopwatch.ElapsedMilliseconds, processedRows.Count);

        // 7. Označíme scope ako úspešný
        scope.MarkSuccess(new
        {
            ImportedRows = processedRows.Count,
            Duration = stopwatch.Elapsed,
            Mode = command.Mode
        });

        return InternalImportResult.CreateSuccess(processedRows.Count, processedRows.Count,
            stopwatch.Elapsed, command.Mode, command.CorrelationId);
    }
    catch (OperationCanceledException ex)
    {
        // Operácia zrušená - INFO level (nie je to chyba)
        _logger.LogInformation("🛑 Import operácia {OperationId} bola zrušená používateľom", operationId);
        scope.MarkFailure(ex);

        return InternalImportResult.Failure(new[] { "Operation was cancelled" },
            stopwatch.Elapsed, command.Mode, command.CorrelationId);
    }
    catch (Exception ex)
    {
        // Neočakávaná chyba - CRITICAL level
        _logger.LogCritical(ex, "💥 KRITICKÁ CHYBA: Import operácia {OperationId} zlyhala s " +
            "neočakávanou chybou: {Message}. Stack trace: {StackTrace}",
            operationId, ex.Message, ex.StackTrace);

        scope.MarkFailure(ex);

        return InternalImportResult.Failure(new[] { $"Import failed: {ex.Message}" },
            stopwatch.Elapsed, command.Mode, command.CorrelationId);
    }
}
```

## 📊 Čo sa má logovať v každej službe

### ImportService ✅ (Implementované)
- ✅ Začiatok importu s parametrami
- ✅ Validácia konfigurácie
- ✅ Spracovanie dát (počet riadkov, čas)
- ✅ Ukladanie dát (režim, počet)
- ✅ Post-import validácia
- ✅ Import metriky
- ✅ Errors a warnings
- ✅ Úspešné dokončenie s metrikami

### ExportService 📝 (TODO)
- Začiatok exportu (formát, filters)
- Pre-export validácia
- Filtrovanie dát (onlyChecked, onlyFiltered)
- Konverzia formátu (DataTable/Dictionary)
- Export metriky (riadky exportované, čas)
- Problémy s konverziou
- Úspešné dokončenie

### ValidationService 📝 (TODO)
- Začiatok validácie (počet rules, rows)
- Vykonávanie každého rule (názov, typ, výsledok, čas)
- Async validácia s timeout tracking
- Duplikáty detection (počet groups, duplicates)
- Automatická revalidácia (affected rules, čas)
- Validation strategy (parallel/sequential, dôvod)
- Validation summary (valid/invalid rows, čas)

### FilterService 📝 (TODO)
- Začiatok filtrovania (filter typ, počet filtrov)
- Filter combination (logic operator, short-circuit)
- Business rule execution (názov, success, čas)
- Custom logic execution
- Filter validation
- Matching rows (total vs matched)
- Filter performance metrics

### Ostatné služby 📝 (TODO)
- CopyPaste: source/dest, počet buniek, validácia, čas
- Selection: rows selected/deselected, batch operations
- Column: add/remove/reorder operations, column count
- AutoRowHeight: measurements, cache hits, calculated heights
- RowNumber: generation, updates, performance
- Sort: column, direction, rows sorted, čas

## 🎯 Logging Levels Usage

### Information (Normálne operácie)
- Začiatok a koniec operácií
- Úspešné dokončenia
- Progress updates (dávky, kroky)
- Performance metriky
- State changes

### Warning (Problémy, nie kritické)
- Validačné chyby
- Fallback scenarios
- Performance degradation
- Partial failures
- Data quality issues

### Error (Chyby zabraňujúce dokončeniu)
- Operation failures
- Storage errors
- Conversion errors
- Invalid state

### Critical (Fatálne chyby systému)
- Unhandled exceptions
- System failures
- Data corruption
- Security breaches
- Infrastructure failures

## ✅ Výhody Tohto Prístupu

1. **Unified Debug & Release** - Rovnaké logy v oboch režimoch
2. **Comprehensive Tracking** - Vidíme čo sa dialo, čo malo/nemalo sa diať
3. **Performance Insights** - Presné meranie času, pamäte, throughput
4. **Error Diagnostics** - Presná lokalizácia a kontext chýb
5. **Structured Logging** - Queryable data pre analýzu
6. **Correlation IDs** - Tracking naprieč systémom
7. **Slovak Comments** - Ľahké pochopenie pre vývojárov
8. **Automatic Timing** - RAII pattern s using statement
9. **Optional Logging** - Funguje aj bez loggera (null pattern)
10. **Production Ready** - Bezpečné a výkonné pre produkčné prostredie
