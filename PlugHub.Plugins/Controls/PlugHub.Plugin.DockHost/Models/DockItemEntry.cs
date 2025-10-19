using Avalonia.Media;
using CommunityToolkit.Mvvm.Input;
using PlugHub.Plugin.DockHost.Interfaces.Services;
using System.Windows.Input;

namespace PlugHub.Plugin.DockHost.Models
{
    public class DockItemEntry
    {
        public Guid Id { get; }
        public string Header { get; }
        public IImage? Icon { get; }

        public string? Group { get; }
        public string[]? Tags { get; }

        public ICommand Command { get; }

        private readonly IDockService service;
        private readonly Guid dockControlId;

        public DockItemEntry(Guid id, string header, IImage? icon, string? group, string[]? tags, IDockService service, Guid dockControlId)
        {
            this.Id = id;
            this.Header = header;
            this.Icon = icon;
            this.Group = group;
            this.Tags = tags;
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
