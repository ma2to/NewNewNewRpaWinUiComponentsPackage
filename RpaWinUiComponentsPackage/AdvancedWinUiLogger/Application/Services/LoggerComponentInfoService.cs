using System.Collections.Generic;
using RpaWinUiComponentsPackage.AdvancedWinUiLogger.Application.Interfaces;

namespace RpaWinUiComponentsPackage.AdvancedWinUiLogger.Application.Services;

/// <summary>
/// INTERNAL SERVICE: Logger component information service implementation
/// CLEAN ARCHITECTURE: Application layer service for component metadata operations
/// </summary>
internal sealed class LoggerComponentInfoService : ILoggerComponentInfoService
{
    public string GetVersion()
    {
        return "1.0.0-placeholder";
    }

    public IReadOnlyList<string> GetSupportedFeatures()
    {
        return new[] { "Basic logging", "Placeholder implementation" }.AsReadOnly();
    }

    public string GetConfigurationSchemaVersion()
    {
        return "1.0";
    }
}