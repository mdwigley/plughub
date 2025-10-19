using Avalonia.Controls;
using PlugHub.Plugin.DockHost.Interfaces.Plugins;
using PlugHub.Plugin.DockHost.ViewModels;
using PlugHub.Plugin.DockHost.Views;
using PlugHub.Shared.Interfaces.Plugins;
using PlugHub.Shared.Models.Plugins;

namespace PlugHub.Plugin.DockHost
{
    public class DockHostDemo : PluginBase, IPluginDependencyInjection, IPluginPages, IPluginDockPanels
    {
        #region DockHostDemo: Key Fields

        public new static Guid PluginID { get; } = Guid.Parse("b738771e-50a7-4918-98a8-44e8b636441f");
        public new static string IconSource { get; } = "";
        public new static string Name { get; } = "PlugHub Control: DockHost Demo";
        public new static string Description { get; } = "Demo of an implmeentation for the PlugHub.DockHost framework.";
        public new static string Version { get; } = "0.0.1";
        public new static string Author { get; } = "Enterlucent";
        public new static List<string> Categories { get; } =
        [
            "Demo"
        ];

        #endregion

        #region DockHostDemo: Metadata

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

        #region DockHostDemo: IPluginDependencyInjection

        public IEnumerable<PluginInjectorDescriptor> GetInjectionDescriptors()
        {
            return [
                /* Interface Pages */
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

                /* DockHost Panels */
                new PluginInjectorDescriptor(
                    PluginID,
                    Guid.Parse("eb318b5f-6e8a-4fcf-a6a8-4c0b53a61fbc"),
                    Version,
                    typeof(DockHostPanelQuestView),
                    typeof(DockHostPanelQuestView),
                    Lifetime: Microsoft.Extensions.DependencyInjection.ServiceLifetime.Transient
                ),
                new PluginInjectorDescriptor(
                    PluginID,
                    Guid.Parse("1133438a-416c-45c4-bc6a-6f6340d72bd4"),
                    Version,
                    typeof(DockHostPanelQuestViewModel),
                    typeof(DockHostPanelQuestViewModel),
                    Lifetime: Microsoft.Extensions.DependencyInjection.ServiceLifetime.Transient
                ),
                new PluginInjectorDescriptor(
                    PluginID,
                    Guid.Parse("02a51a69-e585-4a4f-bbc6-366e366d6d44"),
                    Version,
                    typeof(DockHostPanelCharacterView),
                    typeof(DockHostPanelCharacterView),
                    Lifetime: Microsoft.Extensions.DependencyInjection.ServiceLifetime.Transient
                ),
                new PluginInjectorDescriptor(
                    PluginID,
                    Guid.Parse("75c2ec3f-3d9d-4065-9f8a-580084279c4d"),
                    Version,
                    typeof(DockHostPanelCharacterViewModel),
                    typeof(DockHostPanelCharacterViewModel),
                    Lifetime: Microsoft.Extensions.DependencyInjection.ServiceLifetime.Transient
                ),
                new PluginInjectorDescriptor(
                    PluginID,
                    Guid.Parse("daa2eaa0-c555-48ef-a6a4-60187447b151"),
                    Version,
                    typeof(DockHostPanelFactionView),
                    typeof(DockHostPanelFactionView),
                    Lifetime: Microsoft.Extensions.DependencyInjection.ServiceLifetime.Transient
                ),
                new PluginInjectorDescriptor(
                    PluginID,
                    Guid.Parse("fa1a6cf7-0573-4ae0-9608-d2e4739e5cae"),
                    Version,
                    typeof(DockHostPanelFactionViewModel),
                    typeof(DockHostPanelFactionViewModel),
                    Lifetime: Microsoft.Extensions.DependencyInjection.ServiceLifetime.Transient
                ),
                new PluginInjectorDescriptor(
                    PluginID,
                    Guid.Parse("5ffab2f5-8e73-454a-b05e-0a9f9750e835"),
                    Version,
                    typeof(DockHostPanelWorldView),
                    typeof(DockHostPanelWorldView),
                    Lifetime: Microsoft.Extensions.DependencyInjection.ServiceLifetime.Transient
                ),
                new PluginInjectorDescriptor(
                    PluginID,
                    Guid.Parse("75b40e1e-7131-403f-8e79-e428da1980e5"),
                    Version,
                    typeof(DockHostPanelWorldViewModel),
                    typeof(DockHostPanelWorldViewModel),
                    Lifetime: Microsoft.Extensions.DependencyInjection.ServiceLifetime.Transient
                ),
            ];
        }

        #endregion

        #region DockHostDemo: IPluginPages

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

        #region DockHostDemo: IPluginDockPanels

        public IEnumerable<DockPanelDescriptor> GetDockPanelDescriptors()
        {
            return
            [
                /* Dependency Injection */
                new DockPanelDescriptor(
                    PluginID,
                    Guid.Parse("f1a3a5b2-1c2d-4e3f-9a10-111111111111"),
                    Version,
                    "Characters",
                    TargetedHosts: [Guid.Parse("a878b465-1d57-4b00-9169-eabfa9fe702d")]
,
                    ContentType: typeof(DockHostPanelCharacterView),
                    ViewModelType: typeof(DockHostPanelCharacterViewModel)),
                new DockPanelDescriptor(
                    PluginID,
                    Guid.Parse("f1a3a5b2-1c2d-4e3f-9a10-222222222222"),
                    Version,
                    "Quests",
                    TargetedHosts: [Guid.Parse("a878b465-1d57-4b00-9169-eabfa9fe702d")]
,
                    ContentType: typeof(DockHostPanelQuestView),
                    ViewModelType: typeof(DockHostPanelQuestViewModel)),
                new DockPanelDescriptor(
                    PluginID,
                    Guid.Parse("f1a3a5b2-1c2d-4e3f-9a10-555555555555"),
                    Version,
                    "World",
                    TargetedHosts: [Guid.Parse("a878b465-1d57-4b00-9169-eabfa9fe702d")]
,
                    ContentType: typeof(DockHostPanelWorldView),
                    ViewModelType: typeof(DockHostPanelWorldViewModel)),
                new DockPanelDescriptor(
                    PluginID,
                    Guid.Parse("f1a3a5b2-1c2d-4e3f-9a10-666666666666"),
                    Version,
                    "Factions",
                    TargetedHosts: [Guid.Parse("a878b465-1d57-4b00-9169-eabfa9fe702d")]
,
                    ContentType: typeof(DockHostPanelFactionView),
                    ViewModelType: typeof(DockHostPanelFactionViewModel)),

                /* Factories */
                new DockPanelDescriptor(
                    PluginID,
                    Guid.Parse("f1a3a5b2-1c2d-4e3f-9a10-333333333333"),
                    Version,
                    "Inventory",
                    TargetedHosts: [Guid.Parse("a878b465-1d57-4b00-9169-eabfa9fe702d")]
,
                    Factory: sp => new TextBlock { Text = "Inventory manager goes here", Margin = new Avalonia.Thickness(8) }),
                new DockPanelDescriptor(
                    PluginID,
                    Guid.Parse("f1a3a5b2-1c2d-4e3f-9a10-444444444444"),
                    Version,
                    "Skills",
                    TargetedHosts: [Guid.Parse("a878b465-1d57-4b00-9169-eabfa9fe702d")]
,
                    Factory: sp => new TextBlock { Text = "Skill tree goes here", Margin = new Avalonia.Thickness(8) }),
                new DockPanelDescriptor(
                    PluginID,
                    Guid.Parse("f1a3a5b2-1c2d-4e3f-9a10-777777777777"),
                    Version,
                    "Console",
                    TargetedHosts: [Guid.Parse("a878b465-1d57-4b00-9169-eabfa9fe702d")]
,
                    Factory: sp => new TextBlock { Text = "Debug console goes here", Margin = new Avalonia.Thickness(8) }),
                new DockPanelDescriptor(
                    PluginID,
                    Guid.Parse("f1a3a5b2-1c2d-4e3f-9a10-888888888888"),
                    Version,
                    "Log",
                    TargetedHosts: [Guid.Parse("a878b465-1d57-4b00-9169-eabfa9fe702d")]
,
                    Factory: sp => new TextBlock { Text = "Event log goes here", Margin = new Avalonia.Thickness(8) })
            ];
        }

        #endregion
    }
}