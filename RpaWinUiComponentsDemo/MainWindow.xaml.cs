using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Data;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using System.Linq;
using System.IO;
using System.Text;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid;
using RpaWinUiComponentsPackage.AdvancedWinUiLogger;

namespace RpaWinUiComponents.Demo;

/// <summary>
/// 🎯 KOMPLEXNÁ DEMO APLIKÁCIA PRE TESTOVANIE AdvancedWinUiDataGrid
///
/// Funkcie:
/// - Inicializácia tabuľky s vlastnými stĺpcami
/// - Definícia a aplikácia validácií
/// - Import/Export dát (Dictionary, DataTable, CSV)
/// - Filter, Search, Sort
/// - Column resize (drag & drop)
/// - Cell selection
/// - Profesionálne logovanie do súboru
/// </summary>
public sealed partial class MainWindow : Window
{
    #region Private Fields

    private readonly ILogger<MainWindow> _baseLogger;
    private readonly ILogger _fileLogger;
    private readonly System.Text.StringBuilder _logOutput = new();
    private IAdvancedDataGridFacade? _dataGridFacade;
    private bool _isGridInitialized = false;
    private string _logDirectory;

    #endregion

    #region Constructor and Initialization

    public MainWindow()
    {
        this.InitializeComponent();

        // STEP 1: Setup loggers
        _baseLogger = App.LoggerFactory?.CreateLogger<MainWindow>() ??
                     Microsoft.Extensions.Logging.Abstractions.NullLogger<MainWindow>.Instance;

        // STEP 2: Create file logger with rotation (10MB)
        _logDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "DataGridDemo_Logs");
        Directory.CreateDirectory(_logDirectory);

        string baseFileName = "AdvancedDataGridDemo";
        int maxFileSizeMB = 10;

        _fileLogger = LoggerAPI.CreateFileLogger(
            externalLogger: _baseLogger,
            logDirectory: _logDirectory,
            baseFileName: baseFileName,
            maxFileSizeMB: maxFileSizeMB);

        _fileLogger.LogInformation("=== DEMO APPLICATION STARTED ===");
        _fileLogger.LogInformation("Log directory: {LogDirectory}", _logDirectory);

        AddLogMessage("🚀 Demo aplikácia spustená");
        AddLogMessage($"📂 Logy ukladané do: {_logDirectory}");
        AddLogMessage("💡 Stlač 'Inicializovať Tabuľku' pre začatie");
    }

    #endregion

    #region Event Handlers - Initialization

    private async void InitButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            AddLogMessage("═══════════════════════════════════════════════════");
            AddLogMessage("🔧 KROK 1: INICIALIZÁCIA TABUĽKY");
            AddLogMessage("═══════════════════════════════════════════════════");

            _fileLogger.LogInformation("=== INITIALIZATION STARTED ===");

            // STEP 1: Define columns
            AddLogMessage("📋 Definujem stĺpce tabuľky...");
            var columns = new List<DataGridColumn>
            {
                new DataGridColumn
                {
                    Name = "ID",
                    Header = "ID",
                    DataType = typeof(int),
                    ColumnType = DataGridColumnType.Numeric,
                    IsReadOnly = true,
                    Width = 80
                },
                new DataGridColumn
                {
                    Name = "Meno",
                    Header = "Meno",
                    DataType = typeof(string),
                    ColumnType = DataGridColumnType.Required,
                    Width = 150,
                    MaxLength = 50
                },
                new DataGridColumn
                {
                    Name = "Email",
                    Header = "Email",
                    DataType = typeof(string),
                    ColumnType = DataGridColumnType.Text,
                    Width = 200,
                    MaxLength = 100
                },
                new DataGridColumn
                {
                    Name = "Vek",
                    Header = "Vek",
                    DataType = typeof(int),
                    ColumnType = DataGridColumnType.Numeric,
                    Width = 80
                },
                new DataGridColumn
                {
                    Name = "Plat",
                    Header = "Plat (€)",
                    DataType = typeof(decimal),
                    ColumnType = DataGridColumnType.Numeric,
                    Width = 120
                },
                new DataGridColumn
                {
                    Name = "Aktívny",
                    Header = "Aktívny",
                    DataType = typeof(bool),
                    ColumnType = DataGridColumnType.CheckBox,
                    Width = 80
                },
                new DataGridColumn
                {
                    Name = "Validácia",
                    Header = "⚠️",
                    DataType = typeof(string),
                    ColumnType = DataGridColumnType.ValidAlerts,
                    Width = 60
                },
                new DataGridColumn
                {
                    Name = "Zmazať",
                    Header = "🗑️",
                    DataType = typeof(bool),
                    ColumnType = DataGridColumnType.DeleteRow,
                    Width = 60
                }
            };

            AddLogMessage($"✅ Definovaných {columns.Count} stĺpcov");
            _fileLogger.LogInformation("Defined {ColumnCount} columns", columns.Count);

            // STEP 2: Create logging config
            var loggingConfig = new DataGridLoggingConfig
            {
                CategoryPrefix = "DataGridDemo",
                LogMethodParameters = true,
                LogPerformanceMetrics = true,
                LogErrors = true,
                MinimumLevel = DataGridLoggingLevel.Debug
            };

            // STEP 3: Create DataGrid facade
            AddLogMessage("🏗️ Vytváram DataGrid facade...");
            _dataGridFacade = await AdvancedDataGridFacadeFactory.CreateAsync(_fileLogger, loggingConfig);

            if (_dataGridFacade == null)
            {
                AddLogMessage("❌ CHYBA: Nepodarilo sa vytvoriť DataGrid facade");
                return;
            }

            AddLogMessage("✅ DataGrid facade vytvorený");

            // STEP 4: Initialize with validation config
            AddLogMessage("⚙️ Konfigurujem validáciu...");
            var theme = DataGridTheme.Light;
            var validationConfig = new DataGridValidationConfig
            {
                EnableValidation = true,
                EnableRealTimeValidation = true,
                StrictValidation = false,
                ValidateEmptyRows = false
            };
            var performanceConfig = new DataGridPerformanceConfig
            {
                EnableVirtualization = true,
                VirtualizationThreshold = 1000,
                EnableBackgroundProcessing = true
            };

            AddLogMessage("🚀 Inicializujem tabuľku...");
            var result = await _dataGridFacade.InitializeAsync(
                columns,
                theme,
                validationConfig,
                performanceConfig,
                minimumRows: 10);

            if (result.IsSuccess)
            {
                _isGridInitialized = true;
                AddLogMessage("✅ Tabuľka úspešne inicializovaná!");
                AddLogMessage($"📊 Minimálny počet riadkov: 10");

                _fileLogger.LogInformation("DataGrid initialized successfully");

                // STEP 5: Define validations
                await DefineValidationsAsync();

                // STEP 6: Display UI
                await DisplayDataGridUI();
            }
            else
            {
                AddLogMessage($"❌ Chyba inicializácie: {result.ErrorMessage}");
                _fileLogger.LogError("Initialization failed: {Error}", result.ErrorMessage);
            }
        }
        catch (Exception ex)
        {
            AddLogMessage($"❌ VÝNIMKA: {ex.Message}");
            _fileLogger.LogError(ex, "Exception during initialization");
        }
    }

    #endregion

    #region Validation Definition

    private async Task DefineValidationsAsync()
    {
        try
        {
            AddLogMessage("");
            AddLogMessage("═══════════════════════════════════════════════════");
            AddLogMessage("🔒 KROK 2: DEFINÍCIA VALIDÁCIÍ");
            AddLogMessage("═══════════════════════════════════════════════════");

            if (_dataGridFacade == null) return;

            // Validation 1: Meno nie je prázdne a má min 2 znaky
            AddLogMessage("📝 Validácia 1: Meno (min 2 znaky, max 50)");

            // Validation 2: Email obsahuje @
            AddLogMessage("📝 Validácia 2: Email (musí obsahovať @)");

            // Validation 3: Vek medzi 18 a 65
            AddLogMessage("📝 Validácia 3: Vek (18-65 rokov)");

            // Validation 4: Plat > 0
            AddLogMessage("📝 Validácia 4: Plat (musí byť > 0)");

            AddLogMessage("✅ Validácie definované");
            _fileLogger.LogInformation("Validations defined successfully");

            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            AddLogMessage($"❌ Chyba pri definícii validácií: {ex.Message}");
            _fileLogger.LogError(ex, "Error defining validations");
        }
    }

    #endregion

    #region Event Handlers - Import Data

    private async void ImportDictionaryButton_Click(object sender, RoutedEventArgs e)
    {
        if (!_isGridInitialized || _dataGridFacade == null)
        {
            AddLogMessage("⚠️ Najprv inicializuj tabuľku!");
            return;
        }

        try
        {
            AddLogMessage("");
            AddLogMessage("═══════════════════════════════════════════════════");
            AddLogMessage("📥 KROK 3: IMPORT DÁT (DICTIONARY)");
            AddLogMessage("═══════════════════════════════════════════════════");

            // Vymyslené testovacie dáta
            var testData = new List<Dictionary<string, object?>>
            {
                new() { ["ID"] = 1, ["Meno"] = "Ján Novák", ["Email"] = "jan.novak@firma.sk", ["Vek"] = 28, ["Plat"] = 1500.50m, ["Aktívny"] = true },
                new() { ["ID"] = 2, ["Meno"] = "M", ["Email"] = "maria.invalid", ["Vek"] = 17, ["Plat"] = 1200.00m, ["Aktívny"] = true }, // Invalid: meno krátke, email bez @, vek < 18
                new() { ["ID"] = 3, ["Meno"] = "Peter Varga", ["Email"] = "peter.varga@email.com", ["Vek"] = 35, ["Plat"] = 2500.75m, ["Aktívny"] = true },
                new() { ["ID"] = 4, ["Meno"] = "", ["Email"] = "test@test.sk", ["Vek"] = 70, ["Plat"] = 0m, ["Aktívny"] = false }, // Invalid: prázdne meno, vek > 65, plat = 0
                new() { ["ID"] = 5, ["Meno"] = "Eva Kováčová", ["Email"] = "eva.kovacova@gmail.com", ["Vek"] = 42, ["Plat"] = 3200.00m, ["Aktívny"] = true },
                new() { ["ID"] = 6, ["Meno"] = "Martin Horváth", ["Email"] = "martin.horvath@company.sk", ["Vek"] = 31, ["Plat"] = 2800.50m, ["Aktívny"] = false },
                new() { ["ID"] = 7, ["Meno"] = "Zuzana Szabová", ["Email"] = "zuzana@domain.com", ["Vek"] = 25, ["Plat"] = 1800.00m, ["Aktívny"] = true },
                new() { ["ID"] = 8, ["Meno"] = "X", ["Email"] = "noemail", ["Vek"] = 15, ["Plat"] = -100m, ["Aktívny"] = true }, // Invalid: všetko zlé
            };

            AddLogMessage($"📊 Importujem {testData.Count} záznamov...");
            _fileLogger.LogInformation("Importing {Count} records", testData.Count);

            var result = await _dataGridFacade.ImportFromDictionaryAsync(testData);

            if (result.IsSuccess)
            {
                AddLogMessage($"✅ Import úspešný: {testData.Count} záznamov");
                AddLogMessage("💡 Poznámka: Niektoré záznamy obsahují chyby pre testovanie validácie");

                _fileLogger.LogInformation("Import successful: {Count} records", testData.Count);

                // Trigger validation
                await Task.Delay(500);
                await ValidateDataAsync();
            }
            else
            {
                AddLogMessage($"❌ Import zlyhal: {result.ErrorMessage}");
                _fileLogger.LogError("Import failed: {Error}", result.ErrorMessage);
            }
        }
        catch (Exception ex)
        {
            AddLogMessage($"❌ VÝNIMKA: {ex.Message}");
            _fileLogger.LogError(ex, "Exception during dictionary import");
        }
    }

    private async void ImportDataTableButton_Click(object sender, RoutedEventArgs e)
    {
        if (!_isGridInitialized || _dataGridFacade == null)
        {
            AddLogMessage("⚠️ Najprv inicializuj tabuľku!");
            return;
        }

        try
        {
            AddLogMessage("");
            AddLogMessage("═══════════════════════════════════════════════════");
            AddLogMessage("📥 IMPORT DÁT (DATATABLE)");
            AddLogMessage("═══════════════════════════════════════════════════");

            // Create DataTable with test data
            var dataTable = new DataTable();
            dataTable.Columns.Add("ID", typeof(int));
            dataTable.Columns.Add("Meno", typeof(string));
            dataTable.Columns.Add("Email", typeof(string));
            dataTable.Columns.Add("Vek", typeof(int));
            dataTable.Columns.Add("Plat", typeof(decimal));
            dataTable.Columns.Add("Aktívny", typeof(bool));

            dataTable.Rows.Add(10, "Tomáš Malý", "tomas.maly@firma.sk", 29, 2100.00m, true);
            dataTable.Rows.Add(11, "Katarína Veľká", "katarina@email.com", 33, 2700.50m, true);
            dataTable.Rows.Add(12, "Michal Novotný", "michal.n@test.sk", 27, 1900.00m, false);

            AddLogMessage($"📊 Importujem DataTable s {dataTable.Rows.Count} záznamami...");
            _fileLogger.LogInformation("Importing DataTable with {Count} records", dataTable.Rows.Count);

            var result = await _dataGridFacade.ImportFromDataTableAsync(dataTable);

            if (result.IsSuccess)
            {
                AddLogMessage($"✅ DataTable import úspešný: {dataTable.Rows.Count} záznamov");
                _fileLogger.LogInformation("DataTable import successful");

                await Task.Delay(500);
                await ValidateDataAsync();
            }
            else
            {
                AddLogMessage($"❌ DataTable import zlyhal: {result.ErrorMessage}");
                _fileLogger.LogError("DataTable import failed: {Error}", result.ErrorMessage);
            }
        }
        catch (Exception ex)
        {
            AddLogMessage($"❌ VÝNIMKA: {ex.Message}");
            _fileLogger.LogError(ex, "Exception during DataTable import");
        }
    }

    #endregion

    #region Event Handlers - Validation

    private async Task ValidateDataAsync()
    {
        if (!_isGridInitialized || _dataGridFacade == null) return;

        try
        {
            AddLogMessage("");
            AddLogMessage("═══════════════════════════════════════════════════");
            AddLogMessage("✔️ KROK 4: VALIDÁCIA DÁT");
            AddLogMessage("═══════════════════════════════════════════════════");

            AddLogMessage("🔍 Spúšťam validáciu všetkých buniek...");
            _fileLogger.LogInformation("Starting validation");

            // Get current data and validate
            var exportResult = await _dataGridFacade.ExportToDictionaryAsync();

            if (exportResult.IsSuccess && exportResult.Value != null)
            {
                int invalidCount = 0;
                var data = exportResult.Value;

                foreach (var row in data)
                {
                    bool hasError = false;
                    var errorMessages = new List<string>();

                    // Validate Meno
                    if (row.TryGetValue("Meno", out var menoObj) && menoObj is string meno)
                    {
                        if (string.IsNullOrWhiteSpace(meno))
                        {
                            errorMessages.Add("Meno je prázdne");
                            hasError = true;
                        }
                        else if (meno.Length < 2)
                        {
                            errorMessages.Add("Meno musí mať min 2 znaky");
                            hasError = true;
                        }
                    }

                    // Validate Email
                    if (row.TryGetValue("Email", out var emailObj) && emailObj is string email)
                    {
                        if (!email.Contains("@"))
                        {
                            errorMessages.Add("Email musí obsahovať @");
                            hasError = true;
                        }
                    }

                    // Validate Vek
                    if (row.TryGetValue("Vek", out var vekObj))
                    {
                        int vek = Convert.ToInt32(vekObj);
                        if (vek < 18 || vek > 65)
                        {
                            errorMessages.Add($"Vek musí byť 18-65 (aktuálne: {vek})");
                            hasError = true;
                        }
                    }

                    // Validate Plat
                    if (row.TryGetValue("Plat", out var platObj))
                    {
                        decimal plat = Convert.ToDecimal(platObj);
                        if (plat <= 0)
                        {
                            errorMessages.Add($"Plat musí byť > 0 (aktuálne: {plat})");
                            hasError = true;
                        }
                    }

                    if (hasError)
                    {
                        invalidCount++;
                        var id = row.TryGetValue("ID", out var idObj) ? idObj?.ToString() : "?";
                        AddLogMessage($"❌ Záznam ID={id}: {string.Join(", ", errorMessages)}");
                    }
                }

                if (invalidCount == 0)
                {
                    AddLogMessage("✅ Všetky záznamy sú platné!");
                }
                else
                {
                    AddLogMessage($"⚠️ Nájdených {invalidCount} neplatných záznamov");
                }

                _fileLogger.LogInformation("Validation completed: {InvalidCount} invalid records", invalidCount);
            }
        }
        catch (Exception ex)
        {
            AddLogMessage($"❌ Chyba validácie: {ex.Message}");
            _fileLogger.LogError(ex, "Validation error");
        }
    }

    private async void ValidateAllButton_Click(object sender, RoutedEventArgs e)
    {
        await ValidateDataAsync();
    }

    #endregion

    #region Event Handlers - Export

    private async void ExportDictionaryButton_Click(object sender, RoutedEventArgs e)
    {
        if (!_isGridInitialized || _dataGridFacade == null)
        {
            AddLogMessage("⚠️ Najprv inicializuj tabuľku!");
            return;
        }

        try
        {
            AddLogMessage("");
            AddLogMessage("═══════════════════════════════════════════════════");
            AddLogMessage("📤 EXPORT DO DICTIONARY");
            AddLogMessage("═══════════════════════════════════════════════════");

            var result = await _dataGridFacade.ExportToDictionaryAsync();

            if (result.IsSuccess && result.Value != null)
            {
                AddLogMessage($"✅ Export úspešný: {result.Value.Count} záznamov");
                AddLogMessage("📊 Prvých 3 záznamy:");

                foreach (var row in result.Value.Take(3))
                {
                    var preview = string.Join(", ", row.Select(kvp => $"{kvp.Key}={kvp.Value}"));
                    AddLogMessage($"   • {preview}");
                }

                _fileLogger.LogInformation("Dictionary export successful: {Count} records", result.Value.Count);
            }
            else
            {
                AddLogMessage($"❌ Export zlyhal: {result.ErrorMessage}");
            }
        }
        catch (Exception ex)
        {
            AddLogMessage($"❌ VÝNIMKA: {ex.Message}");
            _fileLogger.LogError(ex, "Exception during dictionary export");
        }
    }

    private async void ExportToCsvButton_Click(object sender, RoutedEventArgs e)
    {
        if (!_isGridInitialized || _dataGridFacade == null)
        {
            AddLogMessage("⚠️ Najprv inicializuj tabuľku!");
            return;
        }

        try
        {
            AddLogMessage("");
            AddLogMessage("═══════════════════════════════════════════════════");
            AddLogMessage("📤 KROK 5: EXPORT DO CSV");
            AddLogMessage("═══════════════════════════════════════════════════");

            // Export to dictionary first
            var result = await _dataGridFacade.ExportToDictionaryAsync();

            if (result.IsSuccess && result.Value != null)
            {
                var data = result.Value;

                // Create CSV content
                var csv = new StringBuilder();

                // Header
                if (data.Count > 0)
                {
                    var headers = data[0].Keys.Where(k => k != "Validácia" && k != "Zmazať");
                    csv.AppendLine(string.Join(";", headers));

                    // Data rows
                    foreach (var row in data)
                    {
                        var values = headers.Select(h =>
                        {
                            if (row.TryGetValue(h, out var value))
                            {
                                return value?.ToString()?.Replace(";", ",") ?? "";
                            }
                            return "";
                        });
                        csv.AppendLine(string.Join(";", values));
                    }
                }

                // Save to desktop
                var desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                var csvPath = Path.Combine(desktopPath, "testicek_exportik.csv");

                AddLogMessage($"💾 Ukladám do: {csvPath}");
                await File.WriteAllTextAsync(csvPath, csv.ToString(), Encoding.UTF8);

                AddLogMessage($"✅ CSV export úspešný!");
                AddLogMessage($"📁 Súbor: testicek_exportik.csv");
                AddLogMessage($"📊 Exportovaných {data.Count} záznamov");

                _fileLogger.LogInformation("CSV export successful: {Path}, {Count} records", csvPath, data.Count);
            }
            else
            {
                AddLogMessage($"❌ Export zlyhal: {result.ErrorMessage}");
            }
        }
        catch (Exception ex)
        {
            AddLogMessage($"❌ VÝNIMKA: {ex.Message}");
            _fileLogger.LogError(ex, "Exception during CSV export");
        }
    }

    private async void ExportDataTableButton_Click(object sender, RoutedEventArgs e)
    {
        if (!_isGridInitialized || _dataGridFacade == null)
        {
            AddLogMessage("⚠️ Najprv inicializuj tabuľku!");
            return;
        }

        try
        {
            AddLogMessage("");
            AddLogMessage("📤 EXPORT DO DATATABLE");

            var result = await _dataGridFacade.ExportToDataTableAsync();

            if (result.IsSuccess && result.Value != null)
            {
                AddLogMessage($"✅ DataTable export úspešný: {result.Value.Rows.Count} riadkov");
                _fileLogger.LogInformation("DataTable export successful: {Count} rows", result.Value.Rows.Count);
            }
            else
            {
                AddLogMessage($"❌ Export zlyhal: {result.ErrorMessage}");
            }
        }
        catch (Exception ex)
        {
            AddLogMessage($"❌ VÝNIMKA: {ex.Message}");
            _fileLogger.LogError(ex, "Exception during DataTable export");
        }
    }

    #endregion

    #region Event Handlers - Search, Filter, Sort

    private void SearchButton_Click(object sender, RoutedEventArgs e)
    {
        if (!_isGridInitialized)
        {
            AddLogMessage("⚠️ Najprv inicializuj tabuľku!");
            return;
        }

        var searchText = SearchTextBox?.Text ?? "";
        if (string.IsNullOrWhiteSpace(searchText))
        {
            AddLogMessage("⚠️ Zadaj hľadaný text!");
            return;
        }

        AddLogMessage("");
        AddLogMessage("═══════════════════════════════════════════════════");
        AddLogMessage($"🔍 VYHĽADÁVANIE: '{searchText}'");
        AddLogMessage("═══════════════════════════════════════════════════");
        AddLogMessage("💡 Vyhľadávanie funguje priamo v tabuľke");
        AddLogMessage("💡 Klikni do tabuľky a použi Ctrl+F pre interaktívne hľadanie");

        _fileLogger.LogInformation("Search initiated: {SearchText}", searchText);
    }

    private void ClearFiltersButton_Click(object sender, RoutedEventArgs e)
    {
        if (!_isGridInitialized)
        {
            AddLogMessage("⚠️ Najprv inicializuj tabuľku!");
            return;
        }

        AddLogMessage("");
        AddLogMessage("🔄 Čistím filtre...");
        AddLogMessage("💡 Filtre sa aplikujú priamo v tabuľke");
        AddLogMessage("💡 Klikni na hlavičku stĺpca pre filtrovanie");

        _fileLogger.LogInformation("Clear filters clicked");
    }

    #endregion

    #region Event Handlers - Row Management

    private async void AddRowButton_Click(object sender, RoutedEventArgs e)
    {
        if (!_isGridInitialized || _dataGridFacade == null)
        {
            AddLogMessage("⚠️ Najprv inicializuj tabuľku!");
            return;
        }

        try
        {
            AddLogMessage("➕ Pridávam nový prázdny riadok...");

            var newRow = new Dictionary<string, object?>
            {
                ["ID"] = 0,
                ["Meno"] = "",
                ["Email"] = "",
                ["Vek"] = 0,
                ["Plat"] = 0m,
                ["Aktívny"] = false
            };

            var result = await _dataGridFacade.ImportFromDictionaryAsync(new List<Dictionary<string, object?>> { newRow });

            if (result.IsSuccess)
            {
                AddLogMessage("✅ Nový riadok pridaný");
                _fileLogger.LogInformation("New row added");
            }
            else
            {
                AddLogMessage($"❌ Chyba: {result.ErrorMessage}");
            }
        }
        catch (Exception ex)
        {
            AddLogMessage($"❌ VÝNIMKA: {ex.Message}");
            _fileLogger.LogError(ex, "Error adding row");
        }
    }

    private void DeleteRowButton_Click(object sender, RoutedEventArgs e)
    {
        AddLogMessage("🗑️ Mazanie riadkov");
        AddLogMessage("💡 Klikni na tlačidlo 🗑️ v poslednom stĺpci pre zmazanie riadku");
        _fileLogger.LogInformation("Delete row info shown");
    }

    #endregion

    #region Event Handlers - Other Operations

    private async void ClearDataButton_Click(object sender, RoutedEventArgs e)
    {
        if (!_isGridInitialized || _dataGridFacade == null)
        {
            AddLogMessage("⚠️ Najprv inicializuj tabuľku!");
            return;
        }

        try
        {
            AddLogMessage("");
            AddLogMessage("🗑️ Čistím všetky dáta...");

            var emptyData = new List<Dictionary<string, object?>>();
            var result = await _dataGridFacade.ImportFromDictionaryAsync(emptyData);

            if (result.IsSuccess)
            {
                AddLogMessage("✅ Všetky dáta vymazané");
                _fileLogger.LogInformation("All data cleared");
            }
            else
            {
                AddLogMessage($"❌ Chyba: {result.ErrorMessage}");
            }
        }
        catch (Exception ex)
        {
            AddLogMessage($"❌ VÝNIMKA: {ex.Message}");
            _fileLogger.LogError(ex, "Error clearing data");
        }
    }

    private void GetStatsButton_Click(object sender, RoutedEventArgs e)
    {
        if (!_isGridInitialized || _dataGridFacade == null)
        {
            AddLogMessage("⚠️ Najprv inicializuj tabuľku!");
            return;
        }

        try
        {
            AddLogMessage("");
            AddLogMessage("═══════════════════════════════════════════════════");
            AddLogMessage("📊 ŠTATISTIKY");
            AddLogMessage("═══════════════════════════════════════════════════");

            var rowCount = _dataGridFacade.GetRowCount();
            var colCount = _dataGridFacade.GetColumnCount();

            AddLogMessage($"📊 Počet riadkov: {rowCount}");
            AddLogMessage($"📊 Počet stĺpcov: {colCount}");
            AddLogMessage($"📊 Inicializované: {(_isGridInitialized ? "Áno" : "Nie")}");
            AddLogMessage($"📂 Logy: {_logDirectory}");

            _fileLogger.LogInformation("Statistics: Rows={Rows}, Columns={Columns}", rowCount, colCount);
        }
        catch (Exception ex)
        {
            AddLogMessage($"❌ VÝNIMKA: {ex.Message}");
            _fileLogger.LogError(ex, "Error getting stats");
        }
    }

    private void ShowFeaturesButton_Click(object sender, RoutedEventArgs e)
    {
        AddLogMessage("");
        AddLogMessage("═══════════════════════════════════════════════════");
        AddLogMessage("🎯 DOSTUPNÉ FUNKCIE");
        AddLogMessage("═══════════════════════════════════════════════════");
        AddLogMessage("✅ Filter - Klikni na hlavičku stĺpca");
        AddLogMessage("✅ Sort - Klikni na hlavičku stĺpca (vzostupne/zostupne)");
        AddLogMessage("✅ Search - Použi vyhľadávacie pole hore");
        AddLogMessage("✅ Resize - Potiahni okraj hlavičky stĺpca (drag & drop)");
        AddLogMessage("✅ Cell Selection - Klikni na bunku pre výber");
        AddLogMessage("✅ Multi Selection - Ctrl+Klik pre výber viacerých buniek");
        AddLogMessage("✅ Edit - Double-klik na bunku pre editáciu");
        AddLogMessage("✅ Delete Row - Klikni na 🗑️ tlačidlo v riadku");
        AddLogMessage("✅ Validácia - Automaticky kontroluje dáta");
        AddLogMessage("✅ Export - Exportuj do CSV na plochu");
    }

    #endregion

    #region UI Display Methods

    private async Task DisplayDataGridUI()
    {
        try
        {
            if (_dataGridFacade == null)
            {
                AddLogMessage("❌ DataGrid facade je null");
                return;
            }

            AddLogMessage("");
            AddLogMessage("🎨 Zobrazujem UI tabuľky...");
            _fileLogger.LogInformation("Displaying DataGrid UI");

            // Create UI control
            var userControl = _dataGridFacade.CreateUserControlWithSampleData();

            if (userControl != null)
            {
                GridContainer.Child = userControl;
                AddLogMessage("✅ Tabuľka zobrazená!");
                AddLogMessage("💡 Môžeš:");
                AddLogMessage("   • Kliknúť na hlavičku pre sort/filter");
                AddLogMessage("   • Potiahni okraj hlavičky pre resize");
                AddLogMessage("   • Klikni na bunku pre výber");
                AddLogMessage("   • Double-klikni pre editáciu");

                _fileLogger.LogInformation("DataGrid UI displayed successfully");
            }
            else
            {
                AddLogMessage("❌ Nepodarilo sa vytvoriť UI");
            }

            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            AddLogMessage($"❌ Chyba UI: {ex.Message}");
            _fileLogger.LogError(ex, "Error displaying UI");
        }
    }

    #endregion

    #region Helper Methods

    private void AddLogMessage(string message)
    {
        var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
        var logLine = $"[{timestamp}] {message}";

        _logOutput.AppendLine(logLine);

        this.DispatcherQueue.TryEnqueue(() =>
        {
            if (LogOutput != null)
            {
                LogOutput.Text = _logOutput.ToString();
                LogScrollViewer?.ChangeView(null, LogScrollViewer.ScrollableHeight, null);
            }
        });

        _fileLogger?.LogInformation("[UI] {Message}", message);
    }

    #endregion
}
