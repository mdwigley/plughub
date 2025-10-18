using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.VisualTree;
using PlugHub.Plugin.Controls.Interfaces.Controls;
using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace PlugHub.Plugin.Controls.Controls
{
    public class ActiveContentChangedEventArgs(RoutedEvent routedEvent, object? oldItem, object? newItem)
        : RoutedEventArgs(routedEvent)
    {
        public object? OldItem { get; } = oldItem;
        public object? NewItem { get; } = newItem;
    }
    public class ItemsReorderedEventArgs(RoutedEvent routedEvent, ISwitchable item, int oldIndex, int newIndex)
        : RoutedEventArgs(routedEvent)
    {
        public ISwitchable Item { get; } = item;
        public int OldIndex { get; } = oldIndex;
        public int NewIndex { get; } = newIndex;
    }

    public class ContentDeck : ItemsControl
    {
        private EventHandler<PointerPressedEventArgs>? defocusHandler;

        private Point? clickPosition;
        private bool isMoving;
        private RotationView? pressedRot;
        private ISwitchable? pressedState;
        private ISwitchable? lastTargetState;
        private Rect? lastTargetBounds;
        private TopLevel? top;
        private Control? activeSource;
        private ISwitchable? currentState;
        private int lastReorderOldIndex = -1;
        private int lastReorderNewIndex = -1;


        public static readonly RoutedEvent<RoutedEventArgs> OpenedEvent =
            RoutedEvent.Register<ContentDeck, RoutedEventArgs>(nameof(Opened), RoutingStrategies.Bubble);
        public event EventHandler<RoutedEventArgs>? Opened
        {
            add => this.AddHandler(OpenedEvent, value);
            remove => this.RemoveHandler(OpenedEvent, value);
        }

        public static readonly RoutedEvent<RoutedEventArgs> ClosedEvent =
            RoutedEvent.Register<ContentDeck, RoutedEventArgs>(nameof(Closed), RoutingStrategies.Bubble);
        public event EventHandler<RoutedEventArgs>? Closed
        {
            add => this.AddHandler(ClosedEvent, value);
            remove => this.RemoveHandler(ClosedEvent, value);
        }

        public static readonly RoutedEvent<ActiveContentChangedEventArgs> ActiveContentChangedEvent =
            RoutedEvent.Register<ContentDeck, ActiveContentChangedEventArgs>(nameof(ActiveContentChanged), RoutingStrategies.Bubble);
        public event EventHandler<ActiveContentChangedEventArgs>? ActiveContentChanged
        {
            add => this.AddHandler(ActiveContentChangedEvent, value);
            remove => this.RemoveHandler(ActiveContentChangedEvent, value);
        }

        public static readonly RoutedEvent<ItemsReorderedEventArgs> ItemsReorderedEvent =
            RoutedEvent.Register<ContentDeck, ItemsReorderedEventArgs>(nameof(ItemsReordered), RoutingStrategies.Bubble);
        public event EventHandler<ItemsReorderedEventArgs>? ItemsReordered
        {
            add => this.AddHandler(ItemsReorderedEvent, value);
            remove => this.RemoveHandler(ItemsReorderedEvent, value);
        }

        #region DockGutter: Style Properties

        public static readonly StyledProperty<Dock> DockEdgeProperty =
            AvaloniaProperty.Register<ContentDeck, Dock>(nameof(DockEdge), Dock.Left);
        public Dock DockEdge
        {
            get => this.GetValue(DockEdgeProperty);
            set => this.SetValue(DockEdgeProperty, value);
        }

        public static readonly StyledProperty<IDataTemplate?> ContentItemTemplateProperty =
            AvaloniaProperty.Register<ContentDeck, IDataTemplate?>(nameof(ContentItemTemplate));
        public IDataTemplate? ContentItemTemplate
        {
            get => this.GetValue(ContentItemTemplateProperty);
            set => this.SetValue(ContentItemTemplateProperty, value);
        }

        #endregion

        #region DockGutter: Direct Properties

        public static readonly DirectProperty<ContentDeck, bool> IsOpenProperty =
            AvaloniaProperty.RegisterDirect<ContentDeck, bool>(nameof(IsOpen), o => o.IsOpen, (o, v) => o.IsOpen = v);
        private bool isOpen;
        public bool IsOpen
        {
            get => this.isOpen;
            set => this.SetAndRaise(IsOpenProperty, ref this.isOpen, value);
        }

        public static readonly DirectProperty<ContentDeck, bool> HasItemsProperty =
            AvaloniaProperty.RegisterDirect<ContentDeck, bool>(nameof(HasItems), o => o.HasItems);
        private bool hasItems;
        public bool HasItems
        {
            get => this.hasItems;
            private set => this.SetAndRaise(HasItemsProperty, ref this.hasItems, value);
        }

        public static readonly DirectProperty<ContentDeck, Orientation> OrientationProperty =
            AvaloniaProperty.RegisterDirect<ContentDeck, Orientation>(nameof(Orientation), o => o.Orientation);
        private Orientation orientation;
        public Orientation Orientation
        {
            get => this.orientation;
            private set => this.SetAndRaise(OrientationProperty, ref this.orientation, value);
        }

        public static readonly DirectProperty<ContentDeck, double> RotationProperty =
            AvaloniaProperty.RegisterDirect<ContentDeck, double>(nameof(Rotation), o => o.Rotation, (o, v) => o.Rotation = v, 0);
        private double rotation;
        public double Rotation
        {
            get => this.rotation;
            private set => this.SetAndRaise(RotationProperty, ref this.rotation, value);
        }

        public static readonly DirectProperty<ContentDeck, object?> ActiveContentProperty =
            AvaloniaProperty.RegisterDirect<ContentDeck, object?>(nameof(ActiveContent), o => o.ActiveContent, (o, v) => o.ActiveContent = v);
        private object? activeContent;
        public object? ActiveContent
        {
            get => this.activeContent;
            set => this.SetAndRaise(ActiveContentProperty, ref this.activeContent, value);
        }

        #endregion

        public ContentDeck()
        {
            IsOpenProperty.Changed.AddClassHandler<ContentDeck>((x, e) =>
            {
                if ((bool)e.NewValue!)
                    x.RaiseEvent(new RoutedEventArgs(OpenedEvent));
                else
                    x.RaiseEvent(new RoutedEventArgs(ClosedEvent));
            });

            ActiveContentProperty.Changed.AddClassHandler<ContentDeck>((x, e) =>
            {
                x.RaiseEvent(new ActiveContentChangedEventArgs(ActiveContentChangedEvent, e.OldValue, e.NewValue));
            });

            DockEdgeProperty.Changed.AddClassHandler<ContentDeck>((x, e) => x.UpdateOrientation());

            ItemsSourceProperty.Changed.AddClassHandler<ContentDeck>((x, e) =>
            {
                if (e.OldValue is ObservableCollection<ISwitchable> oldColl)
                    oldColl.CollectionChanged -= x.CollectionChanged;

                if (e.NewValue is ObservableCollection<ISwitchable> newColl)
                    newColl.CollectionChanged += x.CollectionChanged;

                x.UpdateContent();
            });

            this.Items.CollectionChanged += this.CollectionChanged;
        }

        #region DockGutter: Internal Upkeep

        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            base.OnApplyTemplate(e);

            this.UpdateOrientation();
            this.UpdateContent();

            this.HookClicks();
            this.HookDefocusClose();

            ScrollViewer? scroller = e.NameScope.Find<ScrollViewer>("PART_ContentItemScroller");

            scroller?.AddHandler(PointerWheelChangedEvent, this.OnPointerWheelChanged, RoutingStrategies.Tunnel);
        }

        protected virtual void UpdateOrientation()
        {
            bool isLeftOrRight = this.DockEdge == Dock.Left || this.DockEdge == Dock.Right;

            this.Rotation = isLeftOrRight ? 90 : 0;
            this.Orientation = isLeftOrRight
                ? Orientation.Vertical
                : Orientation.Horizontal;

            this.PseudoClasses.Set(":edge-left", this.DockEdge == Dock.Left);
            this.PseudoClasses.Set(":edge-right", this.DockEdge == Dock.Right);
            this.PseudoClasses.Set(":edge-top", this.DockEdge == Dock.Top);
            this.PseudoClasses.Set(":edge-bottom", this.DockEdge == Dock.Bottom);
        }
        protected virtual void UpdateContent()
        {
            this.HasItems = this.ItemsSource != null && this.Items.Count > 0;

            this.IsVisible = this.HasItems;
        }

        private void HookClicks()
        {
            this.RemoveHandler(PointerPressedEvent, this.OnItemPressed);
            this.RemoveHandler(PointerMovedEvent, this.OnItemMoved);
            this.RemoveHandler(PointerReleasedEvent, this.OnItemReleased);

            this.AddHandler(PointerPressedEvent, this.OnItemPressed, RoutingStrategies.Tunnel | RoutingStrategies.Bubble);
            this.AddHandler(PointerMovedEvent, this.OnItemMoved, RoutingStrategies.Tunnel | RoutingStrategies.Bubble);
            this.AddHandler(PointerReleasedEvent, this.OnItemReleased, RoutingStrategies.Tunnel | RoutingStrategies.Bubble);
        }
        private void HookDefocusClose()
        {
            TopLevel? topLevel = TopLevel.GetTopLevel(this);

            if (topLevel == null) return;

            if (this.top != null && this.defocusHandler != null)
                this.top.RemoveHandler(PointerPressedEvent, this.defocusHandler);

            this.top = topLevel;

            this.defocusHandler = (s, ev) =>
            {
                if (!this.IsOpen)
                    return;

                if (ev.Source is Visual v)
                {
                    if (this.activeSource is Visual btn && (ReferenceEquals(v, btn) || btn.IsVisualAncestorOf(v)))
                        return;

                    if (this.ActiveContent is Visual content)
                    {
                        ResizeBox? host = content.FindAncestorOfType<ResizeBox>(includeSelf: true);
                        if (host != null && (ReferenceEquals(v, host) || host.IsVisualAncestorOf(v)))
                            return;
                    }

                    if (this.IsVisualAncestorOf(v))
                        return;
                }

                this.ClosePanel();
            };

            this.top.AddHandler(PointerPressedEvent, this.defocusHandler, RoutingStrategies.Tunnel);
        }

        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);

            this.HookDefocusClose();
        }
        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            if (this.top != null && this.defocusHandler != null)
                this.top.RemoveHandler(PointerPressedEvent, this.defocusHandler);

            this.defocusHandler = null;
            this.top = null;

            base.OnDetachedFromVisualTree(e);
        }
        private void OnPointerWheelChanged(object? sender, PointerWheelEventArgs e)
        {
            if (sender is ScrollViewer sv)
            {
                if (this.Orientation == Orientation.Horizontal)
                {
                    double delta = e.Delta.Y;

                    sv.Offset = new Vector(
                        Math.Max(0, sv.Offset.X + delta * -40),
                        sv.Offset.Y);

                    e.Handled = true;
                }
            }
        }

        #endregion

        #region DockGutter: Gutter Item Reorder

        private void OnItemPressed(object? sender, PointerPressedEventArgs e)
        {
            this.pressedRot = null;
            this.pressedState = null;

            if (e.Source is Visual v)
            {
                RotationView? rot = v.FindAncestorOfType<RotationView>(includeSelf: true);
                if (rot?.DataContext is ISwitchable d)
                {
                    this.pressedRot = rot;
                    this.pressedState = d;
                }
            }

            this.clickPosition = e.GetPosition(this);
            this.isMoving = false;
        }
        private void OnItemMoved(object? sender, PointerEventArgs e)
        {
            if (this.clickPosition == null || this.pressedRot == null || this.pressedState == null)
                return;

            Point current = e.GetPosition(this);
            double dx = current.X - this.clickPosition.Value.X;
            double dy = current.Y - this.clickPosition.Value.Y;
            double distSq = dx * dx + dy * dy;

            bool beyondThreshold = distSq > 4 * 4;

            if (!beyondThreshold)
                return;

            if (!this.isMoving)
            {
                this.Cursor = new Cursor(StandardCursorType.DragMove);
                e.Pointer.Capture(this);
            }

            this.isMoving = true;
            this.OnItemReorder(this.pressedState, current);
        }
        private void OnItemReorder(ISwitchable pressedState, Point currentPos)
        {
            if (this.ItemsSource == null)
                return;

            int currentIndex = this.Items.IndexOf(pressedState);
            if (currentIndex < 0)
                return;

            ISwitchable? targetState = null;
            Rect? targetBounds = null;

            for (int i = 0; i < this.Items.Count; i++)
            {
                if (this.ContainerFromIndex(i) is Control container)
                {
                    Point? topLeft = container.TranslatePoint(new Point(0, 0), this);
                    if (topLeft == null) continue;

                    Rect rect = new(topLeft.Value, container.Bounds.Size);

                    if (this.Orientation == Orientation.Vertical)
                    {
                        if (currentPos.Y >= rect.Top && currentPos.Y <= rect.Bottom)
                        {
                            targetState = this.Items[i] as ISwitchable;
                            targetBounds = rect;
                            break;
                        }
                    }
                    else
                    {
                        if (currentPos.X >= rect.Left && currentPos.X <= rect.Right)
                        {
                            targetState = this.Items[i] as ISwitchable;
                            targetBounds = rect;
                            break;
                        }
                    }
                }
            }

            if (this.lastTargetState != null && this.lastTargetBounds.HasValue &&
                this.lastTargetBounds.Value.Contains(currentPos))
            {
                return;
            }

            if (this.lastTargetState != null && this.lastTargetBounds.HasValue &&
                !this.lastTargetBounds.Value.Contains(currentPos))
            {
                this.lastTargetState = null;
                this.lastTargetBounds = null;
            }

            if (targetState == null || ReferenceEquals(targetState, pressedState))
                return;

            if (!ReferenceEquals(targetState, this.lastTargetState))
            {
                int targetIndex = this.Items.IndexOf(targetState);
                int currentIndexNow = this.Items.IndexOf(pressedState);

                if (targetIndex >= 0 && targetIndex != currentIndexNow)
                {
                    if (this.ItemsSource is IList list)
                    {
                        list.RemoveAt(currentIndexNow);
                        list.Insert(targetIndex, pressedState);

                        this.lastReorderOldIndex = currentIndexNow;
                        this.lastReorderNewIndex = targetIndex;
                    }
                }

                this.lastTargetState = targetState;
                this.lastTargetBounds = targetBounds;
            }
        }
        private void OnItemReleased(object? sender, RoutedEventArgs e)
        {
            if (this.pressedState != null && this.pressedRot != null)
            {
                if (!this.isMoving)
                    this.TogglePanel(this.pressedState, this.pressedRot);
                else
                    this.RaiseEvent(
                        new ItemsReorderedEventArgs(
                            ItemsReorderedEvent,
                            this.pressedState!,
                            this.lastReorderOldIndex,
                            this.lastReorderNewIndex));

                e.Handled = true;
            }

            this.clickPosition = null;
            this.isMoving = false;
            this.pressedRot = null;
            this.pressedState = null;
            this.Cursor = new Cursor(StandardCursorType.Arrow);
            this.lastReorderOldIndex = -1;
            this.lastReorderNewIndex = -1;
        }

        #endregion

        #region DockGutter: Panel Controlers

        protected virtual void TogglePanel(ISwitchable state, Control sourceControl)
        {
            ISwitchable? oldState = this.currentState;

            if (this.currentState == state)
            {
                this.ClosePanel();
                this.RaiseEvent(new ActiveContentChangedEventArgs(ActiveContentChangedEvent, oldState, null));
            }
            else
            {
                this.OpenPanel(state, sourceControl);
                this.RaiseEvent(new ActiveContentChangedEventArgs(ActiveContentChangedEvent, oldState, this.currentState));
            }
        }
        protected virtual void OpenPanel(ISwitchable state, Control sourceControl)
        {
            this.activeSource = sourceControl;
            this.currentState = state;

            this.IsOpen = true;
            this.ActiveContent = state.Content;

            this.RaiseEvent(new RoutedEventArgs(OpenedEvent, this));
        }

        protected virtual void ClosePanel()
        {
            if (this.currentState != null)
            {
                this.ActiveContent = null;
                this.IsOpen = false;

                this.currentState = null;
                this.activeSource = null;

                this.RaiseEvent(new RoutedEventArgs(ClosedEvent, this));
            }
        }

        protected virtual void CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            this.UpdateContent();

            if (e.Action == NotifyCollectionChangedAction.Remove ||
                e.Action == NotifyCollectionChangedAction.Replace ||
                e.Action == NotifyCollectionChangedAction.Reset)
            {
                if (this.currentState != null && !this.Items.Contains(this.currentState))
                    this.ClosePanel();
            }
        }

        #endregion
    }
}