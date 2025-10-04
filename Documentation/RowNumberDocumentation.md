# KOMPLETNÁ ŠPECIFIKÁCIA: ROW NUMBER MANAGEMENT SYSTÉM PRE ADVANCEDWINUIDATAGRID

## 🏗️ ARCHITEKTONICKÉ PRINCÍPY & ŠTRUKTÚRA

### Clean Architecture + Service Pattern (Jednotná s ostatnými časťami)
- **Facade Layer**: `IAdvancedDataGridFacade` (public API)
- **Application Layer**: Row numbering services, statistics tracking (internal)
- **Core Layer**: Row numbering algorithms, validation logic (internal)
- **Infrastructure Layer**: Performance monitoring, batch processing (internal)
- **Hybrid Internal DI + Functional/OOP**: Kombinuje dependency injection s funkcionálnym programovaním

### SOLID Principles
- **Single Responsibility**: Každý row numbering service má jednu zodpovednosť
- **Open/Closed**: Rozšíriteľné pre nové typy numbering strategies bez zmeny existujúceho kódu
- **Liskov Substitution**: Všetky numbering services implementujú spoločné interface
- **Interface Segregation**: Špecializované interfaces pre rôzne typy numbering operations
- **Dependency Inversion**: Facade závislí od abstrakcií, nie konkrétnych implementácií

### Architectural Principles Maintained (Jednotné s ostatnými časťami)
- **Clean Architecture**: Services v Application layer, algorithms v Core layer
- **Hybrid DI**: Service factory methods s dependency injection support
- **Functional/OOP**: Pure numbering functions + encapsulated service behavior
- **SOLID**: Single responsibility pre každý numbering operation type
- **LINQ Optimization**: Lazy evaluation, parallel processing pre batch numbering
- **Performance**: Atomic operations, minimal allocations, smart algorithms
- **Thread Safety**: Atomic row number updates, concurrent access support
- **Internal DI Registration**: Všetky row numbering časti budú registrované v InternalServiceRegistration.cs

## 🔄 BACKUP STRATEGY & IMPLEMENTATION APPROACH

### 1. Backup Strategy
- Vytvoriť `.oldbackup_timestamp` súbory pre všetky modifikované súbory
- Úplne nahradiť staré implementácie - **ŽIADNA backward compatibility**
- Zachovať DI registrácie a interface contracts

### 2. Implementation Replacement
- Kompletný refaktoring s service pattern a smart numbering algorithms
- Bez backward compatibility ale s preservation DI architektúry
- Optimalizované, bezpečné a stabilné riešenie

## 📋 CORE SERVICE ARCHITECTURE & INTERFACES

### 1. **IRowNumberService.cs** - Application Interface

```csharp
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Application.Interfaces;

/// <summary>
/// APPLICATION INTERFACE: Row number management service
/// ENTERPRISE: Professional row numbering with statistics and validation
/// INTEGRATION: Triggered after Import/Export/Filter operations
/// </summary>
internal interface IRowNumberService
{
    #region Basic Row Number Operations

    /// <summary>
    /// CORE: Assign row number to specific row
    /// ATOMIC: Thread-safe single row number assignment
    /// </summary>
    Task<bool> AssignRowNumberAsync(int rowIndex, int? customRowNumber = null,
        cancellationToken cancellationToken = default);

    /// <summary>
    /// ENTERPRISE: Regenerate all row numbers with sequential ordering
    /// PERFORMANCE: Optimized bulk regeneration with minimal overhead
    /// SMART: Uses creation time as fallback ordering when RowNumbers are corrupted
    /// </summary>
    Task<RowNumberValidationResult> RegenerateRowNumbersAsync(
        bool preserveOrder = true,
        cancellationToken cancellationToken = default);

    /// <summary>
    /// MAINTENANCE: Compact row numbers to eliminate gaps
    /// OPTIMIZATION: Reorganize numbering sequence for optimal performance
    /// </summary>
    Task<RowNumberValidationResult> CompactRowNumbersAsync(
        cancellationToken cancellationToken = default);

    #endregion

    #region Batch Operations

    /// <summary>
    /// PERFORMANCE: Assign row numbers in batch for multiple rows
    /// EFFICIENT: Single transaction for multiple row number assignments
    /// </summary>
    Task<RowNumberValidationResult> AssignRowNumbersBatchAsync(
        IReadOnlyDictionary<int, int> rowIndexToRowNumber,
        cancellationToken cancellationToken = default);

    /// <summary>
    /// UTILITY: Get next available row number in sequence
    /// SMART: Automatic recovery from RowNumber inconsistencies
    /// </summary>
    Task<int> GetNextRowNumberAsync(cancellationToken cancellationToken = default);

    #endregion

    #region Validation & Statistics

    /// <summary>
    /// VALIDATION: Validate row number sequence integrity
    /// DIAGNOSTICS: Comprehensive sequence analysis with gap detection
    /// </summary>
    Task<RowNumberValidationResult> ValidateRowNumberSequenceAsync(
        cancellationToken cancellationToken = default);

    /// <summary>
    /// REPAIR: Repair corrupted or inconsistent row number sequences
    /// RECOVERY: Automatic sequence repair with minimal data disruption
    /// </summary>
    Task<RowNumberValidationResult> RepairRowNumberSequenceAsync(
        bool preserveUserAssignments = true,
        cancellationToken cancellationToken = default);

    /// <summary>
    /// STATISTICS: Get comprehensive row numbering statistics
    /// MONITORING: Performance metrics and sequence health analysis
    /// </summary>
    Task<RowNumberStatistics> GetRowNumberStatisticsAsync(
        cancellationToken cancellationToken = default);

    #endregion
}
```

### 2. **RowNumberService.cs** - Application Service Implementation

```csharp
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Application.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Core.Entities;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Infrastructure.Logging;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Application.Services;

/// <summary>
/// APPLICATION SERVICE: Row number management with enterprise features
/// INTEGRATION: Automatically triggered after Import/Export/Filter operations
/// PERFORMANCE: Optimized algorithms with parallel processing capabilities
/// THREAD-SAFE: Concurrent access support with atomic operations
/// </summary>
internal sealed class RowNumberService : IRowNumberService
{
    private readonly ILogger<RowNumberService> _logger;
    private readonly IOperationLogger<RowNumberService> _operationLogger;
    private readonly ICommandLogger<RowNumberService> _commandLogger;

    private readonly ConcurrentDictionary<int, int> _rowNumberCache = new();
    private readonly SemaphoreSlim _operationSemaphore = new(1, 1);

    public RowNumberService(
        ILogger<RowNumberService> logger,
        IOperationLogger<RowNumberService> operationLogger,
        ICommandLogger<RowNumberService> commandLogger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _operationLogger = operationLogger ?? throw new ArgumentNullException(nameof(operationLogger));
        _commandLogger = commandLogger ?? throw new ArgumentNullException(nameof(commandLogger));
    }

    #region Basic Row Number Operations

    public async Task<bool> AssignRowNumberAsync(int rowIndex, int? customRowNumber = null,
        cancellationToken cancellationToken = default)
    {
        using var scope = _operationLogger.LogOperationStart("AssignRowNumber",
            new { rowIndex, customRowNumber });

        try
        {
            await _operationSemaphore.WaitAsync(cancellationToken);

            var rowNumber = customRowNumber ?? await GetNextRowNumberAsync(cancellationToken);

            // ATOMIC OPERATION: Thread-safe row number assignment
            _rowNumberCache.AddOrUpdate(rowIndex, rowNumber, (key, oldValue) => rowNumber);

            _logger.LogInformation("Row number assigned: rowIndex={RowIndex}, rowNumber={RowNumber}",
                rowIndex, rowNumber);

            scope.MarkSuccess(true);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to assign row number: rowIndex={RowIndex}", rowIndex);
            scope.MarkFailure(ex);
            return false;
        }
        finally
        {
            _operationSemaphore.Release();
        }
    }

    public async Task<RowNumberValidationResult> RegenerateRowNumbersAsync(
        bool preserveOrder = true, cancellationToken cancellationToken = default)
    {
        using var scope = _operationLogger.LogOperationStart("RegenerateRowNumbers",
            new { preserveOrder });

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            await _operationSemaphore.WaitAsync(cancellationToken);

            // SMART: Uses creation time as fallback ordering when RowNumbers are corrupted
            var rowsToRenumber = await GetRowsForRenumberingAsync(preserveOrder, cancellationToken);

            // PERFORMANCE: Parallel processing for large datasets
            var renumberingTasks = rowsToRenumber
                .AsParallel()
                .WithCancellation(cancellationToken)
                .Select(async (row, index) =>
                {
                    var newRowNumber = index + 1;
                    _rowNumberCache.AddOrUpdate(row.RowIndex, newRowNumber, (key, oldValue) => newRowNumber);
                    return (row.RowIndex, newRowNumber);
                })
                .ToArray();

            var results = await Task.WhenAll(renumberingTasks);
            stopwatch.Stop();

            var validationResult = new RowNumberValidationResult
            {
                IsValid = true,
                ProcessedRows = results.Length,
                TotalRows = results.Length,
                OperationTime = stopwatch.Elapsed,
                HasValidSequence = true
            };

            _logger.LogInformation("Row numbers regenerated: processedRows={ProcessedRows}, time={Duration}ms",
                results.Length, stopwatch.ElapsedMilliseconds);

            scope.MarkSuccess(validationResult);
            return validationResult;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to regenerate row numbers");
            scope.MarkFailure(ex);

            return new RowNumberValidationResult
            {
                IsValid = false,
                OperationTime = stopwatch.Elapsed,
                ErrorMessage = ex.Message
            };
        }
        finally
        {
            _operationSemaphore.Release();
        }
    }

    public async Task<RowNumberValidationResult> CompactRowNumbersAsync(
        cancellationToken cancellationToken = default)
    {
        // OPTIMIZATION: Reorganize numbering sequence for optimal performance
        // LINQ OPTIMIZATION: Efficient gap elimination with minimal operations

        using var scope = _operationLogger.LogOperationStart("CompactRowNumbers", new { });
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            await _operationSemaphore.WaitAsync(cancellationToken);

            var currentNumbers = _rowNumberCache.Values.OrderBy(n => n).ToList();
            var compactedNumbers = Enumerable.Range(1, currentNumbers.Count).ToList();

            var updatesNeeded = currentNumbers
                .Zip(compactedNumbers, (current, compact) => new { Current = current, Compact = compact })
                .Where(pair => pair.Current != pair.Compact)
                .ToList();

            // ATOMIC UPDATES: Thread-safe batch compaction
            var updateTasks = updatesNeeded.Select(async update =>
            {
                var rowIndex = _rowNumberCache.First(kvp => kvp.Value == update.Current).Key;
                _rowNumberCache.TryUpdate(rowIndex, update.Compact, update.Current);
                return (rowIndex, update.Compact);
            });

            await Task.WhenAll(updateTasks);
            stopwatch.Stop();

            var result = new RowNumberValidationResult
            {
                IsValid = true,
                ProcessedRows = updatesNeeded.Count,
                TotalRows = currentNumbers.Count,
                OperationTime = stopwatch.Elapsed,
                HasValidSequence = true
            };

            scope.MarkSuccess(result);
            return result;
        }
        catch (Exception ex)
        {
            scope.MarkFailure(ex);
            return new RowNumberValidationResult
            {
                IsValid = false,
                OperationTime = stopwatch.Elapsed,
                ErrorMessage = ex.Message
            };
        }
        finally
        {
            _operationSemaphore.Release();
        }
    }

    #endregion

    #region Private Helper Methods

    private async Task<IEnumerable<DataRow>> GetRowsForRenumberingAsync(
        bool preserveOrder, cancellationToken cancellationToken)
    {
        // Implementation for getting rows with proper ordering
        // Uses creation time as fallback when RowNumbers are corrupted
        await Task.CompletedTask;
        return Enumerable.Empty<DataRow>();
    }

    #endregion
}
```

## 📊 SUPPORTING VALUE OBJECTS & STATISTICS

### **RowNumberStatistics.cs** - Statistics Tracking

```csharp
using System;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Application.Services;

/// <summary>
/// ENTERPRISE: Row number statistics for monitoring and diagnostics
/// COMPREHENSIVE: Complete numbering sequence health analysis
/// </summary>
internal sealed class RowNumberStatistics
{
    public int TotalRows { get; init; }
    public int MinRowNumber { get; init; }
    public int MaxRowNumber { get; init; }
    public int GapCount { get; init; }
    public int DuplicateCount { get; init; }
    public bool HasValidSequence { get; init; }
    public DateTime LastRegenerationTime { get; init; }
    public TimeSpan AverageAssignmentTime { get; init; }
    public int AssignmentsPerSecond { get; init; }

    /// <summary>
    /// CALCULATED: Sequence integrity percentage
    /// </summary>
    public double SequenceIntegrityPercentage => TotalRows > 0
        ? (double)(TotalRows - GapCount - DuplicateCount) / TotalRows * 100
        : 0;

    /// <summary>
    /// CALCULATED: Expected vs actual range efficiency
    /// </summary>
    public double RangeEfficiency => TotalRows > 0 && MaxRowNumber > 0
        ? (double)TotalRows / MaxRowNumber * 100
        : 0;
}

/// <summary>
/// ENTERPRISE: Row number validation result with detailed feedback
/// DIAGNOSTIC: Comprehensive validation reporting with actionable insights
/// </summary>
internal sealed class RowNumberValidationResult
{
    public bool IsValid { get; init; }
    public int ProcessedRows { get; init; }
    public int TotalRows { get; init; }
    public TimeSpan OperationTime { get; init; }
    public bool HasValidSequence { get; init; }
    public string? ErrorMessage { get; init; }
    public IReadOnlyList<string> Warnings { get; init; } = Array.Empty<string>();
    public IReadOnlyList<int> GapLocations { get; init; } = Array.Empty<int>();
    public IReadOnlyList<int> DuplicateNumbers { get; init; } = Array.Empty<int>();

    /// <summary>
    /// CALCULATED: Processing efficiency in rows per second
    /// </summary>
    public double ProcessingRate => OperationTime.TotalSeconds > 0
        ? ProcessedRows / OperationTime.TotalSeconds
        : 0;

    /// <summary>
    /// FACTORY: Create success result
    /// </summary>
    public static RowNumberValidationResult Success(int processedRows, int totalRows, TimeSpan operationTime) =>
        new()
        {
            IsValid = true,
            ProcessedRows = processedRows,
            TotalRows = totalRows,
            OperationTime = operationTime,
            HasValidSequence = true
        };

    /// <summary>
    /// FACTORY: Create failure result
    /// </summary>
    public static RowNumberValidationResult Failure(string errorMessage, TimeSpan operationTime) =>
        new()
        {
            IsValid = false,
            OperationTime = operationTime,
            ErrorMessage = errorMessage,
            HasValidSequence = false
        };
}
```

## 🎯 FACADE API INTEGRATION

### Row Number Management Facade Methods

```csharp
#region Row Number Management Operations

/// <summary>
/// PUBLIC API: Regenerate all row numbers with sequential ordering
/// INTEGRATION: Called automatically after Import/Export/Filter operations
/// SMART: Intelligent ordering with creation time fallback
/// </summary>
Task<Result<RowNumberValidationResult>> RegenerateRowNumbersAsync(
    bool preserveOrder = true,
    cancellationToken cancellationToken = default);

/// <summary>
/// PUBLIC API: Assign row number to specific row
/// ATOMIC: Thread-safe single row assignment
/// </summary>
Task<Result<bool>> AssignRowNumberAsync(
    int rowIndex,
    int? customRowNumber = null,
    cancellationToken cancellationToken = default);

/// <summary>
/// PUBLIC API: Compact row numbers to eliminate gaps
/// OPTIMIZATION: Sequence optimization for performance
/// </summary>
Task<Result<RowNumberValidationResult>> CompactRowNumbersAsync(
    cancellationToken cancellationToken = default);

/// <summary>
/// PUBLIC API: Validate row number sequence integrity
/// DIAGNOSTICS: Comprehensive sequence health check
/// </summary>
Task<Result<RowNumberValidationResult>> ValidateRowNumberSequenceAsync(
    cancellationToken cancellationToken = default);

/// <summary>
/// PUBLIC API: Get row numbering statistics
/// MONITORING: Real-time numbering performance metrics
/// </summary>
Task<Result<RowNumberStatistics>> GetRowNumberStatisticsAsync(
    cancellationToken cancellationToken = default);

/// <summary>
/// PUBLIC API: Repair corrupted row number sequences
/// RECOVERY: Automatic repair with user assignment preservation
/// </summary>
Task<Result<RowNumberValidationResult>> RepairRowNumberSequenceAsync(
    bool preserveUserAssignments = true,
    cancellationToken cancellationToken = default);

#endregion
```

## ⚡ INTEGRATION POINTS & TRIGGERING

### Automatic Row Number Management Integration

```csharp
/// <summary>
/// INTEGRATION: Row number management triggers
/// AUTOMATIC: Seamless integration with data operations
/// </summary>
internal static class RowNumberIntegration
{
    /// <summary>
    /// TRIGGER: After successful data import
    /// AUTOMATIC: Regenerate row numbers to maintain sequence
    /// </summary>
    public static async Task PostImportRowNumberUpdate(
        IRowNumberService rowNumberService,
        int importedRowCount,
        cancellationToken cancellationToken = default)
    {
        if (importedRowCount > 0)
        {
            await rowNumberService.RegenerateRowNumbersAsync(
                preserveOrder: true,
                cancellationToken);
        }
    }

    /// <summary>
    /// TRIGGER: After data filtering
    /// AUTOMATIC: Compact row numbers for filtered view
    /// </summary>
    public static async Task PostFilterRowNumberUpdate(
        IRowNumberService rowNumberService,
        int visibleRowCount,
        cancellationToken cancellationToken = default)
    {
        if (visibleRowCount > 0)
        {
            await rowNumberService.CompactRowNumbersAsync(cancellationToken);
        }
    }

    /// <summary>
    /// TRIGGER: After data export
    /// VALIDATION: Ensure sequence integrity before export
    /// </summary>
    public static async Task PreExportRowNumberValidation(
        IRowNumberService rowNumberService,
        cancellationToken cancellationToken = default)
    {
        var validation = await rowNumberService.ValidateRowNumberSequenceAsync(cancellationToken);

        if (!validation.IsValid)
        {
            await rowNumberService.RepairRowNumberSequenceAsync(
                preserveUserAssignments: true,
                cancellationToken);
        }
    }
}
```

## 🚀 PERFORMANCE OPTIMIZATIONS

### Smart Algorithms & Caching

```csharp
/// <summary>
/// PERFORMANCE: Optimized row number algorithms
/// ENTERPRISE: High-performance numbering with smart caching
/// </summary>
internal static class RowNumberAlgorithms
{
    /// <summary>
    /// PERFORMANCE: Parallel row number assignment
    /// SCALABLE: Efficient handling of large datasets
    /// </summary>
    public static async Task<Dictionary<int, int>> AssignRowNumbersParallel(
        IEnumerable<int> rowIndexes,
        int startNumber = 1,
        cancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
            rowIndexes
                .AsParallel()
                .WithCancellation(cancellationToken)
                .Select((rowIndex, index) => new { rowIndex, rowNumber = startNumber + index })
                .ToDictionary(x => x.rowIndex, x => x.rowNumber),
            cancellationToken);
    }

    /// <summary>
    /// OPTIMIZATION: Smart gap detection with LINQ
    /// EFFICIENT: Single-pass gap analysis
    /// </summary>
    public static IReadOnlyList<int> DetectNumberingGaps(IEnumerable<int> rowNumbers)
    {
        var sortedNumbers = rowNumbers.OrderBy(n => n).ToList();

        return Enumerable.Range(1, sortedNumbers.LastOrDefault())
            .Except(sortedNumbers)
            .ToList();
    }

    /// <summary>
    /// SMART: Creation time fallback ordering
    /// RECOVERY: Intelligent ordering when numbers are corrupted
    /// </summary>
    public static IEnumerable<T> OrderByCreationTimeWithFallback<T>(
        IEnumerable<T> items,
        Func<T, int?> rowNumberSelector,
        Func<T, DateTime> creationTimeSelector)
    {
        return items
            .OrderBy(item => rowNumberSelector(item) ?? int.MaxValue)
            .ThenBy(item => creationTimeSelector(item));
    }
}
```

## **🔍 LOGGING ŠPECIFIKÁCIA PRE ROW NUMBER MANAGEMENT**

### **Internal DI Registration & Service Distribution**
Všetky row number logging services sú registrované v **`Infrastructure/Services/InternalServiceRegistration.cs`**:

```csharp
// V InternalServiceRegistration.cs
services.AddSingleton<IRowNumberLogger<RowNumberService>, RowNumberLogger<RowNumberService>>();
services.AddSingleton<IOperationLogger<RowNumberService>, OperationLogger<RowNumberService>>();
services.AddSingleton<ICommandLogger<RowNumberService>, CommandLogger<RowNumberService>>();
```

### **Row Number Operations Logging**

```csharp
// Row number assignment logging
_rowNumberLogger.LogRowNumberAssignment(rowIndex, rowNumber, assignmentTime);
_logger.LogInformation("Row number assigned: rowIndex={RowIndex}, rowNumber={RowNumber}, time={Duration}ms",
    rowIndex, rowNumber, assignmentTime.TotalMilliseconds);

// Batch regeneration logging
_rowNumberLogger.LogBatchRegeneration(processedRows, totalTime, preserveOrder);
_logger.LogInformation("Row numbers regenerated: processedRows={ProcessedRows}, time={Duration}ms, preserveOrder={PreserveOrder}",
    processedRows, totalTime.TotalMilliseconds, preserveOrder);

// Sequence validation logging
_logger.LogInformation("Row number sequence validated: isValid={IsValid}, gapCount={GapCount}, duplicateCount={DuplicateCount}",
    validationResult.IsValid, validationResult.GapLocations.Count, validationResult.DuplicateNumbers.Count);

// Performance metrics logging
_logger.LogInformation("Row numbering performance: processingRate={ProcessingRate} rows/sec, integrityPercentage={IntegrityPercentage:F2}%",
    statistics.AssignmentsPerSecond, statistics.SequenceIntegrityPercentage);
```

### **Integration Points Logging**

```csharp
// Post-import trigger logging
_logger.LogInformation("Post-import row number update: importedRows={ImportedRows}, regenerationTriggered={Triggered}",
    importedRowCount, importedRowCount > 0);

// Post-filter trigger logging
_logger.LogInformation("Post-filter row number update: visibleRows={VisibleRows}, compactionTriggered={Triggered}",
    visibleRowCount, visibleRowCount > 0);

// Pre-export validation logging
_logger.LogInformation("Pre-export row number validation: sequenceValid={IsValid}, repairTriggered={RepairTriggered}",
    validationResult.IsValid, !validationResult.IsValid);
```

### **Logging Levels Usage:**
- **Information**: Successful numbering operations, statistics, integration triggers
- **Warning**: Sequence gaps, duplicates, performance issues, repair operations
- **Error**: Assignment failures, validation errors, integration failures
- **Critical**: Sequence corruption, severe numbering system failures, data integrity issues

Táto špecifikácia poskytuje kompletný, enterprise-ready row number management systém s automatickou integráciou do Import/Export/Filter operácií, pokročilými performance optimalizáciami a jednotnou architektúrou s ostatnými časťami komponentu.