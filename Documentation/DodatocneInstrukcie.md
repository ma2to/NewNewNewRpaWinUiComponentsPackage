Doplnkové odporúčania (bezpečnosť a lifecycle)

Používaj CancellationToken vždy — umožní zrušiť operáciu bezpečne a nechať resources uvoľniť.

Preperuj background tasks: pri spúšťaní importu/exportu zo služby, vytvor nový DI scope (CreateScope()), získať Scoped service v tom scope a nechať ho skončiť po dokončení.

Pri Singletone neukladaj per-operation objekty ako polia — používaj ConcurrentDictionary<Guid, OperationState> alebo immutable snapshots.

Ak potrebuješ že progress vždy updatne UI, preferuj IProgress<T> (zaručí marshaling do UI thread ak ho voláš z UI kontextu).

Malý checklist pre implementáciu u teba

 IImportService a IExportService majú byť Scoped (alebo každopádne inštancia na operáciu).

 ICopyPasteService ako Singleton (ale bez mutable per-operation polia).

 Pre progress: použij IProgress<T> alebo ProgressHub s per-operation ID.

 Používaj CancellationToken a uvoľňuj zdroje v finally.

 Pri background work: vytvor nový DI scope pre každý import/export.

 tie zásady platia aj v hybridnom prostredí (DI + „hybrid functional/OOP“). Nižšie ti dávam konkrétne vysvetlenie, prečo to platí, a praktické šablóny / alternatívy vhodné pre hybridný štýl (immutability, funkčné prvky, factory-based DI), plus krátke kódy/príklady, ktoré môžeš okamžite použiť alebo vložiť do dokumentácie.

Prečo to stále platí (stručne)

Scoped pre IImportService / IExportService: každá import/export operácia má dočasný, per-operation stav (parsovanie, buffer, progress, cancellation). Ak by ste používali jednu globálnu inštanciu (singleton) a ten stav by bol mutable, rôzne súbežné operácie si ho budú navzájom prepisovať -> závody a nekonzistentné výsledky. To sa netýka len klasického OOP — v hybridnom/funkčnom štýle tiež chceme žiadne zdieľané mutovateľné per-op state medzi operáciami.

Singleton pre ICopyPasteService (bez mutable per-op polia): clipboard manager je prirodzene globálny. To však neznamená, že v ňom budú políčka typu CurrentImportProgress — miesto toho drží buď immutable snapshot alebo thread-safe mapu per-operation stavu identifikovanú ID.

IProgress<T> / ProgressHub: tieto vzory umožnia bezpečne reportovať progress bez zdieľania interného mutovateľného stavu medzi operáciami.

CancellationToken a DI scope per background job: tie zabezpečujú korektné zrušenie a životnosť resources — rovnako dôležité v funkčnom aj OOP prístupe.

Praktické vzory pre hybrid architektúru
1) Funkčný/immutability-friendly import (Scoped)

Namiesto ukladania per-op stavov v poli na triede, predávaj stav ako parametre alebo vracaj immutable výsledky.

// IImportService je Scoped alebo factory-provided per-operation
public interface IImportService
{
    Task<ImportResult> ImportAsync(Stream data,
                                   IProgress<int>? progress,
                                   CancellationToken ct);
}

public record ImportResult(bool Success, int ProcessedRows, ImmutableArray<string> Errors);

// Implementácia používa lokálne immutable/func-style promenne
public class ImportService : IImportService
{
    public async Task<ImportResult> ImportAsync(Stream data, IProgress<int>? progress, CancellationToken ct)
    {
        // lokálne per-op premenné (nie člen triedy)
        int processed = 0;
        var errors = ImmutableArray.CreateBuilder<string>();

        // parsing loop
        for (int i = 0; i < 100; i++)
        {
            ct.ThrowIfCancellationRequested();
            // process...
            processed++;
            progress?.Report((processed * 100) / 100);
            await Task.Yield();
        }

        return new ImportResult(true, processed, errors.ToImmutable());
    }
}


Poznámka pro hybrid: ak preferuješ funkčný štýl, udržiavaj per-operation state v lokálnych immutable objektoch a vracaj výsledok. DI poskytne inštanciu služby na operáciu.

2) Factory-based alebo funkčné vytváranie služieb

Ak používate „factory DI“ (napr. Func<IImportService> alebo IServiceProvider.CreateScope()), zaručíte, že každé volanie dostane novú per-op inštanciu.

// z DI: services.AddScoped<IImportService, ImportService>();
// usage in background task:
using var scope = serviceProvider.CreateScope();
var svc = scope.ServiceProvider.GetRequiredService<IImportService>();
await svc.ImportAsync(stream, progress, ct);


Alebo s fabríkami:

services.AddScoped<IImportService, ImportService>();
services.AddTransient<Func<IImportService>>(sp => () => sp.GetRequiredService<IImportService>());

// usage:
var factory = sp.GetRequiredService<Func<IImportService>>();
var serviceInstance = factory(); // nové scoped instance ak volané vo vlastnom scope

3) ProgressHub (Singleton) — per-op ID, thread-safe (hybrid-friendly)

Singleton hub, ktorý nezdieľa per-op mutable state priamo, ale mapuje per-op ID → snapshot stav:

public interface IProgressHub
{
    Guid Register(string name);
    void Report(Guid id, int percent);
    bool TryGetLatest(Guid id, out int percent);
    event Action<Guid,int>? ProgressChanged;
}

public class ProgressHub : IProgressHub
{
    private readonly ConcurrentDictionary<Guid,int> _map = new();
    public Guid Register(string name)
    {
        var id = Guid.NewGuid();
        _map[id] = 0;
        return id;
    }
    public void Report(Guid id, int percent)
    {
        _map[id] = percent;
        ProgressChanged?.Invoke(id, percent);
    }
    public bool TryGetLatest(Guid id, out int percent) => _map.TryGetValue(id, out percent);
    public event Action<Guid,int>? ProgressChanged;
}


ImportService používa hub len ako consumer (neukladá tam interný per-op buffer):

public async Task ImportAsync(Stream data, IProgress<int>? progress, CancellationToken ct)
{
    var id = _progressHub.Register("import");
    try
    {
        for (...) {
            // ...
            _progressHub.Report(id, computedPercent);
            progress?.Report(computedPercent);
        }
    }
    finally
    {
        // optionally remove id from hub
    }
}


Tento vzor dobre funguje v hybridnom prostredí — funkčné časti používajú immutable snapshots/return values; hub je len koordinátor notifikácií.

4) ICopyPasteService (Singleton) — immutable snapshots / atomic replace

Singleton môže držať len immutable snapshoty (string alebo objekt) a používať Interlocked.Exchange alebo Volatile.Read pre bezpečné čítanie/zapis.

public class CopyPasteService : ICopyPasteService
{
    private object? _snapshot; // could be string or an immutable structure

    public void SetClipboard(string payload)
    {
        Interlocked.Exchange(ref _snapshot, payload);
    }
    public string? GetClipboard() => (string?)Volatile.Read(ref _snapshot);
}


Ak chceš funkčnejší prístup, urob CopySelection() tak, aby vracalo immutable 2D pole (alebo serialized string) a facade len poslal ten objekt do singletonu.

5) CancellationToken & resource cleanup

Vždy prehadzuj CancellationToken hlboko do všetkých async operácií a v finally uvoľniť resources:

public async Task ImportAsync(..., CancellationToken ct)
{
    try
    {
        // allocate buffer
        using var memory = new MemoryStream();
        // do work
    }
    finally
    {
        // cleanup (Dispose/close)
    }
}


V hybridnom štýle je tiež dobré vrátiť Result ktorý nesie informáciu o zrušení.

6) Background work: vytvor nový DI scope per job

Ak spúšťaš background task z singletonu alebo z UI, vytvor si nový scope — tak zabezpečíš správnu životnosť scoped služieb:

public class BackgroundRunner
{
    private readonly IServiceProvider _sp;
    public BackgroundRunner(IServiceProvider sp) => _sp = sp;

    public Task RunImportInBackground(Stream data, CancellationToken ct)
    {
        return Task.Run(async () =>
        {
            using var scope = _sp.CreateScope();
            var importSvc = scope.ServiceProvider.GetRequiredService<IImportService>();
            await importSvc.ImportAsync(data, progress: null, ct);
        }, ct);
    }
}


Toto platí bez ohľadu na to, či v službe používate OOP state alebo funkčné lokálne premenné — scope zabezpečí, že per-op resources sú Disposal-ované a izolované.

Ďalšie poznámky pre hybridný štýl

Ak preferuješ funkčný prístup, predávaj per-op kontexty/ID/immutable buffers ako argumenty namiesto ukladania do this.

Ak potrebuješ zdieľať stav medzi per-operation tasks (napr. orchestrácia importu + transformácie), použite explicitný OperationContext immutable objekt alebo kanál (System.Threading.Channels.Channel<T>) namiesto skrytých mutable polí.

Žiadne statické mutable polia pre per-operation stav — to je najčastejšia chyba.

Použi InternalsVisibleTo pre testovanie internal tried, ak chceš priamo testovať IColumnService / ISelectionService.

Kontrolný zoznam (apply to hybrid DI/functional/OOP)

 IImportService & IExportService = per-op instance (Scoped alebo factory-created instance).

 ICopyPasteService = Singleton, ale iba immutable snapshots / thread-safe map.

 Progress: IProgress<T> pre UI-bound reporting alebo ProgressHub s per-op ID pre centrálny reporting.

 CancellationToken pre každú long-running operáciu; cleanup v finally.

 Background: vždy nové DI scope pre každú import/export jobu.

 Funkčný štýl: drž per-op state lokálne / immutable a vracaj výsledky — nevkladaj ho do členov triedy, ak môže byť služba zdieľaná.







 public class AdvancedDataGridFacade
{
    private readonly IColumnService _col;
    private readonly ISelectionService _sel;
    private readonly ICopyPasteService _copy;

    public AdvancedDataGridFacade(IColumnService col, ISelectionService sel, ICopyPasteService copy)
    {
        _col = col;
        _sel = sel;
        _copy = copy;
    }

    // Column resize
    public void StartColumnResize(int columnIndex, double clientX) => _col.StartResizeInternal(columnIndex, clientX);
    public void UpdateColumnResize(double clientX) => _col.UpdateResizeInternal(clientX);
    public void EndColumnResize() => _col.EndResizeInternal();
    public void ResizeColumn(int columnIndex, double newWidth) => _col.ResizeColumnInternal(columnIndex, newWidth);

    // Selection
    public void SelectCell(int r, int c) => _sel.SelectCellInternal(r,c);
    public void StartDragSelect(int r, int c) => _sel.StartDragSelectInternal(r,c);
    public void UpdateDragSelect(int r, int c) => _sel.UpdateDragSelectInternal(r,c);
    public void EndDragSelect() => _sel.EndDragSelectInternal();
    public void ToggleCellSelection(int r, int c) => _sel.ToggleSelectionInternal(r,c);
    public void ExtendSelectionTo(int r, int c) => _sel.ExtendSelectionInternal(r,c);

    // Copy / Paste
    public CopyResult CopySelection() => _copy.CopySelectionInternal(_sel.SnapshotSelection());
    public void SetClipboard(string serialized) => _copy.SetClipboard(serialized);
}


pricom ja budem budem mat tie DI pripojene v InternalServiceRegistration.cs (interfaces IInternalServiceRegistration.cs)

## Feature-based structure & ServiceRegistrar

This documentation set was updated to use a feature-based module layout. See `SERVICE_REGISTRATION.md` for exact code examples on how to wire up DI (internal registrar + per-module Registration helpers).
