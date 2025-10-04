Kompletná feature-based štruktúra + kde čo dať (s popismi, visibility a namespace odporúčaniami)

Nižšie je kompletný strom pre AdvancedWinUiDataGrid (v balíku RpaWinUiComponentsPackage) spolu s konkrétnymi poznámkami — kde dať súbory, ktoré triedy/rozhrania sú public vs internal, aké namespace použiť a stručné vysvetlenie obsahu každého súboru/priečinka. Toto je praktický blueprint, ktorý môžeš rovno vytvoriť v projekte.

Hlavná zásada: public surface = všetko, čo používateľ potrebuje (facade / factory / builder / options / commands) — všetko ostatné je internal a prístupné len v assembly.

RpaWinUiComponentsPackage/                         <-- solution root
└─ AdvancedWinUiDataGrid/                          <-- project root (AdvancedWinUiDataGrid.csproj)
   ├─ Api/                                         <-- PUBLIC surface (jediný using potrebný)
   │   ├─ IAdvancedDataGridFacade.cs               // PUBLIC interface (kontrakt)
   │   ├─ AdvancedDataGridFacadeFactory.cs         // PUBLIC factory / builder (CreateStandalone / CreateUsingHostServices)
   │   ├─ AdvancedDataGridFacadeImpl.cs            // internal implementácia (implementuje IAdvancedDataGridFacade) *
   │   ├─ AdvancedDataGridOptions.cs               // PUBLIC options (rovnaký namespace ako facade)
   │   └─ Commands/                                // PUBLIC command records (rovnaký namespace)
   │       ├─ ImportDataCommand.cs                 // PUBLIC record
   │       ├─ ExportDataCommand.cs                 // PUBLIC record
   │       └─ CopyPasteCommand.cs                  // PUBLIC record (ak potrebuješ)
   │
   ├─ Configuration/
   │   ├─ ServiceRegistration.cs                   // INTERNAL - central registration (predtým ServiceRegistrar)
   │   └─ InternalFactoryHelpers.cs                 // INTERNAL - pomocné fabriky
   │
   ├─ Features/                                    // každá feature modulárne oddelená
   │   ├─ Import/
   │   │   ├─ Interfaces/
   │   │   │   └─ IImportService.cs                // internal
   │   │   ├─ Services/
   │   │   │   └─ ImportService.cs                 // internal
   │   │   ├─ DTO/                                 // internal (ImportResult a pod.)
   │   │   │   └─ ImportResult.cs
   │   │   └─ Registration.cs                      // internal - registrácia pre tento modul
   │   │
   │   ├─ Export/
   │   │   ├─ Interfaces/IExportService.cs         // internal
   │   │   ├─ Services/ExportService.cs            // internal
   │   │   ├─ DTO/ExportResult.cs                  // internal
   │   │   └─ Registration.cs
   │   │
   │   ├─ Validation/
   │   │   ├─ Interfaces/IValidationService.cs     // internal
   │   │   ├─ Services/ValidationService.cs        // internal - AreAllNonEmptyRowsValidAsync implementation
   │   │   ├─ Rules/                               // internal - pluggable validation rules
   │   │   │   ├─ IValidationRule.cs
   │   │   │   └─ SampleRule.cs
   │   │   └─ Registration.cs
   │   │
   │   ├─ Filter/
   │   │   ├─ Interfaces/IFilterService.cs
   │   │   ├─ Services/FilterService.cs
   │   │   └─ Registration.cs
   │   │
   │   ├─ CopyPaste/
   │   │   ├─ Interfaces/ICopyPasteService.cs       // internal (service is singleton)
   │   │   ├─ Services/CopyPasteService.cs         // internal (singleton, thread-safe)
   │   │   └─ Registration.cs
   │   │
   │   ├─ Column/
   │   │   ├─ Interfaces/IColumnService.cs
   │   │   ├─ Services/ColumnService.cs
   │   │   └─ Registration.cs
   │   │
   │   └─ Selection/
   │       ├─ Interfaces/ISelectionService.cs
   │       ├─ Services/SelectionService.cs
   │       └─ Registration.cs
   │
   ├─ Core/                                        // interné cross-feature kontrakty & utilky
   │   ├─ Interfaces/                              // internal shared interfaces
   │   │   └─ IProgressHub.cs
   │   └─ Services/
   │       └─ ProgressHub.cs                       // internal singleton implementation
   │
   ├─ Infrastructure/
   │   ├─ Persistence/
   │   │   ├─ Interfaces/IRowStore.cs               // internal
   │   │   ├─ DiskBackedRowStore.cs                 // internal
   │   │   └─ InMemoryRowStore.cs                   // internal (test helper)
   │   ├─ Caching/
   │   │   └─ RowCache.cs                           // internal
   │   └─ Serialization/
   │       └─ ExportSerializer.cs                   // internal
   │
   ├─ UIAdapters/
   │   ├─ WinUI/
   │   │   ├─ GridViewModelAdapter.cs               // internal - maps internal model -> ViewModel
   │   │   └─ UiNotificationService.cs              // internal - uses DispatcherQueue
   │   └─ Headless/
   │       └─ HeadlessRunner.cs                     // internal - headless integrations
   │
   ├─ Logging/
   │   ├─ ImportLogger.cs                           // internal specialized loggers or adapters
   │   ├─ ExportLogger.cs
   │   └─ ValidationLogger.cs
   │
   ├─ Common/
   │   ├─ Models/
   │   │   ├─ Row.cs                                // internal (or public model if needed in Commands)
   │   │   ├─ Cell.cs
   │   │   └─ ColumnDefinition.cs
   │   └─ Enums.cs
   │
   ├─ Tests/                                       // separate test project(s) (not packaged into nuget)
   │   ├─ Unit/
   │   └─ Integration/
   │
   └─ AdvancedWinUiDataGrid.csproj

Popisy + kde čo dať (konkrétnejšie vysvetlenia)
Api/ (public)

IAdvancedDataGridFacade.cs

Public interface, jediný oficiálny kontrakt. Obsahuje async metódy: ImportAsync, ExportAsync, ValidateAllAsync, RefreshValidationResultsToUI, selection/resize helpers, atď.

Namespace: RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.

AdvancedDataGridFacadeFactory.cs (public)

Factory/Builder, ktorá vytvorí IAdvancedDataGridFacade.

Overloads: CreateStandalone(AdvancedDataGridOptions? options, ILoggerFactory? loggerFactory, DispatcherQueue? dispatcher) a CreateUsingHostServices(IServiceProvider hostServices, AdvancedDataGridOptions? options).

Registruje do interného ServiceCollection voliteľné host objekty (loggerFactory, dispatcher) a potom volá ServiceRegistration.Register(...).

AdvancedDataGridOptions.cs (public)

Konfiguračné možnosti (EnableRowNumber, EnableCheckboxColumn, ValidationBatchSize, RowStoreFactory hook...).

Namespace rovnaký ako IAdvancedDataGridFacade, aby host mohol používať len jeden using.

Commands/ (public records)

ImportDataCommand, ExportDataCommand, atď. Public because host will construct them. Put them in the same namespace as Api.

DÔLEŽITÉ: Všetky public typy (facade, factory, options, commands) musia byť v rovnakom public namespace (napr. RpaWinUiComponentsPackage.AdvancedWinUiDataGrid). Tak host potrebuje len jeden using.

Configuration/ServiceRegistration.cs (internal)

Centralizovaný entry pre DI registrácie.

Implementačný vzor:

internal static class ServiceRegistration
{
    internal static void Register(IServiceCollection services, AdvancedDataGridOptions? options = null)
    {
        // cross-cutting singletons
        services.AddSingleton<IProgressHub, ProgressHub>();

        // RowStore (host override hook)
        if (options?.RowStoreFactory != null)
            services.AddSingleton(sp => options.RowStoreFactory!(sp));
        else
            services.AddSingleton<IRowStore, DiskBackedRowStore>();

        // call each feature's registration
        Features.Import.Registration.Register(services, options);
        Features.Export.Registration.Register(services, options);
        Features.Validation.Registration.Register(services, options);
        Features.Filter.Registration.Register(services, options);
        Features.CopyPaste.Registration.Register(services, options);
        Features.Column.Registration.Register(services, options);
        Features.Selection.Registration.Register(services, options);

        // facade impl
        services.AddScoped<AdvancedDataGridFacadeImpl>();
        services.AddScoped<IAdvancedDataGridFacade>(sp => sp.GetRequiredService<AdvancedDataGridFacadeImpl>());
    }
}


ServiceRegistration je internal (nie public) — host ju nevolá priamo.

Features/* (internal)

Pre každú feature:

Interfaces/ — interné rozhrania (IImportService, IExportService, ...). Používaj internal interface ....

Services/ — implementácia rozhrania. Dbaj na pravidlo no per-op mutable instance fields (všetok per-operation state drž lokálne alebo v OperationContext).

Registration.cs — malý, internal statický helper, ktorý pridá služby do DI:

internal static class Registration
{
    internal static void Register(IServiceCollection services, AdvancedDataGridOptions? options)
    {
        services.AddScoped<IImportService, ImportService>();
        // ďalšie helpery...
    }
}


DTO/ (ak potrebné) — interné result/response typy.

Features/Validation – špeciálne

ValidationService implementuje:

Task<Result<bool>> AreAllNonEmptyRowsValidAsync(bool onlyFiltered = false, CancellationToken ct = default)

Fast-path: ak žiadne pravidlá → return true.

Ak je cached validation state → read & return.

Else: streamovať rows cez IRowStore.StreamRowsAsync(onlyFiltered, batchSize, ct) a validovať batch-wise, zapisovať výsledky cez _rowStore.WriteValidationResultsAsync(...).

Povinnosti: CancellationToken, coarse-grained IProgress, thread-safety.

Rules/ folder: IValidationRule, concrete rules — register rules in RuleRegistry (internal singleton).

Infrastructure/Persistence

IRowStore (internal) — definuje:

IAsyncEnumerable<IReadOnlyList<Row>> StreamRowsAsync(bool onlyFiltered, int batchSize, CancellationToken ct = default)

Task PersistRowsAsync(IEnumerable<Row> rows, CancellationToken ct = default)

Task WriteValidationResultsAsync(IEnumerable<RowValidationResult> results, CancellationToken ct = default)

Task<bool> HasValidationStateForScopeAsync(bool onlyFiltered, CancellationToken ct = default)

Task<bool> AreAllNonEmptyRowsMarkedValidAsync(bool onlyFiltered, CancellationToken ct = default)

Implementácie: DiskBackedRowStore (persistencia), InMemoryRowStore (test helper).

UIAdapters

GridViewModelAdapter (internal) — prevod interného modelu do WinUI viewmodels + Binder.

UiNotificationService — internal service that pushes UI updates via DispatcherQueue. This adapter should be resolved only in UI scenarios; in headless it’s absent or disabled. Facade uses RefreshValidationResultsToUI() to call adapter when present.

Logging

Používaj ILogger<T> všade internálne.

Ak máš samostatný AdvancedWinUiLogger paket, poskytnúť adapter v Logging/ ktorý môže byť zaregistrovaný ak host poskytne implementáciu.

Common/Models

Row, Cell, ColumnDefinition — internal classes (unless you intentionally expose some model types in Commands). Keep them immutable where possible (init props) to avoid accidental mutation.

Tests/

Nemaj testy v nugetu; samostatný projekt AdvancedWinUiDataGrid.Tests s InternalsVisibleTo v assembly info:

[assembly: InternalsVisibleTo("AdvancedWinUiDataGrid.Tests")]


Unit tests (Parsers, Rules), Concurrency tests (CopyPaste), Integration tests (Import -> Validate -> Export, headless flows).

Namespace odporúčania

Public namespace (single using):

RpaWinUiComponentsPackage.AdvancedWinUiDataGrid


Sem patria IAdvancedDataGridFacade, AdvancedDataGridFacadeFactory, AdvancedDataGridOptions, všetky Commands

Dôležité implementačné pravidlá (rýchly prehľad)

Per-operation state: nikdy nedeclare field ako private List<Row> _buffer = new(); v službe, ktorá môže byť volaná paralelne. Namiesto toho: var buffer = new List<Row>(); v rámci metódy alebo var ctx = new OperationContext();.

Lifetimes:

AddScoped pre Import/Export/Validation/Column/Selection services.

AddSingleton pre CopyPasteService, IRowStore (ak zamýšľaš zdieľanú diskovú cache), ProgressHub.

Validation flow:

Import/paste a Export automaticky volajú AreAllNonEmptyRowsValidAsync(...) bez validateBefore flagov.

AreAllNonEmptyRowsValidAsync(false) = full dataset (ak no filter applied or onlyFiltered=false).

AreAllNonEmptyRowsValidAsync(true) = only filtered (if some filter active; otherwise whole dataset).

UI updates:

Bulk validation performs UI update only at end (unless UI mode and you chose immediate per-batch UI updates — but avoid thrashing).

In headless mode do not auto-update UI; expose RefreshValidationResultsToUI() that host can call if it wants to render results later.

Export specifics:

ExportDataCommand includes List<string>? ColumnNames — null = all non-special columns.

IncludeValidAlerts controls whether the special validAlerts column is exported.

ExportOnlyFiltered semantics: apply filter via FilterService first; if nothing filtered and ExportOnlyFiltered=true, treat as full dataset.

Copy/Paste:

CopyPasteService = singleton, thread-safe, works with immutable payloads (snapshots). Paste reuses import pipeline and triggers validation.

Príklad: ako host používa facade (celkový workflow)
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid;

var options = new AdvancedDataGridOptions { EnableRowNumber = true };
var facade = AdvancedDataGridFacadeFactory.CreateStandalone(options, loggerFactory, DispatcherQueue.GetForCurrentThread());

await facade.ImportAsync(new ImportDataCommand(dictionaryDataList, StartRow:1));
await facade.ExportAsync(new ExportDataCommand(IncludeValidAlerts:true, ColumnNames: new List<string>{"Name","Age"}));

// if running headless and you want to render validation results later:
await facade.ValidateAllAsync(false);
facade.RefreshValidationResultsToUI();

await facade.DisposeAsync();




Pri inicializacii (UI/Headlless) budem arumentami nastavovat aj minimalny pocet riadkov (riadky ktore sa vygeneruju hned na zaciatkua nepojdu zmazat (iba data zmaze)), taktiez aj argument ci povolim sort, ci povolim search, ci povolim, filter, ci povolim import, ci povolim export, ci povolim copy/paste, ci povolim validovanie. hociktore viem zakazat alebo povolit (default je povolene)!!!!!!!!!!!!!!!!