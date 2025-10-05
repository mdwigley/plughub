using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Layout;

namespace PlugHub.Plugin.DockHost.Controls
{
    public enum ResizeConstraintMode { Parent, Window, Custom }

    public class ResizablePanel : ContentControl
    {
        private Thumb? thumb;


        private static double CurrentWidth(Control c) =>
            double.IsNaN(c.Width) ? c.Bounds.Width : c.Width;
        private static double CurrentHeight(Control c) =>
            double.IsNaN(c.Height) ? c.Bounds.Height : c.Height;


        public static readonly StyledProperty<double> ThumbWidthProperty =
            AvaloniaProperty.Register<ResizablePanel, double>(nameof(ThumbWidth), 4);
        public double ThumbWidth
        {
            get => this.GetValue(ThumbWidthProperty);
            set => this.SetValue(ThumbWidthProperty, value);
        }

        public static readonly StyledProperty<Dock> DockEdgeProperty =
            AvaloniaProperty.Register<ResizablePanel, Dock>(nameof(DockEdge), Dock.Left);
        public Dock DockEdge
        {
            get => this.GetValue(DockEdgeProperty);
            set => this.SetValue(DockEdgeProperty, value);
        }

        public static readonly StyledProperty<double> PanelSizeProperty =
            AvaloniaProperty.Register<ResizablePanel, double>(nameof(PanelSize), 150.0);
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

        public ResizablePanel()
        {
            PanelSizeProperty.Changed.AddClassHandler<ResizablePanel>((x, e) => x.ApplyPanelSize());
            DockEdgeProperty.Changed.AddClassHandler<ResizablePanel>((x, e) => { x.ApplyPanelSize(); x.ApplyThumbStyle(); });
            ThumbWidthProperty.Changed.AddClassHandler<ResizablePanel>((x, e) => x.ApplyThumbStyle());
        }

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
                    this.thumb.HorizontalAlignment = HorizontalAlignment.Right;
                    this.thumb.VerticalAlignment = VerticalAlignment.Stretch;
                    this.thumb.Cursor = new Cursor(StandardCursorType.SizeWestEast);
                    this.thumb.Width = this.ThumbWidth;
                    this.thumb.Height = double.NaN;
                    break;
                case Dock.Right:
                    this.thumb.HorizontalAlignment = HorizontalAlignment.Left;
                    this.thumb.VerticalAlignment = VerticalAlignment.Stretch;
                    this.thumb.Cursor = new Cursor(StandardCursorType.SizeWestEast);
                    this.thumb.Width = this.ThumbWidth;
                    this.thumb.Height = double.NaN;
                    break;
                case Dock.Top:
                    this.thumb.VerticalAlignment = VerticalAlignment.Bottom;
                    this.thumb.HorizontalAlignment = HorizontalAlignment.Stretch;
                    this.thumb.Cursor = new Cursor(StandardCursorType.SizeNorthSouth);
                    this.thumb.Height = this.ThumbWidth;
                    this.thumb.Width = double.NaN;
                    break;
                case Dock.Bottom:
                    this.thumb.VerticalAlignment = VerticalAlignment.Top;
                    this.thumb.HorizontalAlignment = HorizontalAlignment.Stretch;
                    this.thumb.Cursor = new Cursor(StandardCursorType.SizeNorthSouth);
                    this.thumb.Height = this.ThumbWidth;
                    this.thumb.Width = double.NaN;
                    break;
            }
        }

        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            base.OnApplyTemplate(e);

            if (this.thumb != null)
                this.thumb.DragDelta -= this.Thumb_DragDelta;

            this.thumb = e.NameScope.Find<Thumb>("PART_ResizeThumb");

            if (this.thumb != null)
            {
                this.thumb.DragDelta += this.Thumb_DragDelta;

                switch (this.DockEdge)
                {
                    case Dock.Left:
                        this.thumb.HorizontalAlignment = HorizontalAlignment.Right;
                        this.thumb.VerticalAlignment = VerticalAlignment.Stretch;
                        this.thumb.Cursor = new Cursor(StandardCursorType.SizeWestEast);
                        this.thumb.Width = this.ThumbWidth;
                        this.thumb.Height = double.NaN;
                        break;
                    case Dock.Right:
                        this.thumb.HorizontalAlignment = HorizontalAlignment.Left;
                        this.thumb.VerticalAlignment = VerticalAlignment.Stretch;
                        this.thumb.Cursor = new Cursor(StandardCursorType.SizeWestEast);
                        this.thumb.Width = this.ThumbWidth;
                        this.thumb.Height = double.NaN;
                        break;
                    case Dock.Top:
                        this.thumb.VerticalAlignment = VerticalAlignment.Bottom;
                        this.thumb.HorizontalAlignment = HorizontalAlignment.Stretch;
                        this.thumb.Cursor = new Cursor(StandardCursorType.SizeNorthSouth);
                        this.thumb.Height = this.ThumbWidth;
                        this.thumb.Width = double.NaN;
                        break;
                    case Dock.Bottom:
                        this.thumb.VerticalAlignment = VerticalAlignment.Top;
                        this.thumb.HorizontalAlignment = HorizontalAlignment.Stretch;
                        this.thumb.Cursor = new Cursor(StandardCursorType.SizeNorthSouth);
                        this.thumb.Height = this.ThumbWidth;
                        this.thumb.Width = double.NaN;
                        break;
                }
            }

            this.ApplyPanelSize();
        }

        private void Thumb_DragDelta(object? sender, VectorEventArgs e)
        {
            e.Handled = true;

            (Visual surface, Size size)? resolved = this.TryGetConstraintSurface();
            if (resolved is null) return;

            (Visual surface, Size size) = resolved.Value;
            Point pos = this.TranslatePoint(new Point(0, 0), surface) ?? default;

            double min = this.MinimumSize;
            double maxConfigured = double.IsNaN(this.MaximumSize) ? double.PositiveInfinity : this.MaximumSize;

            double w = CurrentWidth(this);
            double h = CurrentHeight(this);

            switch (this.DockEdge)
            {
                case Dock.Left:
                    {
                        double rightGap = Math.Max(0, size.Width - (pos.X + w));
                        double dx = Math.Clamp(e.Vector.X, -(w - min), Math.Min(rightGap, maxConfigured - w));
                        this.PanelSize = Math.Clamp(w + dx, min, Math.Min(maxConfigured, w + rightGap));
                        break;
                    }
                case Dock.Right:
                    {
                        double leftGap = Math.Max(0, pos.X);
                        double dx = Math.Clamp(-e.Vector.X, -(w - min), Math.Min(leftGap, maxConfigured - w));
                        this.PanelSize = Math.Clamp(w + dx, min, Math.Min(maxConfigured, w + leftGap));
                        break;
                    }
                case Dock.Top:
                    {
                        double bottomGap = Math.Max(0, size.Height - (pos.Y + h));
                        double dy = Math.Clamp(e.Vector.Y, -(h - min), Math.Min(bottomGap, maxConfigured - h));
                        this.PanelSize = Math.Clamp(h + dy, min, Math.Min(maxConfigured, h + bottomGap));
                        break;
                    }
                case Dock.Bottom:
                    {
                        double topGap = Math.Max(0, pos.Y);
                        double dy = Math.Clamp(-e.Vector.Y, -(h - min), Math.Min(topGap, maxConfigured - h));
                        this.PanelSize = Math.Clamp(h + dy, min, Math.Min(maxConfigured, h + topGap));
                        break;
                    }
            }
        }

        private (Visual surface, Size size)? TryGetConstraintSurface()
        {
            return this.ConstraintMode switch
            {
                ResizeConstraintMode.Window => TopLevel.GetTopLevel(this) is { } tl
                    ? (tl, tl.ClientSize)
                    : null,
                ResizeConstraintMode.Custom when this.ConstraintTarget is { } v
                    => (v, v.Bounds.Size),
                _ when this.Parent is Visual p
                    => (p, p.Bounds.Size),
                _ => null
            };
        }
    }
}
