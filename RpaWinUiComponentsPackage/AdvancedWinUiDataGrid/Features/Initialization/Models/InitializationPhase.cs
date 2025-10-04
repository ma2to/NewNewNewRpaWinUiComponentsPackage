namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Initialization.Models;

/// <summary>
/// Fázy inicializácie komponentu
/// Definuje sekvenčný priebeh startup procesu
/// </summary>
internal enum InitializationPhase
{
    /// <summary>Žiadna fáza - komponenta nie je inicializovaná</summary>
    None = 0,

    /// <summary>Registrácia služieb do DI containera</summary>
    ServiceRegistration = 1,

    /// <summary>Validácia závislostí medzi službami</summary>
    DependencyValidation = 2,

    /// <summary>Načítanie konfigurácie</summary>
    ConfigurationLoading = 3,

    /// <summary>Inicializácia business komponentov</summary>
    ComponentInitialization = 4,

    /// <summary>Nastavenie validation systému</summary>
    ValidationSetup = 5,

    /// <summary>Inicializácia témy a farieb (len UI mode)</summary>
    ThemeInitialization = 6,

    /// <summary>Nastavenie smart operácií</summary>
    SmartOperationsSetup = 7,

    /// <summary>Finalizácia inicializácie</summary>
    Finalization = 8
}
