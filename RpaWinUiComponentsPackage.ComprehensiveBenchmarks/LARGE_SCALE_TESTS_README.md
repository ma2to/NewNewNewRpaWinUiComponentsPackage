# 🚀 Large Scale Validation Tests - 1 Million Rows

## Overview

Komplexné testy pre **1 milión riadkov** s **validačnými pravidlami** v **Automatic** móde.

Testy overujú:
- ✅ Import 1M riadkov s validáciou
- ✅ Export 1M riadkov s validáciou
- ✅ Round-trip (Import + Export) s validáciou
- ✅ Performance porovnanie Headless vs Interactive
- ✅ Výkonnostné metriky (CPU, RAM, GC, Threads)

## 📋 Test Suite

### 1. Test_Import_1MillionRows_WithValidation_Headless
- **Popis:** Import 1M riadkov v Headless móde s validáciou
- **Validácia:** 3 pravidlá (Required, Range, Length)
- **Očakávaný čas:** < 5 minút
- **Očakávaná RAM:** < 2GB

### 2. Test_Export_1MillionRows_WithValidation_Headless
- **Popis:** Export 1M riadkov po importe s validáciou
- **Očakávaný čas:** < 5 minút
- **Očakávaná RAM:** < 2GB

### 3. Test_ImportExport_RoundTrip_1MillionRows_WithValidation
- **Popis:** Import + Export round-trip test
- **Očakávaný čas:** < 10 minút

### 4. Test_Import_1MillionRows_WithValidation_Interactive
- **Popis:** Import 1M riadkov v Interactive móde
- **Očakávaný čas:** < 10 minút (pomalšie než Headless)

### 5. Test_Performance_Comparison_1MillionRows_HeadlessVsInteractive
- **Popis:** Performance porovnanie Headless vs Interactive
- **Metriky:** CPU, RAM, Throughput, Speedup

## 🔧 Validačné Pravidlá

Testy definujú 3 automatické validačné pravidlá:

### Rule 1: Column_1_Required (Error)
```csharp
// Column_1 musí byť vyplnený
if (value == null || string.IsNullOrWhiteSpace(value.ToString()))
    return ValidationResult.Error("Column_1 is required");
```

### Rule 2: Column_2_NumericRange (Warning)
```csharp
// Column_2 musí byť v rozsahu 0-1000
if (numValue < 0 || numValue > 1000)
    return ValidationResult.Error("Column_2 must be between 0 and 1000");
```

### Rule 3: Column_3_Length (Warning)
```csharp
// Column_3 max 50 znakov
if (value.ToString().Length > 50)
    return ValidationResult.Error("Column_3 must be less than 50 characters");
```

## 📊 Performance Monitoring

Každý test monitoruje:

```
CPU:
  - Average, Max, Min usage (%)

Memory (Working Set):
  - Average, Max, Min (MB)

Managed Memory:
  - Average, Max, Min (MB)

Garbage Collection:
  - Gen0, Gen1, Gen2 collections

Threads:
  - Average, Max count

Handles:
  - Average, Max count

Throughput:
  - Rows per second
```

## 🎯 Spustenie Testov

### Option 1: Visual Studio Test Explorer
1. Otvor projekt v Visual Studio
2. Test Explorer → Show All Tests
3. Spusti `LargeScaleValidationTests`

### Option 2: Command Line (dotnet test)
```bash
cd RpaWinUiComponentsPackage.ComprehensiveBenchmarks
dotnet test --filter "FullyQualifiedName~LargeScaleValidationTests"
```

### Option 3: Jednotlivý test
```bash
dotnet test --filter "FullyQualifiedName~Test_Import_1MillionRows_WithValidation_Headless"
```

## ⚙️ Konfigurácia

Testy používajú optimalizované nastavenia:

```csharp
options.EnableValidationAlertsColumn = true; // Validácia zapnutá
options.BatchSize = 10000;                   // Veľké batch
options.ParallelProcessingThreshold = 1000;  // Paralelizácia
options.EnableParallelProcessing = true;
options.DegreeOfParallelism = Environment.ProcessorCount;
```

## 📈 Očakávané Výsledky

### Headless Mode
- **Throughput:** 50,000+ rows/sec
- **Memory:** < 1.5 GB
- **Duration:** 2-4 minúty

### Interactive Mode
- **Throughput:** 30,000+ rows/sec
- **Memory:** < 2 GB
- **Duration:** 3-6 minút

### Speedup
- **Headless vs Interactive:** 1.5-2.0x rýchlejší

## ⚠️ Dôležité Poznámky

1. **Validácia je automatic** - pravidlá sa vykonávajú automaticky počas importu
2. **Headless mode je rýchlejší** - žiadne UI overhead
3. **Memory management** - testy overujú, že neprekračujeme 2GB
4. **Validation timeout** - každé pravidlo má 5s timeout
5. **3 validation rules** - dostatočné na overenie funkcionality

## 🐛 Troubleshooting

### "Required components of windows app are missing"
- **Riešenie:** Projekt je self-contained, mal by fungovať bez Windows App Runtime

### Testy trvajú príliš dlho
- **Možná príčina:** Slabý hardware / pomalý disk
- **Riešenie:** Znížiť počet riadkov alebo vypnúť validáciu

### Out of Memory
- **Možná príčina:** Nedostatok RAM
- **Riešenie:** Zavrieť ostatné aplikácie, znížiť BatchSize

## 📝 Test Output Example

```
=== TEST: Import 1 Million Rows with Validation (Headless) ===
Generating 1 million rows...
Defining validation rules...
✓ 3 validation rules registered successfully
  - Column_1: Required (Error)
  - Column_2: Numeric Range 0-1000 (Warning)
  - Column_3: Max Length 50 (Warning)
Starting import with validation...

=== IMPORT RESULTS ===
Success: True
Imported Rows: 1,000,000
Duration: 187.45s
Throughput: 5,335 rows/sec

=== PERFORMANCE REPORT ===
Duration: 187.45s (1874 samples)

CPU:
  Average: 45.23%
  Max: 87.50%
  Min: 12.30%

Memory (Working Set):
  Average: 1,234.56 MB
  Max: 1,456.78 MB
  Min: 987.65 MB

Managed Memory:
  Average: 456.78 MB
  Max: 678.90 MB
  Min: 345.67 MB

Garbage Collection:
  Gen0: 234
  Gen1: 45
  Gen2: 3
```

## ✅ Success Criteria

Test je **úspešný** ak:
1. ✅ Import/Export vrátil `IsSuccess = true`
2. ✅ Importované/Exportované riadky == 1,000,000
3. ✅ Duration < max limit (5-10 min)
4. ✅ Memory < 2GB
5. ✅ Validačné pravidlá boli aplikované
6. ✅ Žiadne exceptions

---

**Created:** 2025-10-11
**Author:** Claude Code
**Version:** 1.0
