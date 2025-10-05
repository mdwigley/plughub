using Avalonia.Controls;
using PlugHub.Plugin.DockHost.Interfaces.Plugins;
using PlugHub.Plugin.DockHost.ViewModels;
using PlugHub.Plugin.DockHost.Views;
using PlugHub.Shared.Interfaces.Plugins;
using PlugHub.Shared.Models.Plugins;

namespace PlugHub.Plugin.DockHost
{
    public class DockHostDemo : PluginBase, IPluginDependencyInjection, IPluginDockPanels, IPluginPages
    {
        #region DockControl: Key Fields

        public new static Guid PluginID { get; } = Guid.Parse("b738771e-50a7-4918-98a8-44e8b636441f");
        public new static string IconSource { get; } = "";
        public new static string Name { get; } = "PlugHub Control: DockHost Demo";
        public new static string Description { get; } = "Demo of an implmeentation for the PlugHub.DockHost framework.";
        public new static string Version { get; } = "0.0.1";
        public new static string Author { get; } = "Enterlucent";
        public new static List<string> Categories { get; } =
        [
            "Control",
            "Demo"
        ];

        #endregion

        #region DockControl: Metadata

        public new static List<string> Tags { get; } =
        [
            "DockHost",
            "DockControl",
            "DockService",
            "DockPanel",
        ];
        public new static string DocsLink { get; } = "https://github.com/enterlucent/plughub/wiki/";
        public new static string SupportLink { get; } = "https://support.enterlucent.com/plughub/";
        public new static string SupportContact { get; } = "contact@enterlucent.com";
        public new static string License { get; } = "GNU General Public License v3";
        public new static string ChangeLog { get; } = "https://github.com/enterlucent/plughub/releases/";

        #endregion

        #region DockControl: IPluginDependencyInjector

        public IEnumerable<PluginInjectorDescriptor> GetInjectionDescriptors()
        {
            return [
                new PluginInjectorDescriptor(
                    PluginID,
                    Guid.Parse("d87a0de8-0f62-43fe-970b-a5d6810bbe8f"),
                    Version,
                    typeof(DockHostDemoView),
                    typeof(DockHostDemoView)
                ),
                new PluginInjectorDescriptor(
                    PluginID,
                    Guid.Parse("d3a14010-e32c-4fe8-a133-1f50cf31313c"),
                    Version,
                    typeof(DockHostDemoViewModel),
                    typeof(DockHostDemoViewModel)
                ),
            ];
        }

        #endregion

        #region DockControl: IPluginPages

        public IEnumerable<PluginPageDescriptor> GetPageDescriptors()
        {
            return [
                new PluginPageDescriptor(
                    PluginID,
                    Guid.Parse("120559bf-b09d-47e5-a6ed-77fcf0bfb071"),
                    Version,
                    typeof(DockHostDemoView),
                    typeof(DockHostDemoViewModel),
                    "Dock Host Demo",
                    "comment_multiple_regular")
            ];
        }

        #endregion

        #region DockControl: IPluginDockPanels

        public IEnumerable<DockPanelDescriptor> GetDockPanelDescriptors()
        {
            return
            [
                new DockPanelDescriptor(
                    PluginID,
                    Guid.Parse("f1a3a5b2-1c2d-4e3f-9a10-111111111111"),
                    Version,
                    "Characters",
                    null,
                    Factory: sp => new TextBlock { Text = "Character editor goes here" },
                    TargetedHosts: [Guid.Parse("a878b465-1d57-4b00-9169-eabfa9fe702d")]
                ),
                new DockPanelDescriptor(
                    PluginID,
                    Guid.Parse("f1a3a5b2-1c2d-4e3f-9a10-222222222222"),
                    Version,
                    "Quests",
                    null,
                    Factory: sp => new TextBlock { Text = "Quest log goes here" },
                    TargetedHosts: [Guid.Parse("a878b465-1d57-4b00-9169-eabfa9fe702d")]
                ),
                new DockPanelDescriptor(
                    PluginID,
                    Guid.Parse("f1a3a5b2-1c2d-4e3f-9a10-333333333333"),
                    Version,
                    "Inventory",
                    null,
                    Factory: sp => new TextBlock { Text = "Inventory manager goes here" },
                    TargetedHosts: [Guid.Parse("a878b465-1d57-4b00-9169-eabfa9fe702d")]
                ),
                new DockPanelDescriptor(
                    PluginID,
                    Guid.Parse("f1a3a5b2-1c2d-4e3f-9a10-444444444444"),
                    Version,
                    "Skills",
                    null,
                    Factory: sp => new TextBlock { Text = "Skill tree goes here" },
                    TargetedHosts: [Guid.Parse("a878b465-1d57-4b00-9169-eabfa9fe702d")]
                ),
                new DockPanelDescriptor(
                    PluginID,
                    Guid.Parse("f1a3a5b2-1c2d-4e3f-9a10-555555555555"),
                    Version,
                    "World",
                    null,
                    Factory: sp => new TextBlock { Text = "World map goes here" },
                    TargetedHosts: [Guid.Parse("a878b465-1d57-4b00-9169-eabfa9fe702d")]
                ),
                new DockPanelDescriptor(
                    PluginID,
                    Guid.Parse("f1a3a5b2-1c2d-4e3f-9a10-666666666666"),
                    Version,
                    "Factions",
                    null,
                    Factory: sp => new TextBlock { Text = "Faction relations go here" },
                    TargetedHosts: [Guid.Parse("a878b465-1d57-4b00-9169-eabfa9fe702d")]
                ),
                new DockPanelDescriptor(
                    PluginID,
                    Guid.Parse("f1a3a5b2-1c2d-4e3f-9a10-777777777777"),
                    Version,
                    "Console",
                    null,
                    Factory: sp => new TextBlock { Text = "Debug console goes here" },
                    TargetedHosts: [Guid.Parse("a878b465-1d57-4b00-9169-eabfa9fe702d")]
                ),
                new DockPanelDescriptor(
                    PluginID,
                    Guid.Parse("f1a3a5b2-1c2d-4e3f-9a10-888888888888"),
                    Version,
                    "Log",
                    null,
                    Factory: sp => new TextBlock { Text = "Event log goes here" },
                    TargetedHosts: [Guid.Parse("a878b465-1d57-4b00-9169-eabfa9fe702d")]
                )
            ];
        }

        #endregion
    }
}