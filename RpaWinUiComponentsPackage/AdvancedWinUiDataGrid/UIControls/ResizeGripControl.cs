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
            this.ProtectedCursor = InputSystemCursor.Create(InputSystemCursorShape.SizeWestEast);
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
        // CRITICAL: Width must be >= 8 for easy grabbing, Background must be non-null for hit testing
        this.Width = 8;
        this.MinWidth = 8;
        this.Background = new SolidColorBrush(Colors.DarkGray);
        this.ManipulationMode = ManipulationModes.TranslateX;

        // Make it stretch vertically
        this.VerticalAlignment = VerticalAlignment.Stretch;
        this.HorizontalAlignment = HorizontalAlignment.Left;
    }
}
