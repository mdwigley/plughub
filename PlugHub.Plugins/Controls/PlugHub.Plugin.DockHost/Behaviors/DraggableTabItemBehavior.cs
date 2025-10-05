using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Avalonia.Xaml.Interactivity;
using System.Collections;
using System.Diagnostics;

namespace PlugHub.Plugin.DockHost.Behaviors
{
    public class AttachDraggableTabsBehavior : Behavior<TabControl>
    {
        protected override void OnAttached()
        {
            base.OnAttached();
            if (this.AssociatedObject is null) return;

            // Ensure TabControl can accept drops
            DragDrop.SetAllowDrop(this.AssociatedObject, true);

            // Run once after template is applied
            this.AssociatedObject.AttachedToVisualTree += (_, __) =>
                Dispatcher.UIThread.Post(this.AttachToTabs, DispatcherPriority.Background);

            // Also run when layout updates (covers new containers)
            this.AssociatedObject.LayoutUpdated += (_, __) =>
                Dispatcher.UIThread.Post(this.AttachToTabs, DispatcherPriority.Background);

            Debug.WriteLine("[Init] AttachDraggableTabsBehavior attached");
        }

        private void AttachToTabs()
        {
            if (this.AssociatedObject is null) return;

            foreach (TabItem tab in this.AssociatedObject.GetVisualDescendants().OfType<TabItem>())
            {
                BehaviorCollection behaviors = Interaction.GetBehaviors(tab);
                if (!behaviors.OfType<DraggableTabItemBehavior>().Any())
                {
                    behaviors.Add(new DraggableTabItemBehavior());
                    Debug.WriteLine($"[Attach] Added DraggableTabItemBehavior to '{tab.Header}'");
                }
            }
        }
    }

    public class DraggableTabItemBehavior : Behavior<TabItem>
    {
        private Point startPoint;
        private bool pressed;

        protected override void OnAttached()
        {
            base.OnAttached();
            if (this.AssociatedObject is null) return;

            Debug.WriteLine($"[Init] DraggableTabItemBehavior attached to '{this.AssociatedObject.Header}'");

            this.AssociatedObject.AddHandler(InputElement.PointerPressedEvent, this.OnPointerPressed, RoutingStrategies.Tunnel | RoutingStrategies.Bubble);
            this.AssociatedObject.AddHandler(InputElement.PointerMovedEvent, this.OnPointerMoved, RoutingStrategies.Tunnel | RoutingStrategies.Bubble);
            this.AssociatedObject.AddHandler(InputElement.PointerReleasedEvent, this.OnPointerReleased, RoutingStrategies.Tunnel | RoutingStrategies.Bubble);
        }
        protected override void OnDetaching()
        {
            if (this.AssociatedObject is not null)
            {
                this.AssociatedObject.RemoveHandler(InputElement.PointerPressedEvent, (EventHandler<PointerPressedEventArgs>)this.OnPointerPressed);
                this.AssociatedObject.RemoveHandler(InputElement.PointerMovedEvent, (EventHandler<PointerEventArgs>)this.OnPointerMoved);
                this.AssociatedObject.RemoveHandler(InputElement.PointerReleasedEvent, (EventHandler<PointerReleasedEventArgs>)this.OnPointerReleased);
            }
            base.OnDetaching();
        }

        private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            PointerPoint pt = e.GetCurrentPoint(this.AssociatedObject);
            if (!pt.Properties.IsLeftButtonPressed) return;

            this.pressed = true;
            this.startPoint = e.GetPosition(this.AssociatedObject);
            Debug.WriteLine($"[Pressed] '{this.AssociatedObject?.Header}' at {this.startPoint}");
        }
        private async void OnPointerMoved(object? sender, PointerEventArgs e)
        {
            if (!this.pressed) return;

            PointerPoint pt = e.GetCurrentPoint(this.AssociatedObject);
            if (!pt.Properties.IsLeftButtonPressed) return;

            Point pos = e.GetPosition(this.AssociatedObject);
            double dx = Math.Abs(pos.X - this.startPoint.X);
            double dy = Math.Abs(pos.Y - this.startPoint.Y);

            if (dx > 4 || dy > 4)
            {
                this.pressed = false;
                Debug.WriteLine($"[DragStart] '{this.AssociatedObject?.Header}'");

                DataObject data = new();
                data.Set("tab", this.AssociatedObject?.DataContext ?? "[[NULL DATACONTEXT!]]");

                _ = await DragDrop.DoDragDrop(e, data, DragDropEffects.Move);
            }
        }
        private void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
        {
            if (this.pressed)
                Debug.WriteLine($"[Released] '{this.AssociatedObject?.Header}' without drag");
            this.pressed = false;
        }
    }

    public class TabReorderBehavior : Behavior<TabControl>
    {
        protected override void OnAttached()
        {
            base.OnAttached();
            if (this.AssociatedObject is null) return;

            DragDrop.SetAllowDrop(this.AssociatedObject, true);
            this.AssociatedObject.AddHandler(DragDrop.DragOverEvent, this.OnDragOver, RoutingStrategies.Bubble);

            Debug.WriteLine("[Init] TabReorderBehavior attached");
        }

        private void OnDragOver(object? sender, DragEventArgs e)
        {
            if (!e.Data.Contains("tab")) return;

            e.DragEffects = DragDropEffects.Move;
            e.Handled = true;

            TabItem? tab = (e.Source as Visual)?.FindAncestorOfType<TabItem>();
            if (tab == null) return;

            object? source = e.Data.Get("tab");
            object? target = tab.DataContext;
            if (source is null || target is null || ReferenceEquals(source, target)) return;

            if (this.AssociatedObject?.ItemsSource is not IList items) return;

            // Get bounds of the hovered tab
            Point pos = e.GetPosition(tab);
            bool insertAfter = pos.X >= tab.Bounds.Width / 2;

            // Remove the dragged descriptor
            items.Remove(source);

            // Find the target’s current index
            int targetIndex = items.IndexOf(target);

            // Insert before or after the target descriptor
            int insertIndex = insertAfter ? targetIndex + 1 : targetIndex;
            if (insertIndex < 0) insertIndex = 0;
            if (insertIndex > items.Count) insertIndex = items.Count;

            items.Insert(insertIndex, source);
            this.AssociatedObject.SelectedItem = source;

            Debug.WriteLine($"[Reorder] Inserted {source} {(insertAfter ? "after" : "before")} {target}");
        }
    }
}