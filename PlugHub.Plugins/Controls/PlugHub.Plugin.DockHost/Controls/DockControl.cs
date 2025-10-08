using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Threading;
using Microsoft.VisualBasic;
using PlugHub.Plugin.DockHost.Interfaces.Services;
using PlugHub.Plugin.DockHost.Models;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace PlugHub.Plugin.DockHost.Controls
{
    public class DockControl : ContentControl
    {
        public const double DEFAULT_PANEL_SIZE = 300;

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

        private DockHostControlData config;
        private IDockService? dockService;
        private Grid? dropTargetsGrid;

        private Border? leftDropTarget;
        private Border? topDropTarget;
        private Border? rightDropTarget;
        private Border? bottomDropTarget;

        private ContentSwitcher? leftGutter;
        private ContentSwitcher? topGutter;
        private ContentSwitcher? rightGutter;
        private ContentSwitcher? bottomGutter;

        private ResizablePanel? leftResizePanel;
        private ResizablePanel? topResizePanel;
        private ResizablePanel? rightResizePanel;
        private ResizablePanel? bottomResizePanel;

        private bool isReady = false;

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

        public static readonly DirectProperty<DockControl, IList<DockPanelState>> DockPanelsProperty =
            AvaloniaProperty.RegisterDirect<DockControl, IList<DockPanelState>>(nameof(DockPanels), o => o.DockPanels, (o, v) => o.DockPanels = v);
        private IList<DockPanelState> dockPanels = [];
        public IList<DockPanelState> DockPanels
        {
            get => this.dockPanels;
            set => this.SetAndRaise(DockPanelsProperty, ref this.dockPanels, value);
        }

        #endregion

        #region DockControl: Pinned Panel Properties

        public static readonly StyledProperty<GridLength> LeftPanelLengthProperty =
            AvaloniaProperty.Register<DockControl, GridLength>(nameof(LeftPanelLength), new GridLength(DEFAULT_PANEL_SIZE));
        public GridLength LeftPanelLength
        {
            get => this.GetValue(LeftPanelLengthProperty);
            set => this.SetValue(LeftPanelLengthProperty, value);
        }

        public static readonly StyledProperty<GridLength> TopPanelLengthProperty =
            AvaloniaProperty.Register<DockControl, GridLength>(nameof(TopPanelLength), new GridLength(DEFAULT_PANEL_SIZE));
        public GridLength TopPanelLength
        {
            get => this.GetValue(TopPanelLengthProperty);
            set => this.SetValue(TopPanelLengthProperty, value);
        }

        public static readonly StyledProperty<GridLength> RightPanelLengthProperty =
            AvaloniaProperty.Register<DockControl, GridLength>(nameof(RightPanelLength), new GridLength(DEFAULT_PANEL_SIZE));
        public GridLength RightPanelLength
        {
            get => this.GetValue(RightPanelLengthProperty);
            set => this.SetValue(RightPanelLengthProperty, value);
        }

        public static readonly StyledProperty<GridLength> BottomPanelLengthProperty =
            AvaloniaProperty.Register<DockControl, GridLength>(nameof(BottomPanelLength), new GridLength(DEFAULT_PANEL_SIZE));
        public GridLength BottomPanelLength
        {
            get => this.GetValue(BottomPanelLengthProperty);
            set => this.SetValue(BottomPanelLengthProperty, value);
        }

        // *************************************************************** //

        public static readonly DirectProperty<DockControl, bool> HasLeftContentProperty =
            AvaloniaProperty.RegisterDirect<DockControl, bool>(nameof(HasLeftContent), o => o.HasLeftContent);
        public bool HasLeftContent => this.LeftPinned.Count > 0;

        public static readonly DirectProperty<DockControl, bool> HasTopContentProperty =
            AvaloniaProperty.RegisterDirect<DockControl, bool>(nameof(HasTopContent), o => o.HasTopContent);
        public bool HasTopContent => this.TopPinned.Count > 0;

        public static readonly DirectProperty<DockControl, bool> HasRightContentProperty =
            AvaloniaProperty.RegisterDirect<DockControl, bool>(nameof(HasRightContent), o => o.HasRightContent);
        public bool HasRightContent => this.RightPinned.Count > 0;

        public static readonly DirectProperty<DockControl, bool> HasBottomContentProperty =
            AvaloniaProperty.RegisterDirect<DockControl, bool>(nameof(HasBottomContent), o => o.HasBottomContent);
        public bool HasBottomContent => this.BottomPinned.Count > 0;

        // *************************************************************** //

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

        #endregion

        #region DockControl: Pinned Collection Definitions

        public ObservableCollection<DockPanelState> LeftPinned { get; } = [];
        public ObservableCollection<DockPanelState> RightPinned { get; } = [];
        public ObservableCollection<DockPanelState> TopPinned { get; } = [];
        public ObservableCollection<DockPanelState> BottomPinned { get; } = [];

        #endregion

        #region DockControl: Unpinned Panel Properties

        public static readonly StyledProperty<double> LeftFlyoutLengthProperty =
            AvaloniaProperty.Register<DockControl, double>(nameof(LeftFlyoutLength), DEFAULT_PANEL_SIZE);
        public double LeftFlyoutLength
        {
            get => this.GetValue(LeftFlyoutLengthProperty);
            set => this.SetValue(LeftFlyoutLengthProperty, value);
        }

        public static readonly StyledProperty<double> TopFlyoutLengthProperty =
            AvaloniaProperty.Register<DockControl, double>(nameof(TopFlyoutLength), DEFAULT_PANEL_SIZE);
        public double TopFlyoutLength
        {
            get => this.GetValue(TopFlyoutLengthProperty);
            set => this.SetValue(TopFlyoutLengthProperty, value);
        }

        public static readonly StyledProperty<double> RightFlyoutLengthProperty =
            AvaloniaProperty.Register<DockControl, double>(nameof(RightFlyoutLength), DEFAULT_PANEL_SIZE);
        public double RightFlyoutLength
        {
            get => this.GetValue(RightFlyoutLengthProperty);
            set => this.SetValue(RightFlyoutLengthProperty, value);
        }

        public static readonly StyledProperty<double> BottomFlyoutLengthProperty =
            AvaloniaProperty.Register<DockControl, double>(nameof(BottomFlyoutLength), DEFAULT_PANEL_SIZE);
        public double BottomFlyoutLength
        {
            get => this.GetValue(BottomFlyoutLengthProperty);
            set => this.SetValue(BottomFlyoutLengthProperty, value);
        }

        // *************************************************************** //

        public static readonly DirectProperty<DockControl, Thickness> LeftGutterMarginsProperty =
            AvaloniaProperty.RegisterDirect<DockControl, Thickness>(nameof(LeftGutterMargins), o => o.LeftGutterMargins, (o, v) => o.LeftGutterMargins = v, new Thickness(0));
        private Thickness leftGutterMargins = new Thickness(0, 32, 0, 32);
        public Thickness LeftGutterMargins
        {
            get => this.leftGutterMargins;
            set => this.SetAndRaise(LeftGutterMarginsProperty, ref this.leftGutterMargins, value);
        }

        public static readonly DirectProperty<DockControl, Thickness> TopGutterMarginsProperty =
            AvaloniaProperty.RegisterDirect<DockControl, Thickness>(nameof(TopGutterMargins), o => o.TopGutterMargins, (o, v) => o.TopGutterMargins = v, new Thickness(0));
        private Thickness topGutterMargins = new Thickness(0, 0, 0, 0);
        public Thickness TopGutterMargins
        {
            get => this.topGutterMargins;
            set => this.SetAndRaise(TopGutterMarginsProperty, ref this.topGutterMargins, value);
        }

        public static readonly DirectProperty<DockControl, Thickness> RightGutterMarginsProperty =
            AvaloniaProperty.RegisterDirect<DockControl, Thickness>(nameof(RightGutterMargins), o => o.RightGutterMargins, (o, v) => o.RightGutterMargins = v, new Thickness(0));
        private Thickness rightGutterMargins = new Thickness(0, 0, 0, 0);
        public Thickness RightGutterMargins
        {
            get => this.rightGutterMargins;
            set => this.SetAndRaise(RightGutterMarginsProperty, ref this.rightGutterMargins, value);
        }

        public static readonly DirectProperty<DockControl, Thickness> BottomGutterMarginsProperty =
            AvaloniaProperty.RegisterDirect<DockControl, Thickness>(nameof(BottomGutterMargins), o => o.BottomGutterMargins, (o, v) => o.BottomGutterMargins = v, new Thickness(0));
        private Thickness bottomGutterMargins = new Thickness(0, 0, 0, 0);
        public Thickness BottomGutterMargins
        {
            get => this.bottomGutterMargins;
            set => this.SetAndRaise(BottomGutterMarginsProperty, ref this.bottomGutterMargins, value);
        }

        // *************************************************************** //

        public static readonly DirectProperty<DockControl, Thickness> LeftFlyoutMarginProperty =
            AvaloniaProperty.RegisterDirect<DockControl, Thickness>(nameof(LeftFlyoutMargin), o => o.LeftFlyoutMargin);
        private Thickness leftFlyoutMargin;
        public Thickness LeftFlyoutMargin
        {
            get => this.leftFlyoutMargin;
            private set => this.SetAndRaise(LeftFlyoutMarginProperty, ref this.leftFlyoutMargin, value);
        }

        public static readonly DirectProperty<DockControl, Thickness> TopFlyoutMarginProperty =
            AvaloniaProperty.RegisterDirect<DockControl, Thickness>(nameof(TopFlyoutMargin), o => o.TopFlyoutMargin);
        private Thickness topFlyoutMargin;
        public Thickness TopFlyoutMargin
        {
            get => this.topFlyoutMargin;
            private set => this.SetAndRaise(TopFlyoutMarginProperty, ref this.topFlyoutMargin, value);
        }

        public static readonly DirectProperty<DockControl, Thickness> RightFlyoutMarginProperty =
            AvaloniaProperty.RegisterDirect<DockControl, Thickness>(nameof(RightFlyoutMargin), o => o.RightFlyoutMargin);
        private Thickness rightFlyoutMargin;
        public Thickness RightFlyoutMargin
        {
            get => this.rightFlyoutMargin;
            private set => this.SetAndRaise(RightFlyoutMarginProperty, ref this.rightFlyoutMargin, value);
        }

        public static readonly DirectProperty<DockControl, Thickness> BottomFlyoutMarginProperty =
            AvaloniaProperty.RegisterDirect<DockControl, Thickness>(nameof(BottomFlyoutMargin), o => o.BottomFlyoutMargin);
        private Thickness bottomFlyoutMargin;
        public Thickness BottomFlyoutMargin
        {
            get => this.bottomFlyoutMargin;
            private set => this.SetAndRaise(BottomFlyoutMarginProperty, ref this.bottomFlyoutMargin, value);
        }

        // *************************************************************** //

        public static readonly DirectProperty<DockControl, bool> HasOpenFlyoutProperty =
            AvaloniaProperty.RegisterDirect<DockControl, bool>(nameof(HasOpenFlyout), o => o.HasOpenFlyout);
        private bool hasOpenFlyout;
        public bool HasOpenFlyout
        {
            get => this.hasOpenFlyout;
            private set => this.SetAndRaise(HasOpenFlyoutProperty, ref this.hasOpenFlyout, value);
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
            this.config = this.NewConfig();

            DockPanelsProperty.Changed.AddClassHandler<DockControl>((s, e) =>
            {
                if (e.NewValue is IList<DockPanelState> list && s.isReady)
                {
                    foreach (var state in list.ToList())
                    {
                        s.AddPanel(state.Normalize(this));
                        s.dockPanels.Remove(state);
                    }
                }
            });

            DockServiceProperty.Changed.AddClassHandler<DockControl>((s, e) =>
            {
                if (e.NewValue is IDockService service)
                {
                    this.dockService = service;

                    DockHostControlData? replace = this.dockService.RegisterDockControl(this);

                    if (replace != null) this.config = replace;
                }
            });

            this.DetachedFromVisualTree += (s, e) =>
            {
                if (this.dockService == null) return;

                this.dockService.SaveDockControl(this);
            };
        }

        #region DockControl: Lifecycle 

        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            base.OnApplyTemplate(e);

            this.SetupGutters(e);
            this.SetupGutterPanels(e);
            this.SetupGutterPanelsResize(e);
            this.SetupMainGrid(e);
            this.SetupMainGridSplittersResize(e);
            this.SetupDropTargets(e);

            this.isReady = true;

            this.ProcessBufferedPanels();
        }

        protected virtual void SetupGutters(TemplateAppliedEventArgs e)
        {
            this.leftGutter = e.NameScope.Find<ContentSwitcher>("PART_LeftGutter");
            this.topGutter = e.NameScope.Find<ContentSwitcher>("PART_TopGutter");
            this.rightGutter = e.NameScope.Find<ContentSwitcher>("PART_RightGutter");
            this.bottomGutter = e.NameScope.Find<ContentSwitcher>("PART_BottomGutter");

            void InitGutter(ContentSwitcher? gutter, IEnumerable<object> items, Dock edge, Func<Rect, double> dimensionSelector, Func<bool, double> hasItemsSelector)
            {
                if (gutter == null) return;

                gutter.ItemsSource = items;
                gutter.DockEdge = edge;

                gutter.PropertyChanged += (_, args) =>
                {
                    if (args.Property == BoundsProperty && args.NewValue is Rect rect)
                        this.RecalculateMargins(
                            this.leftGutter?.Bounds.Width ?? 0,
                            this.rightGutter?.Bounds.Width ?? 0,
                            this.topGutter?.Bounds.Height ?? 0,
                            this.bottomGutter?.Bounds.Height ?? 0);

                    if (args is AvaloniaPropertyChangedEventArgs hcp && hcp.Property == ContentSwitcher.HasItemsProperty && hcp.NewValue is bool has)
                        this.RecalculateMargins(
                            edge == Dock.Left ? hasItemsSelector(has) : this.leftGutter?.Bounds.Width ?? 0,
                            edge == Dock.Right ? hasItemsSelector(has) : this.rightGutter?.Bounds.Width ?? 0,
                            edge == Dock.Top ? hasItemsSelector(has) : this.topGutter?.Bounds.Height ?? 0,
                            edge == Dock.Bottom ? hasItemsSelector(has) : this.bottomGutter?.Bounds.Height ?? 0);

                    if (args is AvaloniaPropertyChangedEventArgs iep && iep.Property == ContentSwitcher.IsOpenProperty)
                        this.UpdateHasOpenFlyout();
                };
            }

            InitGutter(this.leftGutter, this.LeftUnpinned, Dock.Left, r => r.Width, has => has ? this.leftGutter!.Bounds.Width : 0);
            InitGutter(this.topGutter, this.TopUnpinned, Dock.Top, r => r.Height, has => has ? this.topGutter!.Bounds.Height : 0);
            InitGutter(this.rightGutter, this.RightUnpinned, Dock.Right, r => r.Width, has => has ? this.rightGutter!.Bounds.Width : 0);
            InitGutter(this.bottomGutter, this.BottomUnpinned, Dock.Bottom, r => r.Height, has => has ? this.bottomGutter!.Bounds.Height : 0);
        }
        protected virtual void SetupGutterPanels(TemplateAppliedEventArgs e)
        {
            ResizablePanel? InitPanel(string partName, double configLength)
            {
                ResizablePanel? panel = e.NameScope.Find<ResizablePanel>(partName);

                if (panel == null) return null;

                panel.PanelSize = configLength;

                return panel;
            }

            this.leftResizePanel = InitPanel("PART_LeftFlyoutPanel", this.config.LeftFlyoutLength);
            this.topResizePanel = InitPanel("PART_TopFlyoutPanel", this.config.TopFlyoutLength);
            this.rightResizePanel = InitPanel("PART_RightFlyoutPanel", this.config.RightFlyoutLength);
            this.bottomResizePanel = InitPanel("PART_BottomFlyoutPanel", this.config.BottomFlyoutLength);
        }
        protected virtual void SetupGutterPanelsResize(TemplateAppliedEventArgs e)
        {
            ResizablePanel?[] panels = [this.leftResizePanel, this.topResizePanel, this.rightResizePanel, this.bottomResizePanel];

            foreach (ResizablePanel? panel in panels.Where(p => p != null))
            {
                panel!.ResizeCompleted += (_, __) =>
                {
                    this.LeftFlyoutLength = this.leftResizePanel?.ActualPanelSize ?? DEFAULT_PANEL_SIZE;
                    this.TopFlyoutLength = this.topResizePanel?.ActualPanelSize ?? DEFAULT_PANEL_SIZE;
                    this.RightFlyoutLength = this.rightResizePanel?.ActualPanelSize ?? DEFAULT_PANEL_SIZE;
                    this.BottomFlyoutLength = this.bottomResizePanel?.ActualPanelSize ?? DEFAULT_PANEL_SIZE;

                    this.config.LeftFlyoutLength = this.LeftFlyoutLength;
                    this.config.TopFlyoutLength = this.TopFlyoutLength;
                    this.config.RightFlyoutLength = this.RightFlyoutLength;
                    this.config.BottomFlyoutLength = this.BottomFlyoutLength;
                };
            }
        }
        protected virtual void SetupMainGrid(TemplateAppliedEventArgs e)
        {
            Grid? grid = e.NameScope.Find<Grid>("PART_RootGrid");

            if (grid == null) return;

            grid.ColumnDefinitions[0].Width = new GridLength(this.config.LeftPanelLength);
            grid.RowDefinitions[0].Height = new GridLength(this.config.TopPanelLength);
            grid.ColumnDefinitions[4].Width = new GridLength(this.config.RightPanelLength);
            grid.RowDefinitions[4].Height = new GridLength(this.config.BottomPanelLength);

            PropertyChanged += (_, args) =>
            {
                switch (args.Property)
                {
                    case AvaloniaProperty p when p == LeftPanelLengthProperty:
                        grid.ColumnDefinitions[0].Width = this.LeftPanelLength;
                        break;
                    case AvaloniaProperty p when p == TopPanelLengthProperty:
                        grid.RowDefinitions[0].Height = this.TopPanelLength;
                        break;
                    case AvaloniaProperty p when p == RightPanelLengthProperty:
                        grid.ColumnDefinitions[4].Width = this.RightPanelLength;
                        break;
                    case AvaloniaProperty p when p == BottomPanelLengthProperty:
                        grid.RowDefinitions[4].Height = this.BottomPanelLength;
                        break;
                }
            };

            this.RefreshControl();
        }
        protected virtual void SetupMainGridSplittersResize(TemplateAppliedEventArgs e)
        {
            Grid? grid = e.NameScope.Find<Grid>("PART_RootGrid");

            if (grid is null) return;

            foreach (GridSplitter splitter in grid.GetLogicalDescendants().OfType<GridSplitter>())
            {
                splitter.DragCompleted += (_, __) =>
                {
                    this.LeftPanelLength = grid.ColumnDefinitions[0].Width;
                    this.TopPanelLength = grid.RowDefinitions[0].Height;
                    this.RightPanelLength = grid.ColumnDefinitions[4].Width;
                    this.BottomPanelLength = grid.RowDefinitions[4].Height;

                    this.config.LeftPanelLength = this.LeftPanelLength.Value;
                    this.config.TopPanelLength = this.TopPanelLength.Value;
                    this.config.RightPanelLength = this.RightPanelLength.Value;
                    this.config.BottomPanelLength = this.BottomPanelLength.Value;
                };
            }
        }
        protected virtual void SetupDropTargets(TemplateAppliedEventArgs e)
        {
            this.dropTargetsGrid = e.NameScope.Find<Grid>("PART_DropTargetsGrid");

            this.topDropTarget = e.NameScope.Find<Border>("PART_TopDropTarget");
            this.bottomDropTarget = e.NameScope.Find<Border>("PART_BottomDropTarget");
            this.leftDropTarget = e.NameScope.Find<Border>("PART_LeftDropTarget");
            this.rightDropTarget = e.NameScope.Find<Border>("PART_RightDropTarget");
        }

        protected virtual void ProcessBufferedPanels()
        {
            if (this.isReady == false || this.dockPanels.Count == 0)
                return;

            foreach (DockPanelState state in this.dockPanels)
                this.AddPanel(state.Normalize(this));

            this.dockPanels.Clear();
        }

        private DockHostControlData NewConfig()
        {
            return new DockHostControlData
            {
                ControlID = this.DockId,
                LeftFlyoutLength = this.LeftFlyoutLength,
                TopFlyoutLength = this.TopFlyoutLength,
                RightFlyoutLength = this.RightFlyoutLength,
                BottomFlyoutLength = this.BottomFlyoutLength,

                LeftPanelLength = this.LeftPanelLength.Value,
                TopPanelLength = this.TopPanelLength.Value,
                RightPanelLength = this.RightPanelLength.Value,
                BottomPanelLength = this.BottomPanelLength.Value
            };
        }
        public virtual DockHostControlData? ToConfig()
        {
            Dictionary<Guid, DockHostPanelData> byId = this.config.DockHostDataItems.ToDictionary(x => x.ControlID);
            List<DockHostPanelData> orderedDtos = [];

            foreach (DockPanelState state in this.CollectDockPanelStates())
                if (byId.TryGetValue(state.ControlId, out DockHostPanelData? dto))
                    orderedDtos.Add(dto);

            foreach (DockHostPanelData dto in this.config.DockHostDataItems)
                if (!orderedDtos.Contains(dto))
                    orderedDtos.Add(dto);

            this.config.DockHostDataItems = orderedDtos;

            return this.config;
        }

        #endregion

        #region DockControl: Event Handlers

        private async void OnPanelPinRequested(object? sender, EventArgs e)
        {
            if (sender is DockablePanel panel && panel.DataContext is DockPanelState state)
                await this.PinPanel(state, true);
        }
        private async void OnPanelUnpinRequested(object? sender, EventArgs e)
        {
            if (sender is DockablePanel panel && panel.DataContext is DockPanelState state)
                await this.PinPanel(state, false);
        }
        private async void OnPanelCloseRequested(object? sender, RoutedEventArgs e)
        {
            if (sender is DockablePanel panel && panel.DataContext is DockPanelState state)
                await this.ClosePanel(state);
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
        private async void OnDockPanelDragCompleted(object? sender, PanelDragEventArgs e)
        {
            this.SetDropTargetsVisible(false);

            Point pos = e.PointerEvent.GetPosition(this.dropTargetsGrid);

            if (this.topDropTarget!.Bounds.Contains(pos))
                await this.MovePanel(e.PanelState, Dock.Top);
            else if (this.leftDropTarget!.Bounds.Contains(pos))
                await this.MovePanel(e.PanelState, Dock.Left);
            else if (this.rightDropTarget!.Bounds.Contains(pos))
                await this.MovePanel(e.PanelState, Dock.Right);
            else if (this.bottomDropTarget!.Bounds.Contains(pos))
                await this.MovePanel(e.PanelState, Dock.Bottom);
        }

        #endregion

        #region DockControl: Panel Upkeep

        public IEnumerable<DockPanelState> CollectDockPanelStates()
        {
            if (this.isReady == false)
                return [.. this.dockPanels];

            return
            [.. this.LeftPinned,
             .. this.RightPinned,
             .. this.TopPinned,
             .. this.BottomPinned,
             .. this.LeftUnpinned,
             .. this.RightUnpinned,
             .. this.TopUnpinned,
             .. this.BottomUnpinned];
        }
        public void AddPanel(DockPanelState state)
        {
            if (this.isReady == false)
            {
                this.dockPanels.Add(state);

                return;
            }

            this.config?.DockHostDataItems.Add(state.ToConfig());

            Dispatcher.UIThread.Post(() =>
            {
                if (state.Content is DockablePanel panel)
                    this.HookPanel(panel);

                this.AddToSlice(state);
                this.InvalidateMeasure();

            }, DispatcherPriority.Render);
        }
        public async Task MovePanel(DockPanelState state, Dock edge)
        {
            if (this.isReady == false)
            {
                DockPanelState? item = this.dockPanels.FirstOrDefault(x => x.ControlId == state.ControlId);

                if (item != null)
                    item.DockEdge = edge;

                return;
            }

            if (!this.config.DockHostDataItems.Any(x => x.ControlID == state.ControlId))
                return;

            state.DockEdge = edge;

            DockHostPanelData? dto = this.config.DockHostDataItems.FirstOrDefault(x => x.ControlID == state.ControlId);

            if (dto != null) dto.DockEdge = edge;

            if (state.Content != null)
                await HardDetachAsync(state.Content);

            Dispatcher.UIThread.Post(() =>
            {
                this.Reslice(state);
                this.InvalidateMeasure();

            }, DispatcherPriority.Render);
        }
        public async Task PinPanel(DockPanelState state, bool pinned)
        {
            if (this.isReady == false)
            {
                DockPanelState? item = this.dockPanels.FirstOrDefault(x => x.ControlId == state.ControlId);

                if (item != null)
                    item.IsPinned = pinned;

                return;
            }

            if (!this.config.DockHostDataItems.Any(x => x.ControlID == state.ControlId))
                return;

            state.IsPinned = pinned;

            DockHostPanelData? dto = this.config.DockHostDataItems.FirstOrDefault(x => x.ControlID == state.ControlId);

            if (dto != null) dto.IsPinned = pinned;

            if (state.Content != null)
                await HardDetachAsync(state.Content);

            Dispatcher.UIThread.Post(() =>
            {
                this.Reslice(state);
                this.InvalidateMeasure();

            }, DispatcherPriority.Render);
        }
        public async Task ClosePanel(DockPanelState state)
        {
            if (this.isReady == false)
            {
                for (int i = this.dockPanels.Count - 1; i >= 0; i--)
                    if (this.dockPanels[i].ControlId == state.ControlId)
                        this.dockPanels.RemoveAt(i);
                return;
            }

            if (!this.config.DockHostDataItems.Any(x => x.ControlID == state.ControlId))
                return;

            DockHostPanelData? dto = this.config.DockHostDataItems.FirstOrDefault(x => x.ControlID == state.ControlId);

            if (dto != null) this.config.DockHostDataItems.Remove(dto);

            if (state.Content != null)
                await HardDetachAsync(state.Content);

            Dispatcher.UIThread.Post(() =>
            {
                if (state.Content is DockablePanel panel)
                    this.UnhookPanel(panel);

                this.RemoveFromAllSlices(state);
                this.InvalidateMeasure();

            }, DispatcherPriority.Render);
        }

        private void HookPanel(DockablePanel panel)
        {
            panel.DragStarted -= this.OnDockPanelDragStarted;
            panel.DragStarted += this.OnDockPanelDragStarted;

            panel.DragCompleted -= this.OnDockPanelDragCompleted;
            panel.DragCompleted += this.OnDockPanelDragCompleted;

            panel.DragProgressing -= this.OnDockPanelDragProgressing;
            panel.DragProgressing += this.OnDockPanelDragProgressing;

            panel.RemoveHandler(DockablePanel.PinPanelEvent, this.OnPanelPinRequested);
            panel.AddHandler(DockablePanel.PinPanelEvent, this.OnPanelPinRequested);

            panel.RemoveHandler(DockablePanel.UnpinPanelEvent, this.OnPanelUnpinRequested);
            panel.AddHandler(DockablePanel.UnpinPanelEvent, this.OnPanelUnpinRequested);

            panel.RemoveHandler(DockablePanel.ClosePanelEvent, this.OnPanelCloseRequested);
            panel.AddHandler(DockablePanel.ClosePanelEvent, this.OnPanelCloseRequested);
        }
        private void UnhookPanel(DockablePanel panel)
        {
            panel.DragStarted -= this.OnDockPanelDragStarted;
            panel.DragCompleted -= this.OnDockPanelDragCompleted;
            panel.DragProgressing -= this.OnDockPanelDragProgressing;

            panel.RemoveHandler(DockablePanel.PinPanelEvent, this.OnPanelPinRequested);
            panel.RemoveHandler(DockablePanel.UnpinPanelEvent, this.OnPanelUnpinRequested);
            panel.RemoveHandler(DockablePanel.ClosePanelEvent, this.OnPanelCloseRequested);
        }

        #endregion

        #region DockControl: Slicing

        private ObservableCollection<DockPanelState> GetSlice(Dock edge, bool pinned)
        {
            switch (edge, pinned)
            {
                case (Dock.Left, true): return this.LeftPinned;
                case (Dock.Left, false): return this.LeftUnpinned;
                case (Dock.Right, true): return this.RightPinned;
                case (Dock.Right, false): return this.RightUnpinned;
                case (Dock.Top, true): return this.TopPinned;
                case (Dock.Top, false): return this.TopUnpinned;
                case (Dock.Bottom, true): return this.BottomPinned;
                case (Dock.Bottom, false): return this.BottomUnpinned;
            }
            throw new InvalidOperationException("Unknown edge/pin combination");
        }
        private void AddToSlice(DockPanelState d)
        {
            ObservableCollection<DockPanelState> slice = this.GetSlice(d.DockEdge, d.IsPinned);

            Dispatcher.UIThread.InvokeAsync(() =>
            {
                slice.Add(d);
                this.RefreshControl();

            }, DispatcherPriority.Render);
        }
        private void Reslice(DockPanelState d)
        {
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                this.RemoveFromAllSlices(d);
                this.AddToSlice(d);
                this.RefreshControl();

            }, DispatcherPriority.Render);
        }
        private void RemoveFromAllSlices(DockPanelState d)
        {
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                this.LeftUnpinned.Remove(d);
                this.RightUnpinned.Remove(d);
                this.TopUnpinned.Remove(d);
                this.BottomUnpinned.Remove(d);

                this.LeftPinned.Remove(d);
                this.RightPinned.Remove(d);
                this.TopPinned.Remove(d);
                this.BottomPinned.Remove(d);

                this.RefreshControl();

            }, DispatcherPriority.Render);
        }
        private void RefreshControl()
        {
            this.LeftPanelLength = this.HasLeftContent
                ? new GridLength(this.config.LeftPanelLength)
                : new GridLength(0);

            this.TopPanelLength = this.HasTopContent
                ? new GridLength(this.config.TopPanelLength)
                : new GridLength(0);

            this.RightPanelLength = this.HasRightContent
                ? new GridLength(this.config.RightPanelLength)
                : new GridLength(0);

            this.BottomPanelLength = this.HasBottomContent
                ? new GridLength(this.config.BottomPanelLength)
                : new GridLength(0);

            this.RaisePropertyChanged(HasLeftContentProperty, !this.HasLeftContent, this.HasLeftContent);
            this.RaisePropertyChanged(HasRightContentProperty, !this.HasRightContent, this.HasRightContent);
            this.RaisePropertyChanged(HasTopContentProperty, !this.HasTopContent, this.HasTopContent);
            this.RaisePropertyChanged(HasBottomContentProperty, !this.HasBottomContent, this.HasBottomContent);
        }

        #endregion

        #region DockControl: Layout

        private void UpdateHasOpenFlyout()
        {
            this.HasOpenFlyout =
                (this.leftGutter?.IsOpen ?? false) ||
                (this.rightGutter?.IsOpen ?? false) ||
                (this.topGutter?.IsOpen ?? false) ||
                (this.bottomGutter?.IsOpen ?? false);
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

            this.RightGutterMargins = new Thickness(0);
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
                }
                panel.InvalidateMeasure();
                panel.InvalidateArrange();

                await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.Render);
            }

            if (panel.Parent != null)
            {
                string? parentType = panel.Parent?.GetType().Name;
            }
        }

        #endregion
    }
}