using System.Collections.Generic;

namespace RpaWinUiComponentsPackage.AdvancedWinUiLogger.Application.Interfaces;

/// <summary>
/// INTERNAL INTERFACE: Logger component information service
/// CLEAN ARCHITECTURE: Application layer interface for component metadata operations
/// </summary>
internal interface ILoggerComponentInfoService
{
    string GetVersion();
    IReadOnlyList<string> GetSupportedFeatures();
    string GetConfigurationSchemaVersion();
}