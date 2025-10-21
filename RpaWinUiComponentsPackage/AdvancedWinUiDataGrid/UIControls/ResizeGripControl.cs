using Microsoft.UI;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.UIControls;

/// <summary>
/// Custom control for column resize grip that supports cursor change.
/// Inherits from Control to access protected ProtectedCursor property.
/// Displays as a vertical bar that users can drag to resize columns.
/// </summary>
internal sealed class ResizeGripControl : Control
{
    private InputCursor? _resizeCursor;

    /// <summary>
    /// Creates a new resize grip control with resize cursor.
    /// Sets the cursor to SizeWestEast (horizontal resize arrows).
    /// </summary>
    public ResizeGripControl()
    {
        // Set resize cursor (horizontal arrows <->)
        // This is only possible from inside a Control-derived class
        // because ProtectedCursor is a protected property in UIElement
        try
        {
            _resizeCursor = InputSystemCursor.Create(InputSystemCursorShape.SizeWestEast);
            this.ProtectedCursor = _resizeCursor;
        }
        catch
        {
            // Fallback if cursor creation fails - control still works without cursor change
        }

        // CRITICAL FIX: Enable interaction for manipulation events to work
        this.IsHitTestVisible = true;
        this.IsTapEnabled = true;
        this.IsDoubleTapEnabled = false;
        this.IsRightTapEnabled = false;
        this.IsHoldingEnabled = false;

        // Set default appearance
        // CRITICAL: Width must be wide enough for easy grabbing, Background must be non-null for hit testing
        // SENIOR FIX: Changed to 4px (standard resize grip width) - 16px was too wide and intrusive
        this.Width = 4;
        this.MinWidth = 4;
        this.Background = new SolidColorBrush(Microsoft.UI.Colors.LightGray) { Opacity = 0.4 };
        // SENIOR FIX: Removed ManipulationMode - using PointerEvents in HeadersRowView instead

        // Make it stretch vertically
        this.VerticalAlignment = VerticalAlignment.Stretch;
        this.HorizontalAlignment = HorizontalAlignment.Left;

        // CRITICAL FIX: Set cursor on pointer entered/exited to ensure it works
        // Also change background on hover for better visibility
        this.PointerEntered += (s, e) =>
        {
            if (_resizeCursor != null)
            {
                this.ProtectedCursor = _resizeCursor;
            }
            // SENIOR FIX: Increased hover opacity for better visibility (grip is now 4px)
            this.Background = new SolidColorBrush(Microsoft.UI.Colors.Blue) { Opacity = 0.7 };
        };

        this.PointerExited += (s, e) =>
        {
            // Reset to default cursor
            this.ProtectedCursor = InputSystemCursor.Create(InputSystemCursorShape.Arrow);
            // SENIOR FIX: Slightly higher opacity for better default visibility
            this.Background = new SolidColorBrush(Microsoft.UI.Colors.LightGray) { Opacity = 0.4 };
        };
    }
}
