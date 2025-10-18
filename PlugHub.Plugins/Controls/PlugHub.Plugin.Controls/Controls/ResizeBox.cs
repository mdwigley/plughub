using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;

namespace PlugHub.Plugin.Controls.Controls
{
    public enum ResizeConstraintMode { Parent, Window, Custom }

    public class ResizeEventArgs(RoutedEvent routedEvent, double oldSize, double newSize, Orientation orientation)
        : RoutedEventArgs(routedEvent)
    {
        public double OldSize { get; } = oldSize;
        public double NewSize { get; } = newSize;
        public Orientation Orientation { get; } = orientation;
    }
    public class ResizeDeltaEventArgs(RoutedEvent routedEvent, double delta, double oldSize, double newSize, Orientation orientation)
        : RoutedEventArgs(routedEvent)
    {
        public double Delta { get; } = delta;
        public double OldSize { get; } = oldSize;
        public double NewSize { get; } = newSize;
        public Orientation Orientation { get; } = orientation;
    }

    public class ResizeBox : ContentControl
    {
        private Thumb? thumb;

        #region ResizeBox: Event Properites

        public static readonly RoutedEvent<ResizeEventArgs> ResizeStartedEvent =
            RoutedEvent.Register<ResizeBox, ResizeEventArgs>(nameof(ResizeStarted), RoutingStrategies.Bubble);
        public event EventHandler<ResizeEventArgs> ResizeStarted
        {
            add => this.AddHandler(ResizeStartedEvent, value);
            remove => this.RemoveHandler(ResizeStartedEvent, value);
        }

        public static readonly RoutedEvent<ResizeDeltaEventArgs> ResizeDeltaEvent =
            RoutedEvent.Register<ResizeBox, ResizeDeltaEventArgs>(nameof(ResizeDelta), RoutingStrategies.Bubble);
        public event EventHandler<ResizeDeltaEventArgs> ResizeDelta
        {
            add => this.AddHandler(ResizeDeltaEvent, value);
            remove => this.RemoveHandler(ResizeDeltaEvent, value);
        }

        public static readonly RoutedEvent<ResizeEventArgs> ResizeCompletedEvent =
            RoutedEvent.Register<ResizeBox, ResizeEventArgs>(nameof(ResizeCompleted), RoutingStrategies.Bubble);
        public event EventHandler<ResizeEventArgs> ResizeCompleted
        {
            add => this.AddHandler(ResizeCompletedEvent, value);
            remove => this.RemoveHandler(ResizeCompletedEvent, value);
        }

        #endregion

        #region ResizeBox: Thumb Properties

        public static readonly StyledProperty<double> ThumbThicknessProperty =
            AvaloniaProperty.Register<ResizeBox, double>(nameof(ThumbThickness), 4);
        public double ThumbThickness
        {
            get => this.GetValue(ThumbThicknessProperty);
            set => this.SetValue(ThumbThicknessProperty, value);
        }

        public static readonly StyledProperty<IBrush?> ThumbBrushProperty =
            AvaloniaProperty.Register<ResizeBox, IBrush?>(nameof(ThumbBrush));
        public IBrush? ThumbBrush
        {
            get => this.GetValue(ThumbBrushProperty);
            set => this.SetValue(ThumbBrushProperty, value);
        }

        public static readonly StyledProperty<Cursor?> HorizontalResizeCursorProperty =
            AvaloniaProperty.Register<ResizeBox, Cursor?>(nameof(HorizontalResizeCursor), new Cursor(StandardCursorType.SizeWestEast));
        public Cursor? HorizontalResizeCursor
        {
            get => this.GetValue(HorizontalResizeCursorProperty);
            set => this.SetValue(HorizontalResizeCursorProperty, value);
        }

        public static readonly StyledProperty<Cursor?> VerticalResizeCursorProperty =
            AvaloniaProperty.Register<ResizeBox, Cursor?>(nameof(VerticalResizeCursor), new Cursor(StandardCursorType.SizeNorthSouth));
        public Cursor? VerticalResizeCursor
        {
            get => this.GetValue(VerticalResizeCursorProperty);
            set => this.SetValue(VerticalResizeCursorProperty, value);
        }

        public static readonly DirectProperty<ResizeBox, bool> IsResizingProperty =
            AvaloniaProperty.RegisterDirect<ResizeBox, bool>(nameof(IsResizing), o => o.IsResizing);
        private bool isResizing;
        public bool IsResizing
        {
            get => this.isResizing;
            private set => this.SetAndRaise(IsResizingProperty, ref this.isResizing, value);
        }

        #endregion

        #region ResizeBox: Placement Properties

        public static readonly StyledProperty<Dock> DockEdgeProperty =
            AvaloniaProperty.Register<ResizeBox, Dock>(nameof(DockEdge), Dock.Left);
        public Dock DockEdge
        {
            get => this.GetValue(DockEdgeProperty);
            set => this.SetValue(DockEdgeProperty, value);
        }

        public static readonly DirectProperty<ResizeBox, Orientation> OrientationProperty =
            AvaloniaProperty.RegisterDirect<ResizeBox, Orientation>(nameof(Orientation), o => o.Orientation);
        private Orientation orientation;
        public Orientation Orientation
        {
            get => this.orientation;
            private set => this.SetAndRaise(OrientationProperty, ref this.orientation, value);
        }

        #endregion

        #region ResizeBox: Panel Size Properties

        public static readonly StyledProperty<double> PanelSizeProperty =
            AvaloniaProperty.Register<ResizeBox, double>(nameof(PanelSize), 300);
        public double PanelSize
        {
            get => this.GetValue(PanelSizeProperty);
            set => this.SetValue(PanelSizeProperty, value);
        }

        public static readonly StyledProperty<double> MinimumSizeProperty =
            AvaloniaProperty.Register<ResizeBox, double>(nameof(MinimumSize), 2);
        public double MinimumSize
        {
            get => this.GetValue(MinimumSizeProperty);
            set => this.SetValue(MinimumSizeProperty, value);
        }

        public static readonly StyledProperty<double> MaximumSizeProperty =
            AvaloniaProperty.Register<ResizeBox, double>(nameof(MaximumSize), double.NaN);
        public double MaximumSize
        {
            get => this.GetValue(MaximumSizeProperty);
            set => this.SetValue(MaximumSizeProperty, value);
        }

        public static readonly DirectProperty<ResizeBox, double> ActualPanelSizeProperty =
            AvaloniaProperty.RegisterDirect<ResizeBox, double>(nameof(ActualPanelSize), o => o.ActualPanelSize);
        private double actualPanelSize;
        public double ActualPanelSize
        {
            get => this.actualPanelSize;
            private set => this.SetAndRaise(ActualPanelSizeProperty, ref this.actualPanelSize, value);
        }

        #endregion

        #region ResizeBox: Size Constrtaints Properties

        public static readonly StyledProperty<ResizeConstraintMode> ConstraintModeProperty =
            AvaloniaProperty.Register<ResizeBox, ResizeConstraintMode>(nameof(ConstraintMode), ResizeConstraintMode.Window);
        public ResizeConstraintMode ConstraintMode
        {
            get => this.GetValue(ConstraintModeProperty);
            set => this.SetValue(ConstraintModeProperty, value);
        }

        public static readonly StyledProperty<Visual?> ConstraintTargetProperty =
            AvaloniaProperty.Register<ResizeBox, Visual?>(nameof(ConstraintTarget));
        public Visual? ConstraintTarget
        {
            get => this.GetValue(ConstraintTargetProperty);
            set => this.SetValue(ConstraintTargetProperty, value);
        }

        #endregion

        public ResizeBox()
        {
            PanelSizeProperty.Changed.AddClassHandler<ResizeBox>((x, e) => x.ApplyPanelSize());

            DockEdgeProperty.Changed.AddClassHandler<ResizeBox>((x, e) =>
            {
                x.ApplyPanelSize();
                x.ApplyThumbStyle();
                x.ApplyOrientation();
            });

            ThumbThicknessProperty.Changed.AddClassHandler<ResizeBox>((x, e) => x.ApplyThumbStyle());
        }

        #region ResizeBox: Overrides

        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            base.OnApplyTemplate(e);

            if (this.thumb != null)
            {
                this.thumb.DragStarted -= this.Thumb_DragStarted;
                this.thumb.DragDelta -= this.Thumb_DragDelta;
                this.thumb.DragCompleted -= this.Thumb_DragCompleted;
            }

            this.thumb = e.NameScope.Find<Thumb>("PART_ResizeThumb");

            if (this.thumb != null)
            {
                this.thumb.DragStarted += this.Thumb_DragStarted;
                this.thumb.DragDelta += this.Thumb_DragDelta;
                this.thumb.DragCompleted += this.Thumb_DragCompleted;

                this.ApplyThumbStyle();
            }

            this.ApplyPanelSize();
            this.ApplyOrientation();
        }
        protected override void ArrangeCore(Rect finalRect)
        {
            base.ArrangeCore(finalRect);

            double newSize = this.DockEdge is Dock.Left or Dock.Right
                ? this.Bounds.Width
                : this.Bounds.Height;

            this.SetAndRaise(ActualPanelSizeProperty, ref this.actualPanelSize, newSize);
        }

        #endregion

        #region ResizeBox: Event Handlers

        private void ApplyPanelSize()
        {
            switch (this.DockEdge)
            {
                case Dock.Left:
                case Dock.Right:
                    this.Width = this.PanelSize;
                    this.Height = double.NaN;
                    break;
                case Dock.Top:
                case Dock.Bottom:
                    this.Height = this.PanelSize;
                    this.Width = double.NaN;
                    break;
            }
        }
        private void ApplyThumbStyle()
        {
            if (this.thumb == null) return;

            switch (this.DockEdge)
            {
                case Dock.Left:
                case Dock.Right:
                    this.thumb.VerticalAlignment = VerticalAlignment.Stretch;
                    this.thumb.Cursor = this.HorizontalResizeCursor;
                    this.thumb.Width = this.ThumbThickness;
                    this.thumb.Height = double.NaN;

                    this.thumb.HorizontalAlignment = this.DockEdge == Dock.Left
                        ? HorizontalAlignment.Right
                        : HorizontalAlignment.Left;
                    break;

                case Dock.Top:
                case Dock.Bottom:
                    this.thumb.HorizontalAlignment = HorizontalAlignment.Stretch;
                    this.thumb.Cursor = this.VerticalResizeCursor;
                    this.thumb.Height = this.ThumbThickness;
                    this.thumb.Width = double.NaN;

                    this.thumb.VerticalAlignment = this.DockEdge == Dock.Top
                        ? VerticalAlignment.Bottom
                        : VerticalAlignment.Top;
                    break;
            }
        }
        private void ApplyOrientation()
        {
            this.Orientation = this.DockEdge == Dock.Left || this.DockEdge == Dock.Right
                ? Orientation.Vertical
                : Orientation.Horizontal;
        }

        private void Thumb_DragStarted(object? sender, VectorEventArgs e)
        {
            e.Handled = true;

            this.IsResizing = true;

            ResizeEventArgs args = new(
                ResizeStartedEvent,
                oldSize: this.PanelSize,
                newSize: this.PanelSize,
                orientation: this.Orientation);

            this.RaiseEvent(args);
        }
        private void Thumb_DragDelta(object? sender, VectorEventArgs e)
        {
            e.Handled = true;

            (Visual surface, Size size)? resolved = this.TryGetConstraintSurface();
            if (resolved == null) return;

            (Visual surface, Size size) = resolved.Value;
            Point pos = this.TranslatePoint(new Point(0, 0), surface) ?? default;

            double min = this.MinimumSize;
            double max = double.IsNaN(this.MaximumSize) ? double.PositiveInfinity : this.MaximumSize;

            double w = double.IsNaN(this.Width) ? this.Bounds.Width : this.Width;
            double h = double.IsNaN(this.Height) ? this.Bounds.Height : this.Height;

            double oldSize = this.PanelSize;
            double newSize = oldSize;

            switch (this.DockEdge)
            {
                case Dock.Left:
                    {
                        double g = Math.Max(0, size.Width - (pos.X + w));
                        double dx = Math.Clamp(e.Vector.X, -(w - min), Math.Min(g, max - w));
                        this.PanelSize = Math.Clamp(w + dx, min, Math.Min(max, w + g));
                        break;
                    }
                case Dock.Top:
                    {
                        double g = Math.Max(0, size.Height - (pos.Y + h));
                        double dy = Math.Clamp(e.Vector.Y, -(h - min), Math.Min(g, max - h));
                        this.PanelSize = Math.Clamp(h + dy, min, Math.Min(max, h + g));
                        break;
                    }
                case Dock.Right:
                    {
                        double g = Math.Max(0, pos.X);
                        double dx = Math.Clamp(-e.Vector.X, -(w - min), Math.Min(g, max - w));
                        this.PanelSize = Math.Clamp(w + dx, min, Math.Min(max, w + g));
                        break;
                    }
                case Dock.Bottom:
                    {
                        double g = Math.Max(0, pos.Y);
                        double dy = Math.Clamp(-e.Vector.Y, -(h - min), Math.Min(g, max - h));
                        this.PanelSize = Math.Clamp(h + dy, min, Math.Min(max, h + g));
                        break;
                    }
            }

            ResizeDeltaEventArgs args = new(
                ResizeDeltaEvent,
                delta: newSize - oldSize,
                oldSize: oldSize,
                newSize: newSize,
                orientation: this.Orientation);

            this.RaiseEvent(args);
        }
        private (Visual surface, Size size)? TryGetConstraintSurface()
        {
            if (this.ConstraintMode == ResizeConstraintMode.Window)
            {
                TopLevel? tl = TopLevel.GetTopLevel(this);

                if (tl != null)
                    return (tl, tl.ClientSize);
            }
            else if (this.ConstraintMode == ResizeConstraintMode.Custom)
            {
                if (this.ConstraintTarget != null)
                    return (this.ConstraintTarget, this.ConstraintTarget.Bounds.Size);
            }

            if (this.Parent is Visual p)
            {
                return (p, p.Bounds.Size);
            }

            return null;
        }
        private void Thumb_DragCompleted(object? sender, VectorEventArgs e)
        {
            e.Handled = true;

            this.IsResizing = false;

            ResizeEventArgs args = new(
                ResizeCompletedEvent,
                oldSize: this.PanelSize,
                newSize: this.PanelSize,
                orientation: this.Orientation);

            this.RaiseEvent(args);
        }

        #endregion
    }
}