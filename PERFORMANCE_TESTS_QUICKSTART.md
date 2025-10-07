# Performance Tests - Quick Start Guide

## TL;DR - Run Tests Now

### Option 1: PowerShell (Recommended)
```powershell
.\run-performance-tests.ps1
```

### Option 2: Command Prompt
```cmd
run-performance-tests.bat
```

### Option 3: Manual
```bash
dotnet run --project PerformanceTests.csproj --configuration Release
```

## What Gets Tested

### 3 Operation Modes
1. **Headless** - Pure backend (fastest, baseline)
2. **Readonly** - UI dispatcher inactive (middle)
3. **Interactive** - Full UI refresh (realistic)

### 6 Operations
1. **Sort** - Integer ascending
2. **Filter** - GreaterThan operator
3. **Validation** - Email regex pattern
4. **BulkInsert** - Raw row insertion
5. **GetAllRows** - Data retrieval
6. **UpdateCells** - 1% random updates

### Test Matrix
- **Row Counts**: 100K, 1M
- **Batch Sizes**: 1K, 5K, 10K, 50K
- **Total Tests**: ~144 individual test runs

## Output Files

After running (takes 5-30 minutes depending on hardware):

### 📄 `PERFORMANCE_RESULTS_{timestamp}.txt`
Human-readable report with:
- Detailed results per mode
- Performance comparison table
- Optimal batch size recommendations
- Best practices

### 📊 `PERFORMANCE_RESULTS_{timestamp}.csv`
Excel-compatible data for:
- Creating charts
- Pivot tables
- Custom analysis

## Expected Runtime

| Hardware | 100K Rows | 1M Rows | Total |
|----------|-----------|---------|-------|
| Fast (16+ cores, SSD) | 2-5 min | 10-20 min | ~15-25 min |
| Medium (8 cores, SSD) | 5-10 min | 20-40 min | ~25-50 min |
| Slow (4 cores, HDD) | 10-20 min | 40-80 min | ~50-100 min |

## Key Findings (Typical)

### Performance vs Mode
- **Headless**: Baseline (fastest)
- **Readonly**: +10-30% slower
- **Interactive**: +30-60% slower

### Optimal Batch Sizes
- **Headless**: 10K-50K
- **Readonly**: 5K-10K
- **Interactive**: 1K-5K

### Best Use Cases
- **Headless** → Batch processing, imports, ETL
- **Readonly** → Dashboards, reports, viewers
- **Interactive** → Live editing, data entry

## Customization

### Test Fewer Rows (Faster)
Edit line 44 in `PERFORMANCE_TESTS.cs`:
```csharp
private readonly int[] _rowCounts = { 100_000 }; // Remove 1_000_000
```

### Test Specific Batch Sizes
Edit line 45:
```csharp
private readonly int[] _batchSizes = { 5_000, 10_000 }; // Only test these two
```

### Test Single Operation
Comment out unwanted tests in `TestMode()` method (~line 90-95):
```csharp
// results.Add(await TestSort(mode, rowCount, batchSize));
results.Add(await TestFilter(mode, rowCount, batchSize)); // Only this one
// results.Add(await TestValidation(mode, rowCount, batchSize));
```

## Troubleshooting

### Build Errors
```bash
# Clean and rebuild
dotnet clean
dotnet restore
dotnet build --configuration Release
```

### OutOfMemoryException
- Test with 100K rows only
- Close other applications
- Add more RAM
- Use 64-bit .NET runtime

### Tests Hang
- Check Task Manager for CPU usage
- Some operations (like validation on 1M rows) can take 5-10 minutes
- Wait patiently or reduce row count

### No Output Files
- Check console for errors
- Ensure write permissions in directory
- Look for exception stack traces

## Understanding Results

### Example Output Snippet
```
Operation: Sort | Rows: 100,000 | BatchSize: 10,000
  Time: 1,234 ms
  Throughput: 81,037 rows/sec
  Memory: 45.2 MB allocated
```

- **Time**: How long operation took
- **Throughput**: Rows processed per second (higher = better)
- **Memory**: Additional memory used (lower = better)

### Comparison Table
```
Operation | Mode        | Rows   | Batch | Time (ms) | Overhead
----------|-------------|--------|-------|-----------|----------
Sort      | Headless    | 100K   | 10K   | 1,234     | baseline
Sort      | Readonly    | 100K   | 10K   | 1,357     | +10.0%
Sort      | Interactive | 100K   | 10K   | 1,789     | +45.0%
```

- **Overhead**: Performance cost vs Headless baseline

## Files Created

```
D:\www\RB0120APP\NewRpaWinUiComponentsPackage\
├── PERFORMANCE_TESTS.cs              # Main test implementation (885 lines)
├── PerformanceTests.csproj            # Project file for compilation
├── run-performance-tests.ps1          # PowerShell runner script
├── run-performance-tests.bat          # Batch runner script
├── PERFORMANCE_TESTS_README.md        # Detailed documentation
├── PERFORMANCE_TESTS_QUICKSTART.md    # This file
└── PERFORMANCE_RESULTS_*.{txt,csv}    # Generated after running tests
```

## Next Steps

1. **Run the tests**: Use one of the run scripts above
2. **Review results**: Open the `.txt` file for recommendations
3. **Analyze data**: Import `.csv` into Excel for detailed analysis
4. **Apply findings**: Use optimal batch sizes in your production code

## Production Usage

Based on your test results, configure your DataGrid:

```csharp
var options = new AdvancedDataGridOptions
{
    OperationMode = PublicDataGridOperationMode.Headless, // From test results
    BatchSize = 10_000,                                   // Optimal from tests
    EnableParallelProcessing = true,
    EnableLinqOptimizations = true,
    EnableCaching = true,
    EnableBatchValidation = false,  // If batch processing
    EnableRealTimeValidation = false
};

var facade = AdvancedDataGridFacadeFactory.CreateStandalone(options);
```

## Need Help?

1. **Detailed Docs**: See `PERFORMANCE_TESTS_README.md`
2. **Test Code**: Review `PERFORMANCE_TESTS.cs`
3. **Results**: Check generated `.txt` file for recommendations

## Summary

This performance testing suite helps you:
- ✅ Understand DataGrid performance characteristics
- ✅ Find optimal batch sizes for your workload
- ✅ Compare operation modes (Headless/Readonly/Interactive)
- ✅ Make data-driven configuration decisions
- ✅ Benchmark your specific hardware
- ✅ Track performance regressions over time

**Happy Testing!** 🚀
