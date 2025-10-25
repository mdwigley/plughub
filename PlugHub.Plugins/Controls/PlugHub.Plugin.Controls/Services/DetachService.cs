using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Threading;

namespace PlugHub.Plugin.Controls.Services
{
    public static class DetachService
    {
        public static bool HardDetach(Control control)
        {
            if (control == null) return false;

            try
            {
                DetachInternal(control).GetAwaiter().GetResult();

                return control.Parent == null;
            }
            catch
            {
                return false;
            }
        }
        public static async Task<bool> HardDetachAsync(Control control, CancellationToken cancellationToken = default)
        {
            if (control == null) return false;

            try
            {
                await DetachInternal(control, cancellationToken);
                return control.Parent == null;
            }
            catch
            {
                return false;
            }
        }

        private static async Task DetachInternal(Control control, CancellationToken cancellationToken = default)
        {
            for (int i = 0; i < 3 && control.Parent != null; i++)
            {
                switch (control.Parent)
                {
                    case ContentPresenter cp:
                        cp.Content = null;
                        break;
                    case ContentControl cc:
                        cc.Content = null;
                        break;
                    case Panel panel:
                        panel.Children.Remove(control);
                        break;
                }

                control.InvalidateMeasure();
                control.InvalidateArrange();

                await Dispatcher.UIThread.InvokeAsync(() => { },
                    DispatcherPriority.Render, cancellationToken);
            }
        }
    }
}