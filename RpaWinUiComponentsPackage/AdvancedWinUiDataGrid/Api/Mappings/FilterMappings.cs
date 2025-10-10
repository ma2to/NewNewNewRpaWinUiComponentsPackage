using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Common;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Filter.Interfaces;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Api.Mappings;

/// <summary>
/// Extension methods for mapping filter-related types between public and internal representations
/// </summary>
internal static class FilterMappings
{
    /// <summary>
    /// Convert public PublicFilterDescriptor to internal FilterCriteria
    /// </summary>
    public static FilterCriteria ToInternal(this PublicFilterDescriptor publicFilter)
    {
        return new FilterCriteria
        {
            ColumnName = publicFilter.ColumnName,
            Operator = publicFilter.Operator.ToInternal(),
            Value = publicFilter.Value
        };
    }

    /// <summary>
    /// Convert public PublicFilterOperator to internal FilterOperator
    /// </summary>
    public static FilterOperator ToInternal(this PublicFilterOperator publicOperator)
    {
        return publicOperator switch
        {
            PublicFilterOperator.Equals => FilterOperator.Equals,
            PublicFilterOperator.NotEquals => FilterOperator.NotEquals,
            PublicFilterOperator.Contains => FilterOperator.Contains,
            PublicFilterOperator.NotContains => FilterOperator.NotContains,
            PublicFilterOperator.StartsWith => FilterOperator.StartsWith,
            PublicFilterOperator.EndsWith => FilterOperator.EndsWith,
            PublicFilterOperator.GreaterThan => FilterOperator.GreaterThan,
            PublicFilterOperator.LessThan => FilterOperator.LessThan,
            PublicFilterOperator.GreaterThanOrEqual => FilterOperator.GreaterThanOrEqual,
            PublicFilterOperator.LessThanOrEqual => FilterOperator.LessThanOrEqual,
            PublicFilterOperator.IsNull => FilterOperator.IsNull,
            PublicFilterOperator.IsNotNull => FilterOperator.IsNotNull,
            // InRange not supported in internal implementation yet
            _ => throw new ArgumentException($"Unsupported filter operator: {publicOperator}")
        };
    }

    /// <summary>
    /// Convert internal FilterCriteria to public PublicFilterDescriptor
    /// </summary>
    public static PublicFilterDescriptor ToPublic(this FilterCriteria internalFilter)
    {
        return new PublicFilterDescriptor
        {
            ColumnName = internalFilter.ColumnName,
            Operator = internalFilter.Operator.ToPublic(),
            Value = internalFilter.Value
        };
    }

    /// <summary>
    /// Convert internal FilterOperator to public PublicFilterOperator
    /// </summary>
    public static PublicFilterOperator ToPublic(this FilterOperator internalOperator)
    {
        return internalOperator switch
        {
            FilterOperator.Equals => PublicFilterOperator.Equals,
            FilterOperator.NotEquals => PublicFilterOperator.NotEquals,
            FilterOperator.Contains => PublicFilterOperator.Contains,
            FilterOperator.NotContains => PublicFilterOperator.NotContains,
            FilterOperator.StartsWith => PublicFilterOperator.StartsWith,
            FilterOperator.EndsWith => PublicFilterOperator.EndsWith,
            FilterOperator.GreaterThan => PublicFilterOperator.GreaterThan,
            FilterOperator.LessThan => PublicFilterOperator.LessThan,
            FilterOperator.GreaterThanOrEqual => PublicFilterOperator.GreaterThanOrEqual,
            FilterOperator.LessThanOrEqual => PublicFilterOperator.LessThanOrEqual,
            FilterOperator.IsNull => PublicFilterOperator.IsNull,
            FilterOperator.IsNotNull => PublicFilterOperator.IsNotNull,
            // InRange not in internal enum
            _ => throw new ArgumentException($"Unsupported filter operator: {internalOperator}")
        };
    }
}
