using Avalonia.Controls;
using Avalonia.Media;
using PlugHub.Shared.Attributes;
using PlugHub.Shared.Interfaces.Plugins;
using PlugHub.Shared.Models.Plugins;

namespace PlugHub.Plugin.DockHost.Interfaces.Plugins
{
    /// <summary>
    /// Describes a plugin-provided dock panel.
    /// Contains the display header, optional icon, a type or factory for creating the panel's content,
    /// optional view-model binding, host targeting, and dependency/conflict metadata.
    /// </summary>
    /// <param name="PluginID">Unique identifier for the plugin providing this panel.</param>
    /// <param name="DescriptorID">Unique identifier for this descriptor.</param>
    /// <param name="Version">Version of the descriptor.</param>
    /// <param name="Header">Display header text for the panel.</param>
    /// <param name="Icon">Optional icon to display alongside the header.</param>
    /// <param name="Group">Optional grouping label. Allows panels to be clustered under a common heading; interpretation is left to the host or UI developer.</param>
    /// <param name="Tags">Optional set of tags for categorisation, filtering, or search. Semantics are intentionally open‑ended for consumers to define.</param>
    /// <param name="TargetedHosts">Optional dock host identifiers. If set, the panel is bound only to controls with matching IDs; otherwise the panel is unbound and may appear on any host.</param>
    /// <param name="ContentType">Optional Avalonia <see cref="Control"/> type to instantiate for this panel. If provided, the host will resolve or construct it via DI.</param>
    /// <param name="ViewModelType">Optional view-model type. If provided alongside <paramref name="ContentType"/>, the host will resolve it and assign it to the view's DataContext.</param>
    /// <param name="Factory">Optional factory function to create the panel's content control. Used if <paramref name="ContentType"/> is not provided.</param>
    /// <param name="LoadBefore">Descriptors that should be applied after this one to maintain order.</param>
    /// <param name="LoadAfter">Descriptors that should be applied before this one to maintain order.</param>
    /// <param name="DependsOn">Descriptors that this descriptor explicitly depends on.</param><param name="ConflictsWith">Descriptors with which this descriptor cannot coexist.</param>
    public record DockPanelDescriptor(
        Guid PluginID,
        Guid DescriptorID,
        string Version,

        string Header,
        IImage? Icon = null,

        string? Group = null,
        string[]? Tags = null,
        IEnumerable<Guid>? TargetedHosts = null,

        Type? ContentType = null,
        Type? ViewModelType = null,
        Func<IServiceProvider, Control>? Factory = null,

        IEnumerable<PluginDescriptorReference>? LoadBefore = null,
        IEnumerable<PluginDescriptorReference>? LoadAfter = null,
        IEnumerable<PluginDescriptorReference>? DependsOn = null,
        IEnumerable<PluginDescriptorReference>? ConflictsWith = null)
            : PluginDescriptor(PluginID, DescriptorID, Version, LoadBefore, LoadAfter, DependsOn, ConflictsWith);

    /// <summary>
    /// Interface for plugins that provide dock panels to the host application.
    /// </summary>
    [DescriptorProvider("GetDockPanelDescriptors", false)]
    public interface IPluginDockPanels : IPlugin
    {
        /// <summary>
        /// Returns the set of <see cref="DockPanelDescriptor"/>s contributed by this plugin.
        /// Each descriptor defines a dockable panel’s metadata, content type or factory, and any host targeting or dependency rules.
        /// </summary>
        /// <returns>A sequence of <see cref="DockPanelDescriptor"/> instances describing the panels this plugin makes available to the host.</returns>
        IEnumerable<DockPanelDescriptor> GetDockPanelDescriptors();
    }
}