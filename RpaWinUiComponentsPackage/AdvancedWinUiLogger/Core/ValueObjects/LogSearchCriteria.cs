using System;
using Microsoft.Extensions.Logging;

namespace RpaWinUiComponentsPackage.AdvancedWinUiLogger.Core.ValueObjects;

/// <summary>
/// ENTERPRISE VALUE OBJECT: Log search criteria
/// IMMUTABLE: Search parameters for log file analysis
/// FUNCTIONAL: Query specification for log operations
/// </summary>
public sealed record LogSearchCriteria
{
    public string SearchText { get; init; } = string.Empty;
    public LogLevel? MinLevel { get; init; }
    public LogLevel? MaxLevel { get; init; }
    public DateTime? FromDate { get; init; }
    public DateTime? ToDate { get; init; }
    public bool UseRegex { get; init; }
    public bool CaseSensitive { get; init; }
    public int? MaxResults { get; init; } = 1000;

    public static LogSearchCriteria Create(string searchText) =>
        new() { SearchText = searchText };
}