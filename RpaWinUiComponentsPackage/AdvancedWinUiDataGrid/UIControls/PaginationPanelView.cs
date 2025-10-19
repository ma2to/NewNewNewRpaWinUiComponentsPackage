using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using System.Collections.Specialized;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.ViewModels;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.UIControls;

/// <summary>
/// Pagination panel view that provides page navigation controls.
/// Includes Previous/Next buttons, page number buttons, and page info text.
/// Built programmatically without XAML for maximum flexibility.
/// </summary>
public sealed class PaginationPanelView : UserControl
{
    /// <summary>
    /// Gets the view model that manages pagination state.
    /// </summary>
    public PaginationPanelViewModel ViewModel { get; }

    /// <summary>
    /// Fired when the user navigates to a different page.
    /// </summary>
    public event EventHandler<int>? PageChanged;

    private readonly StackPanel _rootPanel;
    private readonly Button _previousButton;
    private readonly Button _nextButton;
    private readonly StackPanel _pageNumbersPanel;
    private readonly TextBlock _pageInfoText;

    /// <summary>
    /// Creates a new pagination panel view bound to the specified view model.
    /// </summary>
    /// <param name="viewModel">The view model that manages pagination state</param>
    /// <exception cref="ArgumentNullException">Thrown when viewModel is null</exception>
    public PaginationPanelView(PaginationPanelViewModel viewModel)
    {
        ViewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));

        // Subscribe to ViewModel PageChanged event
        ViewModel.PageChanged += OnViewModelPageChanged;

        // Create root StackPanel with horizontal orientation
        _rootPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Padding = new Thickness(8),
            Spacing = 8,
            HorizontalAlignment = HorizontalAlignment.Center
        };

        // Bind root panel visibility to IsPaginationEnabled
        var visibilityBinding = new Binding
        {
            Source = ViewModel,
            Path = new PropertyPath(nameof(ViewModel.IsPaginationEnabled)),
            Mode = BindingMode.OneWay,
            Converter = new BoolToVisibilityConverter()
        };
        _rootPanel.SetBinding(UIElement.VisibilityProperty, visibilityBinding);

        // Previous Button
        _previousButton = new Button
        {
            Content = "◀ Previous",
            VerticalAlignment = VerticalAlignment.Center,
            MinWidth = 100
        };
        _previousButton.Click += OnPreviousButtonClick;

        var previousEnabledBinding = new Binding
        {
            Source = ViewModel,
            Path = new PropertyPath(nameof(ViewModel.CanGoToPreviousPage)),
            Mode = BindingMode.OneWay
        };
        _previousButton.SetBinding(IsEnabledProperty, previousEnabledBinding);

        // Page Numbers Panel (StackPanel with dynamic buttons)
        _pageNumbersPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 4,
            VerticalAlignment = VerticalAlignment.Center
        };

        // Subscribe to PageNumbers collection changes to rebuild buttons
        ViewModel.PageNumbers.CollectionChanged += OnPageNumbersCollectionChanged;

        // Initial population
        RebuildPageNumberButtons();

        // Next Button
        _nextButton = new Button
        {
            Content = "Next ▶",
            VerticalAlignment = VerticalAlignment.Center,
            MinWidth = 100
        };
        _nextButton.Click += OnNextButtonClick;

        var nextEnabledBinding = new Binding
        {
            Source = ViewModel,
            Path = new PropertyPath(nameof(ViewModel.CanGoToNextPage)),
            Mode = BindingMode.OneWay
        };
        _nextButton.SetBinding(IsEnabledProperty, nextEnabledBinding);

        // Page Info Text
        _pageInfoText = new TextBlock
        {
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(16, 0, 0, 0)
        };

        var pageInfoBinding = new Binding
        {
            Source = ViewModel,
            Path = new PropertyPath(nameof(ViewModel.PageInfo)),
            Mode = BindingMode.OneWay
        };
        _pageInfoText.SetBinding(TextBlock.TextProperty, pageInfoBinding);

        // Add all controls to root panel
        _rootPanel.Children.Add(_previousButton);
        _rootPanel.Children.Add(_pageNumbersPanel);
        _rootPanel.Children.Add(_nextButton);
        _rootPanel.Children.Add(_pageInfoText);

        // Set root panel as content
        Content = _rootPanel;
    }

    private void OnPreviousButtonClick(object sender, RoutedEventArgs e)
    {
        ViewModel.GoToPreviousPage();
    }

    private void OnNextButtonClick(object sender, RoutedEventArgs e)
    {
        ViewModel.GoToNextPage();
    }

    private void OnViewModelPageChanged(object? sender, int newPage)
    {
        // Forward event to subscribers
        PageChanged?.Invoke(this, newPage);
    }

    private void OnPageNumbersCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        // Rebuild page number buttons when collection changes
        RebuildPageNumberButtons();
    }

    private void RebuildPageNumberButtons()
    {
        // Clear existing buttons
        _pageNumbersPanel.Children.Clear();

        // Create button for each page number item
        foreach (var pageItem in ViewModel.PageNumbers)
        {
            if (pageItem.IsEllipsis)
            {
                // Ellipsis - just text, not clickable
                var ellipsisText = new TextBlock
                {
                    Text = "...",
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(4, 0, 4, 0)
                };
                _pageNumbersPanel.Children.Add(ellipsisText);
            }
            else
            {
                // Page number button
                var button = new Button
                {
                    Content = pageItem.PageNumber.ToString(),
                    MinWidth = 40,
                    VerticalAlignment = VerticalAlignment.Center
                };

                // Highlight current page
                if (pageItem.IsCurrent)
                {
                    button.Style = Application.Current.Resources["AccentButtonStyle"] as Style;
                }

                // Capture pageNumber in closure
                var pageNumber = pageItem.PageNumber;
                button.Click += (s, e) => ViewModel.GoToPage(pageNumber);

                _pageNumbersPanel.Children.Add(button);
            }
        }
    }
}
