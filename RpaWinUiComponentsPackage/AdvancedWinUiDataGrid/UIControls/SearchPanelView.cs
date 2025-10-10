using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.ViewModels;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.UIControls;

/// <summary>
/// Search panel view that provides text search functionality across all grid data.
/// Includes a search text box, search/clear buttons, and options for case sensitivity and filtered-only search.
/// The "Search in Filtered Only" option only appears when both search and filter features are enabled.
/// Built programmatically without XAML for maximum flexibility.
/// </summary>
public sealed class SearchPanelView : UserControl
{
    /// <summary>
    /// Gets the view model that manages search state and options.
    /// </summary>
    public SearchPanelViewModel ViewModel { get; }

    /// <summary>
    /// Fired when the user clicks the "Search" button or presses Enter in the search box.
    /// </summary>
    public event EventHandler? SearchRequested;

    /// <summary>
    /// Fired when the user clicks the "Clear" button to clear search results.
    /// </summary>
    public event EventHandler? ClearRequested;

    private readonly StackPanel _rootPanel;
    private readonly TextBox _searchTextBox;
    private readonly Button _searchButton;
    private readonly Button _clearButton;
    private readonly CheckBox _caseSensitiveCheckBox;
    private readonly CheckBox _searchInFilteredOnlyCheckBox;

    /// <summary>
    /// Creates a new search panel view bound to the specified view model.
    /// </summary>
    /// <param name="viewModel">The view model that manages search state and options</param>
    /// <exception cref="ArgumentNullException">Thrown when viewModel is null</exception>
    public SearchPanelView(SearchPanelViewModel viewModel)
    {
        ViewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));

        // Create root StackPanel with horizontal orientation
        _rootPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Padding = new Thickness(8),
            Spacing = 8
        };

        // Bind root panel visibility
        var visibilityBinding = new Binding
        {
            Source = ViewModel,
            Path = new PropertyPath(nameof(ViewModel.IsVisible)),
            Mode = BindingMode.OneWay,
            Converter = new BoolToVisibilityConverter()
        };
        _rootPanel.SetBinding(UIElement.VisibilityProperty, visibilityBinding);

        // Search TextBox
        _searchTextBox = new TextBox
        {
            PlaceholderText = "Search...",
            Width = 200,
            VerticalAlignment = VerticalAlignment.Center
        };

        var searchTextBinding = new Binding
        {
            Source = ViewModel,
            Path = new PropertyPath(nameof(ViewModel.SearchText)),
            Mode = BindingMode.TwoWay,
            UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
        };
        _searchTextBox.SetBinding(TextBox.TextProperty, searchTextBinding);
        _searchTextBox.KeyDown += OnSearchTextBoxKeyDown;

        // Search Button
        _searchButton = new Button
        {
            Content = "Search",
            VerticalAlignment = VerticalAlignment.Center
        };
        _searchButton.Click += OnSearchButtonClick;

        // Clear Button
        _clearButton = new Button
        {
            Content = "Clear",
            VerticalAlignment = VerticalAlignment.Center
        };
        _clearButton.Click += OnClearButtonClick;

        // Case Sensitive CheckBox
        _caseSensitiveCheckBox = new CheckBox
        {
            Content = "Case Sensitive",
            VerticalAlignment = VerticalAlignment.Center
        };

        var caseSensitiveBinding = new Binding
        {
            Source = ViewModel,
            Path = new PropertyPath(nameof(ViewModel.CaseSensitive)),
            Mode = BindingMode.TwoWay
        };
        _caseSensitiveCheckBox.SetBinding(CheckBox.IsCheckedProperty, caseSensitiveBinding);

        // Search In Filtered Only CheckBox
        _searchInFilteredOnlyCheckBox = new CheckBox
        {
            Content = "Search in Filtered Only",
            VerticalAlignment = VerticalAlignment.Center
        };

        var searchInFilteredOnlyBinding = new Binding
        {
            Source = ViewModel,
            Path = new PropertyPath(nameof(ViewModel.SearchInFilteredOnly)),
            Mode = BindingMode.TwoWay
        };
        _searchInFilteredOnlyCheckBox.SetBinding(CheckBox.IsCheckedProperty, searchInFilteredOnlyBinding);

        var searchInFilteredOnlyVisibilityBinding = new Binding
        {
            Source = ViewModel,
            Path = new PropertyPath(nameof(ViewModel.ShowSearchInFilteredOnlyButton)),
            Mode = BindingMode.OneWay,
            Converter = new BoolToVisibilityConverter()
        };
        _searchInFilteredOnlyCheckBox.SetBinding(UIElement.VisibilityProperty, searchInFilteredOnlyVisibilityBinding);

        // Add all controls to root panel
        _rootPanel.Children.Add(_searchTextBox);
        _rootPanel.Children.Add(_searchButton);
        _rootPanel.Children.Add(_clearButton);
        _rootPanel.Children.Add(_caseSensitiveCheckBox);
        _rootPanel.Children.Add(_searchInFilteredOnlyCheckBox);

        // Set root panel as UserControl content
        Content = _rootPanel;
    }

    private void OnSearchTextBoxKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == Windows.System.VirtualKey.Enter)
        {
            SearchRequested?.Invoke(this, EventArgs.Empty);
            e.Handled = true;
        }
    }

    private void OnSearchButtonClick(object sender, RoutedEventArgs e)
    {
        SearchRequested?.Invoke(this, EventArgs.Empty);
    }

    private void OnClearButtonClick(object sender, RoutedEventArgs e)
    {
        ViewModel.ClearSearch();
        ClearRequested?.Invoke(this, EventArgs.Empty);
    }
}
