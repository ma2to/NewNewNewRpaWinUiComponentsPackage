using Microsoft.Extensions.Logging.Abstractions;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Api.Models;

namespace RpaWinUiComponentsPackage.PerformanceBenchmarks.Helpers;

/// <summary>
/// Helper class for creating facades with proper configuration
/// </summary>
internal static class BenchmarkHelper
{
    public static IAdvancedDataGridFacade CreateFacade(params GridFeature[] features)
    {
        var loggerFactory = NullLoggerFactory.Instance;

        var options = new AdvancedDataGridOptions
        {
            BatchSize = 5000,
            EnableParallelProcessing = true,
            EnableRealTimeValidation = false,
            LoggerFactory = loggerFactory
        };

        // Enable specific features
        options.EnabledFeatures.Clear();
        foreach (var feature in features)
        {
            options.EnabledFeatures.Add(feature);
        }

        return AdvancedDataGridFacadeFactory.CreateStandalone(options, loggerFactory);
    }

    public static IAdvancedDataGridFacade CreateFacadeWithBatchSize(int batchSize, params GridFeature[] features)
    {
        var loggerFactory = NullLoggerFactory.Instance;

        var options = new AdvancedDataGridOptions
        {
            BatchSize = batchSize,
            EnableParallelProcessing = true,
            EnableRealTimeValidation = false,
            LoggerFactory = loggerFactory
        };

        // Enable specific features
        options.EnabledFeatures.Clear();
        foreach (var feature in features)
        {
            options.EnabledFeatures.Add(feature);
        }

        return AdvancedDataGridFacadeFactory.CreateStandalone(options, loggerFactory);
    }

    public static PublicColumnDefinition CreateColumn(string name, Type dataType)
    {
        return new PublicColumnDefinition
        {
            Name = name,
            Header = name,
            DataType = dataType,
            Width = 100,
            MinWidth = 50,
            IsVisible = true,
            IsSortable = true,
            IsFilterable = true
        };
    }
}
