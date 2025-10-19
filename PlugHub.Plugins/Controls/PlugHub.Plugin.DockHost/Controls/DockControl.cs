using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Interactivity;
using Avalonia.Threading;
using PlugHub.Plugin.Controls.Controls;
using PlugHub.Plugin.DockHost.Interfaces.Services;
using PlugHub.Plugin.DockHost.Models;
using System.Collections.ObjectModel;

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

        public static readonly RoutedEvent<RoutedEventArgs> DockControlReadyEvent =
            RoutedEvent.Register<DockControl, RoutedEventArgs>(nameof(DockControlReady), RoutingStrategies.Bubble);
        public event EventHandler<RoutedEventArgs> DockControlReady
        {
            add => this.AddHandler(DockControlReadyEvent, value);
            remove => this.RemoveHandler(DockControlReadyEvent, value);
        }

        #region DockControl: Internal Members

        private DockHostControlData config;
        private IDockService? dockService;
        private DockCompass? dockCompass;

        private ContentDeck? leftGutter;
        private ContentDeck? topGutter;
        private ContentDeck? rightGutter;
        private ContentDeck? bottomGutter;

        private GridSplitter? leftSplitter;
        private GridSplitter? topSplitter;
        private GridSplitter? rightSplitter;
        private GridSplitter? bottomSplitter;

        private ResizeBox? leftResizePanel;
        private ResizeBox? topResizePanel;
        private ResizeBox? rightResizePanel;
        private ResizeBox? bottomResizePanel;

        public bool IsConstructed { get; private set; } = false;
        public bool IsHydrated { get; private set; } = false;
        public bool IsReady { get; private set; } = true;

        #endregion

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

        public static readonly DirectProperty<DockControl, IList<DockItemState>> DockPanelsProperty =
            AvaloniaProperty.RegisterDirect<DockControl, IList<DockItemState>>(nameof(DockPanels), o => o.DockPanels, (o, v) => o.DockPanels = v);
        private IList<DockItemState> dockPanels = [];
        public IList<DockItemState> DockPanels
        {
            get => this.dockPanels;
            set => this.SetAndRaise(DockPanelsProperty, ref this.dockPanels, value);
        }

        public static readonly StyledProperty<IDataTemplate?> DockControlMissingPanelProperty =
            AvaloniaProperty.Register<DockControl, IDataTemplate?>(nameof(DockControlMissingPanel));
        public IDataTemplate? DockControlMissingPanel
        {
            get => this.GetValue(DockControlMissingPanelProperty);
            set => this.SetValue(DockControlMissingPanelProperty, value);
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

        public ObservableCollection<DockItemState> LeftPinned { get; } = [];
        public ObservableCollection<DockItemState> RightPinned { get; } = [];
        public ObservableCollection<DockItemState> TopPinned { get; } = [];
        public ObservableCollection<DockItemState> BottomPinned { get; } = [];

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
        private Thickness leftGutterMargins = new(0, 32, 0, 32);
        public Thickness LeftGutterMargins
        {
            get => this.leftGutterMargins;
            set => this.SetAndRaise(LeftGutterMarginsProperty, ref this.leftGutterMargins, value);
        }

        public static readonly DirectProperty<DockControl, Thickness> TopGutterMarginsProperty =
            AvaloniaProperty.RegisterDirect<DockControl, Thickness>(nameof(TopGutterMargins), o => o.TopGutterMargins, (o, v) => o.TopGutterMargins = v, new Thickness(0));
        private Thickness topGutterMargins = new(0, 0, 0, 0);
        public Thickness TopGutterMargins
        {
            get => this.topGutterMargins;
            set => this.SetAndRaise(TopGutterMarginsProperty, ref this.topGutterMargins, value);
        }

        public static readonly DirectProperty<DockControl, Thickness> RightGutterMarginsProperty =
            AvaloniaProperty.RegisterDirect<DockControl, Thickness>(nameof(RightGutterMargins), o => o.RightGutterMargins, (o, v) => o.RightGutterMargins = v, new Thickness(0));
        private Thickness rightGutterMargins = new(0, 0, 0, 0);
        public Thickness RightGutterMargins
        {
            get => this.rightGutterMargins;
            set => this.SetAndRaise(RightGutterMarginsProperty, ref this.rightGutterMargins, value);
        }

        public static readonly DirectProperty<DockControl, Thickness> BottomGutterMarginsProperty =
            AvaloniaProperty.RegisterDirect<DockControl, Thickness>(nameof(BottomGutterMargins), o => o.BottomGutterMargins, (o, v) => o.BottomGutterMargins = v, new Thickness(0));
        private Thickness bottomGutterMargins = new(0, 0, 0, 0);
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

        public ObservableCollection<DockItemState> LeftUnpinned { get; } = [];
        public ObservableCollection<DockItemState> RightUnpinned { get; } = [];
        public ObservableCollection<DockItemState> TopUnpinned { get; } = [];
        public ObservableCollection<DockItemState> BottomUnpinned { get; } = [];

        #endregion

        public DockControl()
        {
            this.config = this.NewConfig();

            DockPanelsProperty.Changed.AddClassHandler<DockControl>((s, e) =>
            {
                if (e.NewValue is IList<DockItemState> list && s.IsConstructed)
                    this.ProcessBufferedPanels(list);
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

            this.config.ControlID = this.DockId;

            this.dockCompass = e.NameScope.Find<DockCompass>("PART_DockCompass");
            this.dockCompass?.Hide();

            this.SetupGutters(e);
            this.SetupGutterPanels(e);
            this.SetupGutterPanelsResize(e);
            this.SetupMainGrid(e);
            this.SetupMainGridSplittersResize(e);

            this.IsConstructed = true;

            this.ProcessBufferedPanels(this.DockPanels);

            Dispatcher.UIThread.Post(() =>
            {
                this.NormalizeSlicesBySortOrder();
                this.IsReady = true;
                this.RaiseEvent(new RoutedEventArgs(DockControlReadyEvent));

            }, DispatcherPriority.Background);
        }

        protected virtual void SetupGutters(TemplateAppliedEventArgs e)
        {
            this.leftGutter = e.NameScope.Find<ContentDeck>("PART_LeftGutter");
            this.topGutter = e.NameScope.Find<ContentDeck>("PART_TopGutter");
            this.rightGutter = e.NameScope.Find<ContentDeck>("PART_RightGutter");
            this.bottomGutter = e.NameScope.Find<ContentDeck>("PART_BottomGutter");

            void InitGutter(ContentDeck? gutter, IEnumerable<object> items, Dock edge, Func<Rect, double> dimensionSelector, Func<bool, double> hasItemsSelector)
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

                    if (args is AvaloniaPropertyChangedEventArgs hcp && hcp.Property == ContentDeck.HasItemsProperty && hcp.NewValue is bool has)
                        this.RecalculateMargins(
                            edge == Dock.Left ? hasItemsSelector(has) : this.leftGutter?.Bounds.Width ?? 0,
                            edge == Dock.Right ? hasItemsSelector(has) : this.rightGutter?.Bounds.Width ?? 0,
                            edge == Dock.Top ? hasItemsSelector(has) : this.topGutter?.Bounds.Height ?? 0,
                            edge == Dock.Bottom ? hasItemsSelector(has) : this.bottomGutter?.Bounds.Height ?? 0);

                    if (args is AvaloniaPropertyChangedEventArgs iep && iep.Property == ContentDeck.IsOpenProperty)
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
            ResizeBox? InitPanel(string partName, double configLength)
            {
                ResizeBox? panel = e.NameScope.Find<ResizeBox>(partName);

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
            void OnResizeCompleted(object? sender, EventArgs args)
            {
                if (sender is not ResizeBox panel)
                    return;

                if (panel.IsVisible && panel.ActualPanelSize > 0)
                {
                    if (panel == this.leftResizePanel)
                    {
                        this.LeftFlyoutLength = panel.ActualPanelSize;
                        this.config.LeftFlyoutLength = this.LeftFlyoutLength;
                    }
                    else if (panel == this.topResizePanel)
                    {
                        this.TopFlyoutLength = panel.ActualPanelSize;
                        this.config.TopFlyoutLength = this.TopFlyoutLength;
                    }
                    else if (panel == this.rightResizePanel)
                    {
                        this.RightFlyoutLength = panel.ActualPanelSize;
                        this.config.RightFlyoutLength = this.RightFlyoutLength;
                    }
                    else if (panel == this.bottomResizePanel)
                    {
                        this.BottomFlyoutLength = panel.ActualPanelSize;
                        this.config.BottomFlyoutLength = this.BottomFlyoutLength;
                    }
                }
            }

            if (this.leftResizePanel != null)
            {
                this.leftResizePanel.ResizeCompleted -= OnResizeCompleted;
                this.leftResizePanel.ResizeCompleted += OnResizeCompleted;
            }
            if (this.topResizePanel != null)
            {
                this.topResizePanel.ResizeCompleted -= OnResizeCompleted;
                this.topResizePanel.ResizeCompleted += OnResizeCompleted;
            }
            if (this.rightResizePanel != null)
            {
                this.rightResizePanel.ResizeCompleted -= OnResizeCompleted;
                this.rightResizePanel.ResizeCompleted += OnResizeCompleted;
            }
            if (this.bottomResizePanel != null)
            {
                this.bottomResizePanel.ResizeCompleted -= OnResizeCompleted;
                this.bottomResizePanel.ResizeCompleted += OnResizeCompleted;
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

            this.leftSplitter = e.NameScope.Find<GridSplitter>("PART_LeftGridSplitter");
            this.topSplitter = e.NameScope.Find<GridSplitter>("PART_TopGridSplitter");
            this.rightSplitter = e.NameScope.Find<GridSplitter>("PART_RightGridSplitter");
            this.bottomSplitter = e.NameScope.Find<GridSplitter>("PART_BottomGridSplitter");

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

            if (grid == null) return;

            void OnSplitterDragCompleted(object? sender, EventArgs args)
            {
                if (sender == this.leftSplitter)
                {
                    this.LeftPanelLength = grid.ColumnDefinitions[0].Width;
                    this.config.LeftPanelLength = this.LeftPanelLength.Value;
                }
                else if (sender == this.topSplitter)
                {
                    this.TopPanelLength = grid.RowDefinitions[0].Height;
                    this.config.TopPanelLength = this.TopPanelLength.Value;
                }
                else if (sender == this.rightSplitter)
                {
                    this.RightPanelLength = grid.ColumnDefinitions[4].Width;
                    this.config.RightPanelLength = this.RightPanelLength.Value;
                }
                else if (sender == this.bottomSplitter)
                {
                    this.BottomPanelLength = grid.RowDefinitions[4].Height;
                    this.config.BottomPanelLength = this.BottomPanelLength.Value;
                }
            }

            if (this.leftSplitter != null)
            {
                this.leftSplitter.DragCompleted -= OnSplitterDragCompleted;
                this.leftSplitter.DragCompleted += OnSplitterDragCompleted;
            }

            if (this.topSplitter != null)
            {
                this.topSplitter.DragCompleted -= OnSplitterDragCompleted;
                this.topSplitter.DragCompleted += OnSplitterDragCompleted;
            }

            if (this.rightSplitter != null)
            {
                this.rightSplitter.DragCompleted -= OnSplitterDragCompleted;
                this.rightSplitter.DragCompleted += OnSplitterDragCompleted;
            }

            if (this.bottomSplitter != null)
            {
                this.bottomSplitter.DragCompleted -= OnSplitterDragCompleted;
                this.bottomSplitter.DragCompleted += OnSplitterDragCompleted;
            }
        }

        protected virtual void ProcessBufferedPanels(IList<DockItemState> list)
        {
            if (!this.IsConstructed || this.dockPanels.Count == 0)
                return;

            if (this.IsHydrated == false)
            {
                if (this.config?.DockHostDataItems != null && this.dockService != null)
                {
                    foreach (DockHostPanelData persisted in this.config.DockHostDataItems)
                    {
                        if (list.Any(s => s.ControlId == persisted.ControlID)) continue;

                        this.dockService.RequestPanel(
                            persisted.ControlID,
                            this.DockId,
                            persisted.DescriptorID,
                            persisted.SortOrder,
                            persisted.DockEdge,
                            persisted.IsPinned,
                            canClose: true);
                    }
                }

                this.IsHydrated = true;
            }

            for (int i = list.Count - 1; i >= 0; i--)
            {
                DockItemState state = list[i];

                DockHostPanelData? persisted = this.config?.DockHostDataItems?
                    .FirstOrDefault(d => d.ControlID == state.ControlId);

                if (persisted != null)
                    state.FromConfig(persisted);

                this.AddPanel(state.Normalize(this));
                list.RemoveAt(i);
            }
        }
        protected virtual void NormalizeSlicesBySortOrder()
        {
            void SortByOrder(ObservableCollection<DockItemState> collection)
            {
                List<DockItemState> sorted = [.. collection.OrderBy(p => p.SortOrder)];

                collection.Clear();

                foreach (DockItemState? item in sorted)
                    collection.Add(item);
            }

            SortByOrder(this.LeftPinned);
            SortByOrder(this.RightPinned);
            SortByOrder(this.TopPinned);
            SortByOrder(this.BottomPinned);

            SortByOrder(this.LeftUnpinned);
            SortByOrder(this.RightUnpinned);
            SortByOrder(this.TopUnpinned);
            SortByOrder(this.BottomUnpinned);
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
            Dictionary<Guid, DockHostPanelData> byId =
                this.config.DockHostDataItems.ToDictionary(x => x.ControlID);

            List<DockHostPanelData> orderedDtos = [];

            int order = 0;

            foreach (DockItemState state in this.CollectDockPanelStates())
            {
                if (byId.TryGetValue(state.ControlId, out DockHostPanelData? dto))
                {
                    dto.SortOrder = order++;

                    orderedDtos.Add(dto);
                }
            }

            this.config.DockHostDataItems = orderedDtos;

            return this.config;
        }

        #endregion

        #region DockControl: Event Handlers

        private async void OnPanelPinRequested(object? sender, EventArgs e)
        {
            if (sender is DockItem panel && panel.DataContext is DockItemState state)
                await this.PinPanel(state, true);
        }
        private async void OnPanelUnpinRequested(object? sender, EventArgs e)
        {
            if (sender is DockItem panel && panel.DataContext is DockItemState state)
                await this.PinPanel(state, false);
        }
        private async void OnPanelCloseRequested(object? sender, RoutedEventArgs e)
        {
            if (sender is DockItem panel && panel.DataContext is DockItemState state)
                await this.ClosePanel(state);
        }

        #endregion

        #region DockControl: Drag & Drop

        private void OnDockPanelDragStarted(object? sender, EventArgs e)
        {
            this.dockCompass?.Show();
        }
        private void OnDockPanelDragProgressing(object? sender, PanelDragEventArgs e)
        {
            if (this.dockCompass == null) return;

            Point pos = e.PointerEvent.GetPosition(this.dockCompass);

            this.dockCompass?.UpdatePointer(pos);
        }
        private async void OnDockPanelDragCompleted(object? sender, PanelDragEventArgs e)
        {
            if (this.dockCompass == null) return;

            Dock? update = this.dockCompass.ActiveEdge;

            if (update != null)
                await this.MovePanel(e.PanelState, (Dock)update);

            this.dockCompass.Hide();
        }

        #endregion

        #region DockControl: Panel Upkeep

        public IEnumerable<DockItemState> CollectDockPanelStates()
        {
            if (this.IsConstructed == false)
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
        public void AddPanel(DockItemState state)
        {
            if (this.IsConstructed == false)
            {
                this.dockPanels.Add(state);

                return;
            }

            if (this.config != null)
            {
                DockHostPanelData? existing = this.config.DockHostDataItems.FirstOrDefault(x => x.ControlID == state.ControlId);

                if (existing == null)
                    this.config.DockHostDataItems.Add(state.ToConfig());
                else
                    state.FromConfig(existing);
            }

            Dispatcher.UIThread.Post(() =>
            {
                if (state.Content is DockItem panel)
                    this.HookPanel(panel);

                this.AddToSlice(state);
                this.InvalidateMeasure();

            }, DispatcherPriority.Render);
        }
        public async Task MovePanel(DockItemState state, Dock edge)
        {
            if (this.IsConstructed == false)
            {
                DockItemState? item = this.dockPanels.FirstOrDefault(x => x.ControlId == state.ControlId);

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
        public async Task PinPanel(DockItemState state, bool pinned)
        {
            if (this.IsConstructed == false)
            {
                DockItemState? item = this.dockPanels.FirstOrDefault(x => x.ControlId == state.ControlId);

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
        public async Task ClosePanel(DockItemState state)
        {
            if (this.IsConstructed == false)
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
                if (state.Content is DockItem panel)
                    this.UnhookPanel(panel);

                this.RemoveFromAllSlices(state);
                this.InvalidateMeasure();

            }, DispatcherPriority.Render);
        }

        private void HookPanel(DockItem panel)
        {
            panel.DragStarted -= this.OnDockPanelDragStarted;
            panel.DragStarted += this.OnDockPanelDragStarted;

            panel.DragCompleted -= this.OnDockPanelDragCompleted;
            panel.DragCompleted += this.OnDockPanelDragCompleted;

            panel.DragProgressing -= this.OnDockPanelDragProgressing;
            panel.DragProgressing += this.OnDockPanelDragProgressing;

            panel.RemoveHandler(DockItem.PinPanelEvent, this.OnPanelPinRequested);
            panel.AddHandler(DockItem.PinPanelEvent, this.OnPanelPinRequested);

            panel.RemoveHandler(DockItem.UnpinPanelEvent, this.OnPanelUnpinRequested);
            panel.AddHandler(DockItem.UnpinPanelEvent, this.OnPanelUnpinRequested);

            panel.RemoveHandler(DockItem.ClosePanelEvent, this.OnPanelCloseRequested);
            panel.AddHandler(DockItem.ClosePanelEvent, this.OnPanelCloseRequested);
        }
        private void UnhookPanel(DockItem panel)
        {
            panel.DragStarted -= this.OnDockPanelDragStarted;
            panel.DragCompleted -= this.OnDockPanelDragCompleted;
            panel.DragProgressing -= this.OnDockPanelDragProgressing;

            panel.RemoveHandler(DockItem.PinPanelEvent, this.OnPanelPinRequested);
            panel.RemoveHandler(DockItem.UnpinPanelEvent, this.OnPanelUnpinRequested);
            panel.RemoveHandler(DockItem.ClosePanelEvent, this.OnPanelCloseRequested);
        }

        #endregion

        #region DockControl: Slicing

        private ObservableCollection<DockItemState> GetSlice(Dock edge, bool pinned)
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
        private void AddToSlice(DockItemState d)
        {
            ObservableCollection<DockItemState> slice = this.GetSlice(d.DockEdge, d.IsPinned);

            Dispatcher.UIThread.InvokeAsync(() =>
            {
                slice.Add(d);
                this.RefreshControl();

            }, DispatcherPriority.Render);
        }
        private void Reslice(DockItemState d)
        {
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                this.RemoveFromAllSlices(d);
                this.AddToSlice(d);
                this.RefreshControl();

            }, DispatcherPriority.Render);
        }
        private void RemoveFromAllSlices(DockItemState d)
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
                else if (hasRight)
                    this.LeftGutterMargins = new Thickness(0);
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