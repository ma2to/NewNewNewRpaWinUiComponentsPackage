# DataGrid Performance Testing Suite

## Overview

This is a comprehensive standalone performance testing application for the AdvancedWinUiDataGrid component. It measures performance across **3 operation modes** optimized for datasets ranging from **100K to 10M+ rows**.

## Test File

- **Location**: `PERFORMANCE_TESTS.cs`
- **Type**: Standalone console application (NOT xUnit/NUnit)
- **Framework**: .NET (uses Stopwatch for timing, no BenchmarkDotNet)

## How to Run

### Method 1: Using dotnet run (Recommended)

Create a simple `.csproj` file in the same directory:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net8.0-windows10.0.19041.0</TargetFramework>
    <Nullable>enable</Nullable>
    <AllowUnsafeBlocks>true</AllowUnsafeBlocks>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="RpaWinUiComponentsPackage\RpaWinUiComponentsPackage.csproj" />
  </ItemGroup>
</Project>
```

Then run:

```bash
dotnet run --configuration Release
```

### Method 2: Using csc.exe (Direct Compilation)

```bash
csc PERFORMANCE_TESTS.cs /reference:RpaWinUiComponentsPackage.dll /out:PerformanceTests.exe
PerformanceTests.exe
```

### Method 3: Add to Existing Test Project

Add `PERFORMANCE_TESTS.cs` to your test project and run it as a console app (change `Main` to be the entry point).

## Test Configuration

### 3 Operation Modes

1. **Headless Mode**
   - No UI dispatcher
   - Pure backend performance
   - **Baseline measurements** (0% overhead)
   - Best for: Batch processing, data imports, background tasks

2. **Readonly Mode**
   - UI dispatcher present but inactive
   - No automatic refresh
   - **Middle ground performance** (10-30% overhead)
   - Best for: Read-only grids, reporting dashboards

3. **Interactive Mode**
   - Full UI dispatcher with auto-refresh
   - Real user experience
   - **Slowest but most realistic** (30-60% overhead)
   - Best for: Interactive data grids with live editing

### Test Operations

For each mode and dataset size, the following operations are tested:

1. **Sort** - Integer column ascending sort
2. **Filter** - Numeric GreaterThan filter (>500)
3. **Validation** - Regex validation (email pattern)
4. **BulkInsert** - Raw row insertion performance
5. **GetAllRows** - Data retrieval speed
6. **UpdateCells** - Cell update operations (1% of data)

### Dataset Sizes

- **100,000 rows** - Small dataset
- **1,000,000 rows** - Large dataset

### Batch Sizes Tested

- 1,000
- 5,000
- 10,000
- 50,000

The test automatically finds the **optimal batch size** for each operation and mode.

## Output Files

After running, the following files are generated:

### 1. `PERFORMANCE_RESULTS_{timestamp}.txt`

Full formatted report with:
- System information
- Detailed results per mode
- Performance comparison table
- Overhead calculations vs Headless baseline
- Optimal batch size recommendations
- Best practices and recommendations

Example output:
```
═══════════════════════════════════════════════════════════════
           DATAGRID PERFORMANCE TEST RESULTS
═══════════════════════════════════════════════════════════════

Timestamp: 2025-10-06 14:32:15
System: Windows 10.0.22000, 16 cores, .NET 8.0.1

─── Headless Mode ─────────────────────────────────────────────

Operation: Sort | Rows: 100,000 | BatchSize: 5,000
  Time: 1,234 ms
  Throughput: 81,037 rows/sec
  Memory: 45.2 MB allocated

...

═══════════════════════════════════════════════════════════════
           PERFORMANCE COMPARISON
═══════════════════════════════════════════════════════════════

Operation       | Mode        |     Rows |  Batch | Time (ms) | Overhead
────────────────────────────────────────────────────────────────────────
Sort            | Headless    |  100,000 |   5,000 |     1,234 | baseline
Sort            | Readonly    |  100,000 |   5,000 |     1,357 | +10.0%
Sort            | Interactive |  100,000 |   5,000 |     1,789 | +45.0%
...
```

### 2. `PERFORMANCE_RESULTS_{timestamp}.csv`

Excel-compatible CSV format for data analysis:

```csv
Mode,Operation,RowCount,BatchSize,TimeMs,ThroughputRowsPerSec,MemoryMB,Success,ErrorMessage
Headless,Sort,100000,5000,1234,81037.12,45.2,True,
Readonly,Sort,100000,5000,1357,73706.25,47.8,True,
...
```

Import into Excel/Google Sheets for:
- Charts and graphs
- Pivot tables
- Custom analysis
- Trend visualization

## Expected Results

### Typical Performance Characteristics

**Headless Mode (Baseline)**:
- Sort 100K: ~1-2 seconds
- Filter 100K: ~0.5-1 second
- Validation 100K: ~2-3 seconds
- BulkInsert 100K: ~0.5-1 second
- GetAllRows 100K: <100 ms
- UpdateCells 1K: <50 ms

**Readonly Mode**:
- 10-30% slower than Headless
- UI dispatcher overhead without refresh

**Interactive Mode**:
- 30-60% slower than Headless
- Full UI refresh overhead
- Most realistic for user experience

### Optimal Batch Sizes (Typical)

**Headless Mode**:
- Sort: 10,000-50,000
- Filter: 10,000-50,000
- Validation: 5,000-10,000
- BulkInsert: 10,000-50,000
- GetAllRows: N/A (single operation)
- UpdateCells: 5,000-10,000

**Readonly Mode**:
- Sort: 5,000-10,000
- Filter: 5,000-10,000
- Validation: 5,000
- BulkInsert: 5,000-10,000

**Interactive Mode**:
- Sort: 1,000-5,000
- Filter: 1,000-5,000
- Validation: 1,000-5,000
- BulkInsert: 1,000-5,000

## Recommendations from Tests

### 1. For Batch Processing (No UI)
- **Use**: Headless mode
- **Batch Size**: 10,000-50,000
- **Features**: Enable parallel processing, disable logging
- **Best For**: Data imports, background processing, ETL pipelines

### 2. For Read-Only Grids
- **Use**: Readonly mode
- **Batch Size**: 5,000-10,000
- **Features**: Enable caching, disable real-time validation
- **Best For**: Dashboards, reports, data viewers

### 3. For Interactive Editing
- **Use**: Interactive mode
- **Batch Size**: 1,000-5,000
- **Features**: Enable real-time validation, auto-refresh
- **Best For**: Data entry forms, live editing grids

### 4. For 1M+ Rows
- **Prefer**: Headless or Readonly modes
- **Batch Size**: 10,000+ (larger is better)
- **Enable**: Parallel processing, LINQ optimizations
- **Disable**: Real-time validation, comprehensive logging
- **Consider**: Streaming/chunking for 10M+ rows

### 5. Memory Optimization
- Use streaming for 10M+ rows
- Disable caching if memory-constrained
- Process data in chunks
- Force GC between large operations

## Customization

### Modify Dataset Sizes

Edit line 44-45 in `PERFORMANCE_TESTS.cs`:

```csharp
private readonly int[] _rowCounts = { 100_000, 1_000_000 }; // Add 10_000_000 for 10M test
```

### Modify Batch Sizes

Edit line 45-46:

```csharp
private readonly int[] _batchSizes = { 1_000, 5_000, 10_000, 50_000 }; // Add 100_000 for larger batches
```

### Add New Operations

Add a new test method following the pattern:

```csharp
private async Task<TestResult> TestYourOperation(string mode, int rowCount, int batchSize)
{
    // Setup
    // Measure
    // Return TestResult
}
```

Then call it in `TestMode()` method around line 100.

## Troubleshooting

### Issue: "Cannot create DispatcherQueue"

**Solution**: This is expected for Readonly/Interactive modes in console apps. The test will automatically fall back to Headless mode with a warning.

**For real UI testing**: Run tests from a WinUI 3 application context where `DispatcherQueue.GetForCurrentThread()` is available.

### Issue: OutOfMemoryException with large datasets

**Solution**:
1. Reduce row counts or test smaller datasets first
2. Increase available RAM
3. Run tests individually instead of all at once
4. Add more aggressive GC calls between tests

### Issue: Tests are too slow

**Solution**:
1. Start with 100K rows instead of 1M
2. Use fewer batch sizes (test only 5K and 10K)
3. Run in Release mode, not Debug
4. Disable antivirus during testing

## Performance Metrics Explained

### Throughput (rows/sec)
- Higher is better
- Formula: `RowCount / (TimeMs / 1000)`
- Indicates how many rows per second the operation processes

### Memory (MB)
- Lower is better
- Measured using `GC.GetTotalMemory()` before/after
- Shows memory pressure of operation

### Overhead (%)
- Compares against Headless baseline
- Formula: `((ModeTime - HeadlessTime) / HeadlessTime) * 100`
- Shows performance cost of UI modes

## Integration with CI/CD

### Example GitHub Action

```yaml
name: Performance Tests

on: [push, pull_request]

jobs:
  performance:
    runs-on: windows-latest
    steps:
      - uses: actions/checkout@v3
      - name: Setup .NET
        uses: actions/setup-dotnet@v3
        with:
          dotnet-version: 8.0.x
      - name: Run Performance Tests
        run: dotnet run --project PERFORMANCE_TESTS.csproj --configuration Release
      - name: Upload Results
        uses: actions/upload-artifact@v3
        with:
          name: performance-results
          path: PERFORMANCE_RESULTS_*.txt
```

## Advanced Usage

### Testing with Custom Options

Modify the `CreateFacade()` method to test different configurations:

```csharp
var options = new AdvancedDataGridOptions
{
    BatchSize = batchSize,
    EnableParallelProcessing = true,  // Toggle this
    DegreeOfParallelism = 8,          // Set specific parallelism
    EnableLinqOptimizations = true,   // Toggle optimizations
    EnableCaching = true,              // Toggle caching
    // ... etc
};
```

### Benchmarking Specific Scenarios

Comment out operations you don't want to test in the `TestMode()` method:

```csharp
// results.Add(await TestSort(mode, rowCount, batchSize));
// results.Add(await TestFilter(mode, rowCount, batchSize));
results.Add(await TestValidation(mode, rowCount, batchSize)); // Only test this
```

## Notes

1. **Reproducibility**: Tests use fixed random seed (42) for consistent data generation
2. **Isolation**: Each test creates a new facade instance to avoid interference
3. **Cleanup**: Aggressive GC collection between tests ensures accurate memory measurements
4. **No Mocking**: Tests use real implementations for realistic results
5. **Console App**: Not using BenchmarkDotNet to keep it simple and portable

## Support

For questions or issues:
1. Check the generated `.txt` report for recommendations
2. Review the console output for warnings/errors
3. Examine the `.csv` file in Excel for detailed analysis
4. Compare your results against expected baseline values above

## License

This performance testing suite is part of the RpaWinUiComponentsPackage project.
