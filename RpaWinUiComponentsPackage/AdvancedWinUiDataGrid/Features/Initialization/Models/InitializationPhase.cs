namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Initialization.Models;

/// <summary>
/// Initialization phases executed during component startup
/// Defines the sequential flow of the initialization process
/// </summary>
internal enum InitializationPhase
{
    /// <summary>No initialization phase - component is not yet initialized</summary>
    None = 0,

    /// <summary>Registering services into dependency injection container</summary>
    ServiceRegistration = 1,

    /// <summary>Validating dependencies between registered services</summary>
    DependencyValidation = 2,

    /// <summary>Loading component configuration settings</summary>
    ConfigurationLoading = 3,

    /// <summary>Initializing business logic components and services</summary>
    ComponentInitialization = 4,

    /// <summary>Setting up validation rules and validation system</summary>
    ValidationSetup = 5,

    /// <summary>Initializing theme and color schemes (UI mode only)</summary>
    ThemeInitialization = 6,

    /// <summary>Configuring smart operations (Add Above/Below, Smart Delete)</summary>
    SmartOperationsSetup = 7,

    /// <summary>Completing initialization and performing final setup</summary>
    Finalization = 8
}
