using System;
using System.Collections.Generic;
using System.Linq;
using RpaWinUiComponentsPackage.AdvancedWinUiLogger.Core.ValueObjects;

namespace RpaWinUiComponentsPackage.AdvancedWinUiLogger.Core.Entities;

/// <summary>
/// ENTERPRISE ENTITY: Represents a logging session with state and behavior
/// AGGREGATE ROOT: Manages logging operations and session lifecycle
/// ENCAPSULATION: Encapsulates session state and invariants
/// </summary>
public sealed class LoggerSession
{
    private readonly List<LogEntry> _pendingEntries;
    private readonly List<LogFileInfo> _managedFiles;
    private readonly object _lock = new();

    /// <summary>Unique identifier for this logging session</summary>
    public Guid SessionId { get; }

    /// <summary>Configuration for this logging session</summary>
    public LoggerConfiguration Configuration { get; private set; }

    /// <summary>UTC timestamp when session was started</summary>
    public DateTime StartedAt { get; }

    /// <summary>UTC timestamp when session was last active</summary>
    public DateTime LastActivity { get; private set; }

    /// <summary>Current active log file path</summary>
    public string? CurrentLogFilePath { get; private set; }

    /// <summary>Total number of entries logged in this session</summary>
    public long TotalEntriesLogged { get; private set; }

    /// <summary>Total bytes written during this session</summary>
    public long TotalBytesWritten { get; private set; }

    /// <summary>Number of rotation operations performed</summary>
    public int RotationCount { get; private set; }

    /// <summary>Indicates if session is currently active</summary>
    public bool IsActive { get; private set; }

    /// <summary>Session performance metrics</summary>
    public SessionMetrics Metrics { get; private set; }

    /// <summary>
    /// ENTERPRISE: Create new logging session
    /// </summary>
    public LoggerSession(LoggerConfiguration configuration)
    {
        SessionId = Guid.NewGuid();
        Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        StartedAt = DateTime.UtcNow;
        LastActivity = StartedAt;
        IsActive = true;
        _pendingEntries = new List<LogEntry>();
        _managedFiles = new List<LogFileInfo>();
        Metrics = new SessionMetrics();
    }

    /// <summary>
    /// BUSINESS LOGIC: Add log entry to the session
    /// ENTERPRISE: Thread-safe entry addition with validation
    /// </summary>
    public Result<bool> AddLogEntry(LogEntry entry)
    {
        if (entry == null)
            return Result<bool>.Failure("Log entry cannot be null");

        if (!IsActive)
            return Result<bool>.Failure("Cannot add entries to inactive session");

        if (!Configuration.ShouldLog(entry.Level))
            return Result<bool>.Success(false); // Filtered out, but not an error

        lock (_lock)
        {
            try
            {
                _pendingEntries.Add(entry);
                LastActivity = DateTime.UtcNow;
                Metrics.RecordEntry(entry);

                // Auto-flush if buffer is full
                if (_pendingEntries.Count >= Configuration.BufferSize)
                {
                    var flushResult = FlushPendingEntries();
                    return flushResult.IsSuccess ? Result<bool>.Success(true) : Result<bool>.Failure(flushResult.Error);
                }

                return Result<bool>.Success(true);
            }
            catch (Exception ex)
            {
                return Result<bool>.Failure($"Failed to add log entry: {ex.Message}", ex);
            }
        }
    }

    /// <summary>
    /// BUSINESS LOGIC: Flush pending entries to storage
    /// ENTERPRISE: Batch processing for performance optimization
    /// </summary>
    public Result<int> FlushPendingEntries()
    {
        lock (_lock)
        {
            try
            {
                if (_pendingEntries.Count == 0)
                    return Result<int>.Success(0);

                var entryCount = _pendingEntries.Count;
                var totalBytes = _pendingEntries.Sum(e => e.GetApproximateSize());

                // In a real implementation, this would write to file
                // For now, we'll simulate the operation
                TotalEntriesLogged += entryCount;
                TotalBytesWritten += totalBytes;
                LastActivity = DateTime.UtcNow;

                _pendingEntries.Clear();
                Metrics.RecordFlush(entryCount, totalBytes);

                return Result<int>.Success(entryCount);
            }
            catch (Exception ex)
            {
                return Result<int>.Failure($"Failed to flush entries: {ex.Message}", ex);
            }
        }
    }

    /// <summary>
    /// BUSINESS LOGIC: Update session configuration
    /// ENTERPRISE: Runtime configuration updates with validation
    /// </summary>
    public Result<bool> UpdateConfiguration(LoggerConfiguration newConfiguration)
    {
        if (newConfiguration == null)
            return Result<bool>.Failure("Configuration cannot be null");

        var validationResult = newConfiguration.Validate();
        if (validationResult.IsFailure)
            return Result<bool>.Failure($"Invalid configuration: {validationResult.Error}");

        lock (_lock)
        {
            Configuration = newConfiguration;
            LastActivity = DateTime.UtcNow;
            return Result<bool>.Success(true);
        }
    }

    /// <summary>
    /// BUSINESS LOGIC: Set current active log file
    /// ENTERPRISE: File lifecycle management
    /// </summary>
    public Result<bool> SetCurrentLogFile(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return Result<bool>.Failure("File path cannot be null or empty");

        lock (_lock)
        {
            CurrentLogFilePath = filePath;
            LastActivity = DateTime.UtcNow;
            return Result<bool>.Success(true);
        }
    }

    /// <summary>
    /// BUSINESS LOGIC: Record rotation operation
    /// ENTERPRISE: Rotation tracking and metrics
    /// </summary>
    public Result<bool> RecordRotation(RotationResult rotationResult)
    {
        if (rotationResult == null)
            return Result<bool>.Failure("Rotation result cannot be null");

        lock (_lock)
        {
            RotationCount++;
            LastActivity = DateTime.UtcNow;
            Metrics.RecordRotation(rotationResult);

            if (rotationResult.IsSuccess && !string.IsNullOrEmpty(rotationResult.NewFilePath))
            {
                CurrentLogFilePath = rotationResult.NewFilePath;
            }

            return Result<bool>.Success(true);
        }
    }

    /// <summary>
    /// BUSINESS LOGIC: Add managed file to session
    /// ENTERPRISE: File tracking for lifecycle operations
    /// </summary>
    public Result<bool> AddManagedFile(LogFileInfo fileInfo)
    {
        if (fileInfo == null)
            return Result<bool>.Failure("File info cannot be null");

        lock (_lock)
        {
            if (!_managedFiles.Any(f => f.FilePath.Equals(fileInfo.FilePath, StringComparison.OrdinalIgnoreCase)))
            {
                _managedFiles.Add(fileInfo);
                LastActivity = DateTime.UtcNow;
            }

            return Result<bool>.Success(true);
        }
    }

    /// <summary>
    /// FUNCTIONAL: Get current session duration
    /// </summary>
    public TimeSpan GetSessionDuration() => DateTime.UtcNow - StartedAt;

    /// <summary>
    /// FUNCTIONAL: Get time since last activity
    /// </summary>
    public TimeSpan GetTimeSinceLastActivity() => DateTime.UtcNow - LastActivity;

    /// <summary>
    /// FUNCTIONAL: Get pending entries count (thread-safe)
    /// </summary>
    public int GetPendingEntriesCount()
    {
        lock (_lock)
        {
            return _pendingEntries.Count;
        }
    }

    /// <summary>
    /// FUNCTIONAL: Get managed files count (thread-safe)
    /// </summary>
    public int GetManagedFilesCount()
    {
        lock (_lock)
        {
            return _managedFiles.Count;
        }
    }

    /// <summary>
    /// FUNCTIONAL: Get copy of managed files (thread-safe)
    /// </summary>
    public IReadOnlyList<LogFileInfo> GetManagedFiles()
    {
        lock (_lock)
        {
            return _managedFiles.ToList().AsReadOnly();
        }
    }

    /// <summary>
    /// FUNCTIONAL: Check if session should auto-flush
    /// BUSINESS RULE: Automatic flushing conditions
    /// </summary>
    public bool ShouldAutoFlush()
    {
        lock (_lock)
        {
            return _pendingEntries.Count >= Configuration.BufferSize ||
                   GetTimeSinceLastActivity() >= Configuration.FlushInterval;
        }
    }

    /// <summary>
    /// FUNCTIONAL: Get session throughput (entries per second)
    /// PERFORMANCE: Operational metrics calculation
    /// </summary>
    public double GetThroughputEntriesPerSecond()
    {
        var duration = GetSessionDuration();
        return duration.TotalSeconds > 0 ? TotalEntriesLogged / duration.TotalSeconds : 0;
    }

    /// <summary>
    /// FUNCTIONAL: Get session throughput (MB per second)
    /// PERFORMANCE: Bandwidth metrics calculation
    /// </summary>
    public double GetThroughputMBPerSecond()
    {
        var duration = GetSessionDuration();
        var mbWritten = TotalBytesWritten / (1024.0 * 1024.0);
        return duration.TotalSeconds > 0 ? mbWritten / duration.TotalSeconds : 0;
    }

    /// <summary>
    /// BUSINESS LOGIC: Stop the logging session
    /// LIFECYCLE: Clean session termination
    /// </summary>
    public Result<bool> Stop()
    {
        if (!IsActive)
            return Result<bool>.Success(true);

        lock (_lock)
        {
            try
            {
                // Flush any remaining entries
                var flushResult = FlushPendingEntries();
                if (flushResult.IsFailure)
                    return Result<bool>.Failure($"Failed to flush pending entries during stop: {flushResult.Error}");

                IsActive = false;
                LastActivity = DateTime.UtcNow;

                return Result<bool>.Success(true);
            }
            catch (Exception ex)
            {
                return Result<bool>.Failure($"Failed to stop session: {ex.Message}", ex);
            }
        }
    }

    /// <summary>
    /// ENTERPRISE: Get comprehensive session status
    /// MONITORING: Detailed session information for observability
    /// </summary>
    public SessionStatus GetStatus() => new()
    {
        SessionId = SessionId,
        IsActive = IsActive,
        StartedAt = StartedAt,
        LastActivity = LastActivity,
        Duration = GetSessionDuration(),
        CurrentLogFile = CurrentLogFilePath,
        TotalEntriesLogged = TotalEntriesLogged,
        TotalBytesWritten = TotalBytesWritten,
        PendingEntries = GetPendingEntriesCount(),
        ManagedFiles = GetManagedFilesCount(),
        RotationCount = RotationCount,
        ThroughputEntriesPerSecond = GetThroughputEntriesPerSecond(),
        ThroughputMBPerSecond = GetThroughputMBPerSecond(),
        Metrics = Metrics
    };
}

/// <summary>
/// VALUE OBJECT: Session performance metrics
/// TELEMETRY: Aggregated performance data
/// </summary>
public sealed class SessionMetrics
{
    public long TotalEntries { get; private set; }
    public long ErrorEntries { get; private set; }
    public long WarningEntries { get; private set; }
    public int FlushOperations { get; private set; }
    public int RotationOperations { get; private set; }
    public TimeSpan TotalFlushTime { get; private set; }
    public TimeSpan TotalRotationTime { get; private set; }

    public void RecordEntry(LogEntry entry)
    {
        TotalEntries++;

        switch (entry.Level)
        {
            case Microsoft.Extensions.Logging.LogLevel.Error:
            case Microsoft.Extensions.Logging.LogLevel.Critical:
                ErrorEntries++;
                break;
            case Microsoft.Extensions.Logging.LogLevel.Warning:
                WarningEntries++;
                break;
        }
    }

    public void RecordFlush(int entryCount, long bytesWritten)
    {
        FlushOperations++;
    }

    public void RecordRotation(RotationResult result)
    {
        RotationOperations++;
        TotalRotationTime = TotalRotationTime.Add(result.OperationDuration);
    }
}

/// <summary>
/// VALUE OBJECT: Comprehensive session status
/// MONITORING: Complete session state for observability
/// </summary>
public sealed record SessionStatus
{
    public Guid SessionId { get; init; }
    public bool IsActive { get; init; }
    public DateTime StartedAt { get; init; }
    public DateTime LastActivity { get; init; }
    public TimeSpan Duration { get; init; }
    public string? CurrentLogFile { get; init; }
    public long TotalEntriesLogged { get; init; }
    public long TotalBytesWritten { get; init; }
    public int PendingEntries { get; init; }
    public int ManagedFiles { get; init; }
    public int RotationCount { get; init; }
    public double ThroughputEntriesPerSecond { get; init; }
    public double ThroughputMBPerSecond { get; init; }
    public SessionMetrics Metrics { get; init; } = new();
}