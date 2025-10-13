using System.Diagnostics;
using System.Runtime.InteropServices;

namespace RpaWinUiComponentsPackage.ComprehensiveBenchmarks.Infrastructure;

/// <summary>
/// Comprehensive performance monitoring for CPU, RAM, and system resources
/// </summary>
public sealed class PerformanceMonitor : IDisposable
{
    private readonly Process _currentProcess;
    private readonly PerformanceCounter? _cpuCounter;
    private readonly PerformanceCounter? _ramCounter;
    private readonly List<PerformanceSnapshot> _snapshots = new();
    private readonly Stopwatch _stopwatch = new();
    private bool _isMonitoring;
    private CancellationTokenSource? _monitoringCts;
    private Task? _monitoringTask;

    public PerformanceMonitor()
    {
        _currentProcess = Process.GetCurrentProcess();

        try
        {
            _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total", true);
            _ramCounter = new PerformanceCounter("Memory", "Available MBytes", true);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Performance counters not available: {ex.Message}");
        }
    }

    public void StartMonitoring(int intervalMs = 100)
    {
        if (_isMonitoring) return;

        _isMonitoring = true;
        _stopwatch.Restart();
        _monitoringCts = new CancellationTokenSource();
        _snapshots.Clear();

        _monitoringTask = Task.Run(async () =>
        {
            while (!_monitoringCts.Token.IsCancellationRequested)
            {
                try
                {
                    var snapshot = CaptureSnapshot();
                    lock (_snapshots)
                    {
                        _snapshots.Add(snapshot);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Monitoring error: {ex.Message}");
                }

                await Task.Delay(intervalMs, _monitoringCts.Token);
            }
        }, _monitoringCts.Token);
    }

    public async Task<PerformanceReport> StopMonitoringAsync()
    {
        if (!_isMonitoring) throw new InvalidOperationException("Monitoring is not active");

        _isMonitoring = false;
        _stopwatch.Stop();

        _monitoringCts?.Cancel();
        if (_monitoringTask != null)
        {
            try
            {
                await _monitoringTask;
            }
            catch (OperationCanceledException) { }
        }

        return GenerateReport();
    }

    private PerformanceSnapshot CaptureSnapshot()
    {
        _currentProcess.Refresh();

        var cpuUsage = 0.0;
        var totalMemoryMB = 0.0;

        try
        {
            if (_cpuCounter != null)
            {
                cpuUsage = _cpuCounter.NextValue();
            }

            if (_ramCounter != null)
            {
                var availableMemoryMB = _ramCounter.NextValue();
                var totalMemory = GC.GetTotalMemory(false) / (1024.0 * 1024.0);
                totalMemoryMB = totalMemory;
            }
        }
        catch { }

        return new PerformanceSnapshot
        {
            Timestamp = _stopwatch.Elapsed,
            CpuUsagePercent = cpuUsage,
            WorkingSetMB = _currentProcess.WorkingSet64 / (1024.0 * 1024.0),
            PrivateMemoryMB = _currentProcess.PrivateMemorySize64 / (1024.0 * 1024.0),
            ManagedMemoryMB = GC.GetTotalMemory(false) / (1024.0 * 1024.0),
            ThreadCount = _currentProcess.Threads.Count,
            HandleCount = _currentProcess.HandleCount,
            PagedMemoryMB = _currentProcess.PagedMemorySize64 / (1024.0 * 1024.0),
            VirtualMemoryMB = _currentProcess.VirtualMemorySize64 / (1024.0 * 1024.0),
            Gen0Collections = GC.CollectionCount(0),
            Gen1Collections = GC.CollectionCount(1),
            Gen2Collections = GC.CollectionCount(2)
        };
    }

    private PerformanceReport GenerateReport()
    {
        if (_snapshots.Count == 0)
        {
            throw new InvalidOperationException("No snapshots captured");
        }

        var cpuValues = _snapshots.Select(s => s.CpuUsagePercent).ToList();
        var memoryValues = _snapshots.Select(s => s.WorkingSetMB).ToList();
        var managedMemoryValues = _snapshots.Select(s => s.ManagedMemoryMB).ToList();

        var firstSnapshot = _snapshots.First();
        var lastSnapshot = _snapshots.Last();

        return new PerformanceReport
        {
            TotalDuration = _stopwatch.Elapsed,
            SnapshotCount = _snapshots.Count,

            // CPU Stats
            AvgCpuUsage = cpuValues.Average(),
            MaxCpuUsage = cpuValues.Max(),
            MinCpuUsage = cpuValues.Min(),

            // Memory Stats
            AvgWorkingSetMB = memoryValues.Average(),
            MaxWorkingSetMB = memoryValues.Max(),
            MinWorkingSetMB = memoryValues.Min(),

            // Managed Memory Stats
            AvgManagedMemoryMB = managedMemoryValues.Average(),
            MaxManagedMemoryMB = managedMemoryValues.Max(),
            MinManagedMemoryMB = managedMemoryValues.Min(),

            // GC Stats
            TotalGen0Collections = lastSnapshot.Gen0Collections - firstSnapshot.Gen0Collections,
            TotalGen1Collections = lastSnapshot.Gen1Collections - firstSnapshot.Gen1Collections,
            TotalGen2Collections = lastSnapshot.Gen2Collections - firstSnapshot.Gen2Collections,

            // Thread Stats
            AvgThreadCount = (int)_snapshots.Select(s => (double)s.ThreadCount).Average(),
            MaxThreadCount = _snapshots.Max(s => s.ThreadCount),

            // Handle Stats
            AvgHandleCount = (int)_snapshots.Select(s => (double)s.HandleCount).Average(),
            MaxHandleCount = _snapshots.Max(s => s.HandleCount),

            Snapshots = _snapshots.ToList()
        };
    }

    public void Dispose()
    {
        _monitoringCts?.Cancel();
        _monitoringCts?.Dispose();
        _cpuCounter?.Dispose();
        _ramCounter?.Dispose();
    }
}

public record PerformanceSnapshot
{
    public TimeSpan Timestamp { get; init; }
    public double CpuUsagePercent { get; init; }
    public double WorkingSetMB { get; init; }
    public double PrivateMemoryMB { get; init; }
    public double ManagedMemoryMB { get; init; }
    public int ThreadCount { get; init; }
    public int HandleCount { get; init; }
    public double PagedMemoryMB { get; init; }
    public double VirtualMemoryMB { get; init; }
    public int Gen0Collections { get; init; }
    public int Gen1Collections { get; init; }
    public int Gen2Collections { get; init; }
}

public record PerformanceReport
{
    public TimeSpan TotalDuration { get; init; }
    public int SnapshotCount { get; init; }

    // CPU Metrics
    public double AvgCpuUsage { get; init; }
    public double MaxCpuUsage { get; init; }
    public double MinCpuUsage { get; init; }

    // Memory Metrics
    public double AvgWorkingSetMB { get; init; }
    public double MaxWorkingSetMB { get; init; }
    public double MinWorkingSetMB { get; init; }

    // Managed Memory Metrics
    public double AvgManagedMemoryMB { get; init; }
    public double MaxManagedMemoryMB { get; init; }
    public double MinManagedMemoryMB { get; init; }

    // GC Metrics
    public int TotalGen0Collections { get; init; }
    public int TotalGen1Collections { get; init; }
    public int TotalGen2Collections { get; init; }

    // Thread Metrics
    public int AvgThreadCount { get; init; }
    public int MaxThreadCount { get; init; }

    // Handle Metrics
    public int AvgHandleCount { get; init; }
    public int MaxHandleCount { get; init; }

    public List<PerformanceSnapshot> Snapshots { get; init; } = new();

    public override string ToString()
    {
        return $@"
=== PERFORMANCE REPORT ===
Duration: {TotalDuration.TotalSeconds:F2}s ({SnapshotCount} samples)

CPU:
  Average: {AvgCpuUsage:F2}%
  Max: {MaxCpuUsage:F2}%
  Min: {MinCpuUsage:F2}%

Memory (Working Set):
  Average: {AvgWorkingSetMB:F2} MB
  Max: {MaxWorkingSetMB:F2} MB
  Min: {MinWorkingSetMB:F2} MB

Managed Memory:
  Average: {AvgManagedMemoryMB:F2} MB
  Max: {MaxManagedMemoryMB:F2} MB
  Min: {MinManagedMemoryMB:F2} MB

Garbage Collection:
  Gen0: {TotalGen0Collections}
  Gen1: {TotalGen1Collections}
  Gen2: {TotalGen2Collections}

Threads:
  Average: {AvgThreadCount}
  Max: {MaxThreadCount}

Handles:
  Average: {AvgHandleCount}
  Max: {MaxHandleCount}
";
    }
}
