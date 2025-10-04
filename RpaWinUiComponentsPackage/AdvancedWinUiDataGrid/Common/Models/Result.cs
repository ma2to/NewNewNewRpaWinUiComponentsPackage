namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Common.Models;

/// <summary>
/// Represents the result of an operation with success/failure state
/// </summary>
internal class Result
{
    protected Result(bool isSuccess, string? errorMessage = null)
    {
        IsSuccess = isSuccess;
        ErrorMessage = errorMessage;
    }

    /// <summary>
    /// Gets whether the operation was successful
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    /// Gets whether the operation failed
    /// </summary>
    public bool IsFailure => !IsSuccess;

    /// <summary>
    /// Gets the error message if the operation failed
    /// </summary>
    public string? ErrorMessage { get; }

    /// <summary>
    /// Creates a successful result
    /// </summary>
    /// <returns>Successful result instance</returns>
    public static Result Success() => new(true);

    /// <summary>
    /// Creates a failed result with error message
    /// </summary>
    /// <param name="errorMessage">Error message describing the failure</param>
    /// <returns>Failed result instance</returns>
    public static Result Failure(string errorMessage) => new(false, errorMessage);

    /// <summary>
    /// Creates a result based on a condition
    /// </summary>
    /// <param name="condition">Condition to evaluate</param>
    /// <param name="errorMessage">Error message if condition is false</param>
    /// <returns>Result based on condition</returns>
    public static Result SuccessIf(bool condition, string errorMessage) =>
        condition ? Success() : Failure(errorMessage);

    /// <summary>
    /// Creates a result based on a condition
    /// </summary>
    /// <param name="condition">Condition to evaluate</param>
    /// <param name="errorMessage">Error message if condition is true</param>
    /// <returns>Result based on condition</returns>
    public static Result FailureIf(bool condition, string errorMessage) =>
        condition ? Failure(errorMessage) : Success();
}

/// <summary>
/// Represents the result of an operation with a value
/// </summary>
/// <typeparam name="T">Type of the result value</typeparam>
internal class Result<T> : Result
{
    private readonly T? _value;

    protected Result(T value) : base(true)
    {
        _value = value;
    }

    protected Result(string errorMessage) : base(false, errorMessage)
    {
        _value = default;
    }

    /// <summary>
    /// Gets the result value if successful
    /// </summary>
    public T Value => IsSuccess ? _value! : throw new InvalidOperationException("Cannot access value of failed result");

    /// <summary>
    /// Gets the result value or default if failed
    /// </summary>
    /// <param name="defaultValue">Default value to return if failed</param>
    /// <returns>Result value or default</returns>
    public T GetValueOrDefault(T defaultValue = default!)
    {
        return IsSuccess ? _value! : defaultValue;
    }

    /// <summary>
    /// Creates a successful result with a value
    /// </summary>
    /// <param name="value">The result value</param>
    /// <returns>Successful result with value</returns>
    public static Result<T> Success(T value) => new(value);

    /// <summary>
    /// Creates a failed result with error message
    /// </summary>
    /// <param name="errorMessage">Error message describing the failure</param>
    /// <returns>Failed result</returns>
    public static new Result<T> Failure(string errorMessage) => new(errorMessage);

    /// <summary>
    /// Creates a result based on a condition
    /// </summary>
    /// <param name="condition">Condition to evaluate</param>
    /// <param name="value">Value if condition is true</param>
    /// <param name="errorMessage">Error message if condition is false</param>
    /// <returns>Result based on condition</returns>
    public static Result<T> SuccessIf(bool condition, T value, string errorMessage) =>
        condition ? Success(value) : Failure(errorMessage);

    /// <summary>
    /// Maps the result value to another type
    /// </summary>
    /// <typeparam name="TResult">Target type</typeparam>
    /// <param name="mapper">Function to map the value</param>
    /// <returns>Mapped result</returns>
    public Result<TResult> Map<TResult>(Func<T, TResult> mapper)
    {
        return IsSuccess ? Result<TResult>.Success(mapper(_value!)) : Result<TResult>.Failure(ErrorMessage!);
    }

    /// <summary>
    /// Binds the result to another result-returning operation
    /// </summary>
    /// <typeparam name="TResult">Target type</typeparam>
    /// <param name="binder">Function that returns a result</param>
    /// <returns>Bound result</returns>
    public Result<TResult> Bind<TResult>(Func<T, Result<TResult>> binder)
    {
        return IsSuccess ? binder(_value!) : Result<TResult>.Failure(ErrorMessage!);
    }

    /// <summary>
    /// Implicitly converts a value to a successful result
    /// </summary>
    /// <param name="value">Value to convert</param>
    public static implicit operator Result<T>(T value) => Success(value);
}