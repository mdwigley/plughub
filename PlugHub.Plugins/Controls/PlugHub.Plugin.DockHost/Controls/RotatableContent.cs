using Avalonia;
using Avalonia.Controls;

namespace PlugHub.Plugin.DockHost.Controls
{
    public class RotatableContent : ContentControl
    {
        public static readonly StyledProperty<double> RotationProperty =
            AvaloniaProperty.Register<RotatableContent, double>(nameof(Rotation), 0d);
        public double Rotation
        {
            get => this.GetValue(RotationProperty);
            set => this.SetValue(RotationProperty, value);
        }

        static RotatableContent()
        {
            RotationProperty.Changed.AddClassHandler<RotatableContent>((x, _) =>
            {
                x.InvalidateMeasure();
                x.InvalidateArrange();
            });
        }
    }
}