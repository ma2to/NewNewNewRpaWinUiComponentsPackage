# Performance Testing Suite - File Index

## 📦 Complete Package Contents

All files have been created in: `D:\www\RB0120APP\NewRpaWinUiComponentsPackage\`

### 🎯 Quick Start (Read This First!)
- **`PERFORMANCE_TESTS_QUICKSTART.md`** (6 KB)
  - TL;DR guide to run tests immediately
  - Expected runtime and results
  - Common customizations
  - Troubleshooting tips

### 📖 Detailed Documentation
- **`PERFORMANCE_TESTS_README.md`** (12 KB)
  - Comprehensive documentation
  - Test methodology and configuration
  - Advanced usage and customization
  - Integration with CI/CD
  - Detailed troubleshooting guide

### 📊 Sample Output
- **`PERFORMANCE_TESTS_SAMPLE_OUTPUT.txt`** (13 KB)
  - Example test results
  - What to expect from your run
  - Key insights and patterns
  - Performance characteristics

### 💻 Executable Files

#### Main Test Implementation
- **`PERFORMANCE_TESTS.cs`** (33 KB, 885 lines)
  - Standalone console application
  - Tests 3 operation modes (Headless/Readonly/Interactive)
  - 6 operations tested per mode
  - Batch size optimization
  - Generates TXT and CSV reports

#### Project Configuration
- **`PerformanceTests.csproj`** (1.5 KB)
  - .NET project file
  - Configured for Windows + WinUI 3
  - References DataGrid component
  - Ready to compile and run

#### Run Scripts
- **`run-performance-tests.ps1`** (3.9 KB)
  - PowerShell automated runner
  - Builds and executes tests
  - Shows results summary
  - **Recommended for Windows PowerShell users**

- **`run-performance-tests.bat`** (1.9 KB)
  - Command Prompt automated runner
  - Simple batch file wrapper
  - **Recommended for CMD users**

## 🚀 How to Get Started

### Step 1: Choose Your Method

**Easiest (PowerShell)**:
```powershell
cd D:\www\RB0120APP\NewRpaWinUiComponentsPackage
.\run-performance-tests.ps1
```

**Easiest (CMD)**:
```cmd
cd D:\www\RB0120APP\NewRpaWinUiComponentsPackage
run-performance-tests.bat
```

**Manual**:
```bash
cd D:\www\RB0120APP\NewRpaWinUiComponentsPackage
dotnet run --project PerformanceTests.csproj --configuration Release
```

### Step 2: Wait for Completion
- **Typical runtime**: 15-50 minutes (depending on hardware)
- **100K rows**: ~5-15 minutes
- **1M rows**: ~20-40 minutes
- Console shows real-time progress

### Step 3: Review Results
Generated files:
- `PERFORMANCE_RESULTS_{timestamp}.txt` - Human-readable report
- `PERFORMANCE_RESULTS_{timestamp}.csv` - Excel/data analysis

## 📋 Test Matrix

### 3 Operation Modes
| Mode | Description | Use Case | Expected Overhead |
|------|-------------|----------|-------------------|
| **Headless** | No UI dispatcher | Batch processing, ETL | 0% (baseline) |
| **Readonly** | UI inactive | Dashboards, reports | +10-30% |
| **Interactive** | Full UI refresh | Live editing | +30-60% |

### 6 Operations Tested
| Operation | Description | Complexity |
|-----------|-------------|------------|
| **Sort** | Integer ascending | O(n log n) |
| **Filter** | GreaterThan (>500) | O(n) |
| **Validation** | Email regex | O(n) × regex |
| **BulkInsert** | Raw insertion | O(n) |
| **GetAllRows** | Data retrieval | O(1) to O(n) |
| **UpdateCells** | 1% random updates | O(updates) |

### Dataset Sizes
- **100,000 rows** × 4 batch sizes × 6 operations × 3 modes = **72 tests**
- **1,000,000 rows** × 4 batch sizes × 6 operations × 3 modes = **72 tests**
- **Total: 144 individual test runs**

### Batch Sizes
- 1,000
- 5,000
- 10,000
- 50,000

## 📈 What You'll Learn

### Performance Characteristics
1. **Baseline Performance** (Headless mode)
   - Pure backend speed without UI overhead
   - Best-case throughput numbers

2. **UI Overhead** (Readonly vs Interactive)
   - Cost of UI dispatcher integration
   - Impact of auto-refresh on performance

3. **Optimal Batch Sizes**
   - Best batch size per operation
   - Different optima for different modes
   - Trade-offs between speed and memory

4. **Scaling Behavior**
   - How operations scale from 100K → 1M rows
   - Expected complexity (linear, log-linear, etc.)
   - Memory pressure at scale

### Actionable Recommendations
- Which mode to use for your use case
- Optimal batch size for your workload
- Configuration settings for best performance
- Trade-offs and optimization strategies

## 🔧 Customization

### Quick Tweaks (Edit `PERFORMANCE_TESTS.cs`)

**Test fewer rows** (faster testing):
```csharp
// Line 44
private readonly int[] _rowCounts = { 100_000 }; // Remove 1M
```

**Test specific batch sizes**:
```csharp
// Line 45
private readonly int[] _batchSizes = { 5_000, 10_000 }; // Only these
```

**Test single operation**:
```csharp
// In TestMode() method, comment out unwanted tests
results.Add(await TestSort(mode, rowCount, batchSize)); // Keep
// results.Add(await TestFilter(mode, rowCount, batchSize)); // Skip
// results.Add(await TestValidation(mode, rowCount, batchSize)); // Skip
```

**Test 10M rows** (requires lots of RAM):
```csharp
// Line 44
private readonly int[] _rowCounts = { 100_000, 1_000_000, 10_000_000 };
```

## 📊 Output File Formats

### Text Report (`PERFORMANCE_RESULTS_{timestamp}.txt`)
```
═══════════════════════════════════════════════════════════════
           DATAGRID PERFORMANCE TEST RESULTS
═══════════════════════════════════════════════════════════════

System: Windows 10.0.22000, 16 cores, .NET 8.0.1
Timestamp: 2025-10-06 14:32:15

─── Headless Mode ───
Operation: Sort | Rows: 100,000 | BatchSize: 10,000
  Time: 1,156 ms
  Throughput: 86,505 rows/sec
  Memory: 45.2 MB

[... detailed results ...]

─── Performance Comparison ───
Operation | Mode | Rows | Batch | Time | Overhead
...

─── Optimal Batch Sizes ───
...

─── Recommendations ───
...
```

### CSV Report (`PERFORMANCE_RESULTS_{timestamp}.csv`)
```csv
Mode,Operation,RowCount,BatchSize,TimeMs,ThroughputRowsPerSec,MemoryMB,Success,ErrorMessage
Headless,Sort,100000,10000,1156,86505.19,45.2,True,
Readonly,Sort,100000,10000,1272,78616.35,47.8,True,
...
```

## 🎯 Expected Results (Typical Hardware)

### Intel i7/i9 or Ryzen 7/9 (16GB+ RAM, SSD)

**100K Rows - Headless Mode:**
- Sort: ~1-2 sec
- Filter: ~0.5-1 sec
- Validation: ~2-3 sec
- BulkInsert: ~0.5-1 sec
- GetAllRows: <100 ms
- UpdateCells: <50 ms

**1M Rows - Headless Mode:**
- Sort: ~10-15 sec
- Filter: ~5-10 sec
- Validation: ~20-30 sec
- BulkInsert: ~5-10 sec
- GetAllRows: ~1 sec
- UpdateCells: ~500 ms

**Mode Overhead:**
- Readonly: +10-30%
- Interactive: +30-60%

**Optimal Batch Sizes:**
- Headless: 10K-50K
- Readonly: 5K-10K
- Interactive: 1K-5K

## 🛠️ Troubleshooting

### Build Issues
```bash
# Clean everything
dotnet clean
rm -rf bin obj
dotnet restore
dotnet build --configuration Release
```

### Runtime Issues
| Problem | Solution |
|---------|----------|
| OutOfMemoryException | Test 100K only, close other apps, add RAM |
| Tests hang | Be patient, some ops take 5-10 min on 1M rows |
| No output files | Check console errors, verify write permissions |
| DispatcherQueue warning | Expected for console apps, tests continue in Headless |

### Performance Issues
| Symptom | Likely Cause | Solution |
|---------|--------------|----------|
| Very slow | Debug mode | Use Release mode |
| High memory | Too many rows | Start with 100K |
| Inconsistent results | Background apps | Close other apps |

## 📚 Documentation Hierarchy

```
Start Here → PERFORMANCE_TESTS_QUICKSTART.md
              ↓
         Need Details? → PERFORMANCE_TESTS_README.md
              ↓
         See Example? → PERFORMANCE_TESTS_SAMPLE_OUTPUT.txt
              ↓
         Run Tests → run-performance-tests.ps1 or .bat
              ↓
         View Results → PERFORMANCE_RESULTS_{timestamp}.txt
              ↓
         Analyze Data → PERFORMANCE_RESULTS_{timestamp}.csv
```

## 🎓 Learning Path

### Beginner
1. Read `PERFORMANCE_TESTS_QUICKSTART.md`
2. Run `.\run-performance-tests.ps1`
3. Review generated `.txt` file
4. Apply recommended batch sizes

### Intermediate
1. Read `PERFORMANCE_TESTS_README.md`
2. Review `PERFORMANCE_TESTS_SAMPLE_OUTPUT.txt`
3. Customize test parameters
4. Compare results across hardware

### Advanced
1. Modify `PERFORMANCE_TESTS.cs` source
2. Add custom operations
3. Import `.csv` into Excel for analysis
4. Integrate into CI/CD pipeline
5. Track performance over time

## 📦 File Summary

| File | Size | Purpose |
|------|------|---------|
| `PERFORMANCE_TESTS.cs` | 33 KB | Main test implementation |
| `PerformanceTests.csproj` | 1.5 KB | Project configuration |
| `run-performance-tests.ps1` | 3.9 KB | PowerShell runner |
| `run-performance-tests.bat` | 1.9 KB | Batch runner |
| `PERFORMANCE_TESTS_README.md` | 12 KB | Full documentation |
| `PERFORMANCE_TESTS_QUICKSTART.md` | 6 KB | Quick start guide |
| `PERFORMANCE_TESTS_SAMPLE_OUTPUT.txt` | 13 KB | Example results |
| `PERFORMANCE_TESTS_INDEX.md` | This file | File navigator |

**Total Package Size**: ~75 KB

## 🚀 Next Steps

1. **Run the tests**
   ```powershell
   .\run-performance-tests.ps1
   ```

2. **Review results**
   - Open `PERFORMANCE_RESULTS_{timestamp}.txt`
   - Read the recommendations section

3. **Apply findings**
   ```csharp
   var options = new AdvancedDataGridOptions
   {
       OperationMode = PublicDataGridOperationMode.Headless,
       BatchSize = 10_000, // From test results
       EnableParallelProcessing = true
   };
   ```

4. **Monitor over time**
   - Save results for baseline
   - Re-run after code changes
   - Track performance regressions

## ✅ Success Criteria

After running tests, you should have:
- ✅ `.txt` report with recommendations
- ✅ `.csv` data for analysis
- ✅ Understanding of optimal batch sizes
- ✅ Knowledge of mode overhead
- ✅ Confidence in configuration choices

## 📞 Support

**Issues?**
1. Check troubleshooting sections in README
2. Review console output for errors
3. Compare against sample output
4. Test with smaller datasets first

**Questions?**
1. Read the detailed README
2. Examine the test source code
3. Review generated reports

---

**Ready to start?** Run `.\run-performance-tests.ps1` now! 🚀
