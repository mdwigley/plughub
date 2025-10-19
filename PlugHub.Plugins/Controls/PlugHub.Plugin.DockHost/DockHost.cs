using PlugHub.Plugin.DockHost.Interfaces.Services;
using PlugHub.Plugin.DockHost.Services;
using PlugHub.Shared.Interfaces.Plugins;
using PlugHub.Shared.Models.Plugins;

namespace PlugHub.Plugin.DockHost
{
    public class DockHost : PluginBase, IPluginDependencyInjection, IPluginResourceInclusion, IPluginStyleInclusion
    {
        #region DockHost: Key Fields

        public new static Guid PluginID { get; } = Guid.Parse("1d0a560e-64f9-4989-b5e2-4fb4ad1a9b38");
        public new static string IconSource { get; } = "";
        public new static string Name { get; } = "PlugHub Control: DockHost";
        public new static string Description { get; } = "Extensible panel docking and orchestration framework.";
        public new static string Version { get; } = "0.0.1";
        public new static string Author { get; } = "Enterlucent";
        public new static List<string> Categories { get; } =
        [
            "Controls",
        ];

        #endregion

        #region DockHost: Metadata

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

        #region DockHost: IPluginDependencyInjection

        public IEnumerable<PluginInjectorDescriptor> GetInjectionDescriptors()
        {
            return [
                new PluginInjectorDescriptor(
                    PluginID,
                    Guid.Parse("f0fdd095-4ff0-4d4b-9af1-8b24e2100ba2"),
                    Version,
                    typeof(IDockService),
                    typeof(DockService)
                ),
            ];
        }

        #endregion

        #region DockHost: IPluginResourceInclusion

        public IEnumerable<PluginResourceIncludeDescriptor> GetResourceIncludeDescriptors()
        {
            return [
                new PluginResourceIncludeDescriptor(
                    PluginID,
                    Guid.Parse("bc60dfd7-cffd-4fcf-940a-ac4d0bf435f0"),
                    Version,
                    "avares://PlugHub.Plugin.DockHost/Themes/FluentAvalonia/Theme.axaml"
                ),
            ];
        }

        #endregion

        #region DockHost: IPluginStyleInclusion

        public IEnumerable<PluginStyleIncludeDescriptor> GetStyleIncludeDescriptors()
        {
            return [
                new PluginStyleIncludeDescriptor(
                    PluginID,
                    Guid.Parse("58967054-6364-447d-a1bc-a2d307a05ea1"),
                    Version,
                    "avares://PlugHub.Plugin.DockHost/Themes/FluentAvalonia/Style.axaml"
                )
            ];
        }



        #endregion
    }
}