using Avalonia.Controls;

namespace PlugHub.Plugin.DockHost.Models
{
    public class DockHostPanelData
    {
        public Guid PluginID;
        public Guid ControlID;
        public Guid DescriptorID;
        public Dock DockEdge;
        public bool IsPinned;
        public bool IsVisible;
    }

    public class DockHostControlData
    {
        public Guid ControlID { get; set; } = default;

        public double FlyoutLeftLength { get; set; } = 250.0f;
        public double FlyoutTopLength { get; set; } = 250.0f;
        public double FlyoutRightLength { get; set; } = 250.0f;
        public double FlyoutBottomLength { get; set; } = 250.0f;

        public double PanelLeftLength { get; set; } = 250.0f;
        public double PanelTopLength { get; set; } = 250.0f;
        public double PanelRightLength { get; set; } = 250.0f;
        public double PanelBottomLength { get; set; } = 250.0f;

        public List<DockHostPanelData> DockHostDataItems { get; set; } = [];
    }

    public class DockHostData
    {
        List<DockHostControlData> DockHostControlDataItems { get; set; } = [];
    }
}
