using Avalonia.Controls;
using PlugHub.Plugin.DockHost.Controls;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace PlugHub.Plugin.DockHost.Models
{
    /// <summary>
    /// Represents the runtime state of a dockable panel, including its identifiers,
    /// UI chrome, and mutable state (pinned, visibility, dock edge).
    /// Provides change notifications for data binding and can project itself into
    /// a persistence-friendly <see cref="DockHostPanelData"/> for configuration storage.
    /// </summary>
    public class DockPanelState : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        #region DockPanelState: Identifiers

        /// <summary>
        /// The unique identifier of this panel instance within the dock host.
        /// Assigned and persisted by the host; consumers cannot override it.
        /// </summary>
        public Guid ControlId { get; init; } = Guid.NewGuid();

        /// <summary>
        /// The unique identifier of the plugin (or module) that produced this panel.
        /// This ties the panel back to its originating plugin for discovery or grouping.
        /// </summary>
        public Guid PluginId { get; init; }

        /// <summary>
        /// The identifier of the panel descriptor that defines the panel’s type/recipe.
        /// This allows the host to reconstitute the correct kind of panel on load.
        /// </summary>
        public Guid DescriptorId { get; init; }

        /// <summary>
        /// The identifier of the DockControl that owns this panel.
        /// Used to associate the panel with its parent dock host in persistence.
        /// </summary>
        public Guid DockControlId { get; init; }

        #endregion

        #region DockPanelState: Panel Chrome

        /// <summary>
        /// The display text shown in the panel’s header area.
        /// Typically provided by the plugin or caller when the panel is created.
        /// </summary>
        public string Header { get; init; }

        /// <summary>
        /// The live Avalonia control representing this panel at runtime.
        /// Can be shown/hidden and binds back to this state object.
        /// </summary>
        public DockablePanel DockablePanel { get; init; }

        #endregion

        #region DockPanelState: Panel States

        private bool isPinned;
        /// <summary>
        /// Determines whether the panel is docked in the main control area (true) or lives inside a flyout (false).
        /// </summary>
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
        /// <summary>
        /// Only meaningful for flyouts. Controls whether this panel is currently visible within a shared flyout container. 
        /// Multiple panels can share the same flyout, so visibility is per-panel.
        /// </summary>
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
        /// <summary>
        /// The edge of the host (Left, Right, Top, Bottom) that this panel is pinned or attached to.
        /// </summary>
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

        #endregion

        public DockPanelState(string header, Control control, Dock edge = Dock.Left, bool pinned = false, bool visible = false, Guid controlId = default, Guid descriptorId = default, Guid pluginId = default, Guid dockControlId = default)
        {
            ArgumentNullException.ThrowIfNull(header);
            ArgumentNullException.ThrowIfNull(control);

            this.Header = header;
            this.isPinned = pinned;
            this.isVisible = visible;
            this.dockEdge = edge;

            this.ControlId = controlId == Guid.Empty ? Guid.NewGuid() : controlId;
            this.DescriptorId = descriptorId;
            this.PluginId = pluginId;
            this.DockControlId = dockControlId;

            this.DockablePanel = new DockablePanel
            {
                Header = this.Header,
                Content = control,
                PanelState = this
            };
        }

        /// <summary>
        /// Raised whenever a property value changes on this state object.
        /// Enables data binding and UI updates through <see cref="INotifyPropertyChanged"/>.
        /// </summary>
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        /// <summary>
        /// Projects the current runtime state into a persistence-friendly DTO
        /// <see cref="DockHostPanelData"/> that can be stored by the configuration system.
        /// </summary>
        public virtual DockHostPanelData ToConfig()
        {
            return new DockHostPanelData()
            {
                ControlID = this.ControlId,
                PluginID = this.PluginId,
                DescriptorID = this.DescriptorId,
                DockControlID = this.DockControlId,
                IsPinned = this.isPinned,
                DockEdge = this.DockEdge
            };
        }
    }
}