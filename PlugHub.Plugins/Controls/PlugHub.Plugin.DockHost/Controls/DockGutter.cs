using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.VisualTree;
using PlugHub.Plugin.DockHost.Models;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;

namespace PlugHub.Plugin.DockHost.Controls
{
    public class FlyoutExtendedEventArgs(DockPanelState state, Control control) : EventArgs
    {
        public DockPanelState State { get; } = state;
        public Control Control { get; } = control;
    }
    public class PanelsReorderedEventArgs(DockPanelState state) : EventArgs
    {
        public DockPanelState State = state;
    }


    public class DockGutter : ItemsControl
    {
        public event EventHandler<FlyoutExtendedEventArgs>? FlyoutExpanded;
        public event EventHandler<PanelsReorderedEventArgs>? PanelsReordered;

        private EventHandler<PointerPressedEventArgs>? defocusHandler;

        private Point? clickPosition;
        private bool isMoving;
        private RotatableContent? pressedRot;
        private DockPanelState? pressedState;
        private DockPanelState? lastTargetState;
        private Rect? lastTargetBounds;
        private TopLevel? top;
        private Control? currentContent;
        private DockPanelState? currentState;

        public DockPanelState? CurrentState
        {
            get => this.currentState;
            protected set
            {
                if (this.currentState == value)
                    return;

                if (this.currentState != null)
                    this.currentState.PropertyChanged -= this.StatePropertyChanged;

                this.currentState = value;

                if (this.currentState != null)
                    this.currentState.PropertyChanged += this.StatePropertyChanged;
            }
        }

        #region DockGutter: Style Properties

        public static readonly StyledProperty<double> PanelSizeProperty =
            AvaloniaProperty.Register<DockGutter, double>(nameof(PanelSize), 400);
        public double PanelSize
        {
            get => this.GetValue(PanelSizeProperty);
            set => this.SetValue(PanelSizeProperty, value);
        }

        public static readonly StyledProperty<Dock> DockEdgeProperty =
            AvaloniaProperty.Register<DockGutter, Dock>(nameof(DockEdge), Dock.Left);
        public Dock DockEdge
        {
            get => this.GetValue(DockEdgeProperty);
            set => this.SetValue(DockEdgeProperty, value);
        }

        public static readonly StyledProperty<IDataTemplate?> GutterItemTemplateProperty =
            AvaloniaProperty.Register<DockGutter, IDataTemplate?>(nameof(GutterItemTemplate));
        public IDataTemplate? GutterItemTemplate
        {
            get => this.GetValue(GutterItemTemplateProperty);
            set => this.SetValue(GutterItemTemplateProperty, value);
        }

        #endregion

        #region DockGutter: Direct Properties

        public static readonly DirectProperty<DockGutter, bool> IsExpandedProperty =
            AvaloniaProperty.RegisterDirect<DockGutter, bool>(nameof(IsExpanded), o => o.IsExpanded, (o, v) => o.IsExpanded = v);
        private bool isExpanded;
        public bool IsExpanded
        {
            get => this.isExpanded;
            set => this.SetAndRaise(IsExpandedProperty, ref this.isExpanded, value);
        }

        public static readonly DirectProperty<DockGutter, bool> HasContentProperty =
            AvaloniaProperty.RegisterDirect<DockGutter, bool>(nameof(HasContent), o => o.HasContent);
        private bool hasContent;
        public bool HasContent
        {
            get => this.hasContent;
            private set => this.SetAndRaise(HasContentProperty, ref this.hasContent, value);
        }

        public static readonly DirectProperty<DockGutter, Orientation> ItemOrientationProperty =
            AvaloniaProperty.RegisterDirect<DockGutter, Orientation>(nameof(ItemOrientation), o => o.ItemOrientation);
        private Orientation itemOrientation;
        public Orientation ItemOrientation
        {
            get => this.itemOrientation;
            private set => this.SetAndRaise(ItemOrientationProperty, ref this.itemOrientation, value);
        }

        public static readonly DirectProperty<DockGutter, double> GutterItemRotationProperty =
            AvaloniaProperty.RegisterDirect<DockGutter, double>(nameof(GutterItemRotation), o => o.GutterItemRotation, (o, v) => o.GutterItemRotation = v, 0);
        private double gutterItemRotation;
        public double GutterItemRotation
        {
            get => this.gutterItemRotation;
            private set => this.SetAndRaise(GutterItemRotationProperty, ref this.gutterItemRotation, value);
        }

        public static readonly DirectProperty<DockGutter, Thickness> AccentBorderThicknessProperty =
            AvaloniaProperty.RegisterDirect<DockGutter, Thickness>(nameof(AccentBorderThickness), o => o.AccentBorderThickness);
        private Thickness accentBorderThickness;
        public Thickness AccentBorderThickness
        {
            get => this.accentBorderThickness;
            private set => this.SetAndRaise(AccentBorderThicknessProperty, ref this.accentBorderThickness, value);
        }

        public static readonly DirectProperty<DockGutter, object?> PanelContentProperty =
            AvaloniaProperty.RegisterDirect<DockGutter, object?>(nameof(PanelContent), o => o.PanelContent, (o, v) => o.PanelContent = v);
        private object? panelContent;
        public object? PanelContent
        {
            get => this.panelContent;
            set => this.SetAndRaise(PanelContentProperty, ref this.panelContent, value);
        }

        #endregion

        public DockGutter()
        {
            DockEdgeProperty.Changed.AddClassHandler<DockGutter>((x, e) => x.UpdateGutterItemOrientation());

            ItemsSourceProperty.Changed.AddClassHandler<DockGutter>((x, e) =>
            {
                if (e.OldValue is ObservableCollection<DockPanelState> oldColl)
                    oldColl.CollectionChanged -= x.CollectionChanged;

                if (e.NewValue is ObservableCollection<DockPanelState> newColl)
                    newColl.CollectionChanged += x.CollectionChanged;

                x.UpdateGutterItemContent();
            });

            this.Items.CollectionChanged += this.CollectionChanged;
        }

        #region DockGutter: Internal Upkeep

        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            base.OnApplyTemplate(e);

            this.UpdateGutterItemOrientation();
            this.UpdateGutterItemContent();
            this.UpdateAccent();

            this.HookGutterItemClicks();
            this.HookDefocusClose();

            ScrollViewer? scroller = e.NameScope.Find<ScrollViewer>("PART_GutterScroller");

            scroller?.AddHandler(InputElement.PointerWheelChangedEvent, this.OnPointerWheelChanged, RoutingStrategies.Tunnel);
        }

        protected virtual void UpdateGutterItemOrientation()
        {
            this.GutterItemRotation = this.DockEdge is Dock.Left or Dock.Right ? 90 : 0;
            this.ItemOrientation = this.DockEdge is Dock.Left or Dock.Right
                ? Orientation.Vertical
                : Orientation.Horizontal;
        }
        protected virtual void UpdateGutterItemContent()
        {
            this.HasContent = this.ItemsSource != null && this.Items.Count > 0;

            this.IsVisible = this.HasContent;
        }
        protected virtual void UpdateAccent()
        {
            this.AccentBorderThickness = this.DockEdge switch
            {
                Dock.Left => new Thickness(0, 0, 0, 4),
                Dock.Right => new Thickness(0, 4, 0, 0),
                Dock.Top => new Thickness(0, 4, 0, 0),
                Dock.Bottom => new Thickness(0, 0, 0, 4),
                _ => default
            };
        }

        private void HookGutterItemClicks()
        {
            this.RemoveHandler(InputElement.PointerPressedEvent, this.OnGutterItemPressed);
            this.RemoveHandler(InputElement.PointerMovedEvent, this.OnGutterItemMoved);
            this.RemoveHandler(InputElement.PointerReleasedEvent, this.OnGutterItemReleased);

            this.AddHandler(
                InputElement.PointerPressedEvent,
                this.OnGutterItemPressed,
                RoutingStrategies.Tunnel | RoutingStrategies.Bubble);

            this.AddHandler(
                InputElement.PointerMovedEvent,
                this.OnGutterItemMoved,
                RoutingStrategies.Tunnel | RoutingStrategies.Bubble);

            this.AddHandler(
                InputElement.PointerReleasedEvent,
                this.OnGutterItemReleased,
                RoutingStrategies.Tunnel | RoutingStrategies.Bubble);

        }
        private void HookDefocusClose()
        {
            TopLevel? topLevel = TopLevel.GetTopLevel(this);

            if (topLevel == null) return;

            if (this.top != null && this.defocusHandler != null)
                this.top.RemoveHandler(InputElement.PointerPressedEvent, this.defocusHandler);

            this.top = topLevel;

            this.defocusHandler = (s, ev) =>
            {
                if (!this.IsExpanded)
                    return;

                if (ev.Source is Visual v)
                {
                    if (this.currentContent is Visual btn && (ReferenceEquals(v, btn) || btn.IsVisualAncestorOf(v)))
                        return;

                    if (this.PanelContent is Visual content)
                    {
                        ResizablePanel? host = content.FindAncestorOfType<ResizablePanel>(includeSelf: true);
                        if (host != null && (ReferenceEquals(v, host) || host.IsVisualAncestorOf(v)))
                            return;
                    }

                    if (this.IsVisualAncestorOf(v))
                        return;
                }

                this.ClosePanel();
            };

            this.top.AddHandler(InputElement.PointerPressedEvent, this.defocusHandler, RoutingStrategies.Tunnel);
        }

        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);

            this.HookDefocusClose();
        }
        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            if (this.top != null && this.defocusHandler != null)
                this.top.RemoveHandler(InputElement.PointerPressedEvent, this.defocusHandler);

            this.defocusHandler = null;
            this.top = null;

            base.OnDetachedFromVisualTree(e);
        }
        private void OnPointerWheelChanged(object? sender, PointerWheelEventArgs e)
        {
            if (sender is ScrollViewer sv)
            {
                if (this.ItemOrientation == Orientation.Horizontal)
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

        private void OnGutterItemPressed(object? sender, PointerPressedEventArgs e)
        {
            this.pressedRot = null;
            this.pressedState = null;

            if (e.Source is Visual v)
            {
                RotatableContent? rot = v.FindAncestorOfType<RotatableContent>(includeSelf: true);
                if (rot?.DataContext is DockPanelState d)
                {
                    this.pressedRot = rot;
                    this.pressedState = d;
                }
            }

            this.clickPosition = e.GetPosition(this);
            this.isMoving = false;
        }
        private void OnGutterItemMoved(object? sender, PointerEventArgs e)
        {
            if (this.clickPosition == null || this.pressedRot == null || this.pressedState == null)
                return;

            Point current = e.GetPosition(this);
            double dx = current.X - this.clickPosition.Value.X;
            double dy = current.Y - this.clickPosition.Value.Y;
            double distSq = dx * dx + dy * dy;

            bool beyondThreshold = distSq > (4 * 4);

            if (!beyondThreshold)
                return;

            if (!this.isMoving)
            {
                this.Cursor = new Cursor(StandardCursorType.DragMove);
                e.Pointer.Capture(this);
            }

            this.isMoving = true;
            this.OnGutterItemReorder(this.pressedState, current);
        }
        private void OnGutterItemReorder(DockPanelState pressedState, Point currentPos)
        {
            if (this.ItemsSource == null)
                return;

            int currentIndex = this.Items.IndexOf(pressedState);
            if (currentIndex < 0)
                return;

            DockPanelState? targetState = null;
            Rect? targetBounds = null;

            for (int i = 0; i < this.Items.Count; i++)
            {
                if (this.ContainerFromIndex(i) is Control container)
                {
                    Point? topLeft = container.TranslatePoint(new Point(0, 0), this);
                    if (topLeft == null) continue;

                    Rect rect = new(topLeft.Value, container.Bounds.Size);

                    if (this.ItemOrientation == Orientation.Vertical)
                    {
                        if (currentPos.Y >= rect.Top && currentPos.Y <= rect.Bottom)
                        {
                            targetState = this.Items[i] as DockPanelState;
                            targetBounds = rect;
                            break;
                        }
                    }
                    else
                    {
                        if (currentPos.X >= rect.Left && currentPos.X <= rect.Right)
                        {
                            targetState = this.Items[i] as DockPanelState;
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
                    ObservableCollection<DockPanelState> list =
                        (ObservableCollection<DockPanelState>)this.ItemsSource;

                    list.RemoveAt(currentIndexNow);
                    list.Insert(targetIndex, pressedState);
                }

                this.lastTargetState = targetState;
                this.lastTargetBounds = targetBounds;
            }
        }
        private void OnGutterItemReleased(object? sender, RoutedEventArgs e)
        {
            if (this.pressedState != null && this.pressedRot != null)
            {
                if (!this.isMoving)
                    this.TogglePanel(this.pressedState, this.pressedRot);
                else
                    this.PanelsReordered?.Invoke(this, new PanelsReorderedEventArgs(this.pressedState));

                e.Handled = true;
            }

            // Reset state
            this.clickPosition = null;
            this.isMoving = false;
            this.pressedRot = null;
            this.pressedState = null;
            this.Cursor = new Cursor(StandardCursorType.Arrow);
        }

        #endregion

        #region DockGutter: Panel Controlers

        protected virtual void TogglePanel(DockPanelState state, Control sourceControl)
        {
            if (this.CurrentState == state && state.IsVisible)
                this.ClosePanel();
            else
                this.OpenPanel(state, sourceControl);
        }
        protected virtual void OpenPanel(DockPanelState state, Control sourceControl)
        {
            this.CurrentState = null;

            if (this.ItemsSource != null)
                foreach (DockPanelState d in this.ItemsSource)
                    d.IsVisible = false;

            this.currentContent = sourceControl;
            this.CurrentState = state;
            this.CurrentState.IsVisible = true;

            this.FlyoutExpanded?.Invoke(this, new FlyoutExtendedEventArgs(this.CurrentState, this.currentContent));
        }
        protected virtual void ClosePanel()
        {
            if (this.CurrentState != null)
                this.CurrentState.IsVisible = false;
        }

        protected virtual void CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            this.UpdateGutterItemContent();

            if (e.Action == NotifyCollectionChangedAction.Remove ||
                e.Action == NotifyCollectionChangedAction.Replace ||
                e.Action == NotifyCollectionChangedAction.Reset)
            {
                if (this.CurrentState != null && !this.Items.Contains(this.CurrentState))
                    this.ClosePanel();
            }
        }
        protected virtual void StatePropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(DockPanelState.IsVisible))
            {
                DockPanelState d = (DockPanelState)sender!;

                if (d.IsVisible)
                {
                    this.PanelContent = d.DockablePanel;
                    this.IsExpanded = true;
                }
                else if (ReferenceEquals(this.CurrentState, d))
                {
                    this.CurrentState = null;
                    this.PanelContent = null;
                    this.IsExpanded = false;
                }
            }
        }

        #endregion
    }
}