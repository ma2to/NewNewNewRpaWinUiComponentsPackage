using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using RpaWinUiComponentsPackage.AdvancedWinUiLogger.Core.Functional;
using RpaWinUiComponentsPackage.AdvancedWinUiLogger.Core.ValueObjects;

namespace RpaWinUiComponentsPackage.AdvancedWinUiLogger.Application.Interfaces;

/// <summary>
/// INTERNAL INTERFACE: Logger configuration service for validation and management
/// CLEAN ARCHITECTURE: Application layer interface for configuration operations
/// </summary>
internal interface ILoggerConfigurationService
{
    Result<LoggerConfiguration> CreateConfigurationForEnvironment(string environment, string logDirectory, string baseFileName);
    Result<bool> ValidateProductionConfiguration(LoggerConfiguration configuration);
    Result<bool> ValidateConfiguration(LoggerConfiguration configuration);
    Task<Result<bool>> UpdateLoggerConfigurationAsync(ILogger logger, LoggerConfiguration newConfiguration, CancellationToken cancellationToken = default);
}