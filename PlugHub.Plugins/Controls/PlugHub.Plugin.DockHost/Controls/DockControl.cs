using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Interactivity;
using Avalonia.Threading;
using PlugHub.Plugin.DockHost.Interfaces.Services;
using PlugHub.Plugin.DockHost.Models;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;

namespace PlugHub.Plugin.DockHost.Controls
{
    public class DockControl : ContentControl
    {
        public const int DEFAULT_PANEL_SIZE = 250;

        private sealed class DockControlMargins(bool hasTop, double topH, bool hasRight, double rightW, bool hasBottom, double bottomH, bool hasLeft, double leftW)
        {
            public double Top { get; } = hasTop ? topH : 0;
            public double Right { get; } = hasRight ? rightW : 0;
            public double Bottom { get; } = hasBottom ? bottomH : 0;
            public double Left { get; } = hasLeft ? leftW : 0;

            public Thickness ForBottom() => new(this.Left, 0, this.Right, this.Bottom);
            public Thickness ForTop() => new(this.Left, this.Top, this.Right, 0);
            public Thickness ForLeft() => new(this.Left, this.Top, 0, this.Bottom);
            public Thickness ForRight() => new(0, this.Top, this.Right, this.Bottom);
        }

        private Grid? dropTargetsGrid;
        private Border? leftDropTarget;
        private Border? topDropTarget;
        private Border? rightDropTarget;
        private Border? bottomDropTarget;

        private DockGutter? leftGutter;
        private DockGutter? rightGutter;
        private DockGutter? topGutter;
        private DockGutter? bottomGutter;


        #region DockControl: Control Properties

        public static readonly StyledProperty<Guid> DockIdProperty =
            AvaloniaProperty.Register<DockControl, Guid>(nameof(DockId), Guid.Empty);
        public Guid DockId
        {
            get => this.GetValue(DockIdProperty);
            set => this.SetValue(DockIdProperty, value);
        }

        public static readonly StyledProperty<IDockService?> DockServiceProperty =
            AvaloniaProperty.Register<DockControl, IDockService?>(nameof(DockService));
        public IDockService? DockService
        {
            get => this.GetValue(DockServiceProperty);
            set => this.SetValue(DockServiceProperty, value);
        }

        #endregion

        #region DockControl: Pinned Panel Properties

        public static readonly StyledProperty<IDataTemplate?> DockControlBottomTabControlProperty =
            AvaloniaProperty.Register<DockControl, IDataTemplate?>(nameof(DockControlBottomTabControl));
        public IDataTemplate? DockControlBottomTabControl
        {
            get => this.GetValue(DockControlBottomTabControlProperty);
            set => this.SetValue(DockControlBottomTabControlProperty, value);
        }

        public static readonly StyledProperty<IDataTemplate?> DockControlTopTabControlProperty =
            AvaloniaProperty.Register<DockControl, IDataTemplate?>(nameof(DockControlTopTabControl));
        public IDataTemplate? DockControlTopTabControl
        {
            get => this.GetValue(DockControlTopTabControlProperty);
            set => this.SetValue(DockControlTopTabControlProperty, value);
        }

        public static readonly StyledProperty<GridLength> LeftPanelLengthProperty =
            AvaloniaProperty.Register<DockControl, GridLength>(nameof(LeftPanelLength), new GridLength(DEFAULT_PANEL_SIZE));
        public GridLength LeftPanelLength
        {
            get => this.GetValue(LeftPanelLengthProperty);
            set => this.SetValue(LeftPanelLengthProperty, value);
        }

        public static readonly StyledProperty<GridLength> RightPanelLengthProperty =
            AvaloniaProperty.Register<DockControl, GridLength>(nameof(RightPanelLength), new GridLength(DEFAULT_PANEL_SIZE));
        public GridLength RightPanelLength
        {
            get => this.GetValue(RightPanelLengthProperty);
            set => this.SetValue(RightPanelLengthProperty, value);
        }

        public static readonly StyledProperty<GridLength> TopPanelLengthProperty =
            AvaloniaProperty.Register<DockControl, GridLength>(nameof(TopPanelLength), new GridLength(DEFAULT_PANEL_SIZE));
        public GridLength TopPanelLength
        {
            get => this.GetValue(TopPanelLengthProperty);
            set => this.SetValue(TopPanelLengthProperty, value);
        }

        public static readonly StyledProperty<GridLength> BottomPanelLengthProperty =
            AvaloniaProperty.Register<DockControl, GridLength>(nameof(BottomPanelLength), new GridLength(DEFAULT_PANEL_SIZE));
        public GridLength BottomPanelLength
        {
            get => this.GetValue(BottomPanelLengthProperty);
            set => this.SetValue(BottomPanelLengthProperty, value);
        }

        public static readonly DirectProperty<DockControl, bool> HasLeftContentProperty =
            AvaloniaProperty.RegisterDirect<DockControl, bool>(nameof(HasLeftContent), o => o.HasLeftContent);
        public bool HasLeftContent => this.LeftPinned.Count > 0;

        public static readonly DirectProperty<DockControl, bool> HasRightContentProperty =
            AvaloniaProperty.RegisterDirect<DockControl, bool>(nameof(HasRightContent), o => o.HasRightContent);
        public bool HasRightContent => this.RightPinned.Count > 0;

        public static readonly DirectProperty<DockControl, bool> HasTopContentProperty =
            AvaloniaProperty.RegisterDirect<DockControl, bool>(nameof(HasTopContent), o => o.HasTopContent);
        public bool HasTopContent => this.TopPinned.Count > 0;

        public static readonly DirectProperty<DockControl, bool> HasBottomContentProperty =
            AvaloniaProperty.RegisterDirect<DockControl, bool>(nameof(HasBottomContent), o => o.HasBottomContent);
        public bool HasBottomContent => this.BottomPinned.Count > 0;

        #endregion

        #region DockControl: Pinned Collection Definitions

        public ObservableCollection<DockPanelState> LeftPinned { get; } = [];
        public ObservableCollection<DockPanelState> RightPinned { get; } = [];
        public ObservableCollection<DockPanelState> TopPinned { get; } = [];
        public ObservableCollection<DockPanelState> BottomPinned { get; } = [];

        #endregion

        #region DockControl: Unpinned Panel Properties

        public static readonly DirectProperty<DockControl, Thickness> LeftGutterMarginsProperty =
            AvaloniaProperty.RegisterDirect<DockControl, Thickness>(nameof(LeftGutterMargins), o => o.LeftGutterMargins, (o, v) => o.LeftGutterMargins = v, new Thickness(0));
        private Thickness leftGutterMargins;
        public Thickness LeftGutterMargins
        {
            get => this.leftGutterMargins;
            set => this.SetAndRaise(LeftGutterMarginsProperty, ref this.leftGutterMargins, value);
        }

        public static readonly DirectProperty<DockControl, Thickness> RightGutterMarginsProperty =
            AvaloniaProperty.RegisterDirect<DockControl, Thickness>(nameof(RightGutterMargins), o => o.RightGutterMargins, (o, v) => o.RightGutterMargins = v, new Thickness(0));
        private Thickness rightGutterMargins;
        public Thickness RightGutterMargins
        {
            get => this.rightGutterMargins;
            set => this.SetAndRaise(RightGutterMarginsProperty, ref this.rightGutterMargins, value);
        }

        public static readonly DirectProperty<DockControl, Thickness> TopGutterMarginsProperty =
            AvaloniaProperty.RegisterDirect<DockControl, Thickness>(nameof(TopGutterMargins), o => o.TopGutterMargins, (o, v) => o.TopGutterMargins = v, new Thickness(0));
        private Thickness topGutterMargins;
        public Thickness TopGutterMargins
        {
            get => this.topGutterMargins;
            set => this.SetAndRaise(TopGutterMarginsProperty, ref this.topGutterMargins, value);
        }

        public static readonly DirectProperty<DockControl, Thickness> BottomGutterMarginsProperty =
            AvaloniaProperty.RegisterDirect<DockControl, Thickness>(nameof(BottomGutterMargins), o => o.BottomGutterMargins, (o, v) => o.BottomGutterMargins = v, new Thickness(0));
        private Thickness bottomGutterMargins;
        public Thickness BottomGutterMargins
        {
            get => this.bottomGutterMargins;
            set => this.SetAndRaise(BottomGutterMarginsProperty, ref this.bottomGutterMargins, value);
        }

        public static readonly DirectProperty<DockControl, Thickness> LeftFlyoutMarginProperty =
            AvaloniaProperty.RegisterDirect<DockControl, Thickness>(nameof(LeftFlyoutMargin), o => o.LeftFlyoutMargin);
        private Thickness leftFlyoutMargin;
        public Thickness LeftFlyoutMargin
        {
            get => this.leftFlyoutMargin;
            private set => this.SetAndRaise(LeftFlyoutMarginProperty, ref this.leftFlyoutMargin, value);
        }

        public static readonly DirectProperty<DockControl, Thickness> RightFlyoutMarginProperty =
            AvaloniaProperty.RegisterDirect<DockControl, Thickness>(nameof(RightFlyoutMargin), o => o.RightFlyoutMargin);
        private Thickness rightFlyoutMargin;
        public Thickness RightFlyoutMargin
        {
            get => this.rightFlyoutMargin;
            private set => this.SetAndRaise(RightFlyoutMarginProperty, ref this.rightFlyoutMargin, value);
        }

        public static readonly DirectProperty<DockControl, Thickness> TopFlyoutMarginProperty =
            AvaloniaProperty.RegisterDirect<DockControl, Thickness>(nameof(TopFlyoutMargin), o => o.TopFlyoutMargin);
        private Thickness topFlyoutMargin;
        public Thickness TopFlyoutMargin
        {
            get => this.topFlyoutMargin;
            private set => this.SetAndRaise(TopFlyoutMarginProperty, ref this.topFlyoutMargin, value);
        }

        public static readonly DirectProperty<DockControl, Thickness> BottomFlyoutMarginProperty =
            AvaloniaProperty.RegisterDirect<DockControl, Thickness>(nameof(BottomFlyoutMargin), o => o.BottomFlyoutMargin);
        private Thickness bottomFlyoutMargin;
        public Thickness BottomFlyoutMargin
        {
            get => this.bottomFlyoutMargin;
            private set => this.SetAndRaise(BottomFlyoutMarginProperty, ref this.bottomFlyoutMargin, value);
        }

        public static readonly DirectProperty<DockControl, bool> HasExpandedFlyoutProperty =
            AvaloniaProperty.RegisterDirect<DockControl, bool>(nameof(HasExpandedFlyout), o => o.HasExpandedFlyout);
        private bool hasExpandedFlyout;
        public bool HasExpandedFlyout
        {
            get => this.hasExpandedFlyout;
            private set => this.SetAndRaise(HasExpandedFlyoutProperty, ref this.hasExpandedFlyout, value);
        }

        #endregion

        #region DockControl: Unpinned Collection Definitions

        public ObservableCollection<DockPanelState> LeftUnpinned { get; } = [];
        public ObservableCollection<DockPanelState> RightUnpinned { get; } = [];
        public ObservableCollection<DockPanelState> TopUnpinned { get; } = [];
        public ObservableCollection<DockPanelState> BottomUnpinned { get; } = [];

        #endregion


        public DockControl()
        {
            DockServiceProperty.Changed.AddClassHandler<DockControl>((c, e) =>
            {
                if (e.NewValue is IDockService service)
                {
                    service.RegisterDockControl(this);
                }
            });

            this.LeftGutterMargins = new Thickness(0, 32, 0, 32);
            this.RightGutterMargins = new Thickness(0, 32, 0, 32);
            this.TopGutterMargins = new Thickness(0, 0, 0, 0);
            this.BottomGutterMargins = new Thickness(0, 0, 0, 0);
        }


        #region DockControl: Lifecycle 

        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            base.OnApplyTemplate(e);
            this.SetupOverlayGutters(e);
            this.SetupMainGrid(e);
            this.SetupDropTargets(e);
        }
        private void SetupOverlayGutters(TemplateAppliedEventArgs e)
        {
            this.leftGutter = e.NameScope.Find<DockGutter>("PART_LeftGutter");
            this.rightGutter = e.NameScope.Find<DockGutter>("PART_RightGutter");
            this.topGutter = e.NameScope.Find<DockGutter>("PART_TopGutter");
            this.bottomGutter = e.NameScope.Find<DockGutter>("PART_BottomGutter");

            if (this.leftGutter != null)
            {
                this.leftGutter.Panels = this.LeftUnpinned;
                this.leftGutter.DockEdge = Dock.Left;
                this.leftGutter.PropertyChanged += (_, args) =>
                {
                    if (args.Property == BoundsProperty && args.NewValue is Rect rect)
                        this.RecalculateMargins(rect.Width, this.rightGutter?.Bounds.Width ?? 0, this.topGutter?.Bounds.Height ?? 0, this.bottomGutter?.Bounds.Height ?? 0);

                    if (args is AvaloniaPropertyChangedEventArgs hcp && hcp.Property == DockGutter.HasContentProperty && hcp.NewValue is bool lHas)
                        this.RecalculateMargins(lHas ? this.leftGutter.Bounds.Width : 0, this.rightGutter?.Bounds.Width ?? 0, this.topGutter?.Bounds.Height ?? 0, this.bottomGutter?.Bounds.Height ?? 0);

                    if (args is AvaloniaPropertyChangedEventArgs iep && iep.Property == DockGutter.IsExpandedProperty)
                        this.UpdateHasExpandedFlyout();
                };
            }
            if (this.rightGutter != null)
            {
                this.rightGutter.Panels = this.RightUnpinned;
                this.rightGutter.DockEdge = Dock.Right;
                this.rightGutter.PropertyChanged += (_, args) =>
                {
                    if (args.Property == BoundsProperty && args.NewValue is Rect rect)
                        this.RecalculateMargins(this.leftGutter?.Bounds.Width ?? 0, rect.Width, this.topGutter?.Bounds.Height ?? 0, this.bottomGutter?.Bounds.Height ?? 0);

                    if (args is AvaloniaPropertyChangedEventArgs hcp && hcp.Property == DockGutter.HasContentProperty && hcp.NewValue is bool rHas)
                        this.RecalculateMargins(this.leftGutter?.Bounds.Width ?? 0, rHas ? this.rightGutter.Bounds.Width : 0, this.topGutter?.Bounds.Height ?? 0, this.bottomGutter?.Bounds.Height ?? 0);

                    if (args is AvaloniaPropertyChangedEventArgs iep && iep.Property == DockGutter.IsExpandedProperty)
                        this.UpdateHasExpandedFlyout();
                };
            }
            if (this.topGutter != null)
            {
                this.topGutter.Panels = this.TopUnpinned;
                this.topGutter.DockEdge = Dock.Top;
                this.topGutter.PropertyChanged += (_, args) =>
                {
                    if (args.Property == BoundsProperty && args.NewValue is Rect rect)
                        this.RecalculateMargins(this.leftGutter?.Bounds.Width ?? 0, this.rightGutter?.Bounds.Width ?? 0, rect.Height, this.bottomGutter?.Bounds.Height ?? 0);

                    if (args is AvaloniaPropertyChangedEventArgs hcp && hcp.Property == DockGutter.HasContentProperty && hcp.NewValue is bool tHas)
                        this.RecalculateMargins(this.leftGutter?.Bounds.Width ?? 0, this.rightGutter?.Bounds.Width ?? 0, tHas ? this.topGutter.Bounds.Height : 0, this.bottomGutter?.Bounds.Height ?? 0);

                    if (args is AvaloniaPropertyChangedEventArgs iep && iep.Property == DockGutter.IsExpandedProperty)
                        this.UpdateHasExpandedFlyout();
                };
            }
            if (this.bottomGutter != null)
            {
                this.bottomGutter.Panels = this.BottomUnpinned;
                this.bottomGutter.DockEdge = Dock.Bottom;
                this.bottomGutter.PropertyChanged += (_, args) =>
                {
                    if (args.Property == BoundsProperty && args.NewValue is Rect rect)
                        this.RecalculateMargins(this.leftGutter?.Bounds.Width ?? 0, this.rightGutter?.Bounds.Width ?? 0, this.topGutter?.Bounds.Height ?? 0, rect.Height);

                    if (args is AvaloniaPropertyChangedEventArgs hcp && hcp.Property == DockGutter.HasContentProperty && hcp.NewValue is bool bHas)
                        this.RecalculateMargins(this.leftGutter?.Bounds.Width ?? 0, this.rightGutter?.Bounds.Width ?? 0, this.topGutter?.Bounds.Height ?? 0, bHas ? this.bottomGutter.Bounds.Height : 0);

                    if (args is AvaloniaPropertyChangedEventArgs iep && iep.Property == DockGutter.IsExpandedProperty)
                        this.UpdateHasExpandedFlyout();
                };
            }
        }
        private void SetupMainGrid(TemplateAppliedEventArgs e)
        {
            Grid? grid = e.NameScope.Find<Grid>("PART_RootGrid");

            if (grid is null) return;

            grid.ColumnDefinitions[0].Width = this.LeftPanelLength;
            grid.ColumnDefinitions[4].Width = this.RightPanelLength;
            grid.RowDefinitions[0].Height = this.TopPanelLength;
            grid.RowDefinitions[4].Height = this.BottomPanelLength;

            PropertyChanged += (_, args) =>
            {
                switch (args.Property)
                {
                    case AvaloniaProperty p when p == LeftPanelLengthProperty:
                        grid.ColumnDefinitions[0].Width = this.LeftPanelLength;
                        break;
                    case AvaloniaProperty p when p == RightPanelLengthProperty:
                        grid.ColumnDefinitions[4].Width = this.RightPanelLength;
                        break;
                    case AvaloniaProperty p when p == TopPanelLengthProperty:
                        grid.RowDefinitions[0].Height = this.TopPanelLength;
                        break;
                    case AvaloniaProperty p when p == BottomPanelLengthProperty:
                        grid.RowDefinitions[4].Height = this.BottomPanelLength;
                        break;
                }
            };
        }
        private void SetupDropTargets(TemplateAppliedEventArgs e)
        {
            this.dropTargetsGrid = e.NameScope.Find<Grid>("PART_DropTargetsGrid");

            this.topDropTarget = e.NameScope.Find<Border>("PART_TopDropTarget");
            this.bottomDropTarget = e.NameScope.Find<Border>("PART_BottomDropTarget");
            this.leftDropTarget = e.NameScope.Find<Border>("PART_LeftDropTarget");
            this.rightDropTarget = e.NameScope.Find<Border>("PART_RightDropTarget");

            Debug.WriteLine($"TopDropTarget handlers attached: {this.topDropTarget != null}");
        }

        public DockHostControlData GetHostControlData()
        {
            DockHostControlData data = new()
            {
                PanelLeftLength = this.LeftPanelLength.Value,
                PanelTopLength = this.TopPanelLength.Value,
                PanelRightLength = this.RightPanelLength.Value,
                PanelBottomLength = this.BottomPanelLength.Value,

                FlyoutLeftLength = this.leftGutter == null ? 0.0d : this.leftGutter.PanelSize,
                FlyoutTopLength = this.topGutter == null ? 0.0d : this.topGutter.PanelSize,
                FlyoutRightLength = this.rightGutter == null ? 0.0d : this.rightGutter.PanelSize,
                FlyoutBottomLength = this.bottomGutter == null ? 0.0d : this.bottomGutter.PanelSize
            };

            foreach (DockPanelState state in this.GetAllDockPanelStates())
                data.DockHostDataItems.Add(state.GetHostPanelData());

            return data;
        }

        #endregion

        #region DockControl: Drag & Drop

        private void SetDropTargetsVisible(bool visible)
        {
            if (this.dropTargetsGrid is not null)
                this.dropTargetsGrid.IsVisible = visible;
        }
        private void OnDockPanelDragStarted(object? sender, EventArgs e)
        {
            Debug.WriteLine("[DockControl] Overlay should now be painted");

            this.SetDropTargetsVisible(true);
        }
        private void OnDockPanelDragProgressing(object? sender, PanelDragEventArgs e)
        {
            Point pos = e.PointerEvent.GetPosition(this.dropTargetsGrid);

            this.topDropTarget!.Classes.Remove("IsPointerOver");
            this.leftDropTarget!.Classes.Remove("IsPointerOver");
            this.rightDropTarget!.Classes.Remove("IsPointerOver");
            this.bottomDropTarget!.Classes.Remove("IsPointerOver");

            if (this.topDropTarget.Bounds.Contains(pos))
            {
                this.topDropTarget.Classes.Add("IsPointerOver");
                this.topDropTarget.InvalidateVisual();
            }
            else if (this.leftDropTarget.Bounds.Contains(pos))
            {
                this.leftDropTarget.Classes.Add("IsPointerOver");
                this.leftDropTarget.InvalidateVisual();
            }
            else if (this.rightDropTarget.Bounds.Contains(pos))
            {
                this.rightDropTarget.Classes.Add("IsPointerOver");
                this.rightDropTarget.InvalidateVisual();
            }
            else if (this.bottomDropTarget.Bounds.Contains(pos))
            {
                this.bottomDropTarget.Classes.Add("IsPointerOver");
                this.bottomDropTarget.InvalidateVisual();
            }
        }

        private void OnDockPanelDragCompleted(object? sender, PanelDragEventArgs e)
        {
            this.SetDropTargetsVisible(false);

            Point pos = e.PointerEvent.GetPosition(this.dropTargetsGrid);

            if (this.topDropTarget!.Bounds.Contains(pos))
                this.Move(e.Descriptor, Dock.Top);
            else if (this.leftDropTarget!.Bounds.Contains(pos))
                this.Move(e.Descriptor, Dock.Left);
            else if (this.rightDropTarget!.Bounds.Contains(pos))
                this.Move(e.Descriptor, Dock.Right);
            else if (this.bottomDropTarget!.Bounds.Contains(pos))
                this.Move(e.Descriptor, Dock.Bottom);
        }

        #endregion

        #region DockControl: Panels & Slices

        public IEnumerable<DockPanelState> GetAllDockPanelStates()
        {
            return
            [
                .. this.LeftPinned,
                .. this.RightPinned,
                .. this.TopPinned,
                .. this.BottomPinned,
                .. this.LeftUnpinned,
                .. this.RightUnpinned,
                .. this.TopUnpinned,
                .. this.BottomUnpinned,
            ];
        }

        private async void Descriptor_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (sender is not DockPanelState d)
                return;

            if (e.PropertyName == nameof(DockPanelState.IsPinned))
            {
                await HardDetachAsync(d.DockablePanel);

                this.ReinsertPanel(d);
            }
            else if (e.PropertyName == nameof(DockPanelState.DockEdge))
            {
                if (d.IsPinned)
                    this.ReinsertPanel(d);
                else
                {
                    if (d.DockablePanel is not null)
                        await HardDetachAsync(d.DockablePanel);

                    this.ReinsertPanel(d);
                }
            }
        }
        private void ReinsertPanel(DockPanelState d)
        {
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                this.RemoveFromAllSlices(d);
                this.AddToSlice(d);
                this.RaiseHasFlags();
            }, DispatcherPriority.Render);
        }

        public void Register(DockPanelState descriptor, Dock? edge = null, bool? pinned = null)
        {
            this.AddToSlice(descriptor);

            // Apply optional state if provided
            if (edge.HasValue)
                descriptor.DockEdge = edge.Value;

            if (pinned.HasValue)
                descriptor.IsPinned = pinned.Value;

            descriptor.PropertyChanged -= this.Descriptor_PropertyChanged;
            descriptor.PropertyChanged += this.Descriptor_PropertyChanged;

            if (descriptor.DockablePanel is { } panel)
            {
                panel.DragStarted -= this.OnDockPanelDragStarted;
                panel.DragStarted += this.OnDockPanelDragStarted;

                panel.DragCompleted -= this.OnDockPanelDragCompleted;
                panel.DragCompleted += this.OnDockPanelDragCompleted;

                panel.DragProgressing -= this.OnDockPanelDragProgressing;
                panel.DragProgressing += this.OnDockPanelDragProgressing;

                panel.RemoveHandler(DockablePanel.CloseRequestedEvent, this.OnPanelCloseRequested);
                panel.AddHandler(DockablePanel.CloseRequestedEvent, this.OnPanelCloseRequested);
            }
        }
        private ObservableCollection<DockPanelState> GetSlice(Dock edge, bool pinned) =>
            (edge, pinned) switch
            {
                (Dock.Left, true) => this.LeftPinned,
                (Dock.Left, false) => this.LeftUnpinned,
                (Dock.Right, true) => this.RightPinned,
                (Dock.Right, false) => this.RightUnpinned,
                (Dock.Top, true) => this.TopPinned,
                (Dock.Top, false) => this.TopUnpinned,
                (Dock.Bottom, true) => this.BottomPinned,
                (Dock.Bottom, false) => this.BottomUnpinned,
                _ => throw new InvalidOperationException("Unknown edge")
            };
        private void AddToSlice(DockPanelState d)
        {
            ObservableCollection<DockPanelState> slice = this.GetSlice(d.DockEdge, d.IsPinned);
            slice.Add(d);

            if (d.IsPinned)
            {
                switch (d.DockEdge)
                {
                    case Dock.Left:
                        this.LeftPanelLength = new GridLength(DEFAULT_PANEL_SIZE);
                        break;
                    case Dock.Right:
                        this.RightPanelLength = new GridLength(DEFAULT_PANEL_SIZE);
                        break;
                    case Dock.Top:
                        this.TopPanelLength = new GridLength(DEFAULT_PANEL_SIZE);
                        break;
                    case Dock.Bottom:
                        this.BottomPanelLength = new GridLength(DEFAULT_PANEL_SIZE);
                        break;
                }
            }

            this.RaiseHasFlags();
        }
        private void RemoveFromAllSlices(DockPanelState d)
        {
            this.LeftUnpinned.Remove(d);
            this.RightUnpinned.Remove(d);
            this.TopUnpinned.Remove(d);
            this.BottomUnpinned.Remove(d);

            this.LeftPinned.Remove(d);
            this.RightPinned.Remove(d);
            this.TopPinned.Remove(d);
            this.BottomPinned.Remove(d);

            if (this.LeftPinned.Count == 0)
                this.LeftPanelLength = new GridLength(0);

            if (this.RightPinned.Count == 0)
                this.RightPanelLength = new GridLength(0);

            if (this.TopPinned.Count == 0)
                this.TopPanelLength = new GridLength(0);

            if (this.BottomPinned.Count == 0)
                this.BottomPanelLength = new GridLength(0);

            this.RaiseHasFlags();
        }
        private void RaiseHasFlags()
        {
            this.RaisePropertyChanged(HasLeftContentProperty, !this.HasLeftContent, this.HasLeftContent);
            this.RaisePropertyChanged(HasRightContentProperty, !this.HasRightContent, this.HasRightContent);
            this.RaisePropertyChanged(HasTopContentProperty, !this.HasTopContent, this.HasTopContent);
            this.RaisePropertyChanged(HasBottomContentProperty, !this.HasBottomContent, this.HasBottomContent);
        }

        private void OnPanelCloseRequested(object? sender, RoutedEventArgs e)
        {
            if (sender is DockablePanel panel && panel.Descriptor is DockPanelState state)
                this.ClosePanel(state);
        }
        public void ClosePanel(DockPanelState d)
        {
            if (d == null) return;

            this.RemoveFromAllSlices(d);

            d.PropertyChanged -= this.Descriptor_PropertyChanged;

            if (d.DockablePanel is not null)
            {
                d.DockablePanel.DragStarted -= this.OnDockPanelDragStarted;
                d.DockablePanel.DragCompleted -= this.OnDockPanelDragCompleted;
                d.DockablePanel.DragProgressing -= this.OnDockPanelDragProgressing;
                d.DockablePanel.RemoveHandler(DockablePanel.CloseRequestedEvent, this.OnPanelCloseRequested);
            }
        }

        #endregion

        #region DockControl: Layout & Public API

        private void UpdateHasExpandedFlyout()
        {
            this.HasExpandedFlyout =
                (this.leftGutter?.IsExpanded ?? false) ||
                (this.rightGutter?.IsExpanded ?? false) ||
                (this.topGutter?.IsExpanded ?? false) ||
                (this.bottomGutter?.IsExpanded ?? false);
        }

        private void RecalculateMargins(double leftWidth, double rightWidth, double topHeight, double bottomHeight)
        {
            bool hasLeft = this.LeftUnpinned.Count > 0;
            bool hasRight = this.RightUnpinned.Count > 0;
            bool hasTop = this.TopUnpinned.Count > 0;
            bool hasBottom = this.BottomUnpinned.Count > 0;

            this.RecalculateFlyoutMargins(hasTop, topHeight, hasRight, rightWidth, hasBottom, bottomHeight, hasLeft, leftWidth);
            this.RecalculateGutterMargins(hasTop, hasBottom, hasLeft, hasRight);
        }
        private void RecalculateFlyoutMargins(bool hasTop, double topHeight, bool hasRight, double rightWidth, bool hasBottom, double bottomHeight, bool hasLeft, double leftWidth)
        {
            DockControlMargins m = new(
                hasTop, topHeight,
                hasRight, rightWidth,
                hasBottom, bottomHeight,
                hasLeft, leftWidth);

            this.BottomFlyoutMargin = m.ForBottom();
            this.TopFlyoutMargin = m.ForTop();
            this.LeftFlyoutMargin = m.ForLeft();
            this.RightFlyoutMargin = m.ForRight();
        }
        private void RecalculateGutterMargins(bool hasTop, bool hasBottom, bool hasLeft, bool hasRight)
        {
            if (hasLeft)
            {
                if (hasTop && hasBottom)
                    this.LeftGutterMargins = new Thickness(0, 32, 0, 32);
                else if (hasTop)
                    this.LeftGutterMargins = new Thickness(0, 32, 0, 0);
                else if (hasBottom)
                    this.LeftGutterMargins = new Thickness(0, 0, 0, 32);
                else
                    this.LeftGutterMargins = new Thickness(0);
            }
            else
            {
                this.LeftGutterMargins = new Thickness(0, 32, 0, 32);
            }

            if (hasRight)
            {
                if (hasTop && hasBottom)
                    this.RightGutterMargins = new Thickness(0, 32, 0, 32);
                else if (hasTop)
                    this.RightGutterMargins = new Thickness(0, 32, 0, 0);
                else if (hasBottom)
                    this.RightGutterMargins = new Thickness(0, 0, 0, 32);
                else
                    this.RightGutterMargins = new Thickness(0);
            }
            else
            {
                this.RightGutterMargins = new Thickness(0, 32, 0, 32);
            }

            this.TopGutterMargins = new Thickness(0);
            this.BottomGutterMargins = new Thickness(0);
        }

        private static async Task HardDetachAsync(Control panel)
        {
            for (int i = 0; i < 3 && panel.Parent != null; i++)
            {
                string? parentType = panel.Parent?.GetType().Name;

                switch (panel.Parent)
                {
                    case ContentPresenter cp:
                        cp.Content = null;
                        break;
                    case ContentControl cc:
                        cc.Content = null;
                        break;
                    case Panel container:
                        container.Children.Remove(panel);
                        break;
                    default:
                        Debug.WriteLine($"HardDetach: Encountered unexpected parent type: {parentType}");
                        break;
                }
                panel.InvalidateMeasure();
                panel.InvalidateArrange();

                await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.Render);
            }

            if (panel.Parent != null)
            {
                string? parentType = panel.Parent?.GetType().Name;
                Debug.WriteLine($"HardDetach: Unable to detach panel, parent remains: {parentType}");
            }
        }

        public void Move(DockPanelState d, Dock edge)
        {
            if (d == null || d.DockEdge == edge) return;

            d.DockEdge = edge;

            this.InvalidateMeasure();
        }

        #endregion
    }
}