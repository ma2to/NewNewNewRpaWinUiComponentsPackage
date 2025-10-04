using System;
using RpaWinUiComponentsPackage.AdvancedWinUiLogger;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using RpaWinUiComponentsPackage.AdvancedWinUiLogger.Features.Performance.Interfaces;

namespace RpaWinUiComponentsPackage.AdvancedWinUiLogger.Features.Performance.Services;

/// <summary>
/// INTERNAL SERVICE: Performance monitoring operations implementation
/// CLEAN ARCHITECTURE: Application layer service for performance business logic
/// </summary>
internal sealed class PerformanceService : IPerformanceService
{
    private readonly ConcurrentDictionary<string, PerformanceTracker> _trackers = new();
    private readonly AdvancedLoggerOptions _options;

    public PerformanceService(AdvancedLoggerOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public Task<LoggerPerformanceMetrics> GetPerformanceMetricsAsync(
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        var tracker = GetOrCreateTracker(logger);
        var metrics = tracker.GetMetrics();
        return Task.FromResult(metrics);
    }

    public Task ResetPerformanceCountersAsync(
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        var tracker = GetOrCreateTracker(logger);
        tracker.Reset();
        return Task.CompletedTask;
    }

    public Task<bool> SetPerformanceMonitoringAsync(
        ILogger logger,
        bool enabled,
        CancellationToken cancellationToken = default)
    {
        var tracker = GetOrCreateTracker(logger);
        tracker.Enabled = enabled;
        return Task.FromResult(true);
    }

    private PerformanceTracker GetOrCreateTracker(ILogger logger)
    {
        var key = logger.GetType().FullName ?? "default";
        return _trackers.GetOrAdd(key, _ => new PerformanceTracker());
    }

    private sealed class PerformanceTracker
    {
        private long _totalEntries;
        private long _totalMilliseconds;
        private readonly Stopwatch _stopwatch = Stopwatch.StartNew();
        private readonly DateTime _startTime = DateTime.UtcNow;

        public bool Enabled { get; set; } = true;

        public void RecordEntry(long milliseconds)
        {
            if (!Enabled) return;
            Interlocked.Increment(ref _totalEntries);
            Interlocked.Add(ref _totalMilliseconds, milliseconds);
        }

        public LoggerPerformanceMetrics GetMetrics()
        {
            var totalEntries = Interlocked.Read(ref _totalEntries);
            var totalMs = Interlocked.Read(ref _totalMilliseconds);
            var memoryUsage = GC.GetTotalMemory(false);

            return LoggerPerformanceMetrics.Create(
                totalEntries,
                TimeSpan.FromMilliseconds(totalMs),
                memoryUsage);
        }

        public void Reset()
        {
            Interlocked.Exchange(ref _totalEntries, 0);
            Interlocked.Exchange(ref _totalMilliseconds, 0);
        }
    }
}
