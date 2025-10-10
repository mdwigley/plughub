using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Input;

namespace PlugHub.Plugin.DockHost.Controls
{
    [TemplatePart(Name = PartNorthCell, Type = typeof(Border))]
    [TemplatePart(Name = PartSouthCell, Type = typeof(Border))]
    [TemplatePart(Name = PartWestCell, Type = typeof(Border))]
    [TemplatePart(Name = PartEastCell, Type = typeof(Border))]
    [TemplatePart(Name = PartNorthRegion, Type = typeof(Border))]
    [TemplatePart(Name = PartSouthRegion, Type = typeof(Border))]
    [TemplatePart(Name = PartWestRegion, Type = typeof(Border))]
    [TemplatePart(Name = PartEastRegion, Type = typeof(Border))]
    public class DockCompass : TemplatedControl
    {
        public static readonly DirectProperty<DockCompass, Dock?> ActiveEdgeProperty =
            AvaloniaProperty.RegisterDirect<DockCompass, Dock?>(nameof(ActiveEdge), o => o.ActiveEdge);
        private Dock? activeEdge;
        public Dock? ActiveEdge
        {
            get => this.activeEdge;
            private set => this.SetAndRaise(ActiveEdgeProperty, ref this.activeEdge, value);
        }

        public static readonly StyledProperty<bool> IsDiagnosticModeProperty =
            AvaloniaProperty.Register<DockCompass, bool>(nameof(IsDiagnosticMode));
        public bool IsDiagnosticMode
        {
            get => this.GetValue(IsDiagnosticModeProperty);
            set => this.SetValue(IsDiagnosticModeProperty, value);
        }

        private const string PartNorthCell = "PART_NorthCell";
        private const string PartSouthCell = "PART_SouthCell";
        private const string PartWestCell = "PART_WestCell";
        private const string PartEastCell = "PART_EastCell";

        private const string PartNorthRegion = "PART_NorthRegion";
        private const string PartSouthRegion = "PART_SouthRegion";
        private const string PartWestRegion = "PART_WestRegion";
        private const string PartEastRegion = "PART_EastRegion";

        private Border? northCell, southCell, westCell, eastCell;
        private Border? northRegion, southRegion, westRegion, eastRegion;

        public DockCompass()
        {
        }

        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            base.OnApplyTemplate(e);

            this.northCell = e.NameScope.Find<Border>(PartNorthCell);
            this.southCell = e.NameScope.Find<Border>(PartSouthCell);
            this.westCell = e.NameScope.Find<Border>(PartWestCell);
            this.eastCell = e.NameScope.Find<Border>(PartEastCell);

            this.northRegion = e.NameScope.Find<Border>(PartNorthRegion);
            this.southRegion = e.NameScope.Find<Border>(PartSouthRegion);
            this.westRegion = e.NameScope.Find<Border>(PartWestRegion);
            this.eastRegion = e.NameScope.Find<Border>(PartEastRegion);

            if (this.IsDiagnosticMode) this.Show();
        }

        public void Show()
        {
            this.IsVisible = true;
        }
        public void UpdatePointer(Point pos)
        {
            Dock? edge = this.HitTestCells(pos);

            if (edge != this.ActiveEdge)
            {
                this.ActiveEdge = edge;
                this.UpdateHighlights(edge);
            }
        }
        public void Hide()
        {
            this.IsVisible = false;
            this.ActiveEdge = null;
            this.UpdateHighlights(null);
        }

        public event EventHandler<Dock?>? DockCompleted;

        protected override void OnPointerMoved(PointerEventArgs e)
        {
            base.OnPointerMoved(e);
            if (!this.IsDiagnosticMode || !this.IsVisible) return;

            Point pos = e.GetPosition(this);
            this.UpdatePointer(pos);
        }
        protected override void OnPointerExited(PointerEventArgs e)
        {
            base.OnPointerExited(e);

            if (!this.IsDiagnosticMode) return;

            this.ActiveEdge = null;
            this.UpdateHighlights(null);
        }
        protected override void OnPointerReleased(PointerReleasedEventArgs e)
        {
            base.OnPointerReleased(e);

            if (!this.IsDiagnosticMode) return;

            DockCompleted?.Invoke(this, this.ActiveEdge);
        }

        private Dock? HitTestCells(Point pos)
        {
            if (this.IsInside(this.northCell, pos)) return Dock.Top;
            if (this.IsInside(this.southCell, pos)) return Dock.Bottom;
            if (this.IsInside(this.westCell, pos)) return Dock.Left;
            if (this.IsInside(this.eastCell, pos)) return Dock.Right;
            return null;
        }
        private bool IsInside(Control? cell, Point posInCompass)
        {
            if (cell is null) return false;
            Point? topLeft = cell.TranslatePoint(new Point(0, 0), this);
            if (topLeft is null) return false;
            Rect rect = new Rect(topLeft.Value, cell.Bounds.Size);
            return rect.Contains(posInCompass);
        }
        private void UpdateHighlights(Dock? edge)
        {
            this.PseudoClasses.Set(":edge-top", edge == Dock.Top);
            this.PseudoClasses.Set(":edge-bottom", edge == Dock.Bottom);
            this.PseudoClasses.Set(":edge-left", edge == Dock.Left);
            this.PseudoClasses.Set(":edge-right", edge == Dock.Right);
        }
    }
}