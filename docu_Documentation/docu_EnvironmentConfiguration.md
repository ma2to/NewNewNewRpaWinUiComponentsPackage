# ENVIRONMENT-SPECIFIC CONFIGURATION - Špecifikácia

## 📋 METADATA

**Dátum vytvorenia:** 20.10.2025
**Verzia:** 1.0
**Jazyk kódu:** English
**Jazyk dokumentácie:** Slovenčina
**Komponent:** AdvancedDataGrid - Environment Configuration Module
**Odhadovaný čas vývoja:** 3-4 dni

---

## 🎯 PREHĽAD

Tento dokument špecifikuje Environment-Specific Configuration systém pre AdvancedDataGrid komponent. Systém poskytuje plne konfigurovateľnú podporu pre ľubovoľné prostredia (environments), ktoré si definuje vývojár. Config sa načíta pri **štarte aplikácie** a ostane fixný až do **reštartu** (BEZ hot-reload).

---

## 📐 BOD 3: Environment-Specific Configuration (BEZ hot-reload)

**Odhadovaný čas vývoja: 3-4 dni**

### **Čo to je:**

Plne **konfigurovateľná** podpora pre ľubovoľné prostredia, ktoré si definuješ ty. Config sa načíta pri **štarte aplikácie** a ostane fixný až do **reštartu**.

**Kľúčové vlastnosti:**
- ✅ **Plne konfigurovateľné** - definuješ si VLASTNÉ prostredia (nie hardcoded Development/Staging/Production)
- ✅ **BEZ hot-reload** - config je read-only počas behu aplikácie
- ✅ **Automatic detection** - aplikácia si sama vyberie prostredie podľa pravidiel
- ✅ **Export/Import** - sharuj configy medzi team members
- ✅ **Multi-region support** - ProductionEU vs ProductionUS s rôznymi nastaveniami

---

### **API Špecifikácia:**

#### **1. Definícia prostredí pomocou Builder pattern:**

```csharp
// TY definuješ svoje vlastné prostredia (nie hardcoded Development/Staging/Production)
var envConfig = new EnvironmentConfigurationBuilder()
    .AddEnvironment("LocalDev", new {
        PageSize = 10,
        LogLevel = LogLevel.Debug,
        EnableProfiling = true,
        DatabasePath = "C:\\Dev\\local.db"
    })
    .AddEnvironment("TestServer", new {
        PageSize = 100,
        LogLevel = LogLevel.Info,
        EnableProfiling = false,
        DatabasePath = "C:\\TestServer\\test.db"
    })
    .AddEnvironment("ProductionEU", new {
        PageSize = 1000,
        LogLevel = LogLevel.Warning,
        EnableProfiling = false,
        DatabasePath = "D:\\Production\\eu.db"
    })
    .AddEnvironment("ProductionUS", new {
        PageSize = 1000,
        LogLevel = LogLevel.Warning,
        EnableProfiling = false,
        DatabasePath = "D:\\Production\\us.db"
    })
    .AddEnvironment("CustomEnv1", new {
        // Vlastné nastavenia
    })
    .AddEnvironment("CustomEnv2", new {
        // Vlastné nastavenia
    })
    .Build();
```

#### **2. Načítanie prostredia pri štarte aplikácie:**

```csharp
// Explicitný výber prostredia
var config = await facade.Configuration.LoadEnvironmentConfigAsync("ProductionEU");
// Config sa aplikuje a ostane fixný až do reštartu aplikácie
```

#### **3. Automatická detekcia prostredia na základe pravidiel:**

```csharp
var config = await facade.Configuration.DetectAndLoadEnvironmentAsync(
    detectionRules: new[] {
        new EnvironmentDetectionRule {
            EnvironmentName = "LocalDev",
            Condition = () => Environment.MachineName == "MY-DEV-PC"
        },
        new EnvironmentDetectionRule {
            EnvironmentName = "ProductionEU",
            Condition = () => Environment.MachineName.StartsWith("PROD-EU-")
        },
        new EnvironmentDetectionRule {
            EnvironmentName = "ProductionUS",
            Condition = () => Environment.MachineName.StartsWith("PROD-US-")
        }
    }
);
```

#### **4. Export/Import environment configov:**

```csharp
// Export environment config do JSON
await facade.Configuration.ExportEnvironmentConfigAsync("ProductionEU", "prod_eu_config.json");

// Import environment config z JSON
var imported = await facade.Configuration.ImportEnvironmentConfigAsync("prod_eu_config.json");
```

#### **5. Zoznam dostupných prostredí:**

```csharp
var environments = await facade.Configuration.GetAvailableEnvironmentsAsync();
// Vráti: ["LocalDev", "TestServer", "ProductionEU", "ProductionUS", "CustomEnv1", "CustomEnv2"]
```

---

### **JSON formát pre environment config:**

```json
{
  "environments": {
    "LocalDev": {
      "pageSize": 10,
      "logLevel": "Debug",
      "enableProfiling": true,
      "databasePath": "C:\\Dev\\local.db"
    },
    "ProductionEU": {
      "pageSize": 1000,
      "logLevel": "Warning",
      "enableProfiling": false,
      "databasePath": "D:\\Production\\eu.db"
    },
    "ProductionUS": {
      "pageSize": 1000,
      "logLevel": "Warning",
      "enableProfiling": false,
      "databasePath": "D:\\Production\\us.db"
    }
  },
  "activeEnvironment": "LocalDev"
}
```

---

### **Workflow pri zmene prostredia:**

**DÔLEŽITÉ:** Zmena prostredia vyžaduje **reštart aplikácie** (ŽIADNE hot-reload).

```csharp
// 1. Aplikácia beží s "LocalDev" konfiguráciou

// 2. Chceš prepnúť na "ProductionEU"
await facade.Configuration.SetActiveEnvironmentAsync("ProductionEU");
// -> Uloží sa do config súboru, ale aplikácia STÁLE používa "LocalDev"

// 3. Musíš reštartovať aplikáciu
// Application.Restart() alebo manuálny reštart

// 4. Pri nasledujúcom štarte:
var config = await facade.Configuration.LoadActiveEnvironmentAsync();
// -> Načíta "ProductionEU" config z JSON súboru
```

---

### **Kedy to používať:**

#### **Prečo by si to mal mať:**
- ✅ **Flexibilita** - definuješ si VLASTNÉ prostredia (nie len Dev/Staging/Prod)
- ✅ **Multi-region support** - ProductionEU vs ProductionUS s rôznymi nastaveniami
- ✅ **Easy configuration** - jeden JSON súbor s všetkými prostrediami
- ✅ **Export/Import** - sharuj configy medzi team members
- ✅ **Automatic detection** - aplikácia si sama vyberie prostredie podľa pravidiel
- ✅ **Custom environments** - môžeš mať 10+ vlastných prostredí podľa potreby
- ✅ **Jednoduchosť** - bez hot-reload complexity, config je read-only počas behu

#### **Kedy to NEPOTREBUJEŠ:**
- ❌ Jednoduchá desktop aplikácia s jedným prostredím
- ❌ Nemáš viacero prostredí (dev/test/prod)
- ❌ Všade používaš rovnaký config

---

### **Príklad use-case - Enterprise deployment:**

```
SCENÁR: Firma má 5 prostredí

1. LocalDev (developer PC)
   - PageSize = 10 (malý dataset pre rýchly testing)
   - LogLevel = Debug (verbose logging)
   - EnableProfiling = true
   - DatabasePath = "C:\\Dev\\local.db"

2. TestServer (QA server)
   - PageSize = 100
   - LogLevel = Info
   - EnableProfiling = false
   - DatabasePath = "C:\\TestServer\\test.db"

3. StagingServer (staging pred production)
   - PageSize = 1000
   - LogLevel = Info
   - EnableProfiling = false
   - DatabasePath = "D:\\Staging\\staging.db"

4. ProductionEU (production európsky region)
   - PageSize = 1000
   - LogLevel = Warning
   - EnableProfiling = false
   - DatabasePath = "D:\\Production\\eu.db"

5. ProductionUS (production americký region)
   - PageSize = 1000
   - LogLevel = Warning
   - EnableProfiling = false
   - DatabasePath = "D:\\Production\\us.db"

AUTOMATIC DETECTION:
- Aplikácia sa spustí na "DEV-JOHN-PC" → Auto-select "LocalDev"
- Aplikácia sa spustí na "PROD-EU-SERVER-01" → Auto-select "ProductionEU"
- Aplikácia sa spustí na "PROD-US-SERVER-01" → Auto-select "ProductionUS"

CONFIG SHARING:
- IT department vytvorí "prod_eu_config.json" a distribuuje ho všetkým EU serverom
- DevOps team upraví "staging_config.json" a komitne do Git repo
```

---

### **Implementačné detaily:**

#### **1. EnvironmentConfigurationBuilder**

```csharp
namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Configuration;

public class EnvironmentConfigurationBuilder
{
    private readonly Dictionary<string, object> _environments = new();

    public EnvironmentConfigurationBuilder AddEnvironment(string name, object config)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Environment name cannot be null or empty", nameof(name));

        if (_environments.ContainsKey(name))
            throw new InvalidOperationException($"Environment '{name}' already exists");

        _environments[name] = config;
        return this;
    }

    public EnvironmentConfiguration Build()
    {
        if (_environments.Count == 0)
            throw new InvalidOperationException("At least one environment must be defined");

        return new EnvironmentConfiguration(_environments);
    }
}
```

#### **2. EnvironmentDetectionRule**

```csharp
public class EnvironmentDetectionRule
{
    public string EnvironmentName { get; init; } = "";
    public Func<bool> Condition { get; init; } = () => false;
}
```

#### **3. IEnvironmentConfiguration Interface**

```csharp
public interface IEnvironmentConfiguration
{
    /// <summary>
    /// Load configuration for specific environment
    /// </summary>
    Task<object> LoadEnvironmentConfigAsync(string environmentName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Detect and load environment based on rules
    /// </summary>
    Task<object> DetectAndLoadEnvironmentAsync(
        IEnumerable<EnvironmentDetectionRule> detectionRules,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Export environment config to JSON file
    /// </summary>
    Task ExportEnvironmentConfigAsync(
        string environmentName,
        string filePath,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Import environment config from JSON file
    /// </summary>
    Task<object> ImportEnvironmentConfigAsync(
        string filePath,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get list of available environments
    /// </summary>
    Task<IReadOnlyList<string>> GetAvailableEnvironmentsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Set active environment (requires restart to take effect)
    /// </summary>
    Task SetActiveEnvironmentAsync(
        string environmentName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Load active environment config
    /// </summary>
    Task<object> LoadActiveEnvironmentAsync(CancellationToken cancellationToken = default);
}
```

---

## 📊 ODHADOVANÝ ČAS VÝVOJA

**Bod 3 (Environment-Specific Config bez hot-reload):** **3-4 dni**

**Rozdelenie:**
- Model classes (EnvironmentConfiguration, EnvironmentDetectionRule, atď.) - 0.5 dňa
- EnvironmentConfigurationBuilder - 0.5 dňa
- EnvironmentConfigurationService implementácia - 1 deň
- JSON serialization/deserialization - 0.5 dňa
- Auto-detection logic - 0.5 dňa
- Testing - 0.5 dňa

---

## ✅ ZÁVER

**Bod 3 (Environment-Specific Config):**
- ✅ Plne konfigurovateľné prostredia (definuješ si vlastné, nie hardcoded)
- ✅ BEZ hot-reload - config sa načíta pri štarte a ostane fixný
- ✅ Jednoduchšia implementácia - bez komplexity hot-reload
- ✅ Užitočné ak máš viacero prostredí (dev/test/prod/multi-region)
- ✅ Nie je to potrebné pre jednoduchú desktop app s jedným prostredím

**Kľúčové rozdiely oproti hot-reload prístupu:**
- ❌ **Nemôžeš** meniť config za behu - vyžaduje reštart
- ✅ **Jednoduchšie** - žiadne event handlers, race conditions, memory leak riziká
- ✅ **Thread-safe** - config je read-only počas behu
- ✅ **Predictable** - aplikácia má konzistentný stav počas celého behu
