using Avalonia.Controls;
using Microsoft.Extensions.Logging;
using PlugHub.Plugin.DockHost.Interfaces.Services;
using PlugHub.Plugin.DockHost.Models;
using PlugHub.Shared.ViewModels;
using System.Collections.ObjectModel;

namespace PlugHub.Plugin.DockHost.ViewModels
{
    public class DockHostDemoViewModel : BaseViewModel
    {
        #region DockHostDemoViewModel: Panel IDs

        public static readonly Guid RightPanel1 = Guid.Parse("f1a3a5b2-1c2d-4e3f-9a10-111111111111");
        public static readonly Guid RightPanel2 = Guid.Parse("f1a3a5b2-1c2d-4e3f-9a10-222222222222");
        public static readonly Guid TopPanel1 = Guid.Parse("f1a3a5b2-1c2d-4e3f-9a10-333333333333");
        public static readonly Guid TopPanel2 = Guid.Parse("f1a3a5b2-1c2d-4e3f-9a10-444444444444");
        public static readonly Guid LeftPanel1 = Guid.Parse("f1a3a5b2-1c2d-4e3f-9a10-555555555555");
        public static readonly Guid LeftPanel2 = Guid.Parse("f1a3a5b2-1c2d-4e3f-9a10-666666666666");
        public static readonly Guid BottomPanel1 = Guid.Parse("f1a3a5b2-1c2d-4e3f-9a10-777777777777");
        public static readonly Guid BottomPanel2 = Guid.Parse("f1a3a5b2-1c2d-4e3f-9a10-888888888888");

        #endregion

        #region DockHostDemoViewModel: Control IDs

        public static readonly Guid RightPanel1ControlId = Guid.Parse("a1b2c3d4-1111-2222-3333-444444444444");
        public static readonly Guid RightPanel2ControlId = Guid.Parse("a1b2c3d4-1111-2222-3333-555555555555");

        public static readonly Guid TopPanel1ControlId = Guid.Parse("a1b2c3d4-1111-2222-3333-666666666666");
        public static readonly Guid TopPanel2ControlId = Guid.Parse("a1b2c3d4-1111-2222-3333-777777777777");

        public static readonly Guid LeftPanel1ControlId = Guid.Parse("a1b2c3d4-1111-2222-3333-888888888888");
        public static readonly Guid LeftPanel2ControlId = Guid.Parse("a1b2c3d4-1111-2222-3333-999999999999");

        public static readonly Guid BottomPanel1ControlId = Guid.Parse("a1b2c3d4-1111-2222-3333-aaaaaaaaaaaa");
        public static readonly Guid BottomPanel2ControlId = Guid.Parse("a1b2c3d4-1111-2222-3333-bbbbbbbbbbbb");

        #endregion

        public ObservableCollection<DockPanelState> DockPanels { get; } = [];
        public ObservableCollection<DockPanelItem> DockPanelItems { get; private set; } = [];
        public Guid DockId { get; set; } = Guid.Parse("a878b465-1d57-4b00-9169-eabfa9fe702d");
        public IDockService DockService { get; set; }

        private readonly ILogger<DockHostDemoViewModel> logger;

        public DockHostDemoViewModel(ILogger<DockHostDemoViewModel> logger, IDockService dockService)
        {
            this.logger = logger;
            this.DockService = dockService;
            this.DockPanelItems = [.. dockService.GetPanelItems(this.DockId)];

            dockService.PanelsChanged += (s, e) =>
            {
                this.DockPanelItems = [.. dockService.GetPanelItems(this.DockId)];
            };
            dockService.DockControlChanged += (s, e) =>
            {
                if (e.ControlId == this.DockId && e.ChangeType == DockControlChangeType.Registered)
                    this.ApplyDefaultPanels();
            };
        }

        private void ApplyDefaultPanels()
        {
            this.DockService.RequestPanel(LeftPanel1ControlId, this.DockId, LeftPanel1, edge: Dock.Left, pinned: true);
            this.DockService.RequestPanel(LeftPanel2ControlId, this.DockId, LeftPanel2, edge: Dock.Left, pinned: true);

            this.DockService.RequestPanel(TopPanel1ControlId, this.DockId, TopPanel1, edge: Dock.Top);
            this.DockService.RequestPanel(TopPanel2ControlId, this.DockId, TopPanel2, edge: Dock.Top);

            this.DockService.RequestPanel(RightPanel1ControlId, this.DockId, RightPanel1, edge: Dock.Right);
            this.DockService.RequestPanel(RightPanel2ControlId, this.DockId, RightPanel2, edge: Dock.Right);

            this.DockService.RequestPanel(BottomPanel1ControlId, this.DockId, BottomPanel1, edge: Dock.Bottom, pinned: true);
            this.DockService.RequestPanel(BottomPanel2ControlId, this.DockId, BottomPanel2, edge: Dock.Bottom, pinned: true);

            this.logger.LogInformation("Default panels applied for DockId {DockId}", this.DockId);
        }
    }
}