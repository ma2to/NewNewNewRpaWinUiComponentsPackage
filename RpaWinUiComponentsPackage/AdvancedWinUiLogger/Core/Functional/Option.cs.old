namespace RpaWinUiComponentsPackage.AdvancedWinUiLogger.Core.Functional;

/// <summary>
/// 🔄 FUNCTIONAL: Option type for representing optional values
/// IMMUTABLE: Thread-safe value type for null safety
/// SENIOR ARCHITECTURE: Complements Result<T> for functional programming patterns
/// </summary>
public readonly struct Option<T>
{
    private readonly T? _value;
    private readonly bool _hasValue;

    private Option(T? value, bool hasValue)
    {
        _value = value;
        _hasValue = hasValue;
    }

    #region Static Factory Methods

    /// <summary>FUNCTIONAL: Create option with value</summary>
    public static Option<T> Some(T value) => new(value, true);

    /// <summary>FUNCTIONAL: Create empty option</summary>
    public static Option<T> None() => new(default, false);

    /// <summary>FUNCTIONAL: Create option from nullable value</summary>
    public static Option<T> FromNullable(T? value) => value != null ? Some(value) : None();

    #endregion

    #region Properties

    public bool HasValue => _hasValue;
    public bool IsNone => !_hasValue;
    public T Value => _hasValue ? _value! : throw new InvalidOperationException("Option has no value");

    #endregion

    #region Monadic Operations

    /// <summary>FUNCTIONAL: Map operation for transforming values</summary>
    public Option<TOut> Map<TOut>(Func<T, TOut> func) =>
        _hasValue ? Option<TOut>.Some(func(_value!)) : Option<TOut>.None();

    /// <summary>FUNCTIONAL: Async map operation</summary>
    public async Task<Option<TOut>> MapAsync<TOut>(Func<T, Task<TOut>> func) =>
        _hasValue ? Option<TOut>.Some(await func(_value!)) : Option<TOut>.None();

    /// <summary>FUNCTIONAL: Bind operation for chaining optional operations</summary>
    public Option<TOut> Bind<TOut>(Func<T, Option<TOut>> func) =>
        _hasValue ? func(_value!) : Option<TOut>.None();

    /// <summary>FUNCTIONAL: Async bind operation</summary>
    public async Task<Option<TOut>> BindAsync<TOut>(Func<T, Task<Option<TOut>>> func) =>
        _hasValue ? await func(_value!) : Option<TOut>.None();

    /// <summary>FUNCTIONAL: Filter option based on predicate</summary>
    public Option<T> Where(Func<T, bool> predicate) =>
        _hasValue && predicate(_value!) ? this : None();

    /// <summary>FUNCTIONAL: Execute side effect if value exists</summary>
    public Option<T> Tap(Action<T> action)
    {
        if (_hasValue)
        {
            action(_value!);
        }
        return this;
    }

    #endregion

    #region Value Access

    /// <summary>FUNCTIONAL: Get value or default</summary>
    public T ValueOr(T defaultValue) => _hasValue ? _value! : defaultValue;

    /// <summary>FUNCTIONAL: Get value or default from function</summary>
    public T ValueOr(Func<T> defaultProvider) => _hasValue ? _value! : defaultProvider();

    /// <summary>FUNCTIONAL: Get value or throw custom exception</summary>
    public T ValueOrThrow(Func<Exception> exceptionProvider) =>
        _hasValue ? _value! : throw exceptionProvider();

    #endregion

    #region Conversion Methods

    /// <summary>Convert to nullable value</summary>
    public T? ToNullable() => _hasValue ? _value : default;

    /// <summary>Convert to Result type</summary>
    public Result<T> ToResult(string errorMessage = "Option has no value") =>
        _hasValue ? Result<T>.Success(_value!) : Result<T>.Failure(errorMessage);

    /// <summary>Convert to enumerable</summary>
    public IEnumerable<T> ToEnumerable() => _hasValue ? new[] { _value! } : Enumerable.Empty<T>();

    #endregion

    #region Static Operations

    /// <summary>FUNCTIONAL: Try operation and wrap in Option</summary>
    public static Option<T> Try(Func<T> operation)
    {
        try
        {
            var result = operation();
            return result != null ? Some(result) : None();
        }
        catch
        {
            return None();
        }
    }

    /// <summary>FUNCTIONAL: Async try operation</summary>
    public static async Task<Option<T>> TryAsync(Func<Task<T>> operation)
    {
        try
        {
            var result = await operation();
            return result != null ? Some(result) : None();
        }
        catch
        {
            return None();
        }
    }

    #endregion

    #region Operators

    /// <summary>Implicit conversion from value to Some</summary>
    public static implicit operator Option<T>(T value) => Some(value);

    /// <summary>Equality operator</summary>
    public static bool operator ==(Option<T> left, Option<T> right) =>
        left._hasValue == right._hasValue &&
        (!left._hasValue || EqualityComparer<T>.Default.Equals(left._value, right._value));

    /// <summary>Inequality operator</summary>
    public static bool operator !=(Option<T> left, Option<T> right) => !(left == right);

    #endregion

    #region Object Methods

    public override bool Equals(object? obj) =>
        obj is Option<T> other && this == other;

    public override int GetHashCode() =>
        _hasValue ? EqualityComparer<T>.Default.GetHashCode(_value!) : 0;

    public override string ToString() => _hasValue ? $"Some({_value})" : "None";

    #endregion
}

#region Extension Methods

/// <summary>
/// FUNCTIONAL: Extension methods for Option type
/// </summary>
public static class OptionExtensions
{
    /// <summary>Convert nullable to Option</summary>
    public static Option<T> ToOption<T>(this T? value) where T : class =>
        value != null ? Option<T>.Some(value) : Option<T>.None();

    /// <summary>Convert nullable struct to Option</summary>
    public static Option<T> ToOption<T>(this T? value) where T : struct =>
        value.HasValue ? Option<T>.Some(value.Value) : Option<T>.None();

    /// <summary>FirstOrNone extension for collections</summary>
    public static Option<T> FirstOrNone<T>(this IEnumerable<T> source)
    {
        foreach (var item in source)
        {
            return Option<T>.Some(item);
        }
        return Option<T>.None();
    }

    /// <summary>FirstOrNone with predicate</summary>
    public static Option<T> FirstOrNone<T>(this IEnumerable<T> source, Func<T, bool> predicate)
    {
        foreach (var item in source)
        {
            if (predicate(item))
            {
                return Option<T>.Some(item);
            }
        }
        return Option<T>.None();
    }
}

#endregion