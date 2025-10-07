using System.Text;

namespace RpaWinUiComponentsPackage.PerformanceBenchmarks.Helpers;

/// <summary>
/// Helper class for generating performance comparison reports from benchmark results
/// Provides methods to analyze and visualize performance differences between operation modes
/// </summary>
public static class BenchmarkReportHelper
{
    /// <summary>
    /// Represents benchmark result data for a single operation
    /// </summary>
    public class BenchmarkResult
    {
        public string Operation { get; set; } = string.Empty;
        public string Mode { get; set; } = string.Empty;
        public int RowCount { get; set; }
        public int BatchSize { get; set; }
        public double MeanTimeMs { get; set; }
        public double AllocatedMemoryMB { get; set; }
    }

    /// <summary>
    /// Generates a Markdown comparison table for benchmark results
    /// </summary>
    /// <param name="results">List of benchmark results</param>
    /// <param name="operation">Operation to filter by (optional)</param>
    /// <returns>Markdown formatted table</returns>
    public static string GenerateComparisonTable(List<BenchmarkResult> results, string? operation = null)
    {
        var filtered = operation != null
            ? results.Where(r => r.Operation == operation).ToList()
            : results;

        if (!filtered.Any())
            return "No results to display.";

        var sb = new StringBuilder();

        sb.AppendLine("| Operation | Mode | Row Count | Batch Size | Mean Time (ms) | Memory (MB) | Overhead % |");
        sb.AppendLine("|-----------|------|-----------|------------|----------------|-------------|------------|");

        // Group by operation and row count for overhead calculation
        var groups = filtered.GroupBy(r => new { r.Operation, r.RowCount, r.BatchSize });

        foreach (var group in groups)
        {
            var headless = group.FirstOrDefault(r => r.Mode == "Headless");
            var baseline = headless?.MeanTimeMs ?? 0;

            foreach (var result in group.OrderBy(r => r.Mode))
            {
                var overhead = baseline > 0 ? ((result.MeanTimeMs - baseline) / baseline * 100) : 0;
                var overheadStr = baseline > 0 ? $"{overhead:F1}%" : "N/A";

                sb.AppendLine($"| {result.Operation} | {result.Mode} | {result.RowCount:N0} | {result.BatchSize:N0} | {result.MeanTimeMs:F2} | {result.AllocatedMemoryMB:F2} | {overheadStr} |");
            }

            sb.AppendLine("|-----------|------|-----------|------------|----------------|-------------|------------|");
        }

        return sb.ToString();
    }

    /// <summary>
    /// Calculates overhead percentages for each mode compared to Headless baseline
    /// </summary>
    /// <param name="results">List of benchmark results</param>
    /// <returns>Dictionary mapping mode to average overhead percentage</returns>
    public static Dictionary<string, double> CalculateAverageOverhead(List<BenchmarkResult> results)
    {
        var overheads = new Dictionary<string, List<double>>();

        var groups = results.GroupBy(r => new { r.Operation, r.RowCount, r.BatchSize });

        foreach (var group in groups)
        {
            var headless = group.FirstOrDefault(r => r.Mode == "Headless");
            if (headless == null) continue;

            foreach (var result in group.Where(r => r.Mode != "Headless"))
            {
                var overhead = ((result.MeanTimeMs - headless.MeanTimeMs) / headless.MeanTimeMs) * 100;

                if (!overheads.ContainsKey(result.Mode))
                    overheads[result.Mode] = new List<double>();

                overheads[result.Mode].Add(overhead);
            }
        }

        return overheads.ToDictionary(
            kvp => kvp.Key,
            kvp => kvp.Value.Average()
        );
    }

    /// <summary>
    /// Identifies the optimal batch size for each mode based on performance
    /// </summary>
    /// <param name="results">List of benchmark results</param>
    /// <param name="operation">Operation to analyze</param>
    /// <param name="rowCount">Row count to analyze</param>
    /// <returns>Dictionary mapping mode to optimal batch size</returns>
    public static Dictionary<string, int> FindOptimalBatchSize(
        List<BenchmarkResult> results,
        string operation,
        int rowCount)
    {
        var filtered = results
            .Where(r => r.Operation == operation && r.RowCount == rowCount)
            .ToList();

        var optimal = new Dictionary<string, int>();

        foreach (var mode in filtered.Select(r => r.Mode).Distinct())
        {
            var modeResults = filtered.Where(r => r.Mode == mode).ToList();
            var best = modeResults.OrderBy(r => r.MeanTimeMs).FirstOrDefault();

            if (best != null)
                optimal[mode] = best.BatchSize;
        }

        return optimal;
    }

    /// <summary>
    /// Generates performance recommendations based on benchmark results
    /// </summary>
    /// <param name="results">List of benchmark results</param>
    /// <returns>Formatted recommendations string</returns>
    public static string GenerateRecommendations(List<BenchmarkResult> results)
    {
        var sb = new StringBuilder();
        sb.AppendLine("## Performance Recommendations");
        sb.AppendLine();

        var avgOverhead = CalculateAverageOverhead(results);

        sb.AppendLine("### Mode Selection Guidelines");
        sb.AppendLine();

        foreach (var mode in new[] { "Headless", "Readonly", "Interactive" })
        {
            var overhead = avgOverhead.ContainsKey(mode) ? avgOverhead[mode] : 0;

            sb.AppendLine($"**{mode} Mode**");

            switch (mode)
            {
                case "Headless":
                    sb.AppendLine("- Best for: Batch processing, imports, background tasks");
                    sb.AppendLine("- Performance: Baseline (fastest)");
                    sb.AppendLine("- Overhead: 0% (baseline)");
                    break;

                case "Readonly":
                    sb.AppendLine("- Best for: Read-only grids, reports with occasional manual refresh");
                    sb.AppendLine($"- Performance: ~{overhead:F1}% slower than Headless");
                    sb.AppendLine("- Overhead: UI dispatcher present but inactive");
                    break;

                case "Interactive":
                    sb.AppendLine("- Best for: Interactive editing, real-time data grids");
                    sb.AppendLine($"- Performance: ~{overhead:F1}% slower than Headless");
                    sb.AppendLine("- Overhead: Full UI refresh and notifications");
                    break;
            }

            sb.AppendLine();
        }

        sb.AppendLine("### Batch Size Optimization");
        sb.AppendLine();

        var operations = results.Select(r => r.Operation).Distinct();
        var rowCounts = results.Select(r => r.RowCount).Distinct().OrderBy(x => x);

        foreach (var rowCount in rowCounts)
        {
            sb.AppendLine($"**{rowCount:N0} rows:**");

            foreach (var operation in operations)
            {
                var optimal = FindOptimalBatchSize(results, operation, rowCount);

                if (optimal.Any())
                {
                    sb.AppendLine($"- {operation}:");
                    foreach (var kvp in optimal)
                    {
                        sb.AppendLine($"  - {kvp.Key}: {kvp.Value:N0}");
                    }
                }
            }

            sb.AppendLine();
        }

        sb.AppendLine("### General Guidelines");
        sb.AppendLine();
        sb.AppendLine("1. **For maximum throughput**: Use Headless mode with 10K batch size");
        sb.AppendLine("2. **For balanced performance**: Use Readonly mode with 5K-10K batch size");
        sb.AppendLine("3. **For best UX**: Use Interactive mode with 5K batch size");
        sb.AppendLine("4. **For 1M+ rows**: Consider Headless or Readonly mode to avoid UI blocking");
        sb.AppendLine("5. **Memory considerations**: Larger batch sizes reduce overhead but increase memory usage");

        return sb.ToString();
    }

    /// <summary>
    /// Generates a summary report with key metrics
    /// </summary>
    /// <param name="results">List of benchmark results</param>
    /// <returns>Formatted summary string</returns>
    public static string GenerateSummary(List<BenchmarkResult> results)
    {
        var sb = new StringBuilder();
        sb.AppendLine("## Benchmark Summary");
        sb.AppendLine();

        var modes = results.Select(r => r.Mode).Distinct().OrderBy(m => m);
        var operations = results.Select(r => r.Operation).Distinct().OrderBy(o => o);

        sb.AppendLine($"**Total Benchmarks**: {results.Count}");
        sb.AppendLine($"**Operations Tested**: {string.Join(", ", operations)}");
        sb.AppendLine($"**Modes Tested**: {string.Join(", ", modes)}");
        sb.AppendLine();

        sb.AppendLine("### Performance by Mode (Average across all operations)");
        sb.AppendLine();

        foreach (var mode in modes)
        {
            var modeResults = results.Where(r => r.Mode == mode).ToList();
            var avgTime = modeResults.Average(r => r.MeanTimeMs);
            var avgMemory = modeResults.Average(r => r.AllocatedMemoryMB);

            sb.AppendLine($"**{mode}**:");
            sb.AppendLine($"- Average Time: {avgTime:F2} ms");
            sb.AppendLine($"- Average Memory: {avgMemory:F2} MB");
            sb.AppendLine();
        }

        var overhead = CalculateAverageOverhead(results);

        if (overhead.Any())
        {
            sb.AppendLine("### Average Overhead vs Headless");
            sb.AppendLine();

            foreach (var kvp in overhead.OrderBy(x => x.Value))
            {
                sb.AppendLine($"- **{kvp.Key}**: +{kvp.Value:F1}%");
            }

            sb.AppendLine();
        }

        return sb.ToString();
    }

    /// <summary>
    /// Generates a complete performance report
    /// </summary>
    /// <param name="results">List of benchmark results</param>
    /// <returns>Complete formatted report</returns>
    public static string GenerateCompleteReport(List<BenchmarkResult> results)
    {
        var sb = new StringBuilder();

        sb.AppendLine("# DataGrid Performance Benchmark Report");
        sb.AppendLine();
        sb.AppendLine($"*Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}*");
        sb.AppendLine();

        sb.AppendLine(GenerateSummary(results));
        sb.AppendLine();

        sb.AppendLine("## Detailed Results");
        sb.AppendLine();
        sb.AppendLine(GenerateComparisonTable(results));
        sb.AppendLine();

        sb.AppendLine(GenerateRecommendations(results));

        return sb.ToString();
    }

    /// <summary>
    /// Exports results to CSV format
    /// </summary>
    /// <param name="results">List of benchmark results</param>
    /// <returns>CSV formatted string</returns>
    public static string ExportToCsv(List<BenchmarkResult> results)
    {
        var sb = new StringBuilder();

        sb.AppendLine("Operation,Mode,RowCount,BatchSize,MeanTimeMs,AllocatedMemoryMB");

        foreach (var result in results)
        {
            sb.AppendLine($"{result.Operation},{result.Mode},{result.RowCount},{result.BatchSize},{result.MeanTimeMs},{result.AllocatedMemoryMB}");
        }

        return sb.ToString();
    }

    /// <summary>
    /// Parses benchmark results from CSV format
    /// </summary>
    /// <param name="csv">CSV formatted string</param>
    /// <returns>List of benchmark results</returns>
    public static List<BenchmarkResult> ParseFromCsv(string csv)
    {
        var results = new List<BenchmarkResult>();
        var lines = csv.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        // Skip header
        for (int i = 1; i < lines.Length; i++)
        {
            var parts = lines[i].Split(',');
            if (parts.Length < 6) continue;

            results.Add(new BenchmarkResult
            {
                Operation = parts[0].Trim(),
                Mode = parts[1].Trim(),
                RowCount = int.Parse(parts[2].Trim()),
                BatchSize = int.Parse(parts[3].Trim()),
                MeanTimeMs = double.Parse(parts[4].Trim()),
                AllocatedMemoryMB = double.Parse(parts[5].Trim())
            });
        }

        return results;
    }
}
