using System;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Core.ValueObjects;

/// <summary>
/// FUNCTIONAL: Result monad pattern for error handling without exceptions
/// ENTERPRISE: Professional error handling with detailed context
/// </summary>
internal readonly record struct Result<T>
{
    private readonly T? _value;
    private readonly string _error;
    private readonly Exception? _exception;

    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;

    public T Value => IsSuccess ? _value! : throw new InvalidOperationException($"Cannot access Value of failed result: {_error}");
    public string Error => IsFailure ? _error : throw new InvalidOperationException("Cannot access Error of successful result");
    public Exception? Exception => _exception;

    private Result(T value)
    {
        _value = value;
        _error = string.Empty;
        _exception = null;
        IsSuccess = true;
    }

    private Result(string error, Exception? exception = null)
    {
        _value = default;
        _error = error ?? "Unknown error";
        _exception = exception;
        IsSuccess = false;
    }

    /// <summary>Create successful result</summary>
    public static Result<T> Success(T value) => new(value);

    /// <summary>Create failed result</summary>
    public static Result<T> Failure(string error, Exception? exception = null) => new(error, exception);

    /// <summary>Create failed result from exception</summary>
    public static Result<T> Failure(Exception exception) => new(exception.Message, exception);

    /// <summary>Convert to different type on success</summary>
    public Result<U> Map<U>(Func<T, U> mapper)
    {
        return IsSuccess
            ? Result<U>.Success(mapper(Value))
            : Result<U>.Failure(_error, _exception);
    }

    /// <summary>Chain operations with potential failure</summary>
    public Result<U> Bind<U>(Func<T, Result<U>> binder)
    {
        return IsSuccess
            ? binder(Value)
            : Result<U>.Failure(_error, _exception);
    }

    public override string ToString()
    {
        return IsSuccess
            ? $"Success({_value})"
            : $"Failure({_error})";
    }
}

/// <summary>
/// FUNCTIONAL: Result without value for operations that return success/failure only
/// </summary>
internal readonly record struct Result
{
    private readonly string _error;
    private readonly Exception? _exception;

    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;

    public string Error => IsFailure ? _error : throw new InvalidOperationException("Cannot access Error of successful result");
    public Exception? Exception => _exception;

    private Result(string error, Exception? exception = null)
    {
        _error = error ?? "Unknown error";
        _exception = exception;
        IsSuccess = false;
    }

    private Result(bool success)
    {
        _error = string.Empty;
        _exception = null;
        IsSuccess = success;
    }

    /// <summary>Create successful result</summary>
    public static Result Success() => new(true);

    /// <summary>Create failed result</summary>
    public static Result Failure(string error, Exception? exception = null) => new(error, exception);

    /// <summary>Create failed result from exception</summary>
    public static Result Failure(Exception exception) => new(exception.Message, exception);

    public override string ToString()
    {
        return IsSuccess
            ? "Success"
            : $"Failure({_error})";
    }
}