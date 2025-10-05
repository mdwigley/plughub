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
using System.Diagnostics;

namespace PlugHub.Plugin.DockHost.Controls
{
    public class FlyoutExtendedEventArgs(DockPanelState descriptor, Control control) : EventArgs
    {
        public DockPanelState Descriptor { get; } = descriptor;
        public Control Control { get; } = control;
    }

    public class DockGutter : TemplatedControl
    {
        public event EventHandler<FlyoutExtendedEventArgs>? FlyoutExpanded;

        private ItemsControl? itemsStrip;
        private TopLevel? top;
        private EventHandler<PointerPressedEventArgs>? defocusHandler;

        public Control? ActiveItem { get; protected set; }

        private DockPanelState? activeState;
        public DockPanelState? ActiveState
        {
            get => this.activeState;
            protected set
            {
                if (this.activeState == value)
                    return;

                if (this.activeState != null)
                    this.activeState.PropertyChanged -= this.OnDescriptorPropertyChanged;

                this.activeState = value;

                if (this.activeState != null)
                    this.activeState.PropertyChanged += this.OnDescriptorPropertyChanged;
            }
        }

        public static readonly StyledProperty<ObservableCollection<DockPanelState>> PanelsProperty =
            AvaloniaProperty.Register<DockGutter, ObservableCollection<DockPanelState>>(nameof(Panels), []);
        public ObservableCollection<DockPanelState> Panels
        {
            get => this.GetValue(PanelsProperty);
            set => this.SetValue(PanelsProperty, value);
        }

        #region DockGutter: Style Properties

        public static readonly StyledProperty<bool> IsExpandedProperty =
            AvaloniaProperty.Register<DockGutter, bool>(nameof(IsExpanded), false);
        public bool IsExpanded
        {
            get => this.GetValue(IsExpandedProperty);
            set => this.SetValue(IsExpandedProperty, value);
        }

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

        public static readonly StyledProperty<double> GutterItemRotationProperty =
            AvaloniaProperty.Register<DockGutter, double>(nameof(GutterItemRotation), 0);
        public double GutterItemRotation
        {
            get => this.GetValue(GutterItemRotationProperty);
            set => this.SetValue(GutterItemRotationProperty, value);
        }

        public static readonly StyledProperty<object?> ActiveContentProperty =
            AvaloniaProperty.Register<DockGutter, object?>(nameof(ActiveContent));
        public object? ActiveContent
        {
            get => this.GetValue(ActiveContentProperty);
            set => this.SetValue(ActiveContentProperty, value);
        }

        #endregion

        #region DockGutter: Direct Properties

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

        public static readonly DirectProperty<DockGutter, Thickness> AccentBorderThicknessProperty =
            AvaloniaProperty.RegisterDirect<DockGutter, Thickness>(nameof(AccentBorderThickness), o => o.AccentBorderThickness);
        private Thickness accentBorderThickness;
        public Thickness AccentBorderThickness
        {
            get => this.accentBorderThickness;
            private set => this.SetAndRaise(AccentBorderThicknessProperty, ref this.accentBorderThickness, value);
        }

        #endregion

        public DockGutter()
        {
            DockEdgeProperty.Changed.AddClassHandler<DockGutter>((x, e) => x.UpdateGutterItemOrientation());

            PanelsProperty.Changed.AddClassHandler<DockGutter>((x, e) =>
            {
                if (e.OldValue is ObservableCollection<DockPanelState> oldColl)
                    oldColl.CollectionChanged -= x.OnPanelsChanged;

                if (e.NewValue is ObservableCollection<DockPanelState> newColl)
                    newColl.CollectionChanged += x.OnPanelsChanged;

                x.UpdateGutterItemContent();
            });

            this.Panels.CollectionChanged += this.OnPanelsChanged;
        }

        #region DockGutter: Internal Upkeep

        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            base.OnApplyTemplate(e);

            this.UpdateGutterItemOrientation();
            this.UpdateGutterItemContent();
            this.UpdateAccent();

            this.itemsStrip = e.NameScope.Find<ItemsControl>("PART_GutterItemStrip");

            this.HookGutterItemClicks();
            this.HookDefocusClose();

            ScrollViewer? scroller = e.NameScope.Find<ScrollViewer>("PART_GutterScroller");

            scroller?.AddHandler(InputElement.PointerWheelChangedEvent, this.OnWheel, RoutingStrategies.Tunnel);
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
            this.HasContent = this.Panels != null && this.Panels.Count > 0;

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
            if (this.itemsStrip == null)
                return;

            this.itemsStrip.RemoveHandler(InputElement.PointerPressedEvent, this.OnGutterItemPressed);
            this.itemsStrip.RemoveHandler(InputElement.PointerMovedEvent, this.OnGutterItemMoved);
            this.itemsStrip.RemoveHandler(InputElement.PointerReleasedEvent, this.OnGutterItemReleased);

            this.itemsStrip.AddHandler(
                InputElement.PointerPressedEvent,
                this.OnGutterItemPressed,
                RoutingStrategies.Tunnel | RoutingStrategies.Bubble);

            this.itemsStrip.AddHandler(
                InputElement.PointerMovedEvent,
                this.OnGutterItemMoved,
                RoutingStrategies.Tunnel | RoutingStrategies.Bubble);

            this.itemsStrip.AddHandler(
                InputElement.PointerReleasedEvent,
                this.OnGutterItemReleased,
                RoutingStrategies.Tunnel | RoutingStrategies.Bubble);

        }
        private void HookDefocusClose()
        {
            if (this.itemsStrip == null)
                return;

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
                    if (this.ActiveItem is Visual btn && (ReferenceEquals(v, btn) || btn.IsVisualAncestorOf(v)))
                        return;

                    if (this.ActiveContent is Visual content)
                    {
                        ResizablePanel? host = content.FindAncestorOfType<ResizablePanel>(includeSelf: true);
                        if (host != null && (ReferenceEquals(v, host) || host.IsVisualAncestorOf(v)))
                            return;
                    }

                    if (this.itemsStrip.IsVisualAncestorOf(v))
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
            this.Panels.CollectionChanged -= this.OnPanelsChanged;
        }
        private void OnWheel(object? sender, PointerWheelEventArgs e)
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

        protected Point? ClickPosition;
        protected bool IsMoving;
        private RotatableContent? pressedRot;
        private DockPanelState? pressedDesc;

        private DockPanelState? lastTargetDesc;
        private Rect? lastTargetBounds;

        private void OnGutterItemPressed(object? sender, PointerPressedEventArgs e)
        {
            this.pressedRot = null;
            this.pressedDesc = null;

            if (e.Source is Visual v)
            {
                RotatableContent? rot = v.FindAncestorOfType<RotatableContent>(includeSelf: true);
                if (rot?.DataContext is DockPanelState d)
                {
                    this.pressedRot = rot;
                    this.pressedDesc = d;
                    Debug.WriteLine($"[Pressed] Cached RotatableContent + Descriptor at index {this.Panels?.IndexOf(d)}");
                }
                else
                {
                    Debug.WriteLine("[Pressed] No RotatableContent/DataContext found");
                }
            }

            this.ClickPosition = e.GetPosition(this.itemsStrip);
            this.IsMoving = false;

            Debug.WriteLine($"[Pressed] sender={sender?.GetType().Name}, source={e.Source?.GetType().Name}, " +
                            $"ClickPosition=({this.ClickPosition?.X:0.0},{this.ClickPosition?.Y:0.0}), IsMoving={this.IsMoving}");
        }
        private void OnGutterItemMoved(object? sender, PointerEventArgs e)
        {
            if (this.ClickPosition == null || this.itemsStrip == null || this.pressedRot == null || this.pressedDesc == null)
                return;

            Point current = e.GetPosition(this.itemsStrip);
            double dx = current.X - this.ClickPosition.Value.X;
            double dy = current.Y - this.ClickPosition.Value.Y;
            double distSq = dx * dx + dy * dy;

            bool beyondThreshold = distSq > (4 * 4);

            if (!beyondThreshold)
                return;

            if (!this.IsMoving)
            {
                this.Cursor = new Cursor(StandardCursorType.DragMove);
                e.Pointer.Capture(this.itemsStrip);
                Debug.WriteLine("[Moved] Threshold crossed → captured pointer, cursor=DragMove");
            }

            this.IsMoving = true;
            Debug.WriteLine("[Moved] Calling OnGutterItemReorder");
            this.OnGutterItemReorder(this.pressedDesc, current);
        }
        private void OnGutterItemReorder(DockPanelState pressedDesc, Point currentPos)
        {
            if (this.itemsStrip == null || this.Panels == null)
                return;

            int currentIndex = this.Panels.IndexOf(pressedDesc);
            if (currentIndex < 0)
                return;

            DockPanelState? targetDesc = null;
            Rect? targetBounds = null;

            for (int i = 0; i < this.Panels.Count; i++)
            {
                if (this.itemsStrip.ContainerFromIndex(i) is Control container)
                {
                    Point? topLeft = container.TranslatePoint(new Point(0, 0), this.itemsStrip);
                    if (topLeft == null) continue;

                    Rect rect = new(topLeft.Value, container.Bounds.Size);

                    if (this.ItemOrientation == Orientation.Vertical)
                    {
                        if (currentPos.Y >= rect.Top && currentPos.Y <= rect.Bottom)
                        {
                            targetDesc = this.Panels[i];
                            targetBounds = rect;
                            break;
                        }
                    }
                    else
                    {
                        if (currentPos.X >= rect.Left && currentPos.X <= rect.Right)
                        {
                            targetDesc = this.Panels[i];
                            targetBounds = rect;
                            break;
                        }
                    }
                }
            }

            if (this.lastTargetDesc != null && this.lastTargetBounds.HasValue &&
                this.lastTargetBounds.Value.Contains(currentPos))
            {
                return;
            }

            if (this.lastTargetDesc != null && this.lastTargetBounds.HasValue &&
                !this.lastTargetBounds.Value.Contains(currentPos))
            {
                this.lastTargetDesc = null;
                this.lastTargetBounds = null;
            }

            if (targetDesc == null || ReferenceEquals(targetDesc, pressedDesc))
                return;

            if (!ReferenceEquals(targetDesc, this.lastTargetDesc))
            {
                int targetIndex = this.Panels.IndexOf(targetDesc);
                int currentIndexNow = this.Panels.IndexOf(pressedDesc);

                if (targetIndex >= 0 && targetIndex != currentIndexNow)
                {
                    this.Panels.RemoveAt(currentIndexNow);
                    this.Panels.Insert(targetIndex, pressedDesc);

                    Debug.WriteLine($"[Reorder] Moved descriptor from {currentIndexNow} to {targetIndex}");
                }

                this.lastTargetDesc = targetDesc;
                this.lastTargetBounds = targetBounds;
            }
        }
        private void OnGutterItemReleased(object? sender, RoutedEventArgs e)
        {
            if (this.pressedRot != null && this.pressedDesc != null && !this.IsMoving)
            {
                Debug.WriteLine("[Released] TogglePanel (click path)");
                this.TogglePanel(this.pressedDesc, this.pressedRot);
                e.Handled = true;
            }
            else
            {
                Debug.WriteLine("[Released] No toggle (drag path or missing pressed item)");
            }

            this.ClickPosition = null;
            this.IsMoving = false;
            this.pressedRot = null;
            this.pressedDesc = null;

            this.Cursor = new Cursor(StandardCursorType.Arrow);
            Debug.WriteLine("[Released] State reset, cursor restored to Arrow");
        }

        #endregion

        #region DockGutter: Panel Controlers

        protected virtual void TogglePanel(DockPanelState state, Control sourceControl)
        {
            if (this.ActiveState == state && state.IsVisible)
                this.ClosePanel();
            else
                this.OpenPanel(state, sourceControl);
        }
        protected virtual void OpenPanel(DockPanelState state, Control sourceControl)
        {
            this.ActiveState = null;

            if (this.Panels != null)
                foreach (DockPanelState d in this.Panels)
                    d.IsVisible = false;

            this.ActiveItem = sourceControl;
            this.ActiveState = state;
            this.ActiveState.IsVisible = true;

            this.FlyoutExpanded?.Invoke(this, new FlyoutExtendedEventArgs(this.ActiveState, this.ActiveItem));
        }
        protected virtual void ClosePanel()
        {
            if (this.ActiveState != null)
                this.ActiveState.IsVisible = false;
        }

        protected virtual void OnPanelsChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            this.UpdateGutterItemContent();

            if (e.Action == NotifyCollectionChangedAction.Remove ||
                e.Action == NotifyCollectionChangedAction.Replace ||
                e.Action == NotifyCollectionChangedAction.Reset)
            {
                if (this.ActiveState != null && !this.Panels.Contains(this.ActiveState))
                    this.ClosePanel();
            }
        }
        protected virtual void OnDescriptorPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(DockPanelState.IsVisible))
            {
                DockPanelState d = (DockPanelState)sender!;

                if (d.IsVisible)
                {
                    this.ActiveContent = d.DockablePanel;
                    this.IsExpanded = true;
                }
                else if (ReferenceEquals(this.ActiveState, d))
                {
                    this.ActiveState = null;
                    this.ActiveContent = null;
                    this.IsExpanded = false;
                }
            }
        }

        #endregion
    }
}