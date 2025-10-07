using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Threading;
using PlugHub.Plugin.DockHost.Interfaces.Services;
using PlugHub.Plugin.DockHost.Models;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace PlugHub.Plugin.DockHost.Controls
{
    public class DockControl : ContentControl
    {
        public const int DEFAULT_PANEL_SIZE = 300;

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

        private DockHostControlData? config;
        private IDockService? dockService;
        private Grid? dropTargetsGrid;

        private Border? leftDropTarget;
        private Border? topDropTarget;
        private Border? rightDropTarget;
        private Border? bottomDropTarget;

        private DockGutter? leftGutter;
        private DockGutter? topGutter;
        private DockGutter? rightGutter;
        private DockGutter? bottomGutter;

        private ResizablePanel? leftResizePanel;
        private ResizablePanel? topResizePanel;
        private ResizablePanel? rightResizePanel;
        private ResizablePanel? bottomResizePanel;


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

        public static readonly StyledProperty<double> LeftFlyoutLengthProperty =
            AvaloniaProperty.Register<DockControl, double>(nameof(LeftFlyoutLength), DEFAULT_PANEL_SIZE);
        public double LeftFlyoutLength
        {
            get => this.GetValue(LeftFlyoutLengthProperty);
            set => this.SetValue(LeftFlyoutLengthProperty, value);
        }

        public static readonly StyledProperty<double> RightFlyoutLengthProperty =
            AvaloniaProperty.Register<DockControl, double>(nameof(RightFlyoutLength), DEFAULT_PANEL_SIZE);
        public double RightFlyoutLength
        {
            get => this.GetValue(RightFlyoutLengthProperty);
            set => this.SetValue(RightFlyoutLengthProperty, value);
        }

        public static readonly StyledProperty<double> TopFlyoutLengthProperty =
            AvaloniaProperty.Register<DockControl, double>(nameof(TopFlyoutLength), DEFAULT_PANEL_SIZE);
        public double TopFlyoutLength
        {
            get => this.GetValue(TopFlyoutLengthProperty);
            set => this.SetValue(TopFlyoutLengthProperty, value);
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

        // *************************************************************** //

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

        // *************************************************************** //

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
                if (this.dockService == null && e.NewValue is IDockService service)
                {
                    this.dockService = service;
                    this.config = this.dockService.RegisterDockControl(this);
                    this.config.ControlID = this.DockId;
                }
            });

            this.DetachedFromVisualTree += (s, e) =>
            {
                if (this.dockService == null) return;

                this.dockService.SaveDockControl(this);
            };

            this.LeftGutterMargins = new Thickness(0, 32, 0, 32);
            this.RightGutterMargins = new Thickness(0, 0, 0, 0);
            this.TopGutterMargins = new Thickness(0, 0, 0, 0);
            this.BottomGutterMargins = new Thickness(0, 0, 0, 0);
        }


        #region DockControl: Lifecycle 

        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            base.OnApplyTemplate(e);

            this.SetupConfig(e);
            this.SetupExistingStates(e);
            this.SetupMissingStates(e);
            this.SetupGutters(e);
            this.SetupGutterPanels(e);
            this.SetupGutterPanelsResize(e);
            this.SetupMainGrid(e);
            this.SetupMainGridSplittersResize(e);
            this.SetupDropTargets(e);
        }

        protected virtual void SetupConfig(TemplateAppliedEventArgs e)
        {
            if (this.config is null)
                return;

            if (this.config.LeftFlyoutLength <= 0) this.config.LeftFlyoutLength = this.LeftFlyoutLength;
            if (this.config.TopFlyoutLength <= 0) this.config.TopFlyoutLength = this.TopFlyoutLength;
            if (this.config.RightFlyoutLength <= 0) this.config.RightFlyoutLength = this.RightFlyoutLength;
            if (this.config.BottomFlyoutLength <= 0) this.config.BottomFlyoutLength = this.BottomFlyoutLength;

            if (this.config.LeftPanelLength <= 0) this.config.LeftPanelLength = this.LeftPanelLength.Value;
            if (this.config.TopPanelLength <= 0) this.config.TopPanelLength = this.TopPanelLength.Value;
            if (this.config.RightPanelLength <= 0) this.config.RightPanelLength = this.RightPanelLength.Value;
            if (this.config.BottomPanelLength <= 0) this.config.BottomPanelLength = this.BottomPanelLength.Value;
        }
        protected virtual void SetupExistingStates(TemplateAppliedEventArgs e)
        {
            List<DockPanelState> currentStates = [.. this.CollectDockPanelStates()];

            if (this.config == null) return;

            foreach (DockHostPanelData dto in this.config.DockHostDataItems)
            {
                DockPanelState? state = currentStates.FirstOrDefault(s => s.ControlId == dto.ControlID);

                if (state == null) continue;

                if (state.DockEdge != dto.DockEdge) state.DockEdge = dto.DockEdge;
                if (state.IsPinned != dto.IsPinned) state.IsPinned = dto.IsPinned;

                ObservableCollection<DockPanelState> slice = this.GetSlice(dto.DockEdge, dto.IsPinned);

                if (!slice.Contains(state))
                {
                    slice.Add(state);
                }
                else
                {
                    int currentIndex = slice.IndexOf(state);
                    int desiredIndex = this.config.DockHostDataItems.TakeWhile(x => x != dto).Count(x => x.DockEdge == dto.DockEdge && x.IsPinned == dto.IsPinned);

                    if (currentIndex != desiredIndex)
                        slice.Move(currentIndex, desiredIndex);
                }
            }
        }
        protected virtual void SetupMissingStates(TemplateAppliedEventArgs e)
        {
            if (this.config == null) return;

            List<DockPanelState> currentStates = [.. this.CollectDockPanelStates()];

            foreach (DockPanelState? state in currentStates)
            {
                if (!this.config.DockHostDataItems.Any(x => x.ControlID == state.ControlId))
                {
                    this.config.DockHostDataItems.Add(state.ToConfig());

                    ObservableCollection<DockPanelState> slice = this.GetSlice(state.DockEdge, state.IsPinned);

                    if (!slice.Contains(state)) slice.Add(state);
                }
            }
        }
        protected virtual void SetupGutters(TemplateAppliedEventArgs e)
        {
            if (this.config == null)
                return;

            this.leftGutter = e.NameScope.Find<DockGutter>("PART_LeftGutter");
            this.topGutter = e.NameScope.Find<DockGutter>("PART_TopGutter");
            this.rightGutter = e.NameScope.Find<DockGutter>("PART_RightGutter");
            this.bottomGutter = e.NameScope.Find<DockGutter>("PART_BottomGutter");

            if (this.leftGutter != null)
            {
                this.leftGutter.ItemsSource = this.LeftUnpinned;
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
            if (this.topGutter != null)
            {
                this.topGutter.ItemsSource = this.TopUnpinned;
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
            if (this.rightGutter != null)
            {
                this.rightGutter.ItemsSource = this.RightUnpinned;
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
            if (this.bottomGutter != null)
            {
                this.bottomGutter.ItemsSource = this.BottomUnpinned;
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
        protected virtual void SetupGutterPanels(TemplateAppliedEventArgs e)
        {
            if (this.config == null)
                return;

            this.leftResizePanel = e.NameScope.Find<ResizablePanel>("PART_LeftFlyoutPanel");
            this.topResizePanel = e.NameScope.Find<ResizablePanel>("PART_TopFlyoutPanel");
            this.rightResizePanel = e.NameScope.Find<ResizablePanel>("PART_RightFlyoutPanel");
            this.bottomResizePanel = e.NameScope.Find<ResizablePanel>("PART_BottomFlyoutPanel");

            if (this.leftResizePanel != null)
                this.leftResizePanel.PanelSize = this.config.LeftFlyoutLength;

            if (this.topResizePanel != null)
                this.topResizePanel.PanelSize = this.config.TopFlyoutLength;

            if (this.rightResizePanel != null)
                this.rightResizePanel.PanelSize = this.config.RightFlyoutLength;

            if (this.bottomResizePanel != null)
                this.bottomResizePanel.PanelSize = this.config.BottomFlyoutLength;

            this.LeftFlyoutLength = this.leftResizePanel?.ActualPanelSize ?? DEFAULT_PANEL_SIZE;
            this.TopFlyoutLength = this.topResizePanel?.ActualPanelSize ?? DEFAULT_PANEL_SIZE;
            this.RightFlyoutLength = this.rightResizePanel?.ActualPanelSize ?? DEFAULT_PANEL_SIZE;
            this.BottomFlyoutLength = this.bottomResizePanel?.ActualPanelSize ?? DEFAULT_PANEL_SIZE;

            PropertyChanged += (_, args) =>
            {
                switch (args.Property)
                {
                    case AvaloniaProperty p when p == LeftFlyoutLengthProperty:
                        if (this.leftResizePanel != null)
                            this.leftResizePanel.PanelSize = this.LeftFlyoutLength;
                        break;
                    case AvaloniaProperty p when p == TopFlyoutLengthProperty:
                        if (this.topResizePanel != null)
                            this.topResizePanel.PanelSize = this.TopFlyoutLength;
                        break;
                    case AvaloniaProperty p when p == RightFlyoutLengthProperty:
                        if (this.rightResizePanel != null)
                            this.rightResizePanel.PanelSize = this.RightFlyoutLength;
                        break;
                    case AvaloniaProperty p when p == BottomFlyoutLengthProperty:
                        if (this.bottomResizePanel != null)
                            this.bottomResizePanel.PanelSize = this.BottomFlyoutLength;
                        break;
                }
            };
        }
        protected virtual void SetupGutterPanelsResize(TemplateAppliedEventArgs e)
        {
            ResizablePanel?[] panels = [this.leftResizePanel, this.topResizePanel, this.rightResizePanel, this.bottomResizePanel];

            foreach (ResizablePanel? panel in panels.Where(p => p != null))
            {
                panel!.DragComplete += (_, __) =>
                {
                    if (this.config == null) return;

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

            if (grid == null || this.config == null) return;

            grid.ColumnDefinitions[0].Width = new GridLength(this.config.LeftPanelLength);
            grid.RowDefinitions[0].Height = new GridLength(this.config.TopPanelLength);
            grid.ColumnDefinitions[4].Width = new GridLength(this.config.RightPanelLength);
            grid.RowDefinitions[4].Height = new GridLength(this.config.BottomPanelLength);

            this.LeftPanelLength = grid.ColumnDefinitions[0].Width;
            this.TopPanelLength = grid.RowDefinitions[0].Height;
            this.RightPanelLength = grid.ColumnDefinitions[4].Width;
            this.BottomPanelLength = grid.RowDefinitions[4].Height;

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
        }
        protected virtual void SetupMainGridSplittersResize(TemplateAppliedEventArgs e)
        {
            Grid? grid = e.NameScope.Find<Grid>("PART_RootGrid");

            if (grid is null || this.config is null) return;

            foreach (GridSplitter splitter in grid.GetLogicalDescendants().OfType<GridSplitter>())
            {
                splitter.DragCompleted += (_, __) =>
                {
                    if (this.config == null) return;

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

        public virtual DockHostControlData? ToConfig()
        {
            if (this.config == null)
                return null;

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

        private async void OnPanelStatePropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (sender is not DockPanelState state)
                return;

            if (e.PropertyName == nameof(DockPanelState.IsPinned) ||
                e.PropertyName == nameof(DockPanelState.DockEdge))
            {
                if (state.DockablePanel != null)
                    await HardDetachAsync(state.DockablePanel);

                DockHostPanelData? match = this.config?.DockHostDataItems.FirstOrDefault(x => x.ControlID == state.ControlId);

                if (match != null)
                {
                    match.IsPinned = state.IsPinned;
                    match.DockEdge = state.DockEdge;
                }

                this.Reslice(state);
            }
        }

        private void OnPanelCloseRequested(object? sender, RoutedEventArgs e)
        {
            if (sender is DockablePanel panel && panel.PanelState is DockPanelState state)
                this.ClosePanel(state);
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
        private void OnDockPanelDragCompleted(object? sender, PanelDragEventArgs e)
        {
            this.SetDropTargetsVisible(false);

            Point pos = e.PointerEvent.GetPosition(this.dropTargetsGrid);

            if (this.topDropTarget!.Bounds.Contains(pos))
                this.MovePanel(e.Descriptor, Dock.Top);
            else if (this.leftDropTarget!.Bounds.Contains(pos))
                this.MovePanel(e.Descriptor, Dock.Left);
            else if (this.rightDropTarget!.Bounds.Contains(pos))
                this.MovePanel(e.Descriptor, Dock.Right);
            else if (this.bottomDropTarget!.Bounds.Contains(pos))
                this.MovePanel(e.Descriptor, Dock.Bottom);
        }

        #endregion

        #region DockControl: Panel Upkeep

        public IEnumerable<DockPanelState> CollectDockPanelStates()
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
        public void AddPanel(DockPanelState state, Dock? edge = null, bool? pinned = null)
        {
            this.AddToSlice(state);

            if (edge.HasValue)
                state.DockEdge = edge.Value;

            if (pinned.HasValue)
                state.IsPinned = pinned.Value;

            state.PropertyChanged -= this.OnPanelStatePropertyChanged;
            state.PropertyChanged += this.OnPanelStatePropertyChanged;

            if (state.DockablePanel is { } panel)
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

            this.config?.DockHostDataItems.Add(state.ToConfig());
        }
        public void MovePanel(DockPanelState state, Dock edge)
        {
            if (state == null || state.DockEdge == edge)
                return;

            if (this.config == null || !this.config.DockHostDataItems.Any(x => x.ControlID == state.ControlId))
                return;

            state.DockEdge = edge;

            DockHostPanelData? dto = this.config.DockHostDataItems.FirstOrDefault(x => x.ControlID == state.ControlId);

            if (dto != null) dto.DockEdge = edge;

            this.InvalidateMeasure();
        }
        public void PinPanel(DockPanelState state, bool pinned)
        {
            if (state == null) return;

            if (this.config == null || !this.config.DockHostDataItems.Any(x => x.ControlID == state.ControlId))
                return;

            state.IsPinned = pinned;

            DockHostPanelData? dto = this.config.DockHostDataItems.FirstOrDefault(x => x.ControlID == state.ControlId);

            if (dto != null) dto.IsPinned = pinned;

            this.InvalidateMeasure();
        }
        public void ClosePanel(DockPanelState state)
        {
            if (state == null) return;

            if (this.config == null || !this.config.DockHostDataItems.Any(x => x.ControlID == state.ControlId))
                return;

            this.RemoveFromAllSlices(state);

            state.PropertyChanged -= this.OnPanelStatePropertyChanged;

            if (state.DockablePanel is not null)
            {
                state.DockablePanel.DragStarted -= this.OnDockPanelDragStarted;
                state.DockablePanel.DragCompleted -= this.OnDockPanelDragCompleted;
                state.DockablePanel.DragProgressing -= this.OnDockPanelDragProgressing;
                state.DockablePanel.RemoveHandler(DockablePanel.CloseRequestedEvent, this.OnPanelCloseRequested);
            }

            DockHostPanelData? dto = this.config.DockHostDataItems.FirstOrDefault(x => x.ControlID == state.ControlId);

            if (dto != null) this.config.DockHostDataItems.Remove(dto);

            this.InvalidateMeasure();
        }

        #endregion

        #region DockControl: Slicing

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

            this.RaiseHasFlags();
        }
        private void Reslice(DockPanelState d)
        {
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                this.RemoveFromAllSlices(d);
                this.AddToSlice(d);
                this.RaiseHasFlags();
            }, DispatcherPriority.Render);
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

            this.RaiseHasFlags();
        }
        private void RaiseHasFlags()
        {
            if (this.config == null) return;

            this.RaisePropertyChanged(HasLeftContentProperty, !this.HasLeftContent, this.HasLeftContent);
            this.RaisePropertyChanged(HasRightContentProperty, !this.HasRightContent, this.HasRightContent);
            this.RaisePropertyChanged(HasTopContentProperty, !this.HasTopContent, this.HasTopContent);
            this.RaisePropertyChanged(HasBottomContentProperty, !this.HasBottomContent, this.HasBottomContent);

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
        }

        #endregion

        #region DockControl: Layout

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