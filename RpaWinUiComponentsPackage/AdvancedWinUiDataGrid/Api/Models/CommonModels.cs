namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid;

/// <summary>
/// Public result type for operations without return value
/// </summary>
public class PublicResult
{
    public bool IsSuccess { get; init; }
    public string? ErrorMessage { get; init; }
    public bool IsFailure => !IsSuccess;

    /// <summary>Alias for ErrorMessage for API consistency</summary>
    public string? Message
    {
        get => ErrorMessage;
        init => ErrorMessage = value;
    }

    public static PublicResult Success() => new() { IsSuccess = true };
    public static PublicResult Failure(string errorMessage) => new() { IsSuccess = false, ErrorMessage = errorMessage };
}

/// <summary>
/// Public result type for operations with return value
/// </summary>
public class PublicResult<T>
{
    public bool IsSuccess { get; init; }
    public string? ErrorMessage { get; init; }
    public T? Value { get; init; }
    public bool IsFailure => !IsSuccess;

    /// <summary>Alias for ErrorMessage for API consistency</summary>
    public string? Message
    {
        get => ErrorMessage;
        init => ErrorMessage = value;
    }

    /// <summary>Alias for Value for API consistency</summary>
    public T? Data
    {
        get => Value;
        init => Value = value;
    }

    public static PublicResult<T> Success(T value) => new() { IsSuccess = true, Value = value };
    public static PublicResult<T> Failure(string errorMessage) => new() { IsSuccess = false, ErrorMessage = errorMessage };
}
