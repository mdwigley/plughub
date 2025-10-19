using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Avalonia.Xaml.Interactivity;
using PlugHub.Plugin.DockHost.Models;
using System.Collections;

namespace PlugHub.Plugin.DockHost.Behaviors
{
    public class AttachDraggableTabsBehavior : Behavior<TabControl>
    {
        protected override void OnAttached()
        {
            base.OnAttached();
            if (this.AssociatedObject == null) return;

            DragDrop.SetAllowDrop(this.AssociatedObject, true);

            this.AssociatedObject.AttachedToVisualTree += (_, __) =>
                Dispatcher.UIThread.Post(this.AttachToTabs, DispatcherPriority.Background);

            this.AssociatedObject.LayoutUpdated += (_, __) =>
                Dispatcher.UIThread.Post(this.AttachToTabs, DispatcherPriority.Background);
        }
        private void AttachToTabs()
        {
            if (this.AssociatedObject == null) return;

            foreach (TabItem tab in this.AssociatedObject.GetVisualDescendants().OfType<TabItem>())
            {
                BehaviorCollection behaviors = Interaction.GetBehaviors(tab);

                if (!behaviors.OfType<DraggableTabItemBehavior>().Any())
                    behaviors.Add(new DraggableTabItemBehavior());
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

            if (this.AssociatedObject == null) return;

            this.AssociatedObject.AddHandler(InputElement.PointerPressedEvent, this.OnPointerPressed, RoutingStrategies.Tunnel | RoutingStrategies.Bubble);
            this.AssociatedObject.AddHandler(InputElement.PointerMovedEvent, this.OnPointerMoved, RoutingStrategies.Tunnel | RoutingStrategies.Bubble);
            this.AssociatedObject.AddHandler(InputElement.PointerReleasedEvent, this.OnPointerReleased, RoutingStrategies.Tunnel | RoutingStrategies.Bubble);
        }
        protected override void OnDetaching()
        {
            if (this.AssociatedObject != null)
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

                DataObject data = new();
                data.Set("tab", this.AssociatedObject?.DataContext ?? "[[NULL DATACONTEXT!]]");

                _ = await DragDrop.DoDragDrop(e, data, DragDropEffects.Move);
            }
        }
        private void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
        {
            this.pressed = false;
        }
    }

    public class TabsReorderedEventArgs(DockItemState item) : EventArgs
    {
        public DockItemState Item { get; } = item;
    }

    public class TabReorderBehavior : Behavior<TabControl>
    {
        public event EventHandler<TabsReorderedEventArgs>? TabsReordered;

        private DockItemState? movedItem;

        protected override void OnAttached()
        {
            base.OnAttached();
            if (this.AssociatedObject == null) return;

            DragDrop.SetAllowDrop(this.AssociatedObject, true);
            this.AssociatedObject.AddHandler(DragDrop.DragOverEvent, this.OnDragOver, RoutingStrategies.Bubble);
            this.AssociatedObject.AddHandler(DragDrop.DropEvent, this.OnDrop, RoutingStrategies.Bubble);
        }

        private void OnDragOver(object? sender, DragEventArgs e)
        {
            if (!e.Data.Contains("tab")) return;

            e.DragEffects = DragDropEffects.Move;
            e.Handled = true;

            TabItem? tab = (e.Source as Visual)?.FindAncestorOfType<TabItem>();

            if (tab == null) return;

            object? target = tab.DataContext;

            if (e.Data.Get("tab") is not DockItemState source || target == null || ReferenceEquals(source, target)) return;

            if (this.AssociatedObject?.ItemsSource is not IList items) return;

            Point pos = e.GetPosition(tab);

            bool insertAfter = pos.X >= tab.Bounds.Width / 2;

            items.Remove(source);

            int targetIndex = items.IndexOf(target);
            int insertIndex = insertAfter ? targetIndex + 1 : targetIndex;

            if (insertIndex < 0) insertIndex = 0;
            if (insertIndex > items.Count) insertIndex = items.Count;

            items.Insert(insertIndex, source);

            this.AssociatedObject.SelectedItem = source;

            this.movedItem = source;
        }
        private void OnDrop(object? sender, DragEventArgs e)
        {
            if (this.movedItem != null)
                TabsReordered?.Invoke(this, new TabsReorderedEventArgs(this.movedItem));

            this.movedItem = null;
        }
    }
}