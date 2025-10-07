using Avalonia.Media;
using CommunityToolkit.Mvvm.Input;
using PlugHub.Plugin.DockHost.Interfaces.Services;
using System.Windows.Input;

namespace PlugHub.Plugin.DockHost.Models
{
    public class DockPanelItem
    {
        public Guid Id { get; }
        public string Header { get; }
        public IImage? Icon { get; }
        public ICommand Command { get; }

        private readonly IDockService service;
        private readonly Guid dockControlId;

        public DockPanelItem(Guid id, string header, IImage? icon, IDockService service, Guid dockControlId)
        {
            ArgumentNullException.ThrowIfNull(nameof(header));
            ArgumentNullException.ThrowIfNull(nameof(service));

            this.Id = id;
            this.Header = header;
            this.Icon = icon;
            this.service = service;
            this.dockControlId = dockControlId;

            this.Command = new RelayCommand(this.Activate);
        }

        private void Activate()
        {
            this.service.RequestPanel(Guid.NewGuid(), this.dockControlId, this.Id);
        }
    }
}
