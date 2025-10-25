using Avalonia.Controls;
using PlugHub.Plugin.Controls.Controls;

namespace PlugHub.Plugin.DockHost.Models
{
    public class DockHostPanelData
    {
        public Guid ControlID { get; set; }
        public Guid PluginID { get; set; }
        public Guid DescriptorID { get; set; }
        public Guid DockControlID { get; set; }
        public int SortOrder { get; set; }
        public Dock DockEdge { get; set; }
        public bool IsPinned { get; set; }
    }

    public class DockHostControlData
    {
        public Guid ControlID { get; set; } = default;

        public double LeftFlyoutLength { get; set; } = -1;
        public double TopFlyoutLength { get; set; } = -1;
        public double RightFlyoutLength { get; set; } = -1;
        public double BottomFlyoutLength { get; set; } = -1;

        public double LeftPanelLength { get; set; } = -1;
        public double TopPanelLength { get; set; } = -1;
        public double RightPanelLength { get; set; } = -1;
        public double BottomPanelLength { get; set; } = -1;

        public ContentDeckDisplayMode LeftDeckMode { get; set; } = ContentDeckDisplayMode.Tab;
        public ContentDeckDisplayMode TopDeckMode { get; set; } = ContentDeckDisplayMode.Tab;
        public ContentDeckDisplayMode RightDeckMode { get; set; } = ContentDeckDisplayMode.Tab;
        public ContentDeckDisplayMode BottomDeckMode { get; set; } = ContentDeckDisplayMode.Tab;

        public List<double> LeftContentSizes { get; set; } = [];
        public List<double> TopContentSizes { get; set; } = [];
        public List<double> RightContentSizes { get; set; } = [];
        public List<double> BottomContentSizes { get; set; } = [];

        public int LeftPinnedSelectedIndex { get; set; } = 0;
        public int TopPinnedSelectedIndex { get; set; } = 0;
        public int RightPinnedSelectedIndex { get; set; } = 0;
        public int BottomPinnedSelectedIndex { get; set; } = 0;

        public List<DockHostPanelData> DockHostDataItems { get; set; } = [];
    }

    public class DockHostData
    {
        public List<DockHostControlData> DockHostControlDataItems { get; set; } = [];
    }
}
