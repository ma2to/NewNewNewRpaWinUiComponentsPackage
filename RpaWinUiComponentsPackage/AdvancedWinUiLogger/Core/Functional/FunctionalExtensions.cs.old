namespace RpaWinUiComponentsPackage.AdvancedWinUiLogger.Core.Functional;

/// <summary>
/// 🔄 FUNCTIONAL: Extension methods for functional programming patterns
/// SENIOR ARCHITECTURE: Pipeline operations and functional composition
/// IMMUTABLE: Pure function helpers for data transformation
/// </summary>
public static class FunctionalExtensions
{
    #region Pipeline Operations

    /// <summary>
    /// FUNCTIONAL: Forward pipe operator for method chaining
    /// </summary>
    public static TOut Pipe<TIn, TOut>(this TIn input, Func<TIn, TOut> func) => func(input);

    /// <summary>
    /// FUNCTIONAL: Async forward pipe operator
    /// </summary>
    public static async Task<TOut> PipeAsync<TIn, TOut>(this TIn input, Func<TIn, Task<TOut>> func) => await func(input);

    /// <summary>
    /// FUNCTIONAL: Conditional pipe - only apply if condition is true
    /// </summary>
    public static T PipeIf<T>(this T input, bool condition, Func<T, T> func) =>
        condition ? func(input) : input;

    /// <summary>
    /// FUNCTIONAL: Safe pipe - handle exceptions gracefully
    /// </summary>
    public static T PipeSafe<T>(this T input, Func<T, T> func, T fallback)
    {
        try
        {
            return func(input);
        }
        catch
        {
            return fallback;
        }
    }

    #endregion

    #region Functional Composition

    /// <summary>
    /// FUNCTIONAL: Compose two functions
    /// </summary>
    public static Func<T, TResult> Compose<T, TMiddle, TResult>(
        this Func<T, TMiddle> first,
        Func<TMiddle, TResult> second) =>
        input => second(first(input));

    /// <summary>
    /// FUNCTIONAL: Partial application
    /// </summary>
    public static Func<T2, TResult> Partial<T1, T2, TResult>(
        this Func<T1, T2, TResult> func,
        T1 firstArg) =>
        secondArg => func(firstArg, secondArg);

    /// <summary>
    /// FUNCTIONAL: Curry function
    /// </summary>
    public static Func<T1, Func<T2, TResult>> Curry<T1, T2, TResult>(
        this Func<T1, T2, TResult> func) =>
        firstArg => secondArg => func(firstArg, secondArg);

    #endregion

    #region Side Effects

    /// <summary>
    /// FUNCTIONAL: Execute side effect and return original value
    /// </summary>
    public static T Do<T>(this T input, Action<T> action)
    {
        action(input);
        return input;
    }

    /// <summary>
    /// FUNCTIONAL: Async side effect
    /// </summary>
    public static async Task<T> DoAsync<T>(this T input, Func<T, Task> action)
    {
        await action(input);
        return input;
    }

    /// <summary>
    /// FUNCTIONAL: Conditional side effect
    /// </summary>
    public static T DoIf<T>(this T input, bool condition, Action<T> action)
    {
        if (condition)
        {
            action(input);
        }
        return input;
    }

    /// <summary>
    /// FUNCTIONAL: Safe side effect - ignore exceptions
    /// </summary>
    public static T DoSafe<T>(this T input, Action<T> action)
    {
        try
        {
            action(input);
        }
        catch
        {
            // Ignore exceptions in side effects
        }
        return input;
    }

    #endregion

    #region Validation and Guards

    /// <summary>
    /// FUNCTIONAL: Ensure condition is met
    /// </summary>
    public static T Ensure<T>(this T input, Func<T, bool> predicate, string errorMessage)
    {
        if (!predicate(input))
        {
            throw new ArgumentException(errorMessage);
        }
        return input;
    }

    /// <summary>
    /// FUNCTIONAL: Ensure not null
    /// </summary>
    public static T EnsureNotNull<T>(this T? input, string? paramName = null) where T : class =>
        input ?? throw new ArgumentNullException(paramName ?? nameof(input));

    /// <summary>
    /// FUNCTIONAL: Ensure string not null or empty
    /// </summary>
    public static string EnsureNotEmpty(this string? input, string? paramName = null) =>
        !string.IsNullOrEmpty(input) ? input : throw new ArgumentException("String cannot be null or empty", paramName);

    /// <summary>
    /// FUNCTIONAL: Ensure string not null, empty, or whitespace
    /// </summary>
    public static string EnsureNotWhiteSpace(this string? input, string? paramName = null) =>
        !string.IsNullOrWhiteSpace(input) ? input : throw new ArgumentException("String cannot be null, empty, or whitespace", paramName);

    #endregion

    #region Collection Operations

    /// <summary>
    /// FUNCTIONAL: Apply function to each element and return original collection
    /// </summary>
    public static IEnumerable<T> ForEach<T>(this IEnumerable<T> source, Action<T> action)
    {
        var items = source.ToList();
        foreach (var item in items)
        {
            action(item);
        }
        return items;
    }

    /// <summary>
    /// FUNCTIONAL: Partition collection into two based on predicate
    /// </summary>
    public static (IEnumerable<T> True, IEnumerable<T> False) Partition<T>(
        this IEnumerable<T> source,
        Func<T, bool> predicate)
    {
        var items = source.ToList();
        return (items.Where(predicate), items.Where(x => !predicate(x)));
    }

    /// <summary>
    /// FUNCTIONAL: Safe enumeration - handle null collections
    /// </summary>
    public static IEnumerable<T> OrEmpty<T>(this IEnumerable<T>? source) =>
        source ?? Enumerable.Empty<T>();

    /// <summary>
    /// FUNCTIONAL: Chunk collection into batches
    /// </summary>
    public static IEnumerable<IEnumerable<T>> Batch<T>(this IEnumerable<T> source, int batchSize)
    {
        var batch = new List<T>(batchSize);
        foreach (var item in source)
        {
            batch.Add(item);
            if (batch.Count == batchSize)
            {
                yield return batch;
                batch = new List<T>(batchSize);
            }
        }
        if (batch.Count > 0)
        {
            yield return batch;
        }
    }

    #endregion

    #region Async Helpers

    /// <summary>
    /// FUNCTIONAL: Convert sync function to async
    /// </summary>
    public static Task<T> ToAsync<T>(this T value) => Task.FromResult(value);

    /// <summary>
    /// FUNCTIONAL: Execute async action synchronously (use with caution)
    /// </summary>
    public static T RunSync<T>(this Task<T> task) => task.GetAwaiter().GetResult();

    /// <summary>
    /// FUNCTIONAL: Fire and forget async operation
    /// </summary>
    public static void FireAndForget(this Task task, Action<Exception>? errorHandler = null)
    {
        task.ContinueWith(t =>
        {
            if (t.Exception != null && errorHandler != null)
            {
                errorHandler(t.Exception);
            }
        }, TaskContinuationOptions.OnlyOnFaulted);
    }

    #endregion

    #region Type Conversion

    /// <summary>
    /// FUNCTIONAL: Safe cast to type
    /// </summary>
    public static Option<T> As<T>(this object obj) where T : class =>
        obj is T result ? Option<T>.Some(result) : Option<T>.None();

    /// <summary>
    /// FUNCTIONAL: Try parse to type
    /// </summary>
    public static Option<T> TryParse<T>(this string input, Func<string, (bool Success, T Value)> parser)
    {
        var (success, value) = parser(input);
        return success ? Option<T>.Some(value) : Option<T>.None();
    }

    #endregion
}