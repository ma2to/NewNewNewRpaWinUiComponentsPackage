# ADD ROW MODAL DIALOG - Špecifikácia

## 📋 METADATA

**Dátum vytvorenia:** 19.10.2025
**Verzia:** 1.0
**Jazyk kódu:** English
**Jazyk dokumentácie:** Slovenčina
**Komponent:** AdvancedDataGrid - Add Row Modal Dialog

---

## 🎯 ÚVOD

Tento dokument špecifikuje implementáciu modálneho okna pre pridanie nového riadku do gridu s real-time validáciou.

### Požiadavky

1. **Modal dialog** s textboxmi pre všetky stĺpce
2. **Real-time validácia** priamo v textboxoch (debounce 300ms)
3. **Podpora troch operačných módov:**
   - Interactive Mode: Kliknutie na ikonu → otvorí modal
   - Headless + Manual UI: Volanie metódy → otvorí modal
   - Pure Headless: Bez UI, len API metóda na pridanie riadku s validáciou
4. **Pridanie na koniec datasetu** po potvrdení
5. **Cancel button** - zahodí zmeny, zatvorí modal
6. **Confirm button** - pridá riadok len ak sú všetky hodnoty validné

---

## 📐 ARCHITEKTÚRA

### Component diagram

```
┌─────────────────────────────────────────────────────────────────┐
│  AdvancedDataGridControl (Main UI)                               │
│  ├─ Header row s Add Row ikonou (+)                             │
│  ├─ OnAddRowIconClicked() handler                               │
│  │  └─ Opens AddRowModalDialog                                  │
│  └─ OR facade.Rows.AddRowWithDialogAsync() (programmatic)       │
└──────────────────────┬──────────────────────────────────────────┘
                       │
                       ▼
┌─────────────────────────────────────────────────────────────────┐
│  AddRowModalDialog (ContentDialog)                              │
│                                                                   │
│  ┌───────────────────────────────────────────────────────────┐ │
│  │  Dialog Header: "Add New Row"                             │ │
│  ├───────────────────────────────────────────────────────────┤ │
│  │  Dialog Content:                                           │ │
│  │                                                             │ │
│  │  ┌─────────────────────────────────────────────────────┐ │ │
│  │  │  StackPanel (vertical)                              │ │ │
│  │  │                                                       │ │ │
│  │  │  For each column:                                    │ │ │
│  │  │  ┌───────────────────────────────────────────────┐ │ │ │
│  │  │  │  Column Header Label (bold)                   │ │ │ │
│  │  │  │  TextBox (with value binding)                 │ │ │ │
│  │  │  │  ValidationError TextBlock (red, collapsed)   │ │ │ │
│  │  │  └───────────────────────────────────────────────┘ │ │ │
│  │  │                                                       │ │ │
│  │  │  (repeats for all columns)                          │ │ │
│  │  └─────────────────────────────────────────────────────┘ │ │
│  │                                                             │ │
│  ├───────────────────────────────────────────────────────────┤ │
│  │  Dialog Footer:                                            │ │
│  │  [Cancel Button]  [Add Row Button (Primary)]              │ │
│  │                                                             │ │
│  │  Add Row button enabled ONLY if all values valid          │ │
│  └───────────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────────┘
```

---

## 🔧 API ŠPECIFIKÁCIA

### 1. IDataGridRows - Public API Extension

```csharp
namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Rows;

public interface IDataGridRows
{
    // ... existujúce metódy ...

    /// <summary>
    /// Opens modal dialog for adding new row (Interactive and Headless+ManualUI modes only).
    /// User fills in column values, real-time validation runs, then confirms or cancels.
    /// New row added to end of dataset on confirmation.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result with row ID of added row (null if cancelled)</returns>
    /// <exception cref="InvalidOperationException">If called in Pure Headless mode</exception>
    /// <remarks>
    /// - Interactive Mode: Automatic UI update after add
    /// - Headless+ManualUI Mode: Requires manual RefreshUIAsync() after add
    /// - Pure Headless Mode: Throws exception - use AddRowAsync() instead
    /// </remarks>
    Task<PublicResult<string?>> AddRowWithDialogAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Opens modal dialog for adding new row with pre-filled default values.
    /// Same as AddRowWithDialogAsync but textboxes are pre-populated.
    /// </summary>
    /// <param name="defaultValues">Dictionary of column names to default values</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result with row ID of added row (null if cancelled)</returns>
    Task<PublicResult<string?>> AddRowWithDialogAsync(
        IReadOnlyDictionary<string, object?> defaultValues,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validate row data without adding to grid.
    /// Useful for pre-validating data before calling AddRowAsync.
    /// </summary>
    /// <param name="rowData">Row data to validate</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Validation result with errors (if any)</returns>
    Task<PublicValidationResult> ValidateRowDataAsync(
        IReadOnlyDictionary<string, object?> rowData,
        CancellationToken cancellationToken = default);
}

public record PublicValidationResult
{
    public bool IsValid { get; init; }
    public IReadOnlyList<PublicCellValidationError> Errors { get; init; } = Array.Empty<PublicCellValidationError>();
}

public record PublicCellValidationError
{
    public string ColumnName { get; init; } = "";
    public string ErrorMessage { get; init; } = "";
    public PublicValidationSeverity Severity { get; init; } = PublicValidationSeverity.Error;
}

public enum PublicValidationSeverity
{
    Error,
    Warning,
    Info
}
```

---

## 🎨 UI IMPLEMENTÁCIA

### AddRowModalDialog.xaml.cs

```csharp
namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.UIControls.Dialogs;

/// <summary>
/// Modal dialog for adding new row with real-time validation.
/// Displays TextBox for each column with validation feedback.
/// </summary>
public sealed partial class AddRowModalDialog : ContentDialog
{
    private readonly IAdvancedDataGridFacade _facade;
    private readonly AddRowDialogViewModel _viewModel;
    private readonly ILogger<AddRowModalDialog> _logger;

    public AddRowModalDialog(
        IAdvancedDataGridFacade facade,
        IEnumerable<string> columnNames,
        IReadOnlyDictionary<string, object?>? defaultValues = null,
        ILogger<AddRowModalDialog>? logger = null)
    {
        _facade = facade ?? throw new ArgumentNullException(nameof(facade));
        _logger = logger ?? NullLogger<AddRowModalDialog>.Instance;

        // Create ViewModel
        _viewModel = new AddRowDialogViewModel(columnNames, defaultValues);

        InitializeComponent();
        BuildDialogContent();

        // Set up buttons
        PrimaryButtonText = "Add Row";
        SecondaryButtonText = "Cancel";
        Title = "Add New Row";

        // Primary button enabled only when all values valid
        IsPrimaryButtonEnabled = false;
        _viewModel.PropertyChanged += ViewModel_PropertyChanged;

        _logger.LogInformation("AddRowModalDialog created with {ColumnCount} columns", columnNames.Count());
    }

    private void BuildDialogContent()
    {
        var rootPanel = new StackPanel
        {
            Spacing = 12,
            Margin = new Thickness(0, 12, 0, 12)
        };

        // For each column, create: Label + TextBox + ValidationError TextBlock
        foreach (var column in _viewModel.ColumnFields)
        {
            var columnPanel = new StackPanel { Spacing = 4 };

            // Column header label
            var headerLabel = new TextBlock
            {
                Text = column.ColumnName,
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                Margin = new Thickness(0, 0, 0, 4)
            };
            columnPanel.Children.Add(headerLabel);

            // TextBox with binding
            var textBox = new TextBox
            {
                PlaceholderText = $"Enter {column.ColumnName}...",
                Width = 400
            };

            // Bind TextBox.Text to ViewModel property
            var binding = new Microsoft.UI.Xaml.Data.Binding
            {
                Source = column,
                Path = new PropertyPath(nameof(AddRowFieldViewModel.Value)),
                Mode = Microsoft.UI.Xaml.Data.BindingMode.TwoWay,
                UpdateSourceTrigger = Microsoft.UI.Xaml.Data.UpdateSourceTrigger.PropertyChanged
            };
            textBox.SetBinding(TextBox.TextProperty, binding);

            // Subscribe to TextChanged for debounced validation
            textBox.TextChanged += async (s, e) =>
            {
                await column.TriggerDebouncedValidationAsync(_facade, _logger);
            };

            columnPanel.Children.Add(textBox);

            // Validation error TextBlock
            var errorText = new TextBlock
            {
                Foreground = new SolidColorBrush(Microsoft.UI.Colors.Red),
                FontSize = 12,
                Margin = new Thickness(0, 2, 0, 0),
                Visibility = Microsoft.UI.Xaml.Visibility.Collapsed
            };

            var errorBinding = new Microsoft.UI.Xaml.Data.Binding
            {
                Source = column,
                Path = new PropertyPath(nameof(AddRowFieldViewModel.ValidationError)),
                Mode = Microsoft.UI.Xaml.Data.BindingMode.OneWay
            };
            errorText.SetBinding(TextBlock.TextProperty, errorBinding);

            var visibilityBinding = new Microsoft.UI.Xaml.Data.Binding
            {
                Source = column,
                Path = new PropertyPath(nameof(AddRowFieldViewModel.HasError)),
                Mode = Microsoft.UI.Xaml.Data.BindingMode.OneWay,
                Converter = new BooleanToVisibilityConverter()
            };
            errorText.SetBinding(TextBlock.VisibilityProperty, visibilityBinding);

            columnPanel.Children.Add(errorText);

            rootPanel.Children.Add(columnPanel);
        }

        Content = new ScrollViewer
        {
            Content = rootPanel,
            MaxHeight = 500,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto
        };
    }

    private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(AddRowDialogViewModel.IsAllValid))
        {
            IsPrimaryButtonEnabled = _viewModel.IsAllValid;
            _logger.LogDebug("Primary button enabled: {Enabled}", IsPrimaryButtonEnabled);
        }
    }

    /// <summary>
    /// Called when user clicks "Add Row" button.
    /// </summary>
    public async Task<IReadOnlyDictionary<string, object?>> GetRowDataAsync()
    {
        // Final validation before return
        var rowData = _viewModel.GetRowData();

        var validationResult = await _facade.Rows.ValidateRowDataAsync(rowData);

        if (!validationResult.IsValid)
        {
            _logger.LogWarning("Validation failed on confirm: {ErrorCount} errors", validationResult.Errors.Count);
            throw new InvalidOperationException("Validation failed - this should not happen if button was correctly disabled");
        }

        _logger.LogInformation("Row data confirmed: {RowData}", string.Join(", ", rowData.Select(kv => $"{kv.Key}={kv.Value}")));
        return rowData;
    }
}
```

### AddRowDialogViewModel.cs

```csharp
namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.ViewModels;

internal sealed class AddRowDialogViewModel : ViewModelBase
{
    public ObservableCollection<AddRowFieldViewModel> ColumnFields { get; }

    public bool IsAllValid => ColumnFields.All(f => !f.HasError);

    public AddRowDialogViewModel(
        IEnumerable<string> columnNames,
        IReadOnlyDictionary<string, object?>? defaultValues = null)
    {
        ColumnFields = new ObservableCollection<AddRowFieldViewModel>();

        foreach (var columnName in columnNames)
        {
            var defaultValue = defaultValues?.TryGetValue(columnName, out var val) == true
                ? val?.ToString() ?? ""
                : "";

            var field = new AddRowFieldViewModel(columnName, defaultValue);
            field.PropertyChanged += Field_PropertyChanged;

            ColumnFields.Add(field);
        }
    }

    private void Field_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(AddRowFieldViewModel.HasError))
        {
            OnPropertyChanged(nameof(IsAllValid));
        }
    }

    public IReadOnlyDictionary<string, object?> GetRowData()
    {
        var rowData = new Dictionary<string, object?>();

        foreach (var field in ColumnFields)
        {
            rowData[field.ColumnName] = field.Value;
        }

        return rowData;
    }
}

internal sealed class AddRowFieldViewModel : ViewModelBase
{
    private string _value;
    private string? _validationError;
    private CancellationTokenSource? _validationCts;

    public string ColumnName { get; }

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

    public bool HasError => !string.IsNullOrEmpty(ValidationError);

    public AddRowFieldViewModel(string columnName, string defaultValue = "")
    {
        ColumnName = columnName;
        _value = defaultValue;
    }

    /// <summary>
    /// Trigger debounced validation (300ms delay).
    /// </summary>
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

            // Validate single cell
            var rowData = new Dictionary<string, object?> { [ColumnName] = Value };
            var result = await facade.Rows.ValidateRowDataAsync(rowData, token);

            if (!result.IsValid)
            {
                var error = result.Errors.FirstOrDefault(e => e.ColumnName == ColumnName);
                ValidationError = error?.ErrorMessage ?? "Invalid value";
                logger.LogDebug("Validation error for {Column}: {Error}", ColumnName, ValidationError);
            }
            else
            {
                ValidationError = null;
            }
        }
        catch (TaskCanceledException)
        {
            // Cancelled by newer input - ignore
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Validation failed for {Column}", ColumnName);
            ValidationError = "Validation failed";
        }
    }
}
```

---

## 🔄 TRI OPERAČNÉ MÓDY - USAGE

### Interactive Mode

```csharp
// User clicks Add Row icon (+) in grid header
// UI automatically opens AddRowModalDialog
// After confirmation:
//   - Row added to dataset
//   - UI automatically refreshes (new row appears at end)

// Programmatic API call:
var result = await grid.Rows.AddRowWithDialogAsync();

if (result.IsSuccess && result.Value != null)
{
    Console.WriteLine($"Row added with ID: {result.Value}");
    // UI already refreshed automatically
}
else if (result.Value == null)
{
    Console.WriteLine("User cancelled");
}
```

### Headless + Manual UI Update Mode

```csharp
// Programmatic API call:
var result = await grid.Rows.AddRowWithDialogAsync();

if (result.IsSuccess && result.Value != null)
{
    Console.WriteLine($"Row added with ID: {result.Value}");

    // CRITICAL: Manual UI refresh required
    await grid.RefreshUIAsync();
}
```

### Pure Headless Mode

```csharp
// Modal dialog NOT available - use direct API
var rowData = new Dictionary<string, object?>
{
    ["Name"] = "John Doe",
    ["Age"] = 30,
    ["Email"] = "john@example.com"
};

// Validate first (optional but recommended)
var validationResult = await grid.Rows.ValidateRowDataAsync(rowData);

if (!validationResult.IsValid)
{
    foreach (var error in validationResult.Errors)
    {
        Console.WriteLine($"Validation error [{error.ColumnName}]: {error.ErrorMessage}");
    }
    return;
}

// Add row
var addResult = await grid.Rows.AddRowAsync(rowData);

if (addResult.IsSuccess)
{
    Console.WriteLine($"Row added at index: {addResult.Value}");
}
```

---

## 🧪 VALIDÁCIA V MODAL DIALOGU

### Real-time validácia (per-cell)

**Trigger:** TextBox.TextChanged event
**Debounce:** 300ms
**Validácie:**
- Required (ak column má Required rule)
- Type validation (numeric, DateTime, email, ...)
- Range validation (min/max)
- Custom regex pattern
- Uniqueness check (async call to DB)

**Example validation rule:**
```csharp
// Validation rule for "Email" column
var emailRule = new ValidationRule
{
    ColumnName = "Email",
    RuleType = ValidationRuleType.Regex,
    Pattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
    ErrorMessage = "Invalid email format"
};

await grid.Validation.AddValidationRuleAsync(emailRule);
```

### Uniqueness validation (async)

```csharp
// Uniqueness rule for "Email" column
var uniqueRule = new ValidationRule
{
    ColumnName = "Email",
    RuleType = ValidationRuleType.Unique,
    ErrorMessage = "Email already exists"
};

await grid.Validation.AddValidationRuleAsync(uniqueRule);

// During modal dialog typing:
// User types "john@example.com"
//   ↓ (debounce 300ms)
// Validation checks: SELECT COUNT(*) FROM grid_rows WHERE json_extract(data, '$.Email') = 'john@example.com'
//   ↓
// If count > 0: ValidationError = "Email already exists"
```

---

Koniec štvrtého dokumentu. Pokračujem s posledným hlavným dokumentom...
