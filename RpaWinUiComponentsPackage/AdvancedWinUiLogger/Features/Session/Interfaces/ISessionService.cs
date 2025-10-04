using System.Collections.Generic;
using RpaWinUiComponentsPackage.AdvancedWinUiLogger;
using System.Threading;
using System.Threading.Tasks;

namespace RpaWinUiComponentsPackage.AdvancedWinUiLogger.Features.Session.Interfaces;

/// <summary>
/// INTERNAL INTERFACE: Session management service contract
/// CLEAN ARCHITECTURE: Application layer interface for session operations
/// </summary>
internal interface ISessionService
{
    Task<LoggerSession> StartLoggingSessionAsync(AdvancedLoggerOptions options, string sessionName = "", CancellationToken cancellationToken = default);
    Task<bool> EndLoggingSessionAsync(LoggerSession session, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LoggerSession>> GetActiveSessionsAsync(CancellationToken cancellationToken = default);
}
