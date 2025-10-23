using Avalonia.Controls;
using PlugHub.Plugin.DockHost.Controls;
using PlugHub.Plugin.DockHost.Interfaces.Plugins;
using PlugHub.Plugin.DockHost.Models;
using PlugHub.Shared.Interfaces.Services.Plugins;

namespace PlugHub.Plugin.DockHost.Interfaces.Services
{
    /// <summary>
    /// Indicates the type of change that occurred to a dock panel item.
    /// </summary>
    public enum DockPanelChangeType
    {
        /// <summary>
        /// A new panel item was added.
        /// </summary>
        Added,

        /// <summary>
        /// An existing panel item was removed.
        /// </summary>
        Removed,

        /// <summary>
        /// An existing panel item was updated.
        /// </summary>
        Updated,

        /// <summary>
        /// The set of panel items was reset or reloaded.
        /// </summary>
        Reset
    }

    /// <summary>
    /// Indicates the type of change that occurred to a dock control.
    /// </summary>
    public enum DockControlChangeType
    {
        /// <summary>
        /// A new dock control was registered.
        /// </summary>
        Registered,

        /// <summary>
        /// An existing dock control was unregistered.
        /// </summary>
        Unregistered
    }

    /// <summary>
    /// Provides event data for dock panel change notifications.
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of the <see cref="DockPanelChangedEventArgs"/> class.
    /// </remarks>
    /// <param name="item">The panel item that changed.</param>
    /// <param name="changeType">The type of change that occurred.</param>
    public sealed class DockPanelChangedEventArgs(DockItemEntry item, DockPanelChangeType changeType) : EventArgs
    {
        /// <summary>
        /// Gets the panel item involved in the change.
        /// </summary>
        public DockItemEntry Item { get; } = item;

        /// <summary>
        /// Gets the type of change that occurred.
        /// </summary>
        public DockPanelChangeType ChangeType { get; } = changeType;
    }

    /// <summary>
    /// Provides event data for dock control change notifications.
    /// </summary>
    /// <param name="control">The dock control that changed.</param>
    /// <param name="changeType">The type of change that occurred.</param>
    public sealed class DockControlChangedEventArgs(DockControl control, DockControlChangeType changeType) : EventArgs
    {
        /// <summary>
        /// Gets the identifier of the dock control associated with the change.
        /// </summary>
        public DockControl Control { get; } = control;

        /// <summary>
        /// Gets the type of change that occurred.
        /// </summary>
        public DockControlChangeType ChangeType { get; } = changeType;
    }

    public sealed class DockControlReadyEventArgs(DockControl control) : EventArgs
    {
        public DockControl Control { get; } = control;
    }

    /// <summary>
    /// Defines the contract for managing dock hosts and plugin-provided panels.
    /// Responsible for registering dock controls, exposing available panels,
    /// and instantiating requested panel content.
    /// </summary>
    public interface IDockService
    {
        /// <summary>
        /// Occurs when the set of available dock panels changes for a control.
        /// </summary>
        public event EventHandler<DockPanelChangedEventArgs>? PanelsChanged;

        /// <summary>
        /// Occurs when a dock control is registered or unregistered.
        /// </summary>
        public event EventHandler<DockControlChangedEventArgs>? DockControlChanged;


        public event EventHandler<DockControlReadyEventArgs> DockControlReady;

        /// <summary>
        /// Finds a registered panel descriptor by its unique identifier for the given dock host.
        /// </summary>
        /// <param name="dockId">The identifier of the dock host.</param>
        /// <param name="descriptorId">The unique identifier of the panel descriptor.</param>
        /// <returns>The matching <see cref="DockPanelDescriptor"/> if found; otherwise <c>null</c>.</returns>
        public DockPanelDescriptor? FindDescriptor(Guid dockId, Guid descriptorId);

        /// <summary>
        /// Retrieves all panel descriptors that are valid for the specified dock host.
        /// </summary>
        /// <param name="dockId">The identifier of the dock host.</param>
        /// <returns>A read-only list of descriptors available to that host.</returns>
        public IReadOnlyList<DockPanelDescriptor> GetDescriptorsForHost(Guid dockId);

        /// <summary>
        /// Retrieves the list of available panel items for a given dock control.
        /// </summary>
        /// <param name="controlId">The identifier of the dock control.</param>
        /// <returns>A read-only list of panel items that can be requested for this control.</returns>
        public IReadOnlyList<DockItemEntry> GetPanelItems(Guid controlId);

        /// <summary>
        /// Registers a dock host control with the service and returns its persisted state,
        /// if any exists. This allows the control to reconstitute its panels from the
        /// previously saved configuration.
        /// </summary>
        /// <param name="control"> The Avalonia control instance to register. Only <see cref="Controls.DockControl"/> instances are meaningful, but the signature accepts <see cref="Control"/> for shared interface compatibility.</param>
        /// <returns>
        /// A <see cref="DockHostControlData"/> representing the last known persisted state of the control, or a new/empty instance if no prior state was found.
        /// </returns>
        public DockHostControlData? RegisterDockControl(Control control);

        /// <summary>
        /// Unregisters a previously registered dock control. Optionally saves its current state back into the persisted <see cref="DockHostData"/>.
        /// </summary>
        /// <param name="controlId">The identifier of the control to remove.</param>
        /// <param name="save">When <c>true</c>, the control's state is persisted on unregister. Defaults to <c>false</c>.</param>
        public void UnregisterDockControl(Guid controlId, bool save = false);

        /// <summary>
        /// Registers a new panel descriptor with the service.
        /// The descriptor is resolved through the provided plugin resolver
        /// to enforce ordering, dependencies, and conflicts.
        /// </summary>
        /// <param name="descriptor">The panel descriptor to register.</param>
        /// <param name="resolver">The plugin resolver used to validate and merge descriptors.</param>
        public void RegisterPanel(DockPanelDescriptor descriptor, IPluginResolver resolver);

        /// <summary>
        /// Requests that a panel be instantiated and added to the specified dock control,
        /// with optional initial docking configuration and global sort order.
        /// </summary>
        /// <param name="controlId">The unique identifier of the panel instance. Pass <see cref="Guid.Empty"/> to create a new panel identity, or supply a persisted value to reconstitute an existing panel.</param>
        /// <param name="dockControlId">The identifier of the dock control (host) that will own this panel.</param>
        /// <param name="descriptorId">The identifier of the panel descriptor that defines what type of panel to instantiate.</param>
        /// <param name="sortOrder">The global sort order (flattened across all slices) to assign to the panel. Defaults to <c>0</c>.</param>
        /// <param name="edge">The dock edge where the panel should be placed. Defaults to <see cref="Dock.Left"/>.</param>
        /// <param name="pinned">Whether the panel should be pinned when created. Defaults to <c>false</c>.</param>
        /// <param name="canClose">Whether the panel can be closed by the user. Defaults to <c>true</c>.</param>
        /// <returns>The created <see cref="DockItemState"/> if the panel was successfully instantiated and added; otherwise <c>null</c>.</returns>
        DockItemState? RequestPanel(Guid controlId, Guid dockControlId, Guid descriptorId, int sortOrder = 0, Dock edge = Dock.Left, bool pinned = false, bool canClose = true);

        /// <summary>
        /// Persists the current layout and state of the specified <see cref="DockControl"/>.
        /// </summary>
        /// <param name="dockControl">The dock control instance whose configuration (panel positions, pinned state, flyout lengths, etc.) should be saved to the underlying storage or service.</param>
        /// <remarks>
        /// Implementations are responsible for determining how and where the state is stored (e.g. in memory, configuration files, or a database). This method is typically invoked after structural changes such as panel reorder, pin/unpin, or flyout toggle.
        /// </remarks>
        public void SaveDockControl(DockControl dockControl);
    }
}