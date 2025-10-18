using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using PlugHub.Plugin.DockHost.Models;
using PlugHub.Plugin.DockHost.Utilities;

namespace PlugHub.Plugin.DockHost.Controls
{
    public class PanelDragEventArgs(RoutedEvent routedEvent, Interactive source, DockItemState descriptor, PointerEventArgs eventArgs)
        : RoutedEventArgs(routedEvent, source)
    {
        public DockItemState PanelState { get; } = descriptor;
        public PointerEventArgs PointerEvent { get; } = eventArgs;
    }

    public class DockItem : ContentControl
    {
        private Border? dragHandle;
        private Point? dragStart;

        private Grid? buttonPin;
        private Grid? buttonUnpin;
        private Grid? buttonClose;

        #region DockablePanel: Drag Events

        public static readonly RoutedEvent<RoutedEventArgs> DragStartedEvent =
            RoutedEvent.Register<DockItem, RoutedEventArgs>(nameof(DragStarted), RoutingStrategies.Bubble);
        public event EventHandler<RoutedEventArgs> DragStarted
        {
            add => this.AddHandler(DragStartedEvent, value);
            remove => this.RemoveHandler(DragStartedEvent, value);
        }

        public static readonly RoutedEvent<PanelDragEventArgs> DragProgressingEvent =
            RoutedEvent.Register<DockItem, PanelDragEventArgs>(nameof(DragProgressing), RoutingStrategies.Bubble);
        public event EventHandler<PanelDragEventArgs> DragProgressing
        {
            add => this.AddHandler(DragProgressingEvent, value);
            remove => this.RemoveHandler(DragProgressingEvent, value);
        }

        public static readonly RoutedEvent<PanelDragEventArgs> DragCompletedEvent =
            RoutedEvent.Register<DockItem, PanelDragEventArgs>(nameof(DragCompleted), RoutingStrategies.Bubble);
        public event EventHandler<PanelDragEventArgs> DragCompleted
        {
            add => this.AddHandler(DragCompletedEvent, value);
            remove => this.RemoveHandler(DragCompletedEvent, value);
        }

        #endregion

        #region DockablePanel: State Mutation Events

        public static readonly RoutedEvent<RoutedEventArgs> PinPanelEvent =
            RoutedEvent.Register<DockItem, RoutedEventArgs>(nameof(PinPanel), RoutingStrategies.Bubble);
        public event EventHandler<RoutedEventArgs>? PinPanel
        {
            add => this.AddHandler(PinPanelEvent, value);
            remove => this.RemoveHandler(PinPanelEvent, value);
        }

        public static readonly RoutedEvent<RoutedEventArgs> UnpinPanelEvent =
            RoutedEvent.Register<DockItem, RoutedEventArgs>(nameof(UnpinPanel), RoutingStrategies.Bubble);
        public event EventHandler<RoutedEventArgs>? UnpinPanel
        {
            add => this.AddHandler(UnpinPanelEvent, value);
            remove => this.RemoveHandler(UnpinPanelEvent, value);
        }

        public static readonly RoutedEvent<RoutedEventArgs> ClosePanelEvent =
            RoutedEvent.Register<DockItem, RoutedEventArgs>(nameof(ClosePanel), RoutingStrategies.Bubble);
        public event EventHandler<RoutedEventArgs>? ClosePanel
        {
            add => this.AddHandler(ClosePanelEvent, value);
            remove => this.RemoveHandler(ClosePanelEvent, value);
        }

        #endregion

        #region DockablePanel: Chrome Properties

        public static readonly StyledProperty<string> HeaderProperty =
            AvaloniaProperty.Register<DockItem, string>(nameof(Header), "DockablePanel");
        public string Header
        {
            get => this.GetValue(HeaderProperty);
            set => this.SetValue(HeaderProperty, value);
        }

        public static readonly StyledProperty<bool> IsPinnedProperty =
            AvaloniaProperty.Register<DockItem, bool>(nameof(IsPinned), false);
        public bool IsPinned
        {
            get => this.GetValue(IsPinnedProperty);
            set => this.SetValue(IsPinnedProperty, value);
        }

        public static readonly StyledProperty<bool> CanCloseProperty =
            AvaloniaProperty.Register<DockItem, bool>(nameof(CanClose), true);
        public bool CanClose
        {
            get => this.GetValue(CanCloseProperty);
            set => this.SetValue(CanCloseProperty, value);
        }

        #endregion

        #region DockablePanel: Docking Properties

        public static readonly StyledProperty<double> DragThresholdProperty =
            AvaloniaProperty.Register<DockItem, double>(nameof(DragThreshold), 4d);
        public double DragThreshold
        {
            get => this.GetValue(DragThresholdProperty);
            set => this.SetValue(DragThresholdProperty, value);
        }

        public static readonly DirectProperty<DockItem, bool> IsDraggingProperty =
        AvaloniaProperty.RegisterDirect<DockItem, bool>(nameof(IsDragging), o => o.IsDragging, (o, v) => o.IsDragging = v);
        private bool isDragging;
        public bool IsDragging
        {
            get => this.isDragging;
            private set => this.SetAndRaise(IsDraggingProperty, ref this.isDragging, value);
        }

        #endregion

        public DockItem()
        {
            IsPinnedProperty.Changed.AddClassHandler<DockItem>((x, e) => x.OnApplyPseudoClasses(x, e));
            CanCloseProperty.Changed.AddClassHandler<DockItem>((x, e) => x.OnApplyPseudoClasses(x, e));
        }

        #region DockablePanel: Chrome Updates

        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            base.OnApplyTemplate(e);

            if (this.buttonPin != null)
                this.buttonPin.PointerReleased -= this.PathButtonPin_PointerReleased;
            if (this.buttonUnpin != null)
                this.buttonUnpin.PointerReleased -= this.PathButtonUnpin_PointerReleased;
            if (this.buttonClose != null)
                this.buttonClose.PointerReleased -= this.PathButtonClose_PointerReleased;

            this.buttonPin = e.NameScope.Find<Grid>("PART_ButtonPin");
            this.buttonUnpin = e.NameScope.Find<Grid>("PART_ButtonUnpin");
            this.buttonClose = e.NameScope.Find<Grid>("PART_ButtonClose");

            if (this.buttonPin != null)
                AbortableClickHandler.Attach(this.buttonPin, args => this.PathButtonPin_PointerReleased(this.buttonPin, args));

            if (this.buttonUnpin != null)
                AbortableClickHandler.Attach(this.buttonUnpin, args => this.PathButtonUnpin_PointerReleased(this.buttonUnpin, args));

            if (this.buttonClose != null)
                AbortableClickHandler.Attach(this.buttonClose, args => this.PathButtonClose_PointerReleased(this.buttonClose, args));

            this.dragHandle = e.NameScope.Find<Border>("PART_DragHandle");

            if (this.dragHandle != null)
            {
                this.dragHandle.AddHandler(PointerPressedEvent, this.DragHandle_PointerPressed, RoutingStrategies.Bubble, handledEventsToo: true);
                this.dragHandle.AddHandler(PointerMovedEvent, this.DragHandle_PointerMoved, RoutingStrategies.Bubble, handledEventsToo: true);
                this.dragHandle.AddHandler(PointerReleasedEvent, this.DragHandle_PointerReleased, RoutingStrategies.Bubble, handledEventsToo: true);
            }

            this.IsPinned = (this.DataContext as DockItemState)?.IsPinned ?? false;
            this.CanClose = (this.DataContext as DockItemState)?.CanClose ?? false;

            this.OnApplyPseudoClasses(this, null);
        }
        protected virtual void OnApplyPseudoClasses(object? sender, AvaloniaPropertyChangedEventArgs? e)
        {
            this.PseudoClasses.Set(":pinned", this.IsPinned);
            this.PseudoClasses.Set(":unpinned", !this.IsPinned);
            this.PseudoClasses.Set(":closable", this.CanClose);
            this.PseudoClasses.Set(":nonclosable", !this.CanClose);
        }

        #endregion

        #region DockablePanel: Event Handlers

        protected virtual void DragHandle_PointerPressed(object? sender, PointerPressedEventArgs e)
        {
            if (e.GetCurrentPoint(this.dragHandle).Properties.IsLeftButtonPressed)
            {
                this.dragStart = e.GetPosition(this.dragHandle);

                e.Pointer.Capture(this.dragHandle);
            }
        }
        protected virtual void DragHandle_PointerMoved(object? sender, PointerEventArgs e)
        {
            if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            {
                this.IsDragging = false;
                this.dragStart = null;

                return;
            }

            Point pos = e.GetPosition(this);

            if (!this.IsDragging && this.dragStart.HasValue && (Math.Abs(pos.X - this.dragStart.Value.X) > this.DragThreshold || Math.Abs(pos.Y - this.dragStart.Value.Y) > this.DragThreshold))
            {
                this.IsDragging = true;

                this.RaiseEvent(new RoutedEventArgs(DragStartedEvent));
            }

            if (this.IsDragging)
                if (this.DataContext is DockItemState state)
                    this.RaiseEvent(new PanelDragEventArgs(DragProgressingEvent, this, state, e));

        }
        protected virtual void DragHandle_PointerReleased(object? sender, PointerReleasedEventArgs e)
        {
            if (this.DataContext is DockItemState state)
                this.RaiseEvent(new PanelDragEventArgs(DragCompletedEvent, this, state, e));

            this.IsDragging = false;
            this.dragStart = null;

            e.Pointer.Capture(null);
        }

        protected virtual void PathButtonPin_PointerReleased(object? sender, PointerReleasedEventArgs e)
        {
            e.Handled = true;

            this.IsPinned = true;

            this.RaiseEvent(new RoutedEventArgs(PinPanelEvent));
        }
        protected virtual void PathButtonUnpin_PointerReleased(object? sender, PointerReleasedEventArgs e)
        {
            e.Handled = true;

            this.IsPinned = false;

            this.RaiseEvent(new RoutedEventArgs(UnpinPanelEvent));

        }
        protected virtual void PathButtonClose_PointerReleased(object? sender, PointerReleasedEventArgs e)
        {
            e.Handled = true;

            this.RaiseEvent(new RoutedEventArgs(ClosePanelEvent));
        }

        #endregion
    }
}