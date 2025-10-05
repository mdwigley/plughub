using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using PlugHub.Plugin.DockHost.Models;
using System.ComponentModel;

namespace PlugHub.Plugin.DockHost.Controls
{
    public class PanelDragEventArgs(DockPanelState descriptor, PointerEventArgs @event) : EventArgs
    {
        public DockPanelState Descriptor { get; } = descriptor;
        public PointerEventArgs PointerEvent { get; } = @event;
    }

    public class DockablePanel : ContentControl
    {
        private Border? buttonPin;
        private Border? buttonUnpin;
        private Border? buttonClose;

        private Border? dragHandle;
        private Point? dragStart;
        private bool isDragging;

        public event EventHandler<EventArgs>? DragStarted;
        public event EventHandler<PanelDragEventArgs>? DragProgressing;
        public event EventHandler<PanelDragEventArgs>? DragCompleted;

        public static readonly RoutedEvent<RoutedEventArgs> CloseRequestedEvent =
            RoutedEvent.Register<DockablePanel, RoutedEventArgs>(nameof(CloseRequested), RoutingStrategies.Bubble);
        public event EventHandler<RoutedEventArgs>? CloseRequested
        {
            add => this.AddHandler(CloseRequestedEvent, value);
            remove => this.RemoveHandler(CloseRequestedEvent, value);
        }

        #region DockablePanel: Panel State Properties

        public static readonly StyledProperty<DockPanelState?> PanelStateProperty =
            AvaloniaProperty.Register<DockablePanel, DockPanelState?>(nameof(PanelState));
        public DockPanelState? PanelState
        {
            get => this.GetValue(PanelStateProperty);
            set => this.SetValue(PanelStateProperty, value);
        }

        #endregion

        public static readonly StyledProperty<double> DragThresholdProperty =
            AvaloniaProperty.Register<DockablePanel, double>(nameof(DragThreshold), 4d);
        public double DragThreshold
        {
            get => this.GetValue(DragThresholdProperty);
            set => this.SetValue(DragThresholdProperty, value);
        }

        #region DockablePanel: Header Properties

        public static readonly StyledProperty<string> HeaderProperty =
            AvaloniaProperty.Register<DockablePanel, string>(nameof(Header), "Dock Panel");
        public string Header
        {
            get => this.GetValue(HeaderProperty);
            set => this.SetValue(HeaderProperty, value);
        }

        public static readonly StyledProperty<bool> IsPinnedProperty =
            AvaloniaProperty.Register<DockablePanel, bool>(nameof(IsPinned), false);
        public bool IsPinned
        {
            get => this.GetValue(IsPinnedProperty);
            set => this.SetValue(IsPinnedProperty, value);
        }

        #endregion

        static DockablePanel()
        {
            IsPinnedProperty.Changed.AddClassHandler<DockablePanel>((x, _) =>
            {
                x.PseudoClasses.Set(":pinned", x.IsPinned);
                x.PseudoClasses.Set(":unpinned", !x.IsPinned);

                if (x.PanelState != null && x.PanelState.IsPinned != x.IsPinned)
                    x.PanelState.IsPinned = x.IsPinned;
            });

            PanelStateProperty.Changed.AddClassHandler<DockablePanel>((x, e) =>
            {
                if (e.OldValue is DockPanelState oldState)
                    oldState.PropertyChanged -= x.PanelState_PropertyChanged;

                if (e.NewValue is DockPanelState newState)
                    newState.PropertyChanged += x.PanelState_PropertyChanged;

                x.PanelState_UpdatePanel();
            });
        }

        private void PanelState_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (this.PanelState == null)
                return;

            switch (e.PropertyName)
            {
                case nameof(DockPanelState.IsPinned):
                    this.PanelState_UpdatePanel();
                    break;
            }
        }
        public void PanelState_UpdatePanel()
        {
            if (this.PanelState == null)
                return;

            this.IsPinned = this.PanelState.IsPinned;
        }

        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            base.OnApplyTemplate(e);

            if (this.buttonPin != null) this.buttonPin.PointerReleased -= this.PathButtonPin_PointerReleased;
            if (this.buttonUnpin != null) this.buttonUnpin.PointerReleased -= this.PathButtonUnpin_PointerReleased;
            if (this.buttonClose != null) this.buttonClose.PointerReleased -= this.PathButtonClose_PointerReleased;
            if (this.dragHandle != null) this.dragHandle.PointerPressed -= this.DragHandle_PointerPressed;

            this.buttonPin = e.NameScope.Find<Border>("PART_ButtonPin");
            this.buttonUnpin = e.NameScope.Find<Border>("PART_ButtonUnpin");
            this.buttonClose = e.NameScope.Find<Border>("PART_ButtonClose");
            this.dragHandle = e.NameScope.Find<Border>("PART_DragHandle");

            if (this.buttonPin != null) this.buttonPin.PointerReleased += this.PathButtonPin_PointerReleased;
            if (this.buttonUnpin != null) this.buttonUnpin.PointerReleased += this.PathButtonUnpin_PointerReleased;
            if (this.buttonClose != null) this.buttonClose.PointerReleased += this.PathButtonClose_PointerReleased;

            this.dragHandle?.AddHandler(InputElement.PointerPressedEvent, this.DragHandle_PointerPressed, RoutingStrategies.Bubble, handledEventsToo: true);
            this.dragHandle?.AddHandler(InputElement.PointerMovedEvent, this.DragHandle_PointerMoved, RoutingStrategies.Bubble, handledEventsToo: true);
            this.dragHandle?.AddHandler(InputElement.PointerReleasedEvent, this.DragHandle_PointerReleased, RoutingStrategies.Bubble, handledEventsToo: true);

            this.PanelState_UpdatePanel();
        }

        protected virtual void DragHandle_PointerPressed(object? sender, PointerPressedEventArgs e)
        {
            if (this.PanelState is null)
                return;

            if (e.GetCurrentPoint(this.dragHandle).Properties.IsLeftButtonPressed)
            {
                this.dragStart = e.GetPosition(this.dragHandle);

                e.Pointer.Capture(this.dragHandle);
            }
        }
        protected virtual void DragHandle_PointerMoved(object? sender, PointerEventArgs e)
        {
            if (this.PanelState is null)
                return;

            if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            {
                this.isDragging = false;
                this.dragStart = null;
                return;
            }

            Point pos = e.GetPosition(this);

            if (!this.isDragging && this.dragStart.HasValue && (Math.Abs(pos.X - this.dragStart.Value.X) > this.DragThreshold || Math.Abs(pos.Y - this.dragStart.Value.Y) > this.DragThreshold))
            {
                this.isDragging = true;
                DragStarted?.Invoke(this, EventArgs.Empty);
            }

            if (this.isDragging)
                DragProgressing?.Invoke(this, new PanelDragEventArgs(this.PanelState, e));
        }
        protected virtual void DragHandle_PointerReleased(object? sender, PointerReleasedEventArgs e)
        {
            if (this.PanelState != null)
                DragCompleted?.Invoke(this, new PanelDragEventArgs(this.PanelState, e));

            this.isDragging = false;
            this.dragStart = null;
            e.Pointer.Capture(null);
        }

        protected virtual void PathButtonPin_PointerReleased(object? sender, PointerReleasedEventArgs e)
        {
            e.Handled = true;

            this.IsPinned = true;
        }
        protected virtual void PathButtonUnpin_PointerReleased(object? sender, PointerReleasedEventArgs e)
        {
            e.Handled = true;

            this.IsPinned = false;
        }
        protected virtual void PathButtonClose_PointerReleased(object? sender, PointerReleasedEventArgs e)
        {
            e.Handled = true;

            this.RaiseEvent(new RoutedEventArgs(CloseRequestedEvent));
        }
    }
}