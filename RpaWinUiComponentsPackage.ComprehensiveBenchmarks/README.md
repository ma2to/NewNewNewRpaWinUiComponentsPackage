# AdvancedWinUiDataGrid Comprehensive Benchmark Suite

Komplexná testovacia sada pre AdvancedWinUiDataGrid komponent s meraním výkonu, stability, využitia CPU/RAM a ďalších metrík.

## 📋 Obsah testov

### 1. Interactive Mode Tests (`InteractiveModeTests.cs`)
Testy pre bežný interaktívny UI režim:
- ✅ **Funkcionalita**: LoadData, GetCell, SetCell, AddRow, RemoveRow, Sort, Filter, Select
- ✅ **Stabilita**: Stress test (10,000 operácií), Memory stability (10 cyklov)
- ✅ **Výkon**: Throughput test (ops/sec)
- 📊 **Metriky**: CPU, RAM, GC, Threads, Handles

### 2. Headless + Manual UI Update Mode Tests (`HeadlessManualUpdateModeTests.cs`)
Testy pre headless režim s manuálnymi UI refreshmi:
- ✅ **Batch operácie** s periodickým refreshom UI
- ✅ **Bulk import** s odloženým UI
- ✅ **Background výpočty** s UI snapshotmi
- ✅ **Performance porovnanie** headless vs interactive
- ✅ **Stabilita**: Long-running ops, Concurrent ops
- ✅ **Resource efficiency**: CPU a Memory usage

### 3. Pure Headless Mode Tests (`HeadlessModeTests.cs`)
Testy pre čistý headless režim (bez UI):
- ✅ **Maximum throughput**: Data loading, Bulk ops, Streaming
- ✅ **Extreme scale**: Million row ops, Wide tables
- ✅ **Resource efficiency**: CPU, Memory, Threads
- ✅ **Stabilita**: 24-hour simulation, Error recovery, Concurrent stress
- ✅ **Data integrity**: Verification tests

### 4. Comprehensive Stability Tests (`ComprehensiveStabilityTests.cs`)
Komplexné stability testy pre všetky režimy:
- ✅ **Memory leaks**: Repeated init, Event handler cleanup
- ✅ **Thread safety**: Concurrent ops, Race conditions
- ✅ **Error handling**: Invalid ops, Exception recovery
- ✅ **Long-running**: 24/7 server simulation, Continuous streaming
- ✅ **Cross-mode**: Interactive ↔ Headless transitions

## 🚀 Spustenie testov

### Príprava
```bash
cd RpaWinUiComponentsPackage.ComprehensiveBenchmarks
dotnet restore
dotnet build
```

### Spustenie testov

#### Všetky testy (niekoľko hodín)
```bash
dotnet run -- all
```

#### Len Interactive Mode
```bash
dotnet run -- interactive
```

#### Len Headless + Manual Update Mode
```bash
dotnet run -- headless-manual
```

#### Len Pure Headless Mode
```bash
dotnet run -- headless
```

#### Len Stability testy
```bash
dotnet run -- stability
```

#### Rýchly test (malé datasety)
```bash
dotnet run -- quick
```

### Bez parametrov (zobrazí menu)
```bash
dotnet run
```

## 📊 Merané metriky

### CPU Metrics
- **Average CPU Usage** - priemerné využitie CPU (%)
- **Max CPU Usage** - maximálne využitie CPU (%)
- **Min CPU Usage** - minimálne využitie CPU (%)

### Memory Metrics
- **Working Set** - fyzická pamäť používaná procesom (MB)
- **Private Memory** - privátna pamäť procesu (MB)
- **Managed Memory** - .NET managed heap (MB)
- **Average / Max / Min** pre každú metriku

### Garbage Collection Metrics
- **Gen0 Collections** - počet Gen0 GC
- **Gen1 Collections** - počet Gen1 GC
- **Gen2 Collections** - počet Gen2 GC (indikátor memory leaks)

### Thread Metrics
- **Average Thread Count** - priemerný počet vlákien
- **Max Thread Count** - maximálny počet vlákien

### Handle Metrics
- **Average Handle Count** - priemerný počet handles
- **Max Handle Count** - maximálny počet handles

### Performance Metrics
- **Throughput** - operácie za sekundu (ops/sec)
- **Latency** - trvanie operácie (ms)
- **Duration** - celkové trvanie testu

## 📁 Výsledky testov

Výsledky sa ukladajú do priečinka `BenchmarkDotNet.Artifacts/`:

- **`*.html`** - HTML reporty s vizualizáciou
- **`*.md`** - Markdown reporty (GitHub compatible)
- **`*.csv`** - CSV dáta pre Excel/Google Sheets
- **`*.json`** - JSON dáta pre automatické spracovanie
- **`ComprehensiveBenchmarkSummary.md`** - Súhrnný report

## 🔍 Analýza výsledkov

### 1. Otvorenie HTML reportov
```bash
cd BenchmarkDotNet.Artifacts
start results-*.html
```

### 2. Import do Excelu
1. Otvor Excel
2. Import CSV súbory z `BenchmarkDotNet.Artifacts/`
3. Vytvor grafy pre vizualizáciu

### 3. Vyhľadávanie problémov

#### Memory Leaks
- Sleduj **Gen2 Collections** - vysoké číslo indikuje leak
- Porovnaj **Min vs Max Managed Memory** - veľký rozdiel = problém

#### CPU Problémy
- **Max CPU > 90%** dlhodobo = neefektívny kód
- **Average CPU < 20%** = nevyužité zdroje

#### Thread Problémy
- **Thread Count** rastie = thread leak
- **Max Thread Count > 100** = príliš veľa vlákien

## 📈 Testované scenáre

### Dataset Sizes
- **Small**: 100-1,000 rows
- **Medium**: 5,000-10,000 rows
- **Large**: 50,000-100,000 rows
- **Extreme**: 500,000-1,000,000 rows

### Column Counts
- **Narrow**: 10 columns
- **Normal**: 20 columns
- **Wide**: 50-200 columns

### Operation Types
- **Read**: GetCellValue (1,000-100,000 ops)
- **Write**: SetCellValue (1,000-100,000 ops)
- **Bulk**: LoadData, AppendData
- **Complex**: Sort, Filter, Search

### Concurrency Levels
- **Single**: 1 thread
- **Low**: 10 threads
- **Medium**: 20 threads
- **High**: 50 threads

## 🎯 Očakávané výsledky

### Interactive Mode
- **Throughput**: >100 ops/sec
- **Memory**: <500 MB pre 10K rows
- **CPU**: <80% average
- **Gen2 GC**: <50 za test

### Headless + Manual Update Mode
- **Throughput**: 2-3x rýchlejší ako Interactive
- **Memory**: podobná ako Interactive
- **CPU**: <85% average
- **UI Refreshes**: kontrolované a predvídateľné

### Pure Headless Mode
- **Throughput**: >5,000 ops/sec
- **Memory**: <2 GB pre 1M rows
- **CPU**: <85% average
- **Gen2 GC**: <100 za test
- **Best for**: Server-side processing

## 🔧 Konfigurácia testov

### Upravenie parametrov
V test súboroch (napr. `InteractiveModeTests.cs`):

```csharp
[Params(100, 1000, 5000, 10000)]
public int RowCount { get; set; }

[Params(10, 20, 50)]
public int ColumnCount { get; set; }
```

### Úprava sampling frequency
V `PerformanceMonitor.cs`:

```csharp
_perfMonitor.StartMonitoring(100); // 100ms interval
```

### Úprava warmup/iteration counts
```csharp
[SimpleJob(RuntimeMoniker.Net80, warmupCount: 3, iterationCount: 10)]
```

## 📝 Odporúčania

### Kedy použiť Interactive Mode
- Real-time user interaction
- Small to medium datasets (<10K rows)
- UI responsiveness je kritická

### Kedy použiť Headless + Manual Update Mode
- Batch processing s progress reporting
- Import veľkých dát s periodickým UI update
- Background computations s vizualizáciou

### Kedy použiť Pure Headless Mode
- Server-side processing
- API endpoints
- Data transformation pipelines
- Maximum performance požiadavky
- Very large datasets (>100K rows)

## 🐛 Troubleshooting

### Performance Counter errors
```
Warning: Performance counters not available
```
**Riešenie**: Spusti ako Administrator alebo ignoruj (fallback na Process metrics)

### Out of Memory
**Riešenie**: Zníž `RowCount` parameter alebo použi headless mode

### Testy trvajú príliš dlho
**Riešenie**: Použi `dotnet run -- quick` alebo zníž `iterationCount`

## 📞 Support

Pre problémy alebo otázky:
1. Skontroluj `BenchmarkDotNet.Artifacts/` logs
2. Skontroluj Windows Event Viewer
3. Zapni verbose logging v testoch

## 🔄 CI/CD Integration

### GitHub Actions
```yaml
- name: Run Benchmarks
  run: |
    cd RpaWinUiComponentsPackage.ComprehensiveBenchmarks
    dotnet run -- quick

- name: Upload Results
  uses: actions/upload-artifact@v3
  with:
    name: benchmark-results
    path: BenchmarkDotNet.Artifacts/
```

### Azure DevOps
```yaml
- task: DotNetCoreCLI@2
  inputs:
    command: 'run'
    arguments: '-- quick'
    workingDirectory: 'RpaWinUiComponentsPackage.ComprehensiveBenchmarks'
```

## 📚 Ďalšie zdroje

- [BenchmarkDotNet Documentation](https://benchmarkdotnet.org/)
- [.NET Performance Tips](https://docs.microsoft.com/en-us/dotnet/core/performance/)
- [Memory Management Best Practices](https://docs.microsoft.com/en-us/dotnet/standard/garbage-collection/memory-management-and-gc)
