using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using RpaWinUiComponentsPackage.AdvancedWinUiLogger.Core.Entities;
using RpaWinUiComponentsPackage.AdvancedWinUiLogger.Core.ValueObjects;

namespace RpaWinUiComponentsPackage.AdvancedWinUiLogger.Tests.Core.Entities;

/// <summary>
/// ENTERPRISE TESTS: Comprehensive testing for LoggerSession entity
/// COVERAGE: Business logic, edge cases, and error conditions
/// QUALITY: TDD-driven test suite for mission-critical functionality
/// </summary>
internal static class LoggerSessionTests
{
    /// <summary>
    /// TEST RUNNER: Execute all LoggerSession tests
    /// ENTERPRISE: Comprehensive test execution with detailed reporting
    /// </summary>
    public static async Task<bool> RunAllTests()
    {
        var testResults = new List<(string TestName, bool Passed, string? Error)>();

        // Core functionality tests
        testResults.Add(await RunTest("Constructor_ValidConfiguration_CreatesSession", Test_Constructor_ValidConfiguration_CreatesSession));
        testResults.Add(await RunTest("Constructor_NullConfiguration_ThrowsException", Test_Constructor_NullConfiguration_ThrowsException));
        testResults.Add(await RunTest("AddLogEntry_ValidEntry_ReturnsSuccess", Test_AddLogEntry_ValidEntry_ReturnsSuccess));
        testResults.Add(await RunTest("AddLogEntry_NullEntry_ReturnsFailure", Test_AddLogEntry_NullEntry_ReturnsFailure));
        testResults.Add(await RunTest("AddLogEntry_InactiveSession_ReturnsFailure", Test_AddLogEntry_InactiveSession_ReturnsFailure));

        // Buffer management tests
        testResults.Add(await RunTest("AddLogEntry_BufferFull_AutoFlushes", Test_AddLogEntry_BufferFull_AutoFlushes));
        testResults.Add(await RunTest("FlushPendingEntries_EmptyBuffer_ReturnsZero", Test_FlushPendingEntries_EmptyBuffer_ReturnsZero));
        testResults.Add(await RunTest("FlushPendingEntries_WithEntries_UpdatesCounters", Test_FlushPendingEntries_WithEntries_UpdatesCounters));

        // Configuration management tests
        testResults.Add(await RunTest("UpdateConfiguration_ValidConfig_UpdatesSuccessfully", Test_UpdateConfiguration_ValidConfig_UpdatesSuccessfully));
        testResults.Add(await RunTest("UpdateConfiguration_NullConfig_ReturnsFailure", Test_UpdateConfiguration_NullConfig_ReturnsFailure));
        testResults.Add(await RunTest("UpdateConfiguration_InvalidConfig_ReturnsFailure", Test_UpdateConfiguration_InvalidConfig_ReturnsFailure));

        // Session lifecycle tests
        testResults.Add(await RunTest("Stop_ActiveSession_StopsSuccessfully", Test_Stop_ActiveSession_StopsSuccessfully));
        testResults.Add(await RunTest("Stop_InactiveSession_ReturnsSuccess", Test_Stop_InactiveSession_ReturnsSuccess));

        // Metrics and status tests
        testResults.Add(await RunTest("GetStatus_NewSession_ReturnsValidStatus", Test_GetStatus_NewSession_ReturnsValidStatus));
        testResults.Add(await RunTest("GetThroughput_AfterLogging_ReturnsValidMetrics", Test_GetThroughput_AfterLogging_ReturnsValidMetrics));

        // Report results
        var totalTests = testResults.Count;
        var passedTests = testResults.Count(r => r.Passed);
        var failedTests = totalTests - passedTests;

        Console.WriteLine($"LoggerSession Tests Summary: {passedTests}/{totalTests} passed");
        if (failedTests > 0)
        {
            Console.WriteLine("Failed tests:");
            foreach (var failedTest in testResults.Where(r => !r.Passed))
            {
                Console.WriteLine($"  - {failedTest.TestName}: {failedTest.Error}");
            }
        }

        return failedTests == 0;
    }

    #region Test Runner Helper

    private static async Task<(string TestName, bool Passed, string? Error)> RunTest(string testName, Func<Task<bool>> testMethod)
    {
        try
        {
            var result = await testMethod();
            return (testName, result, null);
        }
        catch (Exception ex)
        {
            return (testName, false, ex.Message);
        }
    }

    #endregion

    #region Constructor Tests

    private static async Task<bool> Test_Constructor_ValidConfiguration_CreatesSession()
    {
        await Task.CompletedTask;

        var config = LoggerConfiguration.CreateMinimal(@"C:\TestLogs", "test");
        var session = new LoggerSession(config);

        return session.SessionId != Guid.Empty &&
               session.Configuration == config &&
               session.IsActive &&
               session.TotalEntriesLogged == 0 &&
               session.StartedAt <= DateTime.UtcNow;
    }

    private static async Task<bool> Test_Constructor_NullConfiguration_ThrowsException()
    {
        await Task.CompletedTask;

        try
        {
            var session = new LoggerSession(null!);
            return false; // Should have thrown
        }
        catch (ArgumentNullException)
        {
            return true;
        }
        catch
        {
            return false; // Wrong exception type
        }
    }

    #endregion

    #region AddLogEntry Tests

    private static async Task<bool> Test_AddLogEntry_ValidEntry_ReturnsSuccess()
    {
        await Task.CompletedTask;

        var config = LoggerConfiguration.CreateMinimal(@"C:\TestLogs", "test");
        var session = new LoggerSession(config);
        var entry = new LogEntry(DateTime.UtcNow, LogLevel.Information, "Test message");

        var result = session.AddLogEntry(entry);

        return result.IsSuccess &&
               result.Value &&
               session.TotalEntriesLogged == 0; // Not flushed yet
    }

    private static async Task<bool> Test_AddLogEntry_NullEntry_ReturnsFailure()
    {
        await Task.CompletedTask;

        var config = LoggerConfiguration.CreateMinimal(@"C:\TestLogs", "test");
        var session = new LoggerSession(config);

        var result = session.AddLogEntry(null!);

        return result.IsFailure &&
               result.Error.Contains("cannot be null");
    }

    private static async Task<bool> Test_AddLogEntry_InactiveSession_ReturnsFailure()
    {
        await Task.CompletedTask;

        var config = LoggerConfiguration.CreateMinimal(@"C:\TestLogs", "test");
        var session = new LoggerSession(config);

        // Stop the session
        session.Stop();

        var entry = new LogEntry(DateTime.UtcNow, LogLevel.Information, "Test message");
        var result = session.AddLogEntry(entry);

        return result.IsFailure &&
               result.Error.Contains("inactive");
    }

    #endregion

    #region Buffer Management Tests

    private static async Task<bool> Test_AddLogEntry_BufferFull_AutoFlushes()
    {
        await Task.CompletedTask;

        var config = LoggerConfiguration.CreateMinimal(@"C:\TestLogs", "test") with { BufferSize = 2 };
        var session = new LoggerSession(config);

        // Add entries up to buffer size
        var entry1 = new LogEntry(DateTime.UtcNow, LogLevel.Information, "Message 1");
        var entry2 = new LogEntry(DateTime.UtcNow, LogLevel.Information, "Message 2");

        var result1 = session.AddLogEntry(entry1);
        var result2 = session.AddLogEntry(entry2);

        return result1.IsSuccess &&
               result2.IsSuccess &&
               session.GetPendingEntriesCount() == 0; // Should have auto-flushed
    }

    private static async Task<bool> Test_FlushPendingEntries_EmptyBuffer_ReturnsZero()
    {
        await Task.CompletedTask;

        var config = LoggerConfiguration.CreateMinimal(@"C:\TestLogs", "test");
        var session = new LoggerSession(config);

        var result = session.FlushPendingEntries();

        return result.IsSuccess &&
               result.Value == 0;
    }

    private static async Task<bool> Test_FlushPendingEntries_WithEntries_UpdatesCounters()
    {
        await Task.CompletedTask;

        var config = LoggerConfiguration.CreateMinimal(@"C:\TestLogs", "test") with { BufferSize = 10 };
        var session = new LoggerSession(config);

        // Add an entry but don't trigger auto-flush
        var entry = new LogEntry(DateTime.UtcNow, LogLevel.Information, "Test message");
        session.AddLogEntry(entry);

        var initialCount = session.TotalEntriesLogged;
        var flushResult = session.FlushPendingEntries();

        return flushResult.IsSuccess &&
               flushResult.Value == 1 &&
               session.TotalEntriesLogged > initialCount &&
               session.GetPendingEntriesCount() == 0;
    }

    #endregion

    #region Configuration Management Tests

    private static async Task<bool> Test_UpdateConfiguration_ValidConfig_UpdatesSuccessfully()
    {
        await Task.CompletedTask;

        var config = LoggerConfiguration.CreateMinimal(@"C:\TestLogs", "test");
        var session = new LoggerSession(config);

        var newConfig = config with { BufferSize = 500 };
        var result = session.UpdateConfiguration(newConfig);

        return result.IsSuccess &&
               session.Configuration.BufferSize == 500;
    }

    private static async Task<bool> Test_UpdateConfiguration_NullConfig_ReturnsFailure()
    {
        await Task.CompletedTask;

        var config = LoggerConfiguration.CreateMinimal(@"C:\TestLogs", "test");
        var session = new LoggerSession(config);

        var result = session.UpdateConfiguration(null!);

        return result.IsFailure &&
               result.Error.Contains("cannot be null");
    }

    private static async Task<bool> Test_UpdateConfiguration_InvalidConfig_ReturnsFailure()
    {
        await Task.CompletedTask;

        var config = LoggerConfiguration.CreateMinimal(@"C:\TestLogs", "test");
        var session = new LoggerSession(config);

        var invalidConfig = config with { BufferSize = -1 }; // Invalid buffer size
        var result = session.UpdateConfiguration(invalidConfig);

        return result.IsFailure &&
               result.Error.Contains("Invalid configuration");
    }

    #endregion

    #region Session Lifecycle Tests

    private static async Task<bool> Test_Stop_ActiveSession_StopsSuccessfully()
    {
        await Task.CompletedTask;

        var config = LoggerConfiguration.CreateMinimal(@"C:\TestLogs", "test");
        var session = new LoggerSession(config);

        // Add some entries first
        var entry = new LogEntry(DateTime.UtcNow, LogLevel.Information, "Test message");
        session.AddLogEntry(entry);

        var result = session.Stop();

        return result.IsSuccess &&
               !session.IsActive &&
               session.GetPendingEntriesCount() == 0; // Should have flushed
    }

    private static async Task<bool> Test_Stop_InactiveSession_ReturnsSuccess()
    {
        await Task.CompletedTask;

        var config = LoggerConfiguration.CreateMinimal(@"C:\TestLogs", "test");
        var session = new LoggerSession(config);

        session.Stop(); // First stop
        var result = session.Stop(); // Second stop

        return result.IsSuccess;
    }

    #endregion

    #region Metrics and Status Tests

    private static async Task<bool> Test_GetStatus_NewSession_ReturnsValidStatus()
    {
        await Task.CompletedTask;

        var config = LoggerConfiguration.CreateMinimal(@"C:\TestLogs", "test");
        var session = new LoggerSession(config);

        var status = session.GetStatus();

        return status.SessionId == session.SessionId &&
               status.IsActive &&
               status.TotalEntriesLogged == 0 &&
               status.PendingEntries == 0 &&
               status.StartedAt <= DateTime.UtcNow;
    }

    private static async Task<bool> Test_GetThroughput_AfterLogging_ReturnsValidMetrics()
    {
        await Task.CompletedTask;

        var config = LoggerConfiguration.CreateMinimal(@"C:\TestLogs", "test");
        var session = new LoggerSession(config);

        // Add and flush some entries
        for (int i = 0; i < 5; i++)
        {
            var entry = new LogEntry(DateTime.UtcNow, LogLevel.Information, $"Test message {i}");
            session.AddLogEntry(entry);
        }
        session.FlushPendingEntries();

        // Wait a moment for time to pass
        await Task.Delay(10);

        var throughputEps = session.GetThroughputEntriesPerSecond();
        var throughputMbps = session.GetThroughputMBPerSecond();

        return throughputEps >= 0 &&
               throughputMbps >= 0 &&
               session.TotalEntriesLogged == 5;
    }

    #endregion
}