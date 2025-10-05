using Avalonia.Controls;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PlugHub.Plugin.DockHost.Controls;
using PlugHub.Plugin.DockHost.Interfaces.Plugins;
using PlugHub.Plugin.DockHost.Interfaces.Services;
using PlugHub.Plugin.DockHost.Models;
using PlugHub.Shared.Interfaces.Services.Plugins;
using System.Collections.Concurrent;

namespace PlugHub.Plugin.DockHost.Services
{
    public class DockService : IDockService
    {
        private readonly ILogger<IDockService> logger;
        private readonly List<DockPanelDescriptor> descriptors;
        private readonly IServiceProvider serviceProvider;

        private readonly ConcurrentDictionary<Guid, DockControl> controlsById = new();
        private readonly ConcurrentDictionary<Guid, List<DockPanelItem>> panelsByControl = new();

        public event EventHandler<DockPanelChangedEventArgs>? PanelsChanged;
        public event EventHandler<DockControlChangedEventArgs>? DockControlChanged;

        public DockService(ILogger<IDockService> logger, IServiceProvider serviceProvider, IPluginResolver pluginResolver, IEnumerable<IPluginDockPanels> panelProviders)
        {
            ArgumentNullException.ThrowIfNull(logger);
            ArgumentNullException.ThrowIfNull(serviceProvider);
            ArgumentNullException.ThrowIfNull(pluginResolver);
            ArgumentNullException.ThrowIfNull(panelProviders);

            this.logger = logger;
            this.serviceProvider = serviceProvider;

            List<DockPanelDescriptor> allDescriptors = [];

            foreach (IPluginDockPanels provider in panelProviders)
            {
                IEnumerable<DockPanelDescriptor> descriptorsFromPlugin = provider.GetDockPanelDescriptors();

                if (descriptorsFromPlugin != null)
                    allDescriptors.AddRange(descriptorsFromPlugin);
            }

            this.descriptors = [.. pluginResolver.ResolveDescriptors(allDescriptors)];
            this.logger.LogInformation("[DockService] Cached {PanelCount} dock panel descriptors.", this.descriptors.Count);

            PanelsChanged?.Invoke(this, new DockPanelChangedEventArgs(Guid.Empty, null!, DockPanelChangeType.Reset));
        }

        #region DockService: Control Handling

        public void RegisterDockControl(Control control)
        {
            ArgumentNullException.ThrowIfNull(control);

            if (control is not DockControl dock)
            {
                this.logger.LogWarning("Attempted to register control of type {ControlType}, but only DockControl instances are supported.", control.GetType().Name);

                return;
            }

            this.controlsById[dock.DockId] = dock;
            this.logger.LogInformation("Registered dock control {ControlId} ({ControlType})", dock.DockId, dock.GetType().Name);

            DockControlChanged?.Invoke(this, new DockControlChangedEventArgs(dock.DockId, DockControlChangeType.Registered));
        }
        public void UnregisterDockControl(Guid controlId)
        {
            if (this.controlsById.TryRemove(controlId, out DockControl? removed))
            {
                this.logger.LogInformation("Unregistered dock control {ControlId} ({ControlType})", controlId, removed.GetType().Name);

                DockControlChanged?.Invoke(this, new DockControlChangedEventArgs(controlId, DockControlChangeType.Unregistered));
            }
        }

        public IReadOnlyList<DockPanelDescriptor> GetDescriptorsForHost(Guid dockId)
        {
            return [.. this.descriptors.Where(d => d.TargetedHosts == null || d.TargetedHosts.Contains(dockId))];
        }
        public DockPanelDescriptor? FindDescriptor(Guid dockId, Guid descriptorId)
        {
            return this.GetDescriptorsForHost(dockId)
                       .FirstOrDefault(d => d.DescriptorID == descriptorId);
        }


        #endregion

        #region DockService: Panel Handling

        public void RegisterPanel(DockPanelDescriptor descriptor, IPluginResolver resolver)
        {
            IEnumerable<DockPanelDescriptor> newDescriptors = this.descriptors.Concat([descriptor]);
            IEnumerable<DockPanelDescriptor> resolved = resolver.ResolveDescriptors(newDescriptors);

            this.descriptors.Clear();
            this.descriptors.AddRange(resolved);

            this.logger.LogInformation("Registered panel {Header} ({PanelId})", descriptor.Header, descriptor.DescriptorID);

            // Raise event for listeners
            DockPanelItem item = new(descriptor.DescriptorID, descriptor.Header, descriptor.Icon, this, Guid.Empty);

            PanelsChanged?.Invoke(this, new DockPanelChangedEventArgs(Guid.Empty, item, DockPanelChangeType.Added));
        }
        public void RemovePanel(Guid panelId)
        {
            DockPanelDescriptor? descriptor = this.descriptors.FirstOrDefault(d => d.DescriptorID == panelId);

            if (descriptor is null)
            {
                this.logger.LogWarning("Attempted to remove panel {PanelId}, but no descriptor was found.", panelId);
                return;
            }

            this.descriptors.Remove(descriptor);
            this.logger.LogInformation("Removed panel {Header} ({PanelId})", descriptor.Header, descriptor.DescriptorID);

            // Raise event for listeners
            DockPanelItem item = new(descriptor.DescriptorID, descriptor.Header, descriptor.Icon, this, Guid.Empty);

            PanelsChanged?.Invoke(this, new DockPanelChangedEventArgs(Guid.Empty, item, DockPanelChangeType.Removed));
        }

        public IReadOnlyList<DockPanelItem> GetPanelItems(Guid controlId)
        {
            List<DockPanelItem> items = [];

            foreach (DockPanelDescriptor descriptor in this.descriptors)
            {
                if (descriptor.TargetedHosts != null)
                {
                    if (descriptor.TargetedHosts.Contains(controlId))
                        items.Add(new DockPanelItem(descriptor.DescriptorID, descriptor.Header, descriptor.Icon, this, controlId));
                }
                else
                {
                    items.Add(new DockPanelItem(descriptor.DescriptorID, descriptor.Header, descriptor.Icon, this, controlId));
                }
            }

            return items;
        }

        public DockPanelState? RequestPanel(Guid controlId, Guid panelId, Dock edge = Dock.Left, bool pinned = false)
        {
            this.logger.LogInformation("Requesting panel {PanelId} for control {ControlId}", panelId, controlId);

            DockPanelDescriptor? descriptor = this.descriptors.FirstOrDefault(d => d.DescriptorID == panelId);

            if (descriptor is null)
            {
                this.logger.LogWarning("No descriptor found for panel {PanelId}", panelId);

                return null;
            }

            if (!this.controlsById.TryGetValue(controlId, out DockControl? targetControl))
            {
                this.logger.LogWarning("No control registered with id {ControlId}", controlId);

                return null;
            }

            if (descriptor.TargetedHosts != null && !descriptor.TargetedHosts.Contains(controlId))
            {
                this.logger.LogInformation("Descriptor {PanelId} not targeted for control {ControlId}", panelId, controlId);

                return null;
            }

            Control? content = null;

            if (descriptor.ContentType is not null)
            {
                content = this.serviceProvider.GetService(descriptor.ContentType) as Control
                          ?? (Control)ActivatorUtilities.CreateInstance(this.serviceProvider, descriptor.ContentType);

                if (descriptor.ViewModelType is not null && content is not null)
                {
                    object vm = this.serviceProvider.GetService(descriptor.ViewModelType)
                             ?? ActivatorUtilities.CreateInstance(this.serviceProvider, descriptor.ViewModelType);
                    content.DataContext = vm;
                }
            }
            else if (descriptor.Factory is not null)
            {
                content = descriptor.Factory(this.serviceProvider);
            }

            if (content is null)
            {
                this.logger.LogWarning("Descriptor {PanelId} did not provide a valid ContentType, ViewModelType, or Factory", panelId);

                return null;
            }

            DockPanelState state = new(
                descriptor.Header,
                content,
                edge: edge,
                pinned: pinned,
                visible: false,
                pluginId: descriptor.PluginID,
                descriptorId: descriptor.DescriptorID,
                controlId: controlId
            );

            targetControl.Register(state);

            this.logger.LogInformation("Panel {PanelId} added to control {ControlId}", panelId, controlId);

            return state;
        }

        #endregion

        #region DockService: Panel Persistence

        public void Save()
        {

        }

        #endregion
    }
}