using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.VisualTree;

namespace PlugHub.Plugin.DockHost.Utilities
{
    public static class AbortableClickHandler
    {
        public static void Attach(Control control, Action<PointerReleasedEventArgs> onClick)
        {
            Control? pressed = null;

            control.PointerPressed += (s, e) =>
            {
                if (e.Source is Control c)
                {
                    pressed = c;
                    e.Pointer.Capture(pressed);
                    e.Handled = true;
                }
            };

            control.PointerReleased += (s, e) =>
            {
                if (pressed == null)
                    return;

                Visual? sourceVisual = e.Source as Visual;
                Point pt = e.GetPosition(pressed);

                bool hasSource = sourceVisual != null;
                bool releasedOn = ReferenceEquals(sourceVisual, pressed);
                bool releasedInside = sourceVisual?.GetVisualAncestors().Any(a => ReferenceEquals(a, pressed)) == true;
                bool insideBounds = pressed.Bounds.Contains(pt);

                bool validClick = hasSource && (releasedOn || releasedInside) && insideBounds;

                e.Pointer.Capture(null);

                Control released = pressed;
                pressed = null;

                if (validClick)
                    onClick(e);
                else
                    e.Handled = true;
            };
        }
    }
}