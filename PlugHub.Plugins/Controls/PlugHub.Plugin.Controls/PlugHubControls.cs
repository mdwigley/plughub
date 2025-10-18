using PlugHub.Shared.Interfaces.Plugins;
using PlugHub.Shared.Models.Plugins;

namespace PlugHub.Plugin.Controls
{
    public class PlugHubControls : PluginBase, IPluginResourceInclusion, IPluginStyleInclusion
    {
        #region PlugHubControls: Key Fields

        public new static Guid PluginID { get; } = Guid.Parse("85518ef6-1697-48c0-a254-21344e0e384e");
        public new static string IconSource { get; } = "";
        public new static string Name { get; } = "PlugHub Controls: Core";
        public new static string Description { get; } = "Core controls for the plughub infrastructure.";
        public new static string Version { get; } = "0.0.1";
        public new static string Author { get; } = "Enterlucent";
        public new static List<string> Categories { get; } =
        [
            "Controls",
        ];

        #endregion

        #region PlugHubControls: Metadata

        public new static List<string> Tags { get; } =
        [
            "Controls",
        ];
        public new static string DocsLink { get; } = "https://github.com/enterlucent/plughub/wiki/";
        public new static string SupportLink { get; } = "https://support.enterlucent.com/plughub/";
        public new static string SupportContact { get; } = "contact@enterlucent.com";
        public new static string License { get; } = "GNU General Public License v3";
        public new static string ChangeLog { get; } = "https://github.com/enterlucent/plughub/releases/";

        #endregion

        #region PlugHubControls: IPluginResourceInclusion

        public IEnumerable<PluginResourceIncludeDescriptor> GetResourceIncludeDescriptors()
        {
            return [
                new PluginResourceIncludeDescriptor(
                    PluginID,
                    Guid.Parse("136f6078-03a7-4a1f-812c-509ecc765b57"),
                    Version,
                    "avares://PlugHub.Plugin.Controls/Themes/FluentAvalonia/Theme.axaml"
                )
            ];
        }

        #endregion

        #region PlugHubControls: IPluginStyleInclusion

        public IEnumerable<PluginStyleIncludeDescriptor> GetStyleIncludeDescriptors()
        {
            return [
                new PluginStyleIncludeDescriptor(
                    PluginID,
                    Guid.Parse("57d522f7-b583-4402-9fe5-bc4e55404d33"),
                    Version,
                    "avares://PlugHub.Plugin.Controls/Themes/FluentAvalonia/Style.axaml"
                )
            ];
        }

        #endregion
    }
}