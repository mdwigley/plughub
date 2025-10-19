using Avalonia;
using Avalonia.Controls;

namespace PlugHub.Plugin.Controls.Controls
{
    public class RotationView : ContentControl
    {
        public static readonly StyledProperty<double> RotationProperty =
            AvaloniaProperty.Register<RotationView, double>(nameof(Rotation), 0d);
        public double Rotation
        {
            get => this.GetValue(RotationProperty);
            set => this.SetValue(RotationProperty, value);
        }

        static RotationView()
        {
            RotationProperty.Changed.AddClassHandler<RotationView>((x, _) =>
            {
                x.InvalidateMeasure();
                x.InvalidateArrange();
            });
        }
    }
}