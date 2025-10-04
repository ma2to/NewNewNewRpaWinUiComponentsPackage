using System;
using RpaWinUiComponentsPackage.AdvancedWinUiLogger;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RpaWinUiComponentsPackage.AdvancedWinUiLogger.Features.Session.Interfaces;

namespace RpaWinUiComponentsPackage.AdvancedWinUiLogger.Features.Session.Services;

/// <summary>
/// INTERNAL SERVICE: Session management operations implementation
/// CLEAN ARCHITECTURE: Application layer service for session business logic
/// </summary>
internal sealed class SessionService : ISessionService
{
    private readonly ConcurrentDictionary<string, LoggerSession> _activeSessions = new();

    public Task<LoggerSession> StartLoggingSessionAsync(
        AdvancedLoggerOptions options,
        string sessionName = "",
        CancellationToken cancellationToken = default)
    {
        var currentLogFile = options.GetCurrentLogFilePath();
        var session = LoggerSession.Create(sessionName, options.LogDirectory, currentLogFile);

        _activeSessions.TryAdd(session.SessionId, session);

        return Task.FromResult(session);
    }

    public Task<bool> EndLoggingSessionAsync(
        LoggerSession session,
        CancellationToken cancellationToken = default)
    {
        if (_activeSessions.TryRemove(session.SessionId, out var activeSession))
        {
            activeSession.End();
            return Task.FromResult(true);
        }

        return Task.FromResult(false);
    }

    public Task<IReadOnlyList<LoggerSession>> GetActiveSessionsAsync(CancellationToken cancellationToken = default)
    {
        var sessions = _activeSessions.Values.Where(s => s.IsActive).ToList();
        return Task.FromResult<IReadOnlyList<LoggerSession>>(sessions);
    }
}
