using System;

namespace RpaWinUiComponentsPackage.AdvancedWinUiLogger;

/// <summary>
/// PUBLIC MODEL: Result of file rotation operation
/// ENTERPRISE: Complete rotation metadata for tracking
/// </summary>
public sealed record RotationResult
{
    public bool IsSuccess { get; init; }
    public string? OldFilePath { get; init; }
    public string? NewFilePath { get; init; }
    public long RotatedFileSize { get; init; }
    public int FilesProcessed { get; init; }
    public long TotalBytesProcessed { get; init; }
    public TimeSpan OperationDuration { get; init; }
    public string? ErrorMessage { get; init; }
    public RotationType RotationType { get; init; }

    // Backward compatibility properties
    public string? OriginalFilePath => OldFilePath;
    public long OriginalFileSize => RotatedFileSize;
    public int RotationNumber => FilesProcessed;
    public long CompressedSize => TotalBytesProcessed;

    public static RotationResult Success(
        string newFilePath,
        long rotatedFileSize,
        string? oldFilePath = null,
        int filesProcessed = 1,
        long totalBytesProcessed = 0,
        TimeSpan operationDuration = default,
        RotationType rotationType = RotationType.SizeBased)
    {
        return new RotationResult
        {
            IsSuccess = true,
            OldFilePath = oldFilePath,
            NewFilePath = newFilePath,
            RotatedFileSize = rotatedFileSize,
            FilesProcessed = filesProcessed,
            TotalBytesProcessed = totalBytesProcessed == 0 ? rotatedFileSize : totalBytesProcessed,
            OperationDuration = operationDuration,
            RotationType = rotationType
        };
    }

    public static RotationResult Failure(string errorMessage)
    {
        return new RotationResult
        {
            IsSuccess = false,
            ErrorMessage = errorMessage
        };
    }
}
