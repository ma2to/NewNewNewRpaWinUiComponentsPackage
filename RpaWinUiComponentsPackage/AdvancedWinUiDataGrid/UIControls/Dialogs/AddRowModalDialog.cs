using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.ViewModels;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.UIControls.Dialogs;

/// <summary>
/// Modal dialog for adding new row with real-time validation.
/// Displays TextBox for each column with validation feedback.
/// Primary button enabled only when all values are valid.
/// </summary>
public sealed class AddRowModalDialog : ContentDialog
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

        // Build dialog content
        BuildDialogContent();

        // Set up buttons
        PrimaryButtonText = "Add Row";
        SecondaryButtonText = "Cancel";
        Title = "Add New Row";
        DefaultButton = ContentDialogButton.Primary;

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
            var binding = new Binding
            {
                Source = column,
                Path = new PropertyPath(nameof(AddRowFieldViewModel.Value)),
                Mode = BindingMode.TwoWay,
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
            };
            BindingOperations.SetBinding(textBox, TextBox.TextProperty, binding);

            // Subscribe to TextChanged for debounced validation
            textBox.TextChanged += async (s, e) =>
            {
                await column.TriggerDebouncedValidationAsync(_facade, _logger);
            };

            columnPanel.Children.Add(textBox);

            // Validation error TextBlock
            var errorText = new TextBlock
            {
                Foreground = new SolidColorBrush(Colors.Red),
                FontSize = 12,
                Margin = new Thickness(0, 2, 0, 0),
                Visibility = Visibility.Collapsed
            };

            var errorBinding = new Binding
            {
                Source = column,
                Path = new PropertyPath(nameof(AddRowFieldViewModel.ValidationError)),
                Mode = BindingMode.OneWay
            };
            BindingOperations.SetBinding(errorText, TextBlock.TextProperty, errorBinding);

            var visibilityBinding = new Binding
            {
                Source = column,
                Path = new PropertyPath(nameof(AddRowFieldViewModel.HasError)),
                Mode = BindingMode.OneWay,
                Converter = new BoolToVisibilityConverter()
            };
            BindingOperations.SetBinding(errorText, TextBlock.VisibilityProperty, visibilityBinding);

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
    /// Returns row data after final validation.
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

/// <summary>
/// Converter to convert bool to Visibility
/// </summary>
internal class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is bool boolValue)
        {
            return boolValue ? Visibility.Visible : Visibility.Collapsed;
        }
        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
