namespace RpaWinUiComponentsPackage.ComprehensiveBenchmarks.Infrastructure;

/// <summary>
/// Generates realistic test data for benchmarks
/// </summary>
public static class TestDataGenerator
{
    private static readonly Random _random = new(42); // Fixed seed for reproducibility

    public static List<List<object>> GenerateGridData(int rows, int columns)
    {
        var data = new List<List<object>>();

        for (int i = 0; i < rows; i++)
        {
            var row = new List<object>();
            for (int j = 0; j < columns; j++)
            {
                row.Add(GenerateCellValue(i, j));
            }
            data.Add(row);
        }

        return data;
    }

    private static object GenerateCellValue(int row, int col)
    {
        return col switch
        {
            0 => $"Item-{row:D6}",
            1 => _random.Next(1, 10000),
            2 => _random.NextDouble() * 1000,
            3 => DateTime.Now.AddDays(_random.Next(-365, 365)),
            4 => _random.Next(0, 2) == 0,
            5 => GenerateRandomString(10, 50),
            _ => $"Data-{row}-{col}"
        };
    }

    private static string GenerateRandomString(int minLength, int maxLength)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789 ";
        var length = _random.Next(minLength, maxLength);
        return new string(Enumerable.Range(0, length)
            .Select(_ => chars[_random.Next(chars.Length)])
            .ToArray());
    }

    public static List<string> GenerateColumnHeaders(int count)
    {
        var headers = new List<string>();
        for (int i = 0; i < count; i++)
        {
            headers.Add($"Column_{i + 1}");
        }
        return headers;
    }

    public static (int row, int col) GenerateRandomCell(int maxRows, int maxCols)
    {
        return (_random.Next(0, maxRows), _random.Next(0, maxCols));
    }

    public static List<(int row, int col)> GenerateRandomCells(int count, int maxRows, int maxCols)
    {
        var cells = new List<(int row, int col)>();
        for (int i = 0; i < count; i++)
        {
            cells.Add(GenerateRandomCell(maxRows, maxCols));
        }
        return cells;
    }
}
