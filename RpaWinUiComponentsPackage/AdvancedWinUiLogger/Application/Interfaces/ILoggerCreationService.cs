using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using RpaWinUiComponentsPackage.AdvancedWinUiLogger.Core.ValueObjects;

namespace RpaWinUiComponentsPackage.AdvancedWinUiLogger.Application.Interfaces;

/// <summary>
/// INTERNAL INTERFACE: Logger creation service for different logger types
/// CLEAN ARCHITECTURE: Application layer interface for logger factory operations
/// </summary>
internal interface ILoggerCreationService
{
    Result<ILogger> CreateFileLogger(string logDirectory, string baseFileName = "application");
    Result<ILogger> CreateHighPerformanceLogger(string logDirectory, string baseFileName = "application");
    Result<ILogger> CreateDevelopmentLogger(string logDirectory, string baseFileName = "dev");
    Result<ILogger> CreateCustomLogger(LoggerConfiguration configuration);
}