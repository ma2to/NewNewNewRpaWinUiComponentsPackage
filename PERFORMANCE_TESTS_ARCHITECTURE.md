# Performance Testing Suite - Architecture & Flow

## 🏗️ System Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                  PERFORMANCE TEST SUITE                     │
│                    (PERFORMANCE_TESTS.cs)                   │
└─────────────────────────────────────────────────────────────┘
                            │
                            │ Creates
                            ▼
┌─────────────────────────────────────────────────────────────┐
│               PerformanceTestRunner                         │
│  - Coordinates all tests                                    │
│  - Manages test matrix (modes × rows × batches × ops)      │
│  - Collects results                                         │
│  - Generates reports                                        │
└─────────────────────────────────────────────────────────────┘
                            │
                            │ Runs
                            ▼
┌─────────────────────────────────────────────────────────────┐
│                    Test Mode Loop                           │
│  ┌───────────────┬───────────────┬───────────────┐         │
│  │   Headless    │   Readonly    │  Interactive  │         │
│  │   Mode        │   Mode        │   Mode        │         │
│  └───────────────┴───────────────┴───────────────┘         │
└─────────────────────────────────────────────────────────────┘
                            │
                            │ For each mode
                            ▼
┌─────────────────────────────────────────────────────────────┐
│                  Row Count Loop                             │
│  ┌──────────────────┬──────────────────┐                   │
│  │   100K rows      │    1M rows       │                   │
│  └──────────────────┴──────────────────┘                   │
└─────────────────────────────────────────────────────────────┘
                            │
                            │ For each row count
                            ▼
┌─────────────────────────────────────────────────────────────┐
│                  Batch Size Loop                            │
│  ┌─────┬─────┬──────┬──────┐                               │
│  │ 1K  │ 5K  │ 10K  │ 50K  │                               │
│  └─────┴─────┴──────┴──────┘                               │
└─────────────────────────────────────────────────────────────┘
                            │
                            │ For each batch size
                            ▼
┌─────────────────────────────────────────────────────────────┐
│                  Operation Tests                            │
│  ┌──────────┬──────────┬────────────┐                      │
│  │  Sort    │  Filter  │ Validation │                      │
│  └──────────┴──────────┴────────────┘                      │
│  ┌──────────┬──────────┬────────────┐                      │
│  │BulkInsert│GetAllRows│UpdateCells │                      │
│  └──────────┴──────────┴────────────┘                      │
└─────────────────────────────────────────────────────────────┘
                            │
                            │ Each test uses
                            ▼
┌─────────────────────────────────────────────────────────────┐
│          IAdvancedDataGridFacade Instance                   │
│  (Created via AdvancedDataGridFacadeFactory)                │
│                                                             │
│  ┌───────────────────────────────────────────────┐         │
│  │  Configure Options:                           │         │
│  │  - Operation mode (Headless/Readonly/Interact)│         │
│  │  - Batch size                                 │         │
│  │  - Disable logging for perf                   │         │
│  │  - Enable parallel processing                 │         │
│  └───────────────────────────────────────────────┘         │
│                                                             │
│  ┌───────────────────────────────────────────────┐         │
│  │  Measure:                                     │         │
│  │  - Time (Stopwatch)                           │         │
│  │  - Memory (GC.GetTotalMemory)                 │         │
│  │  - Throughput (rows/sec)                      │         │
│  └───────────────────────────────────────────────┘         │
└─────────────────────────────────────────────────────────────┘
                            │
                            │ Results collected in
                            ▼
┌─────────────────────────────────────────────────────────────┐
│                    TestResult Objects                       │
│  - Mode, Operation, RowCount, BatchSize                     │
│  - TimeMs, MemoryMB, Throughput                             │
│  - Success, ErrorMessage                                    │
└─────────────────────────────────────────────────────────────┘
                            │
                            │ Aggregated and analyzed
                            ▼
┌─────────────────────────────────────────────────────────────┐
│                   Report Generation                         │
│  ┌─────────────────────┬──────────────────────┐            │
│  │  Text Report (.txt) │  CSV Report (.csv)   │            │
│  │  - Formatted output │  - Excel-compatible  │            │
│  │  - Comparisons      │  - Raw data          │            │
│  │  - Recommendations  │  - Pivot-friendly    │            │
│  └─────────────────────┴──────────────────────┘            │
└─────────────────────────────────────────────────────────────┘
```

## 🔄 Test Execution Flow

```
START
  │
  ├─► Print System Info (OS, CPU, RAM)
  │
  ├─► FOR each Mode in [Headless, Readonly, Interactive]:
  │     │
  │     ├─► Print Mode Header
  │     │
  │     ├─► FOR each RowCount in [100K, 1M]:
  │     │     │
  │     │     ├─► Print Row Count Header
  │     │     │
  │     │     ├─► FOR each BatchSize in [1K, 5K, 10K, 50K]:
  │     │     │     │
  │     │     │     ├─► Print Batch Size Header
  │     │     │     │
  │     │     │     ├─► Test Sort
  │     │     │     │    ├─► Create Facade with config
  │     │     │     │    ├─► Add column (ID, int)
  │     │     │     │    ├─► Insert N rows with random data
  │     │     │     │    ├─► Measure: Sort ascending
  │     │     │     │    ├─► Record result
  │     │     │     │    └─► Dispose facade
  │     │     │     │
  │     │     │     ├─► Test Filter
  │     │     │     │    ├─► Create Facade
  │     │     │     │    ├─► Add column (Value, int)
  │     │     │     │    ├─► Insert N rows with random data
  │     │     │     │    ├─► Measure: Filter GreaterThan 500
  │     │     │     │    ├─► Record result
  │     │     │     │    └─► Dispose facade
  │     │     │     │
  │     │     │     ├─► Test Validation
  │     │     │     │    ├─► Create Facade
  │     │     │     │    ├─► Add column (Email, string)
  │     │     │     │    ├─► Add email validation rule (regex)
  │     │     │     │    ├─► Insert N rows (mix valid/invalid)
  │     │     │     │    ├─► Measure: ValidateAll
  │     │     │     │    ├─► Record result
  │     │     │     │    └─► Dispose facade
  │     │     │     │
  │     │     │     ├─► Test BulkInsert
  │     │     │     │    ├─► Create Facade
  │     │     │     │    ├─► Add column (Data, string)
  │     │     │     │    ├─► Measure: Insert N rows
  │     │     │     │    ├─► Record result
  │     │     │     │    └─► Dispose facade
  │     │     │     │
  │     │     │     ├─► Test GetAllRows
  │     │     │     │    ├─► Create Facade
  │     │     │     │    ├─► Add column and insert N rows
  │     │     │     │    ├─► Measure: GetCurrentData()
  │     │     │     │    ├─► Record result
  │     │     │     │    └─► Dispose facade
  │     │     │     │
  │     │     │     ├─► Test UpdateCells
  │     │     │     │    ├─► Create Facade
  │     │     │     │    ├─► Add column and insert N rows
  │     │     │     │    ├─► Measure: Update 1% of cells
  │     │     │     │    ├─► Record result
  │     │     │     │    └─► Dispose facade
  │     │     │     │
  │     │     │     └─► GC Collect (cleanup between batches)
  │     │     │
  │     │     └─► GC Collect (cleanup between row counts)
  │     │
  │     └─► Continue to next mode
  │
  ├─► Generate Text Report
  │     ├─► System info
  │     ├─► Results by mode
  │     ├─► Comparison table
  │     ├─► Optimal batch sizes
  │     └─► Recommendations
  │
  ├─► Generate CSV Report
  │     └─► All results in tabular format
  │
  ├─► Print Summary
  │
  └─► Wait for user (Press any key)
```

## 📊 Data Flow

```
┌──────────────┐
│ Test Config  │ (Mode, Rows, Batch)
└──────┬───────┘
       │
       ▼
┌──────────────────────────────────────┐
│  Facade Factory                      │
│  AdvancedDataGridFacadeFactory       │
│    .CreateStandalone(options)        │
└──────┬───────────────────────────────┘
       │
       ▼
┌──────────────────────────────────────┐
│  IAdvancedDataGridFacade             │
│  - AddColumn()                       │
│  - AddRowAsync()                     │
│  - SortByColumnAsync()               │
│  - ApplyFilterAsync()                │
│  - ValidateAllAsync()                │
│  - GetCurrentData()                  │
│  - UpdateCellAsync()                 │
└──────┬───────────────────────────────┘
       │
       ▼
┌──────────────────────────────────────┐
│  Measurement Layer                   │
│  - Stopwatch (time)                  │
│  - GC.GetTotalMemory (memory)        │
│  - Calculate throughput              │
└──────┬───────────────────────────────┘
       │
       ▼
┌──────────────────────────────────────┐
│  TestResult                          │
│  {                                   │
│    Mode, Operation, RowCount,        │
│    BatchSize, TimeMs, MemoryMB,      │
│    Success, ErrorMessage             │
│  }                                   │
└──────┬───────────────────────────────┘
       │
       ▼
┌──────────────────────────────────────┐
│  Results Collection                  │
│  List<TestResult> (all tests)        │
└──────┬───────────────────────────────┘
       │
       ▼
┌──────────────────────────────────────┐
│  Analysis & Reporting                │
│  - Group by mode/operation           │
│  - Calculate overhead vs baseline    │
│  - Find optimal batch sizes          │
│  - Generate recommendations          │
└──────┬───────────────────────────────┘
       │
       ▼
┌──────────────────────────────────────┐
│  Output Files                        │
│  - PERFORMANCE_RESULTS_*.txt         │
│  - PERFORMANCE_RESULTS_*.csv         │
└──────────────────────────────────────┘
```

## 🧪 Individual Test Pattern

Each operation test follows this pattern:

```csharp
async Task<TestResult> TestOperation(mode, rowCount, batchSize)
{
    facade = null
    stopwatch = new Stopwatch()
    memBefore = 0
    memAfter = 0

    try:
        // 1. SETUP PHASE
        memBefore = GC.GetTotalMemory(true)  // Force GC first
        facade = CreateFacade(mode, batchSize)

        // 2. DATA PREPARATION
        facade.AddColumn(...)
        for i in 0..rowCount:
            await facade.AddRowAsync(...)

        // 3. RESET MEASUREMENT (exclude setup time)
        stopwatch.Restart()
        memBefore = GC.GetTotalMemory(false)

        // 4. ACTUAL OPERATION TO TEST
        await facade.OperationAsync(...)

        // 5. CAPTURE MEASUREMENTS
        stopwatch.Stop()
        memAfter = GC.GetTotalMemory(false)

        // 6. RECORD RESULT
        return TestResult {
            Mode, Operation, RowCount, BatchSize,
            TimeMs = stopwatch.ElapsedMilliseconds,
            MemoryMB = (memAfter - memBefore) / 1024^2,
            Success = true
        }

    catch (Exception ex):
        return TestResult { Success = false, ErrorMessage = ex.Message }

    finally:
        if facade != null:
            await facade.DisposeAsync()  // Always cleanup
}
```

## 🎯 Test Matrix Dimensions

```
┌─────────────────────────────────────────────────────────────┐
│                    TEST MATRIX                              │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  3 Modes                                                    │
│   ├─ Headless                                              │
│   ├─ Readonly                                              │
│   └─ Interactive                                           │
│                                                             │
│  2 Row Counts                                              │
│   ├─ 100,000                                               │
│   └─ 1,000,000                                             │
│                                                             │
│  4 Batch Sizes                                             │
│   ├─ 1,000                                                 │
│   ├─ 5,000                                                 │
│   ├─ 10,000                                                │
│   └─ 50,000                                                │
│                                                             │
│  6 Operations                                              │
│   ├─ Sort (integer ascending)                             │
│   ├─ Filter (GreaterThan 500)                             │
│   ├─ Validation (email regex)                             │
│   ├─ BulkInsert (string data)                             │
│   ├─ GetAllRows (retrieval)                               │
│   └─ UpdateCells (1% random)                              │
│                                                             │
│  TOTAL TESTS: 3 × 2 × 4 × 6 = 144 tests                   │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

## 📈 Result Analysis Pipeline

```
Raw Results
    │
    ├─► Group by Mode
    │     ├─ Headless results
    │     ├─ Readonly results
    │     └─ Interactive results
    │
    ├─► Calculate Throughput
    │     └─ rows/sec = RowCount / (TimeMs / 1000)
    │
    ├─► Calculate Overhead
    │     └─ % = ((ModeTime - HeadlessTime) / HeadlessTime) × 100
    │
    ├─► Find Optimal Batch Sizes
    │     └─ For each (Mode, Operation, RowCount):
    │           Find BatchSize with minimum TimeMs
    │
    ├─► Generate Comparison Table
    │     └─ Side-by-side mode comparison
    │
    └─► Generate Recommendations
          └─ Based on use case patterns
```

## 🔍 Measurement Accuracy

### Time Measurement
```csharp
var sw = Stopwatch.StartNew();
// ... operation ...
sw.Stop();
// Accuracy: ~microsecond precision
```

### Memory Measurement
```csharp
var before = GC.GetTotalMemory(true);  // Force full GC
// ... operation ...
var after = GC.GetTotalMemory(false);  // Don't force GC
var delta = after - before;
// Accuracy: Approximate due to GC behavior
```

### Throughput Calculation
```csharp
double throughput = RowCount / (TimeMs / 1000.0);
// Unit: rows per second
// Higher = better
```

## 🎨 Output File Structure

### Text Report Structure
```
═══ HEADER ═══
- Title
- Timestamp
- System info

─── MODE RESULTS ───
For each mode:
  For each test:
    - Operation | Rows | BatchSize
    - Time, Throughput, Memory

═══ COMPARISON ═══
Table with all results
+ Overhead calculations

═══ OPTIMAL BATCHES ═══
Best batch per (mode, operation)

═══ RECOMMENDATIONS ═══
Use case specific advice
```

### CSV File Structure
```
Header Row:
  Mode,Operation,RowCount,BatchSize,TimeMs,ThroughputRowsPerSec,MemoryMB,Success,ErrorMessage

Data Rows (one per test):
  Headless,Sort,100000,10000,1156,86505.19,45.2,True,
  Readonly,Sort,100000,10000,1272,78616.35,47.8,True,
  ...
```

## 🧩 Component Dependencies

```
PERFORMANCE_TESTS.cs
    │
    ├─► Microsoft.Extensions.DependencyInjection
    │     └─ For facade factory
    │
    ├─► Microsoft.Extensions.Logging.Abstractions
    │     └─ For null logger pattern
    │
    ├─► RpaWinUiComponentsPackage.AdvancedWinUiDataGrid
    │     ├─ IAdvancedDataGridFacade
    │     ├─ AdvancedDataGridFacadeFactory
    │     ├─ AdvancedDataGridOptions
    │     ├─ PublicColumnDefinition
    │     ├─ PublicDataGridOperationMode
    │     ├─ PublicSortDirection
    │     ├─ PublicFilterOperator
    │     ├─ IValidationRule
    │     └─ ValidationResult
    │
    ├─► System.Diagnostics
    │     └─ Stopwatch
    │
    ├─► System
    │     ├─ GC (memory measurement)
    │     ├─ Environment (system info)
    │     ├─ Console (output)
    │     └─ Random (reproducible data)
    │
    └─► System.IO
          └─ File (report writing)
```

## 🚀 Optimization Opportunities

### Current Implementation
- ✅ Aggressive GC between tests
- ✅ Facade disposal after each test
- ✅ Fixed random seed (reproducibility)
- ✅ Separate measurement windows
- ✅ Real-time console feedback

### Potential Enhancements
- ⚡ Parallel mode testing (currently sequential)
- ⚡ Warmup runs before measurement
- ⚡ Multiple iterations per test (average)
- ⚡ Statistical analysis (std dev, confidence)
- ⚡ Hardware profiling (CPU, disk I/O)
- ⚡ Comparison with previous runs
- ⚡ Visualization charts generation

## 📐 Complexity Analysis

| Operation | Time Complexity | Space Complexity |
|-----------|----------------|------------------|
| Sort | O(n log n) | O(n) |
| Filter | O(n) | O(m) where m = matches |
| Validation | O(n × regex) | O(errors) |
| BulkInsert | O(n) | O(n) |
| GetAllRows | O(1) to O(n) | O(n) |
| UpdateCells | O(updates) | O(1) per update |

## 🎯 Success Metrics

### Test Execution
- ✅ All 144 tests complete
- ✅ No unhandled exceptions
- ✅ Results saved to files

### Data Quality
- ✅ Consistent random seed
- ✅ Clean measurement windows
- ✅ Proper resource disposal

### Output Quality
- ✅ Human-readable text report
- ✅ Machine-readable CSV
- ✅ Actionable recommendations
- ✅ Clear comparisons

---

**This architecture ensures**:
- 🎯 Accurate measurements
- 🔄 Reproducible results
- 📊 Comprehensive coverage
- 🚀 Actionable insights
