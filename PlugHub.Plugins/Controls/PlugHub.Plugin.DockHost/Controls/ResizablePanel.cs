using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;

namespace PlugHub.Plugin.DockHost.Controls
{
    public enum ResizeConstraintMode { Parent, Window, Custom }

    public class ResizablePanel : ContentControl
    {
        private Thumb? thumb;

        public static readonly RoutedEvent<RoutedEventArgs> DragCompleteEvent =
            RoutedEvent.Register<ResizablePanel, RoutedEventArgs>(nameof(DragComplete), RoutingStrategies.Bubble);
        public event EventHandler<RoutedEventArgs> DragComplete
        {
            add => this.AddHandler(DragCompleteEvent, value);
            remove => this.RemoveHandler(DragCompleteEvent, value);
        }

        #region ResizablePanel: Thumb Properties

        public static readonly StyledProperty<double> ThumbThicknessProperty =
            AvaloniaProperty.Register<ResizablePanel, double>(nameof(ThumbThickness), 4);
        public double ThumbThickness
        {
            get => this.GetValue(ThumbThicknessProperty);
            set => this.SetValue(ThumbThicknessProperty, value);
        }

        public static readonly StyledProperty<IBrush?> ThumbBrushProperty =
            AvaloniaProperty.Register<ResizablePanel, IBrush?>(nameof(ThumbBrush));
        public IBrush? ThumbBrush
        {
            get => this.GetValue(ThumbBrushProperty);
            set => this.SetValue(ThumbBrushProperty, value);
        }

        #endregion

        #region ResizablePanel: Placement Properties

        public static readonly StyledProperty<Dock> DockEdgeProperty =
            AvaloniaProperty.Register<ResizablePanel, Dock>(nameof(DockEdge), Dock.Left);
        public Dock DockEdge
        {
            get => this.GetValue(DockEdgeProperty);
            set => this.SetValue(DockEdgeProperty, value);
        }

        #endregion

        #region ResizablePanel: Panel Size Properties

        public static readonly StyledProperty<double> PanelSizeProperty =
            AvaloniaProperty.Register<ResizablePanel, double>(nameof(PanelSize), 300);
        public double PanelSize
        {
            get => this.GetValue(PanelSizeProperty);
            set => this.SetValue(PanelSizeProperty, value);
        }

        public static readonly StyledProperty<double> MinimumSizeProperty =
            AvaloniaProperty.Register<ResizablePanel, double>(nameof(MinimumSize), 2);
        public double MinimumSize
        {
            get => this.GetValue(MinimumSizeProperty);
            set => this.SetValue(MinimumSizeProperty, value);
        }

        public static readonly StyledProperty<double> MaximumSizeProperty =
            AvaloniaProperty.Register<ResizablePanel, double>(nameof(MaximumSize), double.NaN);
        public double MaximumSize
        {
            get => this.GetValue(MaximumSizeProperty);
            set => this.SetValue(MaximumSizeProperty, value);
        }

        public static readonly DirectProperty<ResizablePanel, double> ActualPanelSizeProperty =
            AvaloniaProperty.RegisterDirect<ResizablePanel, double>(nameof(ResizablePanel.ActualPanelSize), (Func<ResizablePanel, double>)(o => o.ActualPanelSize));
        private double actualPanelSize;
        public double ActualPanelSize
        {
            get => this.actualPanelSize;
            private set => this.SetAndRaise(ActualPanelSizeProperty, ref this.actualPanelSize, value);
        }

        #endregion

        #region ResizablePanel: Size Constrtaints Properties

        public static readonly StyledProperty<ResizeConstraintMode> ConstraintModeProperty =
            AvaloniaProperty.Register<ResizablePanel, ResizeConstraintMode>(nameof(ConstraintMode), ResizeConstraintMode.Window);
        public ResizeConstraintMode ConstraintMode
        {
            get => this.GetValue(ConstraintModeProperty);
            set => this.SetValue(ConstraintModeProperty, value);
        }

        public static readonly StyledProperty<Visual?> ConstraintTargetProperty =
            AvaloniaProperty.Register<ResizablePanel, Visual?>(nameof(ConstraintTarget));
        public Visual? ConstraintTarget
        {
            get => this.GetValue(ConstraintTargetProperty);
            set => this.SetValue(ConstraintTargetProperty, value);
        }

        #endregion

        public ResizablePanel()
        {
            PanelSizeProperty.Changed.AddClassHandler<ResizablePanel>((x, e) => x.ApplyPanelSize());
            DockEdgeProperty.Changed.AddClassHandler<ResizablePanel>((x, e) => { x.ApplyPanelSize(); x.ApplyThumbStyle(); });
            ThumbThicknessProperty.Changed.AddClassHandler<ResizablePanel>((x, e) => x.ApplyThumbStyle());
        }

        #region ResizablePanel: Overrides

        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            base.OnApplyTemplate(e);

            if (this.thumb != null)
            {
                this.thumb.DragDelta -= this.Thumb_DragDelta;
                this.thumb.DragCompleted -= this.Thumb_DragCompleted;

            }

            this.thumb = e.NameScope.Find<Thumb>("PART_ResizeThumb");

            if (this.thumb != null)
            {
                this.thumb.DragDelta += this.Thumb_DragDelta;
                this.thumb.DragCompleted += this.Thumb_DragCompleted;

                this.ApplyThumbStyle();
            }

            this.ApplyPanelSize();
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

        #region ResizablePanel: Event Handlers

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
                    this.thumb.Cursor = new Cursor(StandardCursorType.SizeWestEast);
                    this.thumb.Width = this.ThumbThickness;
                    this.thumb.Height = double.NaN;

                    this.thumb.HorizontalAlignment = this.DockEdge == Dock.Left
                        ? HorizontalAlignment.Right
                        : HorizontalAlignment.Left;
                    break;

                case Dock.Top:
                case Dock.Bottom:
                    this.thumb.HorizontalAlignment = HorizontalAlignment.Stretch;
                    this.thumb.Cursor = new Cursor(StandardCursorType.SizeNorthSouth);
                    this.thumb.Height = this.ThumbThickness;
                    this.thumb.Width = double.NaN;

                    this.thumb.VerticalAlignment = this.DockEdge == Dock.Top
                        ? VerticalAlignment.Bottom
                        : VerticalAlignment.Top;
                    break;
            }
        }

        private void Thumb_DragDelta(object? sender, VectorEventArgs e)
        {
            e.Handled = true;

            (Visual surface, Size size)? resolved = this.TryGetConstraintSurface();
            if (resolved is null) return;

            (Visual surface, Size size) = resolved.Value;
            Point pos = this.TranslatePoint(new Point(0, 0), surface) ?? default;

            double min = this.MinimumSize;
            double max = double.IsNaN(this.MaximumSize) ? double.PositiveInfinity : this.MaximumSize;

            double w = double.IsNaN(this.Width) ? this.Bounds.Width : this.Width;
            double h = double.IsNaN(this.Height) ? this.Bounds.Height : this.Height;

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
            this.RaiseEvent(new RoutedEventArgs(DragCompleteEvent));
        }

        #endregion
    }
}