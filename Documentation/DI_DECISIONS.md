# Feature-based DI and ServiceRegistrar (added)

We updated the DI decisions to use a **feature-based** structure. Instead of a single flat Interfaces/Services folder, each feature (Import, Export, Validation, Filter, CopyPaste, Column, Selection) owns its own `Interfaces/`, `Services/`, `Registration` and `Tests`.

A new internal `ServiceRegistrar.Register(IServiceCollection, AdvancedDataGridOptions?)` centralizes registration and calls each feature's internal `Registration.Register(...)`.

See SERVICE_REGISTRATION.md for detailed instructions and recommended code patterns.


# DI lifetime decisions (applied across the documentation)

For consistency the following lifetimes are recommended and applied in the documentation examples:

- `IImportService` -> **Scoped**
  - Reason: import operations often carry per-operation state (parsing context, progress) and Scoped avoids unintended shared state.
- `IExportService` -> **Scoped**
  - Reason: export operations frequently use operation-specific buffers and settings; Scoped avoids concurrency/state issues when used in web or background contexts.
- `ICopyPasteService` -> **Singleton**
  - Reason: clipboard/copy-paste semantics are globally shared and typically stateless wrappers; Singleton provides a single coordinated clipboard manager. Ensure thread-safety in the implementation.

These decisions are reflected in all DI registration examples in the documentation.
