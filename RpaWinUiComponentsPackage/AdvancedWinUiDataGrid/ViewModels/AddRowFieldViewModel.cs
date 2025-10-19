using Microsoft.Extensions.Logging;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.ViewModels;

/// <summary>
/// ViewModel for a single field in Add Row modal dialog.
/// Handles value binding, validation error display, and debounced validation.
/// </summary>
internal sealed class AddRowFieldViewModel : ViewModelBase
{
    private string _value;
    private string? _validationError;
    private CancellationTokenSource? _validationCts;

    /// <summary>
    /// Column name this field represents
    /// </summary>
    public string ColumnName { get; }

    /// <summary>
    /// Current value of the field
    /// </summary>
    public string Value
    {
        get => _value;
        set
        {
            if (SetProperty(ref _value, value))
            {
                // Value changed - clear error immediately, debounce validation
                ValidationError = null;
            }
        }
    }

    /// <summary>
    /// Current validation error message (null if valid)
    /// </summary>
    public string? ValidationError
    {
        get => _validationError;
        set
        {
            if (SetProperty(ref _validationError, value))
            {
                OnPropertyChanged(nameof(HasError));
            }
        }
    }

    /// <summary>
    /// True if field has validation error
    /// </summary>
    public bool HasError => !string.IsNullOrEmpty(ValidationError);

    public AddRowFieldViewModel(string columnName, string defaultValue = "")
    {
        ColumnName = columnName;
        _value = defaultValue;
    }

    /// <summary>
    /// Trigger debounced validation (300ms delay).
    /// Cancels previous validation if new input arrives.
    /// </summary>
    /// <param name="facade">AdvancedDataGrid facade for accessing validation</param>
    /// <param name="logger">Logger for diagnostic information</param>
    public async Task TriggerDebouncedValidationAsync(
        IAdvancedDataGridFacade facade,
        ILogger logger)
    {
        // Cancel previous validation
        _validationCts?.Cancel();
        _validationCts = new CancellationTokenSource();
        var token = _validationCts.Token;

        try
        {
            // Debounce 300ms
            await Task.Delay(300, token);

            // Validate single cell - create row with just this column
            var rowData = new Dictionary<string, object?> { [ColumnName] = Value };
            var result = await facade.Rows.ValidateRowDataAsync(rowData, token);

            if (!result.IsValid)
            {
                // Find error for this column
                var error = result.Errors.FirstOrDefault(e => e.ColumnName == ColumnName);
                ValidationError = error?.ErrorMessage ?? "Invalid value";
                logger.LogDebug("Validation error for {Column}: {Error}", ColumnName, ValidationError);
            }
            else
            {
                ValidationError = null;
                logger.LogDebug("Validation passed for {Column}", ColumnName);
            }
        }
        catch (TaskCanceledException)
        {
            // Cancelled by newer input - ignore
            logger.LogDebug("Validation cancelled for {Column}", ColumnName);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Validation failed for {Column}", ColumnName);
            ValidationError = "Validation failed";
        }
    }
}
