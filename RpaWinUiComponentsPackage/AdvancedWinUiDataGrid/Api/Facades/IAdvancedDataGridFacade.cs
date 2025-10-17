using System.Data;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Common.Models;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Common;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.AutoRowHeight.Interfaces;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid;

/// <summary>
/// Public interface for the AdvancedDataGrid facade providing comprehensive data grid functionality
/// </summary>
public interface IAdvancedDataGridFacade : IAsyncDisposable
{
    #region Feature Module Properties

    /// <summary>
    /// Column management operations
    /// </summary>
    Columns.IDataGridColumns Columns { get; }

    /// <summary>
    /// Cell editing operations
    /// </summary>
    Editing.IDataGridEditing Editing { get; }

    /// <summary>
    /// Filtering operations
    /// </summary>
    Filtering.IDataGridFiltering Filtering { get; }

    /// <summary>
    /// Selection operations
    /// </summary>
    Selection.IDataGridSelection Selection { get; }

    /// <summary>
    /// Sorting operations
    /// </summary>
    Sorting.IDataGridSorting Sorting { get; }

    /// <summary>
    /// Configuration management
    /// </summary>
    Configuration.IDataGridConfiguration Configuration { get; }

    /// <summary>
    /// Row management operations
    /// </summary>
    Rows.IDataGridRows Rows { get; }

    /// <summary>
    /// Batch operations
    /// </summary>
    Batch.IDataGridBatch Batch { get; }

    /// <summary>
    /// Import/Export operations
    /// </summary>
    IO.IDataGridIO IO { get; }

    /// <summary>
    /// Clipboard operations
    /// </summary>
    Clipboard.IDataGridClipboard Clipboard { get; }

    /// <summary>
    /// Search operations
    /// </summary>
    Search.IDataGridSearch Search { get; }

    /// <summary>
    /// Validation operations
    /// </summary>
    Validation.IDataGridValidation Validation { get; }

    /// <summary>
    /// Performance monitoring
    /// </summary>
    Performance.IDataGridPerformance Performance { get; }

    /// <summary>
    /// Theme and color management
    /// </summary>
    Theming.IDataGridTheming Theming { get; }

    /// <summary>
    /// UI notifications and subscriptions
    /// </summary>
    Notifications.IDataGridNotifications Notifications { get; }

    /// <summary>
    /// Auto row height management
    /// </summary>
    AutoRowHeight.IDataGridAutoRowHeight AutoRowHeight { get; }

    /// <summary>
    /// Keyboard shortcuts
    /// </summary>
    Shortcuts.IDataGridShortcuts Shortcuts { get; }

    /// <summary>
    /// MVVM binding support
    /// </summary>
    MVVM.IDataGridMVVM MVVM { get; }

    /// <summary>
    /// Smart row management operations (add/delete with minimum rows)
    /// </summary>
    SmartOperations.IDataGridSmartOperations SmartOperations { get; }

    #endregion

    #region UI Control Access

    /// <summary>
    /// Gets the UI control for the DataGrid (Interactive mode only).
    /// In Interactive mode, the component manages its own ViewModel and UI control with automatic updates.
    /// The UI control is pre-configured with event handlers and data binding.
    /// </summary>
    /// <returns>The AdvancedDataGridControl instance, or null if in Headless mode</returns>
    /// <exception cref="InvalidOperationException">Thrown when called in Headless mode or when DispatcherQueue is not provided</exception>
    UIControls.AdvancedDataGridControl? GetUIControl();

    #endregion
}

/// <summary>
/// Interface for validation rules
/// </summary>
public interface IValidationRule
{
    /// <summary>
    /// Gets the unique identifier for this validation rule
    /// </summary>
    string RuleId { get; }

    /// <summary>
    /// Gets the name of the validation rule
    /// </summary>
    string RuleName { get; }

    /// <summary>
    /// Gets the columns this rule depends on
    /// </summary>
    IReadOnlyList<string> DependentColumns { get; }

    /// <summary>
    /// Gets whether this rule is enabled
    /// </summary>
    bool IsEnabled { get; }

    /// <summary>
    /// Gets the validation timeout
    /// </summary>
    TimeSpan ValidationTimeout { get; }

    /// <summary>
    /// Validates a row (synchronous version for compatibility)
    /// </summary>
    /// <param name="row">Row data to validate</param>
    /// <param name="context">Validation context</param>
    /// <returns>Validation result</returns>
    ValidationResult Validate(
        IReadOnlyDictionary<string, object?> row,
        ValidationContext context);

    /// <summary>
    /// Validates a row (asynchronous version)
    /// </summary>
    /// <param name="row">Row data to validate</param>
    /// <param name="context">Validation context</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Validation result</returns>
    Task<ValidationResult> ValidateAsync(
        IReadOnlyDictionary<string, object?> row,
        ValidationContext context,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Validation context for rules
/// </summary>
public class ValidationContext
{
    /// <summary>
    /// Gets or sets the row index being validated
    /// </summary>
    public int RowIndex { get; set; }

    /// <summary>
    /// Gets or sets the column name being validated
    /// </summary>
    public string? ColumnName { get; set; }

    /// <summary>
    /// Gets or sets all row data for cross-row validation
    /// </summary>
    public IReadOnlyList<IReadOnlyDictionary<string, object?>>? AllRows { get; set; }

    /// <summary>
    /// Gets or sets custom validation properties
    /// </summary>
    public Dictionary<string, object?> Properties { get; set; } = new();

    /// <summary>
    /// Gets or sets the operation ID for tracking
    /// </summary>
    public string? OperationId { get; set; }
}

/// <summary>
/// PublicResult of validation operation
/// </summary>
public class ValidationResult
{
    /// <summary>
    /// Gets whether validation passed
    /// </summary>
    public bool IsValid { get; init; }

    /// <summary>
    /// Gets the validation error message
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Gets the validation severity
    /// </summary>
    public PublicValidationSeverity Severity { get; init; } = PublicValidationSeverity.Error;

    /// <summary>
    /// Gets the affected column
    /// </summary>
    public string? AffectedColumn { get; init; }

    /// <summary>
    /// Creates a successful validation PublicResult
    /// </summary>
    /// <returns>Successful validation result</returns>
    public static ValidationResult Success() => new() { IsValid = true };

    /// <summary>
    /// Creates a failed validation PublicResult
    /// </summary>
    /// <param name="errorMessage">Error message</param>
    /// <param name="severity">Validation severity</param>
    /// <param name="affectedColumn">Affected column name</param>
    /// <returns>Failed validation result</returns>
    public static ValidationResult Error(string errorMessage, PublicValidationSeverity severity = PublicValidationSeverity.Error, string? affectedColumn = null) =>
        new() { IsValid = false, ErrorMessage = errorMessage, Severity = severity, AffectedColumn = affectedColumn };
}