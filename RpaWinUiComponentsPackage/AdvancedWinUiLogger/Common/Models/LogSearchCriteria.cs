using System;
using Microsoft.Extensions.Logging;

namespace RpaWinUiComponentsPackage.AdvancedWinUiLogger;

/// <summary>
/// PUBLIC MODEL: Search criteria for log analysis
/// ENTERPRISE: Flexible search configuration
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

    public static LogSearchCriteria Create(string searchText)
    {
        return new LogSearchCriteria { SearchText = searchText };
    }
}
