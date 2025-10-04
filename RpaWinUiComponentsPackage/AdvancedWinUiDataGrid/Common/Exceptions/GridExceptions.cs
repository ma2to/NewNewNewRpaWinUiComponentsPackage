namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Common.Exceptions;

/// <summary>
/// SECURITY: Base exception for all grid operations
/// </summary>
internal class GridException : Exception
{
    public GridException() { }
    public GridException(string message) : base(message) { }
    public GridException(string message, Exception innerException) : base(message, innerException) { }
}

/// <summary>
/// SECURITY: Exception for validation failures
/// </summary>
internal class GridValidationException : GridException
{
    public IReadOnlyList<string> ValidationErrors { get; }

    public GridValidationException(string message) : base(message)
    {
        ValidationErrors = Array.Empty<string>();
    }

    public GridValidationException(string message, IEnumerable<string> validationErrors) : base(message)
    {
        ValidationErrors = validationErrors.ToList();
    }

    public GridValidationException(string message, Exception innerException) : base(message, innerException)
    {
        ValidationErrors = Array.Empty<string>();
    }
}

/// <summary>
/// SECURITY: Exception for operation failures
/// </summary>
internal class GridOperationException : GridException
{
    public string? OperationName { get; }
    public object? OperationContext { get; }

    public GridOperationException(string message) : base(message) { }

    public GridOperationException(string message, string operationName) : base(message)
    {
        OperationName = operationName;
    }

    public GridOperationException(string message, string operationName, object? context) : base(message)
    {
        OperationName = operationName;
        OperationContext = context;
    }

    public GridOperationException(string message, Exception innerException) : base(message, innerException) { }
}

/// <summary>
/// SECURITY: Exception for configuration issues
/// </summary>
internal class GridConfigurationException : GridException
{
    public string? ConfigurationKey { get; }
    public object? InvalidValue { get; }

    public GridConfigurationException(string message) : base(message) { }

    public GridConfigurationException(string message, string configurationKey) : base(message)
    {
        ConfigurationKey = configurationKey;
    }

    public GridConfigurationException(string message, string configurationKey, object? invalidValue) : base(message)
    {
        ConfigurationKey = configurationKey;
        InvalidValue = invalidValue;
    }

    public GridConfigurationException(string message, Exception innerException) : base(message, innerException) { }
}

/// <summary>
/// SECURITY: Exception for data-related issues
/// </summary>
internal class GridDataException : GridException
{
    public int? RowIndex { get; }
    public string? ColumnName { get; }
    public object? InvalidData { get; }

    public GridDataException(string message) : base(message) { }

    public GridDataException(string message, int rowIndex) : base(message)
    {
        RowIndex = rowIndex;
    }

    public GridDataException(string message, int rowIndex, string columnName) : base(message)
    {
        RowIndex = rowIndex;
        ColumnName = columnName;
    }

    public GridDataException(string message, int rowIndex, string columnName, object? invalidData) : base(message)
    {
        RowIndex = rowIndex;
        ColumnName = columnName;
        InvalidData = invalidData;
    }

    public GridDataException(string message, Exception innerException) : base(message, innerException) { }
}
