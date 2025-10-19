namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Database.Models;

/// <summary>
/// Internal model for database statistics.
/// Provides information about database file size, row counts, and timestamps.
/// </summary>
internal record DatabaseStatistics
{
    /// <summary>Full path to database file</summary>
    public string? FilePath { get; init; }

    /// <summary>File size in bytes</summary>
    public long FileSizeBytes { get; init; }

    /// <summary>File size formatted (e.g., "2.5 MB")</summary>
    public string FileSizeFormatted => FormatFileSize(FileSizeBytes);

    /// <summary>Total row count (including soft-deleted rows)</summary>
    public long TotalRowCount { get; init; }

    /// <summary>Active row count (WHERE __isDeleted = 0)</summary>
    public long ActiveRowCount { get; init; }

    /// <summary>Deleted row count (WHERE __isDeleted = 1)</summary>
    public long DeletedRowCount { get; init; }

    /// <summary>Database creation timestamp</summary>
    public DateTime? CreatedAt { get; init; }

    /// <summary>Last modification timestamp</summary>
    public DateTime? LastModifiedAt { get; init; }

    private static string FormatFileSize(long bytes)
    {
        if (bytes < 1024) return $"{bytes} B";
        if (bytes < 1024 * 1024) return $"{bytes / 1024.0:F2} KB";
        if (bytes < 1024 * 1024 * 1024) return $"{bytes / (1024.0 * 1024):F2} MB";
        return $"{bytes / (1024.0 * 1024 * 1024):F2} GB";
    }
}
