using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Common.Models;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Api.Mappings;

/// <summary>
/// Extension methods for mapping internal Result to public PublicResult
/// </summary>
internal static class ResultMappings
{
    /// <summary>
    /// Convert internal Result to PublicResult
    /// </summary>
    public static PublicResult ToPublic(this Result result)
    {
        if (result.IsSuccess)
        {
            return PublicResult.Success();
        }
        return PublicResult.Failure(result.ErrorMessage ?? "Operation failed");
    }

    /// <summary>
    /// Convert internal Result<T> to PublicResult<T>
    /// </summary>
    public static PublicResult<TPublic> ToPublic<TInternal, TPublic>(this Result<TInternal> result, Func<TInternal, TPublic> mapper)
    {
        if (result.IsSuccess)
        {
            var mappedValue = mapper(result.Value);
            return PublicResult<TPublic>.Success(mappedValue);
        }
        return PublicResult<TPublic>.Failure(result.ErrorMessage ?? "Operation failed");
    }

    /// <summary>
    /// Convert internal Result<T> to PublicResult<T> when types are the same
    /// </summary>
    public static PublicResult<T> ToPublic<T>(this Result<T> result)
    {
        if (result.IsSuccess)
        {
            return PublicResult<T>.Success(result.Value);
        }
        return PublicResult<T>.Failure(result.ErrorMessage ?? "Operation failed");
    }

    /// <summary>
    /// Convert internal EditResult to PublicResult
    /// </summary>
    public static PublicResult ToPublic(this EditResult editResult)
    {
        if (editResult.IsSuccess)
        {
            return PublicResult.Success();
        }
        return PublicResult.Failure(editResult.ErrorMessage ?? "Edit operation failed");
    }
}
