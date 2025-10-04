using System;
using System.Collections.Generic;

namespace RpaWinUiComponentsPackage.AdvancedWinUiLogger;

/// <summary>
/// PUBLIC MODEL: Result of cleanup operation
/// ENTERPRISE: Complete cleanup metadata for tracking
/// </summary>
public sealed record CleanupResult
{
    public bool IsSuccess { get; init; }
    public int FilesDeleted { get; init; }
    public long BytesFreed { get; init; }
    public TimeSpan OperationDuration { get; init; }
    public string? ErrorMessage { get; init; }
    public IReadOnlyList<string> DeletedFiles { get; init; } = Array.Empty<string>();

    public static CleanupResult Success(
        int filesDeleted,
        long bytesFreed,
        TimeSpan duration,
        IReadOnlyList<string> deletedFiles)
    {
        return new CleanupResult
        {
            IsSuccess = true,
            FilesDeleted = filesDeleted,
            BytesFreed = bytesFreed,
            OperationDuration = duration,
            DeletedFiles = deletedFiles
        };
    }

    public static CleanupResult Failure(string errorMessage)
    {
        return new CleanupResult
        {
            IsSuccess = false,
            ErrorMessage = errorMessage
        };
    }
}
