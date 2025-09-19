namespace RpaWinUiComponentsPackage.AdvancedWinUiLogger.Core.Functional;

/// <summary>
/// 🔄 FUNCTIONAL: Result monad for composable error handling
/// IMMUTABLE: Thread-safe value type for functional composition
/// SENIOR ARCHITECTURE: Monadic operations for clean error propagation
/// </summary>
public readonly struct Result<T>
{
    private readonly T? _value;
    private readonly string? _errorMessage;
    private readonly Exception? _exception;
    private readonly bool _isSuccess;

    #region Constructors

    private Result(T? value, string? errorMessage, Exception? exception, bool isSuccess)
    {
        _value = value;
        _errorMessage = errorMessage;
        _exception = exception;
        _isSuccess = isSuccess;
    }

    #endregion

    #region Static Factory Methods

    /// <summary>FUNCTIONAL: Create successful result</summary>
    public static Result<T> Success(T value) => new(value, null, null, true);

    /// <summary>FUNCTIONAL: Create failure result with message</summary>
    public static Result<T> Failure(string errorMessage) => new(default, errorMessage, null, false);

    /// <summary>FUNCTIONAL: Create failure result with message and exception</summary>
    public static Result<T> Failure(string errorMessage, Exception exception) =>
        new(default, errorMessage, exception, false);

    /// <summary>FUNCTIONAL: Create failure result from exception</summary>
    public static Result<T> Failure(Exception exception) =>
        new(default, exception.Message, exception, false);

    #endregion

    #region Properties

    public bool IsSuccess => _isSuccess;
    public bool IsFailure => !_isSuccess;
    public T Value => _isSuccess ? _value! : throw new InvalidOperationException("Cannot access value of failed result");
    public string ErrorMessage => _errorMessage ?? "Unknown error";
    public Exception? Exception => _exception;

    #endregion

    #region Monadic Operations

    /// <summary>
    /// FUNCTIONAL: Monadic bind operation for composable error handling
    /// </summary>
    public Result<TOut> Bind<TOut>(Func<T, Result<TOut>> func)
    {
        if (_isSuccess)
        {
            try
            {
                return func(_value!);
            }
            catch (Exception ex)
            {
                return Result<TOut>.Failure($"Bind operation failed: {ex.Message}", ex);
            }
        }

        return Result<TOut>.Failure(_errorMessage!, _exception);
    }

    /// <summary>
    /// FUNCTIONAL: Async monadic bind operation
    /// </summary>
    public async Task<Result<TOut>> BindAsync<TOut>(Func<T, Task<Result<TOut>>> func)
    {
        if (_isSuccess)
        {
            try
            {
                return await func(_value!);
            }
            catch (Exception ex)
            {
                return Result<TOut>.Failure($"Async bind operation failed: {ex.Message}", ex);
            }
        }

        return Result<TOut>.Failure(_errorMessage!, _exception);
    }

    /// <summary>
    /// FUNCTIONAL: Map operation for transforming successful values
    /// </summary>
    public Result<TOut> Map<TOut>(Func<T, TOut> func)
    {
        if (_isSuccess)
        {
            try
            {
                var result = func(_value!);
                return Result<TOut>.Success(result);
            }
            catch (Exception ex)
            {
                return Result<TOut>.Failure($"Map operation failed: {ex.Message}", ex);
            }
        }

        return Result<TOut>.Failure(_errorMessage!, _exception);
    }

    /// <summary>
    /// FUNCTIONAL: Async map operation
    /// </summary>
    public async Task<Result<TOut>> MapAsync<TOut>(Func<T, Task<TOut>> func)
    {
        if (_isSuccess)
        {
            try
            {
                var result = await func(_value!);
                return Result<TOut>.Success(result);
            }
            catch (Exception ex)
            {
                return Result<TOut>.Failure($"Async map operation failed: {ex.Message}", ex);
            }
        }

        return Result<TOut>.Failure(_errorMessage!, _exception);
    }

    /// <summary>
    /// FUNCTIONAL: Execute side effect if successful, return original result
    /// </summary>
    public Result<T> Tap(Action<T> action)
    {
        if (_isSuccess)
        {
            try
            {
                action(_value!);
            }
            catch (Exception ex)
            {
                return Result<T>.Failure($"Tap operation failed: {ex.Message}", ex);
            }
        }

        return this;
    }

    /// <summary>
    /// FUNCTIONAL: Provide default value if result is failure
    /// </summary>
    public T ValueOr(T defaultValue) => _isSuccess ? _value! : defaultValue;

    /// <summary>
    /// FUNCTIONAL: Provide default value from function if result is failure
    /// </summary>
    public T ValueOr(Func<T> defaultProvider) => _isSuccess ? _value! : defaultProvider();

    #endregion

    #region Combinators

    /// <summary>
    /// FUNCTIONAL: Combine two results, both must succeed
    /// </summary>
    public static Result<(T1, T2)> Combine<T1, T2>(Result<T1> result1, Result<T2> result2)
    {
        if (result1.IsSuccess && result2.IsSuccess)
        {
            return Result<(T1, T2)>.Success((result1.Value, result2.Value));
        }

        var errors = new List<string>();
        var exceptions = new List<Exception>();

        if (result1.IsFailure)
        {
            errors.Add(result1.ErrorMessage);
            if (result1.Exception != null)
                exceptions.Add(result1.Exception);
        }

        if (result2.IsFailure)
        {
            errors.Add(result2.ErrorMessage);
            if (result2.Exception != null)
                exceptions.Add(result2.Exception);
        }

        var combinedException = exceptions.Any() ? new AggregateException(exceptions) : null;
        return Result<(T1, T2)>.Failure(string.Join("; ", errors), combinedException);
    }

    /// <summary>
    /// FUNCTIONAL: Try operation and wrap in Result
    /// </summary>
    public static Result<T> Try(Func<T> operation)
    {
        try
        {
            var result = operation();
            return Success(result);
        }
        catch (Exception ex)
        {
            return Failure(ex);
        }
    }

    /// <summary>
    /// FUNCTIONAL: Async try operation
    /// </summary>
    public static async Task<Result<T>> TryAsync(Func<Task<T>> operation)
    {
        try
        {
            var result = await operation();
            return Success(result);
        }
        catch (Exception ex)
        {
            return Failure(ex);
        }
    }

    #endregion

    #region Operators

    /// <summary>Implicit conversion from value to successful result</summary>
    public static implicit operator Result<T>(T value) => Success(value);

    /// <summary>Implicit conversion from exception to failed result</summary>
    public static implicit operator Result<T>(Exception exception) => Failure(exception);

    #endregion

    public override string ToString() => _isSuccess ? $"Success: {_value}" : $"Failure: {_errorMessage}";
}

#region Extension Methods

/// <summary>
/// FUNCTIONAL: Extension methods for Result type
/// </summary>
public static class ResultExtensions
{
    /// <summary>Convert Task<T> to Task<Result<T>></summary>
    public static async Task<Result<T>> ToResult<T>(this Task<T> task)
    {
        try
        {
            var result = await task;
            return Result<T>.Success(result);
        }
        catch (Exception ex)
        {
            return Result<T>.Failure(ex);
        }
    }

    /// <summary>Filter result based on predicate</summary>
    public static Result<T> Where<T>(this Result<T> result, Func<T, bool> predicate, string errorMessage = "Predicate failed")
    {
        if (result.IsFailure)
            return result;

        if (predicate(result.Value))
            return result;

        return Result<T>.Failure(errorMessage);
    }

    /// <summary>Ensure result meets condition</summary>
    public static Result<T> Ensure<T>(this Result<T> result, Func<T, bool> predicate, string errorMessage)
    {
        return result.Where(predicate, errorMessage);
    }
}

#endregion