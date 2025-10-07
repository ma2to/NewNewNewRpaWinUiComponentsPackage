# RpaWinUiComponentsPackage Performance Benchmarks

Comprehensive performance profiling and benchmarking suite for AdvancedWinUiDataGrid using BenchmarkDotNet.

## 📊 Benchmark Categories

### 1. Import/Export Benchmarks
- **ImportDictionaryToGrid**: Tests Dictionary to Grid import performance
- **ExportGridToDataTable**: Tests Grid to DataTable export performance
- **TransformDataTableToDictionaries**: Pure DataTable → Dictionary transformation
- **TransformDictionariesToDataTable**: Pure Dictionary → DataTable transformation
- **Row counts**: 1,000 | 10,000 | 100,000

### 2. Validation Benchmarks
- **ValidateAll**: Batch validation performance
- **ValidateWithStatistics**: Validation with detailed statistics tracking
- **Row counts**: 1,000 | 10,000 | 100,000

### 3. Sort Benchmarks
- **SortByInteger**: Integer column sorting
- **SortByString**: String column sorting
- **SortByDecimal**: Decimal column sorting
- **Row counts**: 1,000 | 10,000 | 100,000

### 4. Filter Benchmarks
- **FilterByEquals**: Exact value filtering
- **FilterByGreaterThan**: Numeric comparison filtering
- **Row counts**: 1,000 | 10,000 | 100,000

### 5. Search Benchmarks
- **BasicSearch**: Basic text search
- **Row counts**: 1,000 | 10,000 | 100,000

### 6. Batch Size Optimization ⚡ ENHANCED
- **Sort_Headless_BatchSize**: Sort without UI dispatcher (baseline)
- **Sort_WithUI_BatchSize**: Sort with UI dispatcher overhead
- **Filter_Headless_BatchSize**: Filter without UI
- **Filter_WithUI_BatchSize**: Filter with UI overhead
- **Validation_Headless_BatchSize**: Validation without UI
- **Validation_WithUI_BatchSize**: Validation with UI overhead
- **Batch sizes tested**: 1,000 | 5,000 | 10,000
- **Fixed dataset**: 100,000 rows
- **Purpose**: Compare UI vs Headless performance, find optimal batch size

### 7. Memory Profile Benchmarks
- **GetAllRows_MemoryAllocation**: Memory allocation for full dataset retrieval
- **UpdateRows_MemoryPressure**: Memory pressure during row updates
- **Row counts**: 10,000 | 100,000 | 1,000,000

### 8. Concurrent Operations Benchmarks ⚡ NEW
- **ConcurrentReads**: Multiple threads reading data simultaneously
- **ConcurrentRowReads**: Multiple threads reading different rows
- **ConcurrentSorts**: Multiple threads sorting simultaneously
- **ConcurrentFilters**: Multiple threads applying filters
- **ConcurrentSearches**: Multiple threads searching
- **MixedConcurrentOperations**: Mix of read/sort/filter/search operations
- **Row counts**: 10,000 | 50,000
- **Concurrency levels**: 2, 4, 8 threads

### 9. Large Dataset Stress Tests 💪 UPDATED
- **FullDatasetIteration**: Iterate through entire dataset
- **SortLargeDataset**: Sort very large datasets
- **FilterLargeDataset**: Filter very large datasets
- **MemoryFootprint**: Measure memory usage per row
- **SequentialRowAccess**: Sequential access pattern performance
- **RandomRowAccess**: Random access pattern performance
- **Row counts**: 1M | 5M | 10M rows

### 10. Large Dataset Benchmarks (New) 🚀 COMPREHENSIVE
- **Import_LargeDataset**: Import performance with various batch sizes
- **Sort_LargeDataset**: Sort scalability testing
- **Filter_LargeDataset**: Filter performance at scale
- **Validation_LargeDataset**: Validation throughput testing
- **MemoryFootprint**: Precise memory per row measurement
- **SequentialRowAccess**: Linear access patterns (limited to 100K for 10M datasets)
- **RandomRowAccess**: Random access patterns (10K accesses)
- **GetAllRows_Performance**: Full dataset retrieval timing
- **ClearAll_Performance**: Mass deletion performance
- **Row counts**: 100K | 1M | 5M | 10M rows
- **Batch sizes**: 1K | 5K | 10K | 50K
- **Matrix testing**: All combinations of row count × batch size

### 11. UI Performance Benchmarks 🎨 NEW
Compares UI-enabled vs Headless mode performance:

#### Sorting Comparison
- **Sort_Headless** (baseline): Sort without UI dispatcher
- **Sort_WithUI**: Sort with UI dispatcher overhead

#### Filtering Comparison
- **Filter_Headless** (baseline): Filter without UI
- **Filter_WithUI**: Filter with UI overhead

#### Selection Comparison
- **Selection_Headless**: Multi-row selection without UI
- **Selection_WithUI**: Multi-row selection with UI notifications

#### Update Operations
- **RowUpdate_Headless**: Cell updates without UI
- **RowUpdate_WithUI**: Cell updates with UI notifications
- **UINotifications_Overhead**: Combined operation overhead

#### Data Access
- **DataAccess_Headless**: GetCurrentData() without UI
- **DataAccess_WithUI**: GetCurrentData() with UI overhead

#### Bulk Operations
- **BulkInsert_Headless**: Mass insert without UI (up to 10K rows)
- **BulkInsert_WithUI**: Mass insert with UI overhead (up to 10K rows)

**Row counts tested**: 10K | 100K | 1M
**Purpose**: Quantify UI dispatcher overhead for different operation types

## 🚀 Usage

### Run All Benchmarks
```bash
dotnet run -c Release --project RpaWinUiComponentsPackage.PerformanceBenchmarks
```

### Run Specific Benchmark Class
```bash
dotnet run -c Release --project RpaWinUiComponentsPackage.PerformanceBenchmarks --filter "*ImportExportBenchmarks*"
```

### Run Specific Method
```bash
dotnet run -c Release --project RpaWinUiComponentsPackage.PerformanceBenchmarks --filter "*SortByIntegerAscending*"
```

### Run with Memory Profiler
```bash
dotnet run -c Release --project RpaWinUiComponentsPackage.PerformanceBenchmarks --filter "*MemoryProfileBenchmarks*"
```

### Run UI Performance Benchmarks
```bash
dotnet run -c Release --project RpaWinUiComponentsPackage.PerformanceBenchmarks --filter "*UIPerformanceBenchmarks*"
```

### Run Large Dataset Benchmarks
```bash
# WARNING: This can take hours and requires significant memory (up to 16GB for 10M rows)
dotnet run -c Release --project RpaWinUiComponentsPackage.PerformanceBenchmarks --filter "*LargeDatasetBenchmarks*"
```

### Run Enhanced Batch Size Tests
```bash
dotnet run -c Release --project RpaWinUiComponentsPackage.PerformanceBenchmarks --filter "*BatchSizeBenchmarks*"
```

## 📈 Metrics Collected

### Performance Metrics
- **Mean**: Average execution time
- **Error**: Half of 99.9% confidence interval
- **StdDev**: Standard deviation of all measurements
- **Median**: 50th percentile
- **Min/Max**: Minimum and maximum execution times

### Memory Metrics
- **Gen0/Gen1/Gen2**: Number of garbage collections
- **Allocated**: Total memory allocated
- **Native Memory**: Unmanaged memory allocations

### Threading Metrics
- **Completed Work Items**: Number of ThreadPool work items
- **Lock Contentions**: Thread synchronization conflicts

## 🎯 Performance Goals

### Target Throughput
- **Import**: > 100,000 rows/second
- **Validation**: > 50,000 rows/second
- **Sort**: > 200,000 rows/second
- **Filter**: > 500,000 rows/second
- **Search**: > 1,000,000 rows/second

### Memory Goals
- **Allocation**: < 100 bytes per row for most operations
- **GC Pressure**: Minimal Gen2 collections for < 1M rows
- **Peak Memory**: < 2GB for 10M row dataset
- **Large Dataset Memory**: < 200 bytes per row for 10M rows

### UI Performance Goals
- **UI Overhead**: < 20% performance penalty vs headless mode
- **Notification Latency**: < 100ms for UI updates
- **Dispatcher Throughput**: > 10,000 operations/sec

### Optimal Batch Size
- Determined through BatchSizeBenchmarks
- Expected optimal range: 5,000 - 10,000 rows

## 📁 Output

Results are saved to:
- `BenchmarkDotNet.Artifacts/results/` - Detailed results in multiple formats (CSV, HTML, Markdown)
- `BenchmarkDotNet.Artifacts/logs/` - Execution logs
- `BenchmarkDotNet.Artifacts/reports/` - ETW profiler reports (if enabled)

## 🔧 Configuration

Benchmarks use:
- **MemoryDiagnoser**: Tracks memory allocations and GC
- **NativeMemoryProfiler**: Tracks unmanaged memory (disabled in some benchmarks)
- **ThreadingDiagnoser**: Tracks threading metrics
- **EtwProfiler**: Detailed Windows ETW profiling (LargeDataset benchmarks only)

### UI Benchmark Configuration
- **UIBenchmarkHelper**: Manages UI dispatcher lifecycle
- **Automatic Skip**: UI benchmarks skip gracefully if dispatcher unavailable
- **Thread Safety**: All UI operations properly synchronized
- **Cleanup**: Automatic dispatcher cleanup after benchmarks

## 📊 Interpreting Results

### CPU Bottlenecks
- High execution time with low memory allocation
- Look for synchronization issues (Lock Contentions)
- Check threading efficiency (Completed Work Items)

### Memory Bottlenecks
- High allocation rates
- Frequent Gen2 collections
- Increasing memory usage over time

### I/O Bottlenecks
- Long execution times in Import/Export benchmarks
- Consider SSD vs HDD performance
- Check file system caching effects

### UI Dispatcher Bottlenecks
- Compare headless vs UI benchmarks to identify overhead
- High lock contentions may indicate dispatcher queue pressure
- Check threading diagnostics for dispatcher thread utilization

## 🏆 Best Practices

1. **Always run in Release mode** (`-c Release`)
2. **Close all other applications** to reduce noise
3. **Run multiple times** to ensure consistency
4. **Compare results** before and after optimizations
5. **Focus on relevant metrics** for your use case

## 📝 Adding New Benchmarks

```csharp
[MemoryDiagnoser]
[ThreadingDiagnoser]
public class MyBenchmark
{
    [Params(1000, 10000)]
    public int RowCount { get; set; }

    [GlobalSetup]
    public void Setup() { /* setup code */ }

    [Benchmark]
    public void MyBenchmarkMethod() { /* benchmark code */ }

    [GlobalCleanup]
    public void Cleanup() { /* cleanup code */ }
}
```

## 🐛 Troubleshooting

### "Benchmark was not executed" Error
- Ensure you're running in Release mode
- Check that all dependencies are restored
- Verify .NET 8.0 Windows SDK is installed

### High Variance in Results
- Close background applications
- Disable Windows Defender real-time protection temporarily
- Run on AC power (not battery)
- Increase warmup and iteration counts in BenchmarkDotNet config

### Out of Memory Errors
- Reduce row counts for memory-intensive benchmarks
- Increase available system memory (recommend 16GB+ for 10M rows)
- Check for memory leaks in tested code
- Run LargeDatasetBenchmarks individually, not all at once

### UI Benchmarks Being Skipped
- UI dispatcher may not be available in console environment
- Try running benchmarks on a thread with UI context
- Check console output for "[SKIP]" messages
- UI benchmarks require Windows desktop environment

### Large Dataset Benchmark Timeouts
- Increase `DefaultOperationTimeout` in options
- Run with fewer row count parameters
- Split benchmarks into smaller batches
- Monitor system resources during execution

## 📚 References

- [BenchmarkDotNet Documentation](https://benchmarkdotnet.org/)
- [Performance Best Practices](https://docs.microsoft.com/en-us/dotnet/core/diagnostics/)
- [Memory Profiling Guide](https://docs.microsoft.com/en-us/visualstudio/profiling/memory-usage)
- [WinUI DispatcherQueue](https://docs.microsoft.com/en-us/windows/windows-app-sdk/api/winrt/microsoft.ui.dispatching.dispatcherqueue)

## 📝 New Benchmark Files Created

### Helper Classes
- **`Helpers/UIBenchmarkHelper.cs`**: UI dispatcher management and facade creation
  - `CreateWithUI()`: Creates facade with UI dispatcher support
  - `CreateHeadless()`: Creates facade in headless mode
  - `EnsureDispatcher()`: Thread-safe dispatcher initialization
  - `IsUIAvailable()`: Check if UI context is available
  - `Cleanup()`: Proper dispatcher cleanup

### Benchmark Suites
- **`Benchmarks/BatchSizeBenchmarks.cs`** (Enhanced):
  - Compares UI vs Headless for Sort, Filter, and Validation
  - Tests 3 batch sizes (1K, 5K, 10K) on 100K rows
  - Baseline comparison for performance analysis

- **`Benchmarks/LargeDatasetBenchmarks.cs`** (New):
  - Matrix testing: 4 row counts × 4 batch sizes = 16 combinations
  - Tests scalability from 100K to 10M rows
  - Comprehensive operation coverage (import, sort, filter, validation, access patterns)
  - Memory footprint analysis

- **`Benchmarks/UIPerformanceBenchmarks.cs`** (New):
  - Direct UI vs Headless comparison for all operations
  - Tests 3 dataset sizes (10K, 100K, 1M)
  - Measures dispatcher overhead per operation type
  - Bulk operation performance analysis

## 🎯 Benchmark Execution Strategy

### Quick Performance Check (< 5 minutes)
```bash
dotnet run -c Release --filter "*Sort_Headless*"
```

### UI Overhead Analysis (10-20 minutes)
```bash
dotnet run -c Release --filter "*UIPerformanceBenchmarks*"
```

### Batch Size Optimization (30-60 minutes)
```bash
dotnet run -c Release --filter "*BatchSizeBenchmarks*"
```

### Full Scale Test (2-6 hours, requires 16GB+ RAM)
```bash
dotnet run -c Release --filter "*LargeDatasetBenchmarks*"
```

### Complete Suite (8-12 hours)
```bash
dotnet run -c Release
```

## 🎯 Three-Mode Benchmark System

### Benchmark Modes

The DataGrid supports three distinct operation modes, each optimized for different use cases:

#### 1. Headless Mode (Baseline)
- **OperationMode**: `PublicDataGridOperationMode.Headless`
- **UI Dispatcher**: None (no DispatcherQueue)
- **Auto-refresh**: N/A
- **Performance**: Fastest (baseline)
- **Use Cases**:
  - Batch processing and data imports
  - Background tasks without UI
  - Server-side operations
  - Maximum throughput scenarios
- **Creation**: `UIBenchmarkHelper.CreateHeadless(batchSize, features)`

#### 2. Readonly Mode (Headless+UI)
- **OperationMode**: `PublicDataGridOperationMode.Readonly`
- **UI Dispatcher**: Present (DispatcherQueue initialized)
- **Auto-refresh**: Disabled (no automatic UI notifications)
- **Performance**: ~110-120% of Headless
- **Use Cases**:
  - Read-only data grids with manual refresh
  - Report generation with UI context
  - Occasional UI updates without real-time sync
  - Balance between performance and UI availability
- **Creation**: `UIBenchmarkHelper.CreateReadonly(batchSize, features)`

#### 3. Interactive Mode (Full UI)
- **OperationMode**: `PublicDataGridOperationMode.Interactive`
- **UI Dispatcher**: Active (DispatcherQueue with full integration)
- **Auto-refresh**: Enabled (automatic UI notifications)
- **Performance**: ~140-200% of Headless
- **Use Cases**:
  - Interactive data editing
  - Real-time data grids
  - User-facing applications with live updates
  - Best user experience with visual feedback
- **Creation**: `UIBenchmarkHelper.CreateWithUI(batchSize, features)`

### Running Unified Mode Benchmarks

The `UnifiedModeBenchmarks` class provides comprehensive side-by-side comparison of all three modes:

#### Quick Comparison (100K rows)
```bash
dotnet run -c Release --filter "*UnifiedModeBenchmarks*" --job short
```

#### Full Comparison (100K + 1M rows)
```bash
dotnet run -c Release --filter "*UnifiedModeBenchmarks*"
```

#### Specific Operation Comparison
```bash
# Compare sort performance across all modes
dotnet run -c Release --filter "*Sort_UnifiedMode*"

# Compare filter performance
dotnet run -c Release --filter "*Filter_UnifiedMode*"

# Compare validation performance
dotnet run -c Release --filter "*Validation_UnifiedMode*"
```

### Expected Performance Characteristics

Based on 1M rows with 5K batch size:

| Mode | Performance | Overhead | Memory Impact | UI Availability |
|------|-------------|----------|---------------|-----------------|
| Headless | 100% (baseline) | 0% | Lowest | None |
| Readonly | ~110-120% | +10-20% | Low (dispatcher overhead) | Available but inactive |
| Interactive | ~140-200% | +40-100% | Medium (notifications) | Full with auto-refresh |

**Key Insights**:
- Overhead decreases with larger batch sizes (optimal: 5K-10K)
- Memory overhead is primarily from UI dispatcher queue allocation
- Interactive mode overhead varies by operation type:
  - Bulk insert: +40-60% (one-time notification)
  - Sort/Filter: +50-80% (collection notifications)
  - Validation: +80-120% (per-row notifications)
  - Update/Delete: +100-200% (immediate UI sync)

### Benchmark Operations Tested

The `UnifiedModeBenchmarks` class tests 8 key operations:

1. **Sort_UnifiedMode**: Ascending integer sort
2. **Filter_UnifiedMode**: Numeric filter (GreaterThan)
3. **Validation_UnifiedMode**: Regex validation (email pattern)
4. **BulkInsert_UnifiedMode**: Raw insertion performance
5. **GetAllRows_UnifiedMode**: Data retrieval performance
6. **CombinedOperations_UnifiedMode**: Insert → Sort → Filter
7. **UpdateCells_UnifiedMode**: Cell update operations (1% of data)
8. **DeleteRows_UnifiedMode**: Row deletion (10% of data)

### Performance Report Generation

Use the `BenchmarkReportHelper` class to analyze results:

```csharp
using RpaWinUiComponentsPackage.PerformanceBenchmarks.Helpers;

// Create sample results
var results = new List<BenchmarkReportHelper.BenchmarkResult>
{
    new() { Operation = "Sort", Mode = "Headless", RowCount = 1_000_000,
            BatchSize = 5000, MeanTimeMs = 1250.5, AllocatedMemoryMB = 45.2 },
    new() { Operation = "Sort", Mode = "Readonly", RowCount = 1_000_000,
            BatchSize = 5000, MeanTimeMs = 1375.8, AllocatedMemoryMB = 47.8 },
    new() { Operation = "Sort", Mode = "Interactive", RowCount = 1_000_000,
            BatchSize = 5000, MeanTimeMs = 1812.3, AllocatedMemoryMB = 52.1 }
};

// Generate reports
var summary = BenchmarkReportHelper.GenerateSummary(results);
var table = BenchmarkReportHelper.GenerateComparisonTable(results);
var recommendations = BenchmarkReportHelper.GenerateRecommendations(results);

// Complete report
var fullReport = BenchmarkReportHelper.GenerateCompleteReport(results);
File.WriteAllText("benchmark-report.md", fullReport);

// Export to CSV
var csv = BenchmarkReportHelper.ExportToCsv(results);
File.WriteAllText("benchmark-results.csv", csv);
```

### Mode Selection Guidelines

Choose the appropriate mode based on your requirements:

**Choose Headless when**:
- Maximum throughput is critical
- No UI interaction needed
- Running on server or background thread
- Processing large datasets (5M+ rows)
- Memory constraints are tight

**Choose Readonly when**:
- You need UI context but not real-time updates
- Manual refresh is acceptable
- Balance between performance and UI availability
- Large datasets with occasional UI access
- Report generation with UI controls

**Choose Interactive when**:
- User experience is priority
- Real-time visual feedback required
- Interactive data editing
- Moderate dataset sizes (< 1M rows)
- Acceptable to trade performance for UX

### Batch Size Optimization by Mode

Recommended batch sizes based on mode and dataset size:

| Dataset Size | Headless | Readonly | Interactive |
|--------------|----------|----------|-------------|
| < 100K rows | 5,000 | 5,000 | 2,500 |
| 100K - 1M rows | 10,000 | 10,000 | 5,000 |
| 1M - 5M rows | 50,000 | 25,000 | 10,000 |
| 5M+ rows | 100,000 | 50,000 | Avoid* |

\* For 5M+ rows, consider using Headless or Readonly mode instead of Interactive

### Integration with Existing Benchmarks

The three-mode system integrates with existing benchmark suites:

- **BatchSizeBenchmarks**: Now tests Headless vs Interactive (can be extended to include Readonly)
- **UIPerformanceBenchmarks**: Focused UI vs Headless comparison
- **UnifiedModeBenchmarks**: Comprehensive three-way comparison (NEW)
- **LargeDatasetBenchmarks**: Tests scalability (currently Headless only)

### Troubleshooting Three-Mode Benchmarks

**Issue**: Readonly/Interactive benchmarks being skipped
- **Cause**: UI dispatcher not available
- **Solution**: Ensure Windows desktop environment, check `IsUIAvailable()`

**Issue**: Interactive mode much slower than expected
- **Cause**: UI dispatcher queue saturation
- **Solution**: Increase batch size or switch to Readonly mode

**Issue**: Readonly mode showing Interactive-like performance
- **Cause**: Auto-refresh accidentally enabled
- **Solution**: Verify `OperationMode = PublicDataGridOperationMode.Readonly`

**Issue**: Headless mode slower than Readonly
- **Cause**: Possible measurement error or system interference
- **Solution**: Re-run benchmarks, close background applications
