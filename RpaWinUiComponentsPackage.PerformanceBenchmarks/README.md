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

### 6. Batch Size Optimization
- **SortWithBatchSize**: Sort with different batch sizes
- **Batch sizes tested**: 1,000 | 5,000 | 10,000
- **Fixed dataset**: 100,000 rows

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

### 9. Large Dataset Stress Tests 💪 NEW
- **FullDatasetIteration**: Iterate through entire dataset
- **SortLargeDataset**: Sort very large datasets
- **FilterLargeDataset**: Filter very large datasets
- **MemoryFootprint**: Measure memory usage per row
- **SequentialRowAccess**: Sequential access pattern performance
- **RandomRowAccess**: Random access pattern performance
- **Row counts**: 1M | 5M | 10M rows

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
- **NativeMemoryProfiler**: Tracks unmanaged memory
- **ThreadingDiagnoser**: Tracks threading metrics
- **EtwProfiler**: Detailed Windows ETW profiling (MemoryProfileBenchmarks only)

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
- Increase available system memory
- Check for memory leaks in tested code

## 📚 References

- [BenchmarkDotNet Documentation](https://benchmarkdotnet.org/)
- [Performance Best Practices](https://docs.microsoft.com/en-us/dotnet/core/diagnostics/)
- [Memory Profiling Guide](https://docs.microsoft.com/en-us/visualstudio/profiling/memory-usage)
