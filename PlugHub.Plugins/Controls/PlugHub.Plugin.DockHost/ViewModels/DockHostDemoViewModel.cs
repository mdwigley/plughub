using Avalonia.Controls;
using Microsoft.Extensions.Logging;
using PlugHub.Plugin.DockHost.Controls;
using PlugHub.Plugin.DockHost.Interfaces.Services;
using PlugHub.Plugin.DockHost.Models;
using PlugHub.Shared.Interfaces.Accessors;
using PlugHub.Shared.Interfaces.Services.Configuration;
using PlugHub.Shared.Models;
using PlugHub.Shared.Models.Configuration.Parameters;
using PlugHub.Shared.ViewModels;
using System.Collections.ObjectModel;

namespace PlugHub.Plugin.DockHost.ViewModels
{
    public class DockHostDemoData
    {
        public bool FirstRun { get; set; } = false;
    }

    public class DockHostDemoViewModel : BaseViewModel
    {
        #region DockHostDemoViewModel: Panel IDs

        public static readonly Guid CharactersDescriptorId = Guid.Parse("f1a3a5b2-1c2d-4e3f-9a10-111111111111");
        public static readonly Guid QuestsDescriptorId = Guid.Parse("f1a3a5b2-1c2d-4e3f-9a10-222222222222");
        public static readonly Guid InventoryDescriptorId = Guid.Parse("f1a3a5b2-1c2d-4e3f-9a10-333333333333");
        public static readonly Guid SkillsDescriptorId = Guid.Parse("f1a3a5b2-1c2d-4e3f-9a10-444444444444");
        public static readonly Guid WorldDescriptorId = Guid.Parse("f1a3a5b2-1c2d-4e3f-9a10-555555555555");
        public static readonly Guid FactionsDescriptorId = Guid.Parse("f1a3a5b2-1c2d-4e3f-9a10-666666666666");
        public static readonly Guid ConsoleDescriptorId = Guid.Parse("f1a3a5b2-1c2d-4e3f-9a10-777777777777");
        public static readonly Guid LogDescriptorId = Guid.Parse("f1a3a5b2-1c2d-4e3f-9a10-888888888888");

        #endregion

        #region DockHostDemoViewModel: Control IDs

        public static readonly Guid CharacterPanelId = Guid.Parse("a1b2c3d4-1111-2222-3333-111111111111");
        public static readonly Guid QuestsPanelPanelId = Guid.Parse("a1b2c3d4-1111-2222-3333-222222222222");
        public static readonly Guid InventoryPanelId = Guid.Parse("a1b2c3d4-1111-2222-3333-333333333333");
        public static readonly Guid SkillsPanelId = Guid.Parse("a1b2c3d4-1111-2222-3333-444444444444");
        public static readonly Guid WorldPanelId = Guid.Parse("a1b2c3d4-1111-2222-3333-555555555555");
        public static readonly Guid FactionsPanelId = Guid.Parse("a1b2c3d4-1111-2222-3333-666666666666");
        public static readonly Guid ConsolePanelId = Guid.Parse("a1b2c3d4-1111-2222-3333-777777777777");
        public static readonly Guid LogPanelId = Guid.Parse("a1b2c3d4-1111-2222-3333-888888888888");

        #endregion

        private readonly ILogger<DockHostDemoViewModel> logger;

        public ObservableCollection<DockItemState> DockPanels { get; } = [];
        public ObservableCollection<DockItemEntry> DockPanelItems { get; private set; } = [];
        public Guid DockId { get; set; } = Guid.Parse("a878b465-1d57-4b00-9169-eabfa9fe702d");
        public IDockService DockService { get; set; }

        public DockHostDemoViewModel(ILogger<DockHostDemoViewModel> logger, IConfigService configService, IDockService dockService)
        {
            this.logger = logger;
            this.DockService = dockService;
            this.DockPanelItems = [.. dockService.GetPanelItems(this.DockId)];

            configService.RegisterConfig(
                new ConfigFileParams(Owner: Token.New(), Read: Token.Blocked, Write: Token.Blocked),
                out IConfigAccessorFor<DockHostDemoData> accessor);

            DockHostDemoData data = accessor.Get();

            dockService.PanelsChanged += (s, e) =>
                this.DockPanelItems = [.. dockService.GetPanelItems(this.DockId)];

            dockService.DockControlChanged += (s, e) =>
            {
                if (e.Control is DockControl control)
                {
                    if (data.FirstRun == false)
                    {
                        this.ApplyDefaultPanels();

                        data.FirstRun = true;

                        accessor.Save(data);
                    }
                }
            };

            dockService.DockControlReady += (s, e) => { };
        }

        private void ApplyDefaultPanels()
        {
            this.DockService.RequestPanel(WorldPanelId, this.DockId, WorldDescriptorId, edge: Dock.Left, pinned: true);
            this.DockService.RequestPanel(FactionsPanelId, this.DockId, FactionsDescriptorId, edge: Dock.Left, pinned: true);

            this.DockService.RequestPanel(InventoryPanelId, this.DockId, InventoryDescriptorId, edge: Dock.Top);
            this.DockService.RequestPanel(SkillsPanelId, this.DockId, SkillsDescriptorId, edge: Dock.Top);

            this.DockService.RequestPanel(CharacterPanelId, this.DockId, CharactersDescriptorId, edge: Dock.Right);
            this.DockService.RequestPanel(QuestsPanelPanelId, this.DockId, QuestsDescriptorId, edge: Dock.Right);

            this.DockService.RequestPanel(ConsolePanelId, this.DockId, ConsoleDescriptorId, edge: Dock.Bottom, pinned: true);
            this.DockService.RequestPanel(LogPanelId, this.DockId, LogDescriptorId, edge: Dock.Bottom, pinned: true);

            this.logger.LogInformation("Default panels applied for DockId {DockId}", this.DockId);
        }
    }
}