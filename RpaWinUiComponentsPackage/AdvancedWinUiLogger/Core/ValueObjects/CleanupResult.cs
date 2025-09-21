using System;
using System.Collections.Generic;

namespace RpaWinUiComponentsPackage.AdvancedWinUiLogger.Core.ValueObjects;

/// <summary>
/// ENTERPRISE VALUE OBJECT: Log cleanup operation result
/// IMMUTABLE: Contains detailed cleanup operation metrics
/// FUNCTIONAL: Factory methods for different cleanup outcomes
/// </summary>
public sealed record CleanupResult
{
    public bool IsSuccess { get; init; }
    public int FilesDeleted { get; init; }
    public long BytesFreed { get; init; }
    public TimeSpan OperationDuration { get; init; }
    public string? ErrorMessage { get; init; }
    public IReadOnlyList<string> DeletedFiles { get; init; } = Array.Empty<string>();

    public static CleanupResult Success(int filesDeleted, long bytesFreed, TimeSpan duration, IReadOnlyList<string> deletedFiles) =>
        new()
        {
            IsSuccess = true,
            FilesDeleted = filesDeleted,
            BytesFreed = bytesFreed,
            OperationDuration = duration,
            DeletedFiles = deletedFiles
        };

    public static CleanupResult Failure(string errorMessage) =>
        new() { IsSuccess = false, ErrorMessage = errorMessage };
}