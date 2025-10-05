using Avalonia.Controls;
using PlugHub.Plugin.DockHost.Controls;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace PlugHub.Plugin.DockHost.Models
{
    public class DockPanelState : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        public Guid PluginId { get; init; }
        public Guid DescriptorId { get; init; }
        public Guid ControlId { get; init; }

        public string Header { get; init; }
        public DockablePanel DockablePanel { get; init; }

        private bool isPinned;
        public bool IsPinned
        {
            get => this.isPinned;
            set
            {
                if (this.isPinned != value)
                {
                    this.isPinned = value;
                    this.OnPropertyChanged();
                }
            }
        }

        private bool isVisible;
        public bool IsVisible
        {
            get => this.isVisible;
            set
            {
                if (this.isVisible != value)
                {
                    this.isVisible = value;
                    this.OnPropertyChanged();
                }
            }
        }

        private Dock dockEdge;
        public Dock DockEdge
        {
            get => this.dockEdge;
            set
            {
                if (this.dockEdge != value)
                {
                    this.dockEdge = value;
                    this.OnPropertyChanged();
                }
            }
        }

        public DockPanelState(string header, Control control, Dock edge = Dock.Left, bool pinned = false, bool visible = false, Guid descriptorId = default, Guid pluginId = default, Guid controlId = default)
        {
            ArgumentNullException.ThrowIfNull(header);
            ArgumentNullException.ThrowIfNull(control);

            this.Header = header;
            this.isPinned = pinned;
            this.isVisible = visible;
            this.dockEdge = edge;

            this.DescriptorId = descriptorId;
            this.PluginId = pluginId;
            this.ControlId = controlId;

            this.DockablePanel = new DockablePanel
            {
                Header = this.Header,
                Content = control,
                Descriptor = this
            };
            this.ControlId = controlId;
        }

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        public DockHostPanelData GetHostPanelData()
        {
            DockHostPanelData data = new DockHostPanelData
            {
                PluginID = this.PluginId,
                DescriptorID = this.DescriptorId,
                ControlID = this.ControlId,
                DockEdge = this.DockEdge,
                IsPinned = this.isPinned,
                IsVisible = this.isVisible
            };

            return data;
        }
        public static DockPanelState ApplyHostData(DockHostPanelData data)
        {
            // TODO: STUBBED
            return null!;
        }
    }
}