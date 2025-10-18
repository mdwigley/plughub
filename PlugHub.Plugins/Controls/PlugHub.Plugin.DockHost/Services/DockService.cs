using Avalonia;
using Avalonia.Controls;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PlugHub.Plugin.DockHost.Controls;
using PlugHub.Plugin.DockHost.Interfaces.Plugins;
using PlugHub.Plugin.DockHost.Interfaces.Services;
using PlugHub.Plugin.DockHost.Models;
using PlugHub.Shared.Interfaces.Accessors;
using PlugHub.Shared.Interfaces.Services.Configuration;
using PlugHub.Shared.Interfaces.Services.Plugins;
using PlugHub.Shared.Models;
using PlugHub.Shared.Models.Configuration.Parameters;
using System.Collections.Concurrent;

namespace PlugHub.Plugin.DockHost.Services
{
    public class DockService : IDockService
    {
        private readonly ILogger<IDockService> logger;
        private readonly IConfigService configService;
        private readonly IConfigAccessorFor<DockHostData> configAccessor;
        private readonly List<DockPanelDescriptor> descriptors;
        private readonly IServiceProvider serviceProvider;

        private readonly ConcurrentDictionary<Guid, DockControl> controlsById = new();
        private readonly ConcurrentDictionary<Guid, List<DockItemEntry>> panelsByControl = new();

        public event EventHandler<DockPanelChangedEventArgs>? PanelsChanged;
        public event EventHandler<DockControlChangedEventArgs>? DockControlChanged;
        public event EventHandler<DockControlReadyEventArgs>? DockControlReady;

        public DockService(ILogger<IDockService> logger, IServiceProvider serviceProvider, IConfigService configService, IPluginResolver pluginResolver, IEnumerable<IPluginDockPanels> panelProviders)
        {
            ArgumentNullException.ThrowIfNull(logger);
            ArgumentNullException.ThrowIfNull(serviceProvider);
            ArgumentNullException.ThrowIfNull(configService);
            ArgumentNullException.ThrowIfNull(pluginResolver);
            ArgumentNullException.ThrowIfNull(panelProviders);

            this.logger = logger;
            this.serviceProvider = serviceProvider;
            this.configService = configService;

            this.configService.RegisterConfig(
                new ConfigFileParams(Owner: Token.New(), Read: Token.Blocked, Write: Token.Blocked),
                out this.configAccessor
            );

            List<DockPanelDescriptor> allDescriptors = [];

            foreach (IPluginDockPanels provider in panelProviders)
            {
                IEnumerable<DockPanelDescriptor> descriptorsFromPlugin = provider.GetDockPanelDescriptors();

                if (descriptorsFromPlugin != null)
                    allDescriptors.AddRange(descriptorsFromPlugin);
            }

            this.descriptors = [.. pluginResolver.ResolveDescriptors(allDescriptors)];
            this.logger.LogInformation("[DockService] Cached {PanelCount} dock panel descriptors.", this.descriptors.Count);

            PanelsChanged?.Invoke(this, new DockPanelChangedEventArgs(null!, DockPanelChangeType.Reset));
        }

        #region DockService: Control Handling

        public DockHostControlData? RegisterDockControl(Control control)
        {
            ArgumentNullException.ThrowIfNull(control);

            if (control is not DockControl dock)
            {
                this.logger.LogWarning("Attempted to register control of type {ControlType}, but only DockControl instances are supported.", control.GetType().Name);

                throw new InvalidOperationException("Attempted to register control of type unsupported type. MUst be a DockControl.");
            }

            this.controlsById[dock.DockId] = dock;
            this.logger.LogInformation("Registered dock control {ControlId} ({ControlType})", dock.DockId, dock.GetType().Name);

            DockControlChanged?.Invoke(this, new DockControlChangedEventArgs(dock, DockControlChangeType.Registered));

            DockHostData data = this.configAccessor.Get();
            DockHostControlData? controlData = data.DockHostControlDataItems.FirstOrDefault(d => d.ControlID == dock.DockId);

            control.AddHandler(DockControl.DockControlReadyEvent, (_, __) =>
            {
                DockControlReady?.Invoke(this, new DockControlReadyEventArgs(dock));
            });

            return controlData;
        }
        public void UnregisterDockControl(Guid controlId, bool save = true)
        {
            if (this.controlsById.TryRemove(controlId, out DockControl? removed))
            {
                this.logger.LogInformation("Unregistered dock control {ControlId} ({ControlType})", controlId, removed.GetType().Name);

                DockControlChanged?.Invoke(this, new DockControlChangedEventArgs(removed, DockControlChangeType.Unregistered));

                if (save)
                {
                    DockHostControlData? dto = removed.ToConfig();

                    if (dto == null) return;

                    DockHostData hostData = this.configAccessor.Get();

                    int existing = hostData.DockHostControlDataItems.FindIndex(d => d.ControlID == controlId);

                    if (existing >= 0)
                        hostData.DockHostControlDataItems[existing] = dto;
                    else
                        hostData.DockHostControlDataItems.Add(dto);

                    this.configAccessor.Save(hostData);
                }
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

            DockItemEntry item = new(descriptor.DescriptorID, descriptor.Header, descriptor.Icon, descriptor.Group, descriptor.Tags, this, Guid.Empty);

            PanelsChanged?.Invoke(this, new DockPanelChangedEventArgs(item, DockPanelChangeType.Added));
        }
        public void RemovePanel(Guid panelId)
        {
            DockPanelDescriptor? descriptor = this.descriptors.FirstOrDefault(d => d.DescriptorID == panelId);

            if (descriptor == null)
            {
                this.logger.LogWarning("Attempted to remove panel {PanelId}, but no descriptor was found.", panelId);
                return;
            }

            this.descriptors.Remove(descriptor);
            this.logger.LogInformation("Removed panel {Header} ({PanelId})", descriptor.Header, descriptor.DescriptorID);

            DockItemEntry item = new(descriptor.DescriptorID, descriptor.Header, descriptor.Icon, descriptor.Group, descriptor.Tags, this, Guid.Empty);

            PanelsChanged?.Invoke(this, new DockPanelChangedEventArgs(item, DockPanelChangeType.Removed));
        }

        public IReadOnlyList<DockItemEntry> GetPanelItems(Guid controlId)
        {
            List<DockItemEntry> items = [];

            foreach (DockPanelDescriptor descriptor in this.descriptors)
            {
                if (descriptor.TargetedHosts != null)
                {
                    if (descriptor.TargetedHosts.Contains(controlId))
                        items.Add(new DockItemEntry(descriptor.DescriptorID, descriptor.Header, descriptor.Icon, descriptor.Group, descriptor.Tags, this, controlId));
                }
                else
                {
                    items.Add(new DockItemEntry(descriptor.DescriptorID, descriptor.Header, descriptor.Icon, descriptor.Group, descriptor.Tags, this, controlId));
                }
            }

            return items;
        }

        public DockItemState? RequestPanel(Guid controlId, Guid dockControlId, Guid descriptorId, int sortOrder = 0, Dock edge = Dock.Left, bool pinned = false, bool canClose = true)
        {
            this.logger.LogInformation("Requesting panel {DescriptorId} for control {ControlId}", descriptorId, dockControlId);

            DockPanelDescriptor? descriptor = this.descriptors.FirstOrDefault(d => d.DescriptorID == descriptorId);

            if (descriptor == null)
                this.logger.LogWarning("No descriptor found for panel {DescriptorId}", descriptorId);

            if (!this.controlsById.TryGetValue(dockControlId, out DockControl? targetControl))
            {
                this.logger.LogWarning("No control registered with id {ControlId}", dockControlId);

                return null;
            }

            if (descriptor?.TargetedHosts != null && !descriptor.TargetedHosts.Contains(dockControlId))
            {
                this.logger.LogInformation("Descriptor {PanelId} not targeted for control {ControlId}", descriptorId, dockControlId);

                return null;
            }

            Control content = this.InstantiatePanelContent(descriptor, targetControl);

            DockItemState state = new(
                header: descriptor?.Header ?? "Unavailable",
                control: content,
                sortOrder: sortOrder,
                edge: edge,
                pinned: pinned,
                controlId: controlId,
                pluginId: descriptor?.PluginID ?? Guid.Empty,
                descriptorId: descriptorId,
                dockControlId: dockControlId,
                canClose: canClose
            );

            targetControl.AddPanel(state);

            this.logger.LogInformation("Panel {DescriptorId} added to control {ControlId}", descriptorId, dockControlId);

            return state;
        }
        protected virtual Control InstantiatePanelContent(DockPanelDescriptor? descriptor, DockControl dockControl)
        {
            Control? content = null;

            if (descriptor != null)
            {
                if (descriptor.ContentType != null)
                {
                    content = this.serviceProvider.GetService(descriptor.ContentType) as Control
                              ?? (Control)ActivatorUtilities.CreateInstance(this.serviceProvider, descriptor.ContentType);

                    if (descriptor.ViewModelType != null && content != null)
                    {
                        object vm = this.serviceProvider.GetService(descriptor.ViewModelType)
                                 ?? ActivatorUtilities.CreateInstance(this.serviceProvider, descriptor.ViewModelType);
                        content.DataContext = vm;
                    }
                }
                else if (descriptor.Factory != null)
                {
                    content = descriptor.Factory(this.serviceProvider);
                }
            }

            if (content == null)
            {
                Avalonia.Controls.Templates.IDataTemplate? template =
                    dockControl.GetValue(DockControl.DockControlMissingPanelProperty);

                if (template != null)
                    content = template.Build(descriptor) as Control;

                content ??= new TextBlock
                {
                    Text = $"Panel not found: {descriptor?.DescriptorID}",
                    Margin = new Thickness(8)
                };
            }

            return content;
        }

        #endregion

        #region DockService: Panel Persistence

        public void SaveDockControl(DockControl dockControl)
        {
            if (dockControl == null) return;

            DockHostControlData? dto = dockControl.ToConfig();
            DockHostData config = this.configAccessor.Get();

            if (config == null || dto == null) return;

            DockHostControlData? existing = config.DockHostControlDataItems.FirstOrDefault(x => x.ControlID == dto.ControlID);

            if (existing != null)
            {
                int index = config.DockHostControlDataItems.IndexOf(existing);

                config.DockHostControlDataItems[index] = dto;
            }
            else
            {
                config.DockHostControlDataItems.Add(dto);
            }

            TaskCompletionSource tcs = new();

            void Handler(object? s, ConfigServiceSaveCompletedEventArgs e)
            {
                if (e.ConfigType == typeof(DockHostData))
                {
                    this.configService.SyncSaveCompleted -= Handler;

                    tcs.TrySetResult();
                }
            }

            this.configService.SyncSaveCompleted += Handler;

            try
            {
                this.configAccessor.SaveAsync(config);

                tcs.Task.GetAwaiter().GetResult();
            }
            finally
            {
                this.configService.SyncSaveCompleted -= Handler;
            }
        }

        #endregion
    }
}