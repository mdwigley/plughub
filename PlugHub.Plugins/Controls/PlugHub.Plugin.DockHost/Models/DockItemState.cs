using Avalonia.Controls;
using Avalonia.Metadata;
using PlugHub.Plugin.Controls.Interfaces.Controls;
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
    public class DockItemState : INotifyPropertyChanged, ISwitchable
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        #region DockPanelState: Identifiers

        /// <summary>
        /// The unique identifier of this panel instance within the dock host.
        /// Assigned and persisted by the host; consumers cannot override it.
        /// </summary>
        public Guid ControlId { get; set; } = Guid.NewGuid();

        /// <summary>
        /// The unique identifier of the plugin (or module) that produced this panel.
        /// This ties the panel back to its originating plugin for discovery or grouping.
        /// </summary>
        public Guid PluginId { get; set; }

        /// <summary>
        /// The identifier of the panel descriptor that defines the panel’s type/recipe.
        /// This allows the host to reconstitute the correct kind of panel on load.
        /// </summary>
        public Guid DescriptorId { get; set; }

        /// <summary>
        /// The identifier of the DockControl that owns this panel.
        /// Used to associate the panel with its parent dock host in persistence.
        /// </summary>
        public Guid DockControlId { get; set; }

        #endregion

        #region DockPanelState: Panel Chrome

        /// <summary>
        /// The display text shown in the panel’s header area.
        /// Typically provided by the plugin or caller when the panel is created.
        /// </summary>
        public string Header { get; set; }

        /// <summary>
        /// The live Avalonia control representing this panel at runtime.
        /// Can be shown/hidden and binds back to this state object.
        /// </summary>
        [Content]
        public Control Content { get; set; }

        #endregion

        #region DockPanelState: Panel States

        private int sortOrder;
        /// <summary>
        /// Global sort order in the flattened sequence of all panels across all slices.
        /// This value is used to normalize and restore the relative ordering regardless
        /// of which slice (pinned/unpinned, edge) the panel belongs to.
        /// </summary>
        public int SortOrder
        {
            get => this.sortOrder;
            set
            {
                if (this.sortOrder != value)
                {
                    this.sortOrder = value;
                    this.OnPropertyChanged();
                }
            }
        }

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

        private bool canClose = true;
        /// <summary>
        /// Determines whether the panel can be closed by the user. Defaults to true.
        /// </summary>
        public bool CanClose
        {
            get => this.canClose;
            set
            {
                if (this.canClose != value)
                {
                    this.canClose = value;
                    this.OnPropertyChanged();
                }
            }
        }

        #endregion

        public DockItemState()
            : this(header: string.Empty, control: new ContentControl(), sortOrder: 0, edge: Dock.Left, pinned: false, controlId: default, descriptorId: default, pluginId: default, dockControlId: default) { }
        public DockItemState(string header, Control control, int sortOrder = 0, Dock edge = Dock.Left, bool pinned = false, Guid controlId = default, Guid descriptorId = default, Guid pluginId = default, Guid dockControlId = default, bool canClose = true)
        {
            ArgumentNullException.ThrowIfNull(header);
            ArgumentNullException.ThrowIfNull(control);

            this.Header = header;
            this.SortOrder = sortOrder;
            this.isPinned = pinned;
            this.dockEdge = edge;
            this.canClose = canClose;

            this.ControlId = controlId == Guid.Empty ? Guid.NewGuid() : controlId;
            this.DescriptorId = descriptorId;
            this.PluginId = pluginId;
            this.DockControlId = dockControlId;

            this.Content = new DockItem
            {
                Header = this.Header,
                Content = control,
                DataContext = this
            };
        }

        public DockItemState Normalize(DockControl owner)
        {
            // Ensure identity
            if (this.ControlId == Guid.Empty)
                this.ControlId = Guid.NewGuid();
            if (this.DockControlId == Guid.Empty)
                this.DockControlId = owner.DockId;

            // Already a DockablePanel with correct DataContext
            if (this.Content is DockItem dp && dp.DataContext == this)
            {
                dp.Header = this.Header;
                return this;
            }

            // It's a DockablePanel but not wired correctly
            if (this.Content is DockItem raw)
            {
                raw.DataContext = this;
                raw.Header = this.Header;
                this.Content = raw;
                return this;
            }

            // Otherwise wrap whatever content was provided
            this.Content = new DockItem
            {
                Header = this.Header,
                Content = this.Content,
                DataContext = this,
                CanClose = this.CanClose
            };
            this.OnPropertyChanged(nameof(this.Content));

            return this;
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
                SortOrder = this.SortOrder,
                PluginID = this.PluginId,
                DescriptorID = this.DescriptorId,
                DockControlID = this.DockControlId,
                IsPinned = this.isPinned,
                DockEdge = this.DockEdge
            };
        }

        /// <summary>
        /// Updates this <see cref="DockItemState"/> from a persisted
        /// <see cref="DockHostPanelData"/> DTO.
        /// </summary>
        /// <param name="data">The persisted panel data to apply.</param>
        public virtual void FromConfig(DockHostPanelData data)
        {
            ArgumentNullException.ThrowIfNull(data);

            this.ControlId = data.ControlID == Guid.Empty ? this.ControlId : data.ControlID;
            this.SortOrder = data.SortOrder;
            this.PluginId = data.PluginID;
            this.DescriptorId = data.DescriptorID;
            this.DockControlId = data.DockControlID;

            this.IsPinned = data.IsPinned;
            this.DockEdge = data.DockEdge;

            // Ensure Content is still a DockablePanel bound to this state
            if (this.Content is DockItem dp)
            {
                dp.Header = this.Header;
                dp.DataContext = this;
            }
            else
            {
                this.Content = new DockItem
                {
                    Header = this.Header,
                    Content = this.Content,
                    DataContext = this,
                    CanClose = this.CanClose
                };
            }
        }
    }
}