# Service registration pattern (feature-based) for AdvancedWinUiDataGrid

This document explains how to register services for the **feature-based** layout used by AdvancedWinUiDataGrid.
It shows the recommended `ServiceRegistration` central entry and the per-module `Registration.Register(IServiceCollection, AdvancedDataGridOptions?)` methods.

## Goals
- Keep all implementation details `internal`.
- Expose only `IAdvancedDataGridFacade`, `AdvancedDataGridFacadeFactory`, `AdvancedDataGridOptions` and public Commands.
- Allow easy addition/removal of features (Import/Export/Validation/Filter/CopyPaste/Column/Selection) by editing only the per-module Registration.

## Central registration (internal)
Place a single internal class at:
`Configuration/ServiceRegistration.cs`

Example pattern:

```csharp
internal static class ServiceRegistration
{
    internal static void Register(IServiceCollection services, AdvancedDataGridOptions? options = null)
    {
        // common singletons / cross-cutting services
        services.AddSingleton<IProgressHub, ProgressHub>();

        // infrastructure registration (RowStore may be provided by options.RowStoreFactory)
        if (options?.RowStoreFactory != null)
        {
            services.AddSingleton(sp => options.RowStoreFactory!(sp));
        }
        else
        {
            services.AddSingleton<IRowStore, DiskBackedRowStore>();
        }

        // register per-feature modules
        Features.Import.Registration.Register(services, options);
        Features.Export.Registration.Register(services, options);
        Features.Validation.Registration.Register(services, options);
        Features.Filter.Registration.Register(services, options);
        Features.CopyPaste.Registration.Register(services, options);
        Features.Column.Registration.Register(services, options);

        // register facade implementation (internal)
        services.AddScoped<AdvancedDataGridFacade>();
        services.AddScoped<IAdvancedDataGridFacade>(sp => sp.GetRequiredService<AdvancedDataGridFacade>());
    }
}
```

## Per-module registration (internal)
Each feature implements a small `Registration` static helper inside its feature folder.

Example: `Features/Import/Registration.cs`
```csharp
internal static class Registration
{
    internal static void Register(IServiceCollection services, AdvancedDataGridOptions? options)
    {
        // Interfaces and implementations are internal
        services.AddScoped<IImportService, ImportService>();
        // If import needs its own helpers, register them here
        services.AddScoped<IImportBatchProcessor, ImportBatchProcessor>();
    }
}
```

Repeat for Export, Validation, Filter, CopyPaste, Column, Selection.

## Guiding rules
- Per-operation state must be local to methods (no mutable private fields in scoped services).
- Lifetimes: Import/Export/Validation/Column/Selection => Scoped; CopyPaste/IRowStore/ProgressHub => Singleton by default.
- Allow host overrides via `options.RowStoreFactory` or other factory delegates on `AdvancedDataGridOptions`.
- For host integration with logging/dispatcher, the public factory (`AdvancedDataGridFacadeFactory.CreateStandalone`) accepts optional `ILoggerFactory` and `DispatcherQueue` and registers them into the internal provider before calling `ServiceRegistration.Register(...)`.

## Tests
- Each feature folder should contain Tests/ with unit tests for the feature and integration tests for cross-feature flows.
- Use `InternalsVisibleTo` to allow Tests project to access internals.
