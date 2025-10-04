
# Thread-safety guidance and concurrency test examples

This document expands the brief "Thread-safety" notes in the main documentation. It contains concrete guidelines, recommended synchronization primitives, and unit/integration test examples (xUnit) for the most critical services in the component:

- ICopyPasteService (Singleton)
- IImportService / IExportService (Scoped)
- IColumnService (internal)
- ISelectionService (internal)
- AdvancedDataGridFacade (public entry point)

---

## High-level concurrency model

1. **UI thread model**: UI-driven interactions (mouse events, keyboard) should be marshalled to the UI thread. The component should assume that DOM / UI-level events call the facade on the UI thread. Non-UI background tasks (import/export) may run on worker threads.

2. **Service ownership**:
   - **Scoped services** (IImportService, IExportService): meant to be created per logical operation (e.g., per HTTP request, per user action, per import/export job). They may assume single-threaded use within their scope unless explicitly documented otherwise.
   - **Singleton services** (ICopyPasteService): must be implemented as fully thread-safe. Expect concurrent calls from UI thread and background tasks (e.g., clipboard manager + background auto-export).

3. **Internal services** (IColumnService, ISelectionService) are `internal` and mapped behind the public `AdvancedDataGridFacade`. The facade must ensure correct concurrency when delegating calls: either guarantee calls arrive serialized (UI thread) or rely on internal services to be thread-safe where concurrent access is expected.

---

## Recommended synchronization primitives & patterns

- For **read-mostly** data structures: use `ReaderWriterLockSlim` (favor EnterReadLock / ExitReadLock for readers, EnterWriteLock for writers).
- For **high-frequency** counters or small atomic updates: use `Interlocked` operations (Interlocked.Increment, Exchange).
- For **collections** shared across threads: use `ConcurrentDictionary<TKey,TValue>`, `ConcurrentQueue<T>`, `ConcurrentStack<T>` when possible.
- For **coarse-grain** operations that mutate complex state: prefer a `lock` (Monitor) around a private `object _sync = new();` to keep reasoning simple.
- For per-operation buffers in Scoped services: avoid sharing between scopes. If you must share, synchronize access or convert to immutable snapshots for readers.

---

## Service-specific guidance

### ICopyPasteService (Singleton)
- Responsibility: central clipboard manager, serializing `CopySelection()` payloads and handling paste operations.
- Implementation rules:
  - Keep internal clipboard buffer immutable once produced. For example, store `string` payload or an immutable 2D cell array.
  - Use `lock` or `ConcurrentQueue` for mutation points (e.g., when replacing the clipboard contents).
  - Public methods should be thread-safe, e.g.:
    ```csharp
    private readonly object _sync = new();
    private string _clipboardPayload; // immutable string snapshot for pasting

    public void SetClipboard(string payload)
    {
        lock (_sync)
        {
            _clipboardPayload = payload;
        }
    }

    public string GetClipboard()
    {
        // snapshot read without lock is acceptable if assignment of string is atomic.
        return Volatile.Read(ref _clipboardPayload);
    }
    ```
- Avoid keeping references to mutable per-operation buffers in the singleton; instead, make copies or create immutable snapshots before storing.

### IImportService / IExportService (Scoped)
- Expect single-threaded use inside the scope. Do **not** use singletons to hold per-operation state.
- If these services start background tasks that outlive the scope, they must copy necessary state (no stacking scope references).
- Example rule: **no static mutable fields** for per-operation data.

### IColumnService (internal)
- Typical operations: `StartResizeInternal`, `UpdateResizeInternal`, `EndResizeInternal`, `ResizeColumnInternal`.
- UI interactions should happen on the UI thread; internal service should accept concurrent calls but may throw if called concurrently in unsupported ways (documented). Prefer to guard `Start/Update/End` sequence with a small `lock`:
  ```csharp
  private readonly object _resizeSync = new();
  private int? _activeResizeColumn = null;

  public void StartResizeInternal(int columnIndex, double clientX)
  {
      lock (_resizeSync)
      {
          _activeResizeColumn = columnIndex;
          // store start pos
      }
  }

  public void UpdateResizeInternal(double clientX)
  {
      lock (_resizeSync)
      {
          if (_activeResizeColumn is null) return;
          // compute and apply width
      }
  }

  public void EndResizeInternal()
  {
      lock (_resizeSync)
      {
          _activeResizeColumn = null;
      }
  }
  ```
- Note: Using UI-thread-only model simplifies this: if operations are only ever called on the UI thread, the lock becomes cheap/optional. Document the expectation clearly.

### ISelectionService (internal)
- Maintains selected cell set (sparse selection) and last-focused cell. This set is mutated by toggle/drag/select operations.
- Recommended internal representation: `HashSet<(int row,int col)>` protected by a `lock` or `ReaderWriterLockSlim`:
  ```csharp
  private readonly object _selSync = new();
  private readonly HashSet<(int r, int c)> _selected = new();

  public void ToggleSelectionInternal(int r, int c)
  {
      lock (_selSync)
      {
          if (!_selected.Remove((r,c)))
              _selected.Add((r,c));
      }
  }

  public (int minR, int maxR, int minC, int maxC, List<List<CellPayload>>) SnapshotSelection()
  {
      lock (_selSync)
      {
          // create snapshot array with blanks where necessary
      }
  }
  ```
- For performance with large selections, consider a `Span`-friendly creation of rectangular snapshots and avoid per-cell allocations where possible.

---

## Testing concurrency: xUnit examples

The following xUnit test snippets illustrate patterns to test for race conditions and ensure the selection/clipboard logic remains consistent under concurrent access.

> Note: these are *integration-style* unit tests. In CI, run them multiple times to increase the chance of catching races.

### 1) Test concurrent ToggleCellSelection on SelectionService
```csharp
using System.Threading.Tasks;
using Xunit;
using FluentAssertions;

public class SelectionConcurrencyTests
{
    [Fact]
    public async Task ToggleSelection_ConcurrentTasks_LeavesConsistentState()
    {
        var svc = new SelectionService(); // assume internal but testable via InternalsVisibleTo or test hook
        var tasks = new List<Task>();
        int iterations = 1000;
        for (int i = 0; i < iterations; i++)
        {
            int r = i % 10;
            int c = (i / 10) % 10;
            tasks.Add(Task.Run(() => svc.ToggleSelectionInternal(r, c)));
        }
        await Task.WhenAll(tasks);

        // Validate: selection set contains only valid coordinates and no duplicates (HashSet ensures that)
        var snapshot = svc.SnapshotSelection();
        snapshot.Should().NotBeNull();
        // Additional invariants can be validated depending on expected behavior
    }
}
```

### 2) Test CopySelection concurrent with Toggle/Modify
```csharp
[Fact]
public async Task CopySelection_ConcurrentModify_DoesNotThrowAndProducesValidSnapshot()
{
    var sel = new SelectionService();
    // setup some initial selection
    sel.StartDragSelectInternal(0,0);
    sel.UpdateDragSelectInternal(2,2);
    sel.EndDragSelectInternal();

    // Start copying in background while modifications happen
    var tasks = new List<Task>();
    tasks.Add(Task.Run(() => {
        for (int i=0;i<100;i++)
        {
            var copy = sel.SnapshotSelection(); // should produce consistent snapshot
            Assert.NotNull(copy);
        }
    }));

    for (int i=0;i<200;i++)
    {
        int r = i % 5;
        int c = (i/5) % 5;
        tasks.Add(Task.Run(() => sel.ToggleSelectionInternal(r,c)));
    }

    await Task.WhenAll(tasks);

    // If no exceptions and snapshots are shape-consistent, test passes
}
```

### 3) Test ICopyPasteService thread-safe clipboard replacement
```csharp
[Fact]
public void Clipboard_MultipleSetters_ProducesLatestSnapshot()
{
    var svc = new CopyPasteService(); // singleton in production
    Parallel.For(0, 200, i =>
    {
        svc.SetClipboard($"payload-{i}");
    });

    var latest = svc.GetClipboard();
    latest.Should().StartWith("payload-");
    // we cannot assert exact value due to race, but ensure no corruption (string should not be null/partial)
    latest.Should().NotContain("\0");
}
```

---

## Concurrency scenarios & mitigation checklist

- [ ] Document that UI event handlers should be invoked on UI thread.
- [ ] For background jobs that access facade methods, mark which methods are safe for cross-thread calls.
- [ ] Ensure Singletons expose only thread-safe operations (no leaking mutable references).
- [ ] Use immutable snapshots for copy/paste payloads.
- [ ] Add stress tests to CI that run concurrency tests repeatedly (e.g., 1000x) to increase confidence.

---

## Pseudocode: drag-resize event handling (UI-level)
```text
onMouseMove(event):
    if resizingActive:
        facade.UpdateColumnResize(event.clientX)

onMouseDown(event):
    if event.isOnColumnBorder:
        resizingActive = true
        facade.StartColumnResize(columnIndexUnderCursor, event.clientX)

onMouseUp(event):
    if resizingActive:
        facade.EndColumnResize()
        resizingActive = false
```

---

## Notes about testing strategy
- Where services are internal, use `InternalsVisibleTo("Your.Tests")` in the component assembly to allow direct unit tests.
- Use fuzzing / randomized inputs in tests to surface edge cases (randomly choose coordinates, random sequences of toggles and drags).
- Consider adding a reproducible seed logger for failed concurrency runs to reproduce specific interleavings.

---
