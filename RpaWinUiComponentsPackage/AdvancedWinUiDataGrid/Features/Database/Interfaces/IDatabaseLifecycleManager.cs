using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Common.Models;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Database.Models;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Database.Interfaces;

/// <summary>
/// Internal interface for database lifecycle management.
/// Handles initialization, shutdown, and cleanup operations for SQLite database.
/// </summary>
internal interface IDatabaseLifecycleManager : IAsyncDisposable
{
    /// <summary>
    /// Initialize SQLite database at specified path.
    /// Creates file, applies PRAGMA settings, creates schema (tables, indexes, FTS).
    /// </summary>
    /// <param name="databasePath">Full path to database file (or null for auto-generated temp path)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result indicating success or failure</returns>
    Task<Result> InitializeDatabaseAsync(
        string? databasePath = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Shutdown database and cleanup resources.
    /// Closes connection, deletes temp file (if possible).
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result indicating success or failure</returns>
    Task<Result> ShutdownDatabaseAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get current database file path.
    /// Returns null if database not initialized.
    /// </summary>
    string? GetDatabasePath();

    /// <summary>
    /// Get database statistics (file size, row counts, timestamps).
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Database statistics</returns>
    /// <exception cref="InvalidOperationException">If database not initialized</exception>
    Task<DatabaseStatistics> GetDatabaseStatisticsAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete ALL database files (*.db) in specified directory.
    /// USE WITH CAUTION - deletes ALL .db files!
    /// Intended for cleanup on application startup.
    /// </summary>
    /// <param name="directoryPath">Directory to scan for .db files</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result with count of deleted files</returns>
    Task<Result<int>> DeleteAllDatabasesInPathAsync(
        string directoryPath,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get SQLite connection (for internal use by HybridRowStore).
    /// Returns null if database not initialized.
    /// </summary>
    Microsoft.Data.Sqlite.SqliteConnection? GetConnection();

    /// <summary>
    /// Indicates whether database is initialized and ready for operations.
    /// </summary>
    bool IsInitialized { get; }
}
