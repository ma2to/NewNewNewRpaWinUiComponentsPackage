using Microsoft.UI;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.Extensions.Logging;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.UIControls;

/// <summary>
/// Custom control for column resize grip that supports cursor change.
/// Inherits from Control to access protected ProtectedCursor property.
/// Displays as a vertical bar that users can drag to resize columns.
/// </summary>
internal sealed class ResizeGripControl : Control
{
    // SENIOR FIX: Create cursors once as static fields to avoid recreation overhead
    // and ensure cursors are available for all instances
    private static readonly InputCursor? _resizeCursor;
    private static readonly InputCursor? _arrowCursor;
    private static ILogger<ResizeGripControl>? _logger;

    public static void SetLogger(ILogger<ResizeGripControl>? logger)
    {
        _logger = logger;
    }

    static ResizeGripControl()
    {
        // Initialize cursors once for all instances
        try
        {
            _resizeCursor = InputSystemCursor.Create(InputSystemCursorShape.SizeWestEast);
            _arrowCursor = InputSystemCursor.Create(InputSystemCursorShape.Arrow);

            _logger?.LogTrace("ResizeGrip cursors created successfully");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "ResizeGrip cursor creation failed: {Message}", ex.Message);
            // Cursors not supported on this platform - control will work without cursor change
        }
    }

    /// <summary>
    /// Creates a new resize grip control with resize cursor.
    /// Sets the cursor to SizeWestEast (horizontal resize arrows).
    /// </summary>
    public ResizeGripControl()
    {
        _logger?.LogTrace("ResizeGripControl: Constructor called - creating new resize grip instance");

        // Set initial resize cursor
        if (_resizeCursor != null)
        {
            this.ProtectedCursor = _resizeCursor;
            _logger?.LogTrace("ResizeGripControl: Resize cursor set successfully");
        }
        else
        {
            _logger?.LogWarning("ResizeGripControl: Resize cursor is NULL - cursor won't change");
        }

        // CRITICAL FIX: Enable interaction for manipulation events to work
        this.IsHitTestVisible = true;
        this.IsTapEnabled = true;
        this.IsDoubleTapEnabled = false;
        this.IsRightTapEnabled = false;
        this.IsHoldingEnabled = false;

        _logger?.LogTrace("ResizeGripControl: IsHitTestVisible={IsHitTestVisible}, IsTapEnabled={IsTapEnabled}",
            this.IsHitTestVisible, this.IsTapEnabled);

        // Set default appearance
        // CRITICAL: Width must be wide enough for easy grabbing, Background must be non-null for hit testing
        // FIX: Increased to 8px width for easier grabbing and better visibility
        this.Width = 8;
        this.MinWidth = 8;
        this.Background = new SolidColorBrush(Microsoft.UI.Colors.LightGray) { Opacity = 0.4 };
        // SENIOR FIX: Removed ManipulationMode - using PointerEvents in HeadersRowView instead

        _logger?.LogTrace("ResizeGripControl: Width={Width}, Background={HasBackground}",
            this.Width, this.Background != null);

        // Make it stretch vertically
        this.VerticalAlignment = VerticalAlignment.Stretch;
        this.HorizontalAlignment = HorizontalAlignment.Left;

        // SENIOR FIX: Set cursor on multiple pointer events to ensure it persists
        // Some WinUI 3 versions need cursor set on both Enter and Pressed events
        this.PointerEntered += (s, e) =>
        {
            _logger?.LogTrace("ResizeGripControl: PointerEntered - cursor hovering over resize grip");
            if (_resizeCursor != null)
            {
                this.ProtectedCursor = _resizeCursor;
            }
            // SENIOR FIX: Increased hover opacity for better visibility (grip is now 4px)
            this.Background = new SolidColorBrush(Microsoft.UI.Colors.Blue) { Opacity = 0.7 };
            _logger?.LogTrace("ResizeGripControl: Background changed to Blue (hover state)");
        };

        this.PointerPressed += (s, e) =>
        {
            _logger?.LogTrace("ResizeGripControl: PointerPressed - user clicked resize grip!");
            // CRITICAL FIX: Reinforce cursor during press to prevent override by parent elements
            if (_resizeCursor != null)
            {
                this.ProtectedCursor = _resizeCursor;
            }
        };

        this.PointerExited += (s, e) =>
        {
            _logger?.LogTrace("ResizeGripControl: PointerExited - cursor left resize grip");
            // Reset to default cursor (use pre-created static instance)
            if (_arrowCursor != null)
            {
                this.ProtectedCursor = _arrowCursor;
            }
            // SENIOR FIX: Slightly higher opacity for better default visibility
            this.Background = new SolidColorBrush(Microsoft.UI.Colors.LightGray) { Opacity = 0.4 };
            _logger?.LogTrace("ResizeGripControl: Background changed to LightGray (default state)");
        };

        _logger?.LogTrace("ResizeGripControl: Constructor completed - all event handlers attached");
    }
}
