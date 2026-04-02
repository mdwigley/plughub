using Microsoft.Extensions.Logging;
using NucleusAF.Attributes;
using NucleusAF.Interfaces.Services.Configuration;
using NucleusAF.Interfaces.Services.Modules;
using NucleusAF.Models.Descriptors;
using NucleusAF.Models.Modules;
using System.Reflection;

namespace NucleusAF.Services.Modules
{
    public class ModuleRegistrar(ILogger<IModuleRegistrar> logger, IConfigAccessorFor<ModuleManifest> manifestAccessor, IModuleService moduleService, IModuleCache moduleCache) : IModuleRegistrar
    {
        private readonly ILogger<IModuleRegistrar> logger = logger ??
            throw new ArgumentNullException(nameof(logger));

        private readonly IConfigAccessorFor<ModuleManifest> manifestAccessor = manifestAccessor ??
            throw new ArgumentNullException(nameof(manifestAccessor));

        private readonly IModuleService moduleService = moduleService ??
            throw new ArgumentNullException(nameof(moduleService));

        private readonly IModuleCache moduleCache = moduleCache ??
            throw new ArgumentNullException(nameof(moduleCache));

        public bool IsEnabled(Guid moduleId, Type providerType) =>
            this.GetManifest().DescriptorStates.Any(s =>
                s.ModuleId == moduleId &&
                s.ProviderName == providerType.FullName &&
                s.Enabled);

        public void SetEnabled(Guid moduleId, Type providerType, bool enabled = true)
        {
            ModuleManifest manifest = this.GetManifest();
            DescriptorLoadState? state = manifest.DescriptorStates
                .FirstOrDefault(s => s.ModuleId == moduleId && s.ProviderName == providerType.FullName);

            if (state == null || state.System == true) return;
            if (state.Enabled == enabled) return;

            state.Enabled = enabled;

            this.SaveManifest(manifest);
            this.logger.LogInformation("[ModuleRegistrar] {Action} provider {Provider} of module {ModuleId}.", enabled ? "Enabled" : "Disabled", providerType.Name, moduleId);
        }
        public void SetAllEnabled(Guid moduleId, bool enabled = true)
        {
            ModuleManifest manifest = this.GetManifest();

            bool changed = false;

            foreach (DescriptorLoadState? state in manifest.DescriptorStates.Where(s => s.ModuleId == moduleId && s.System != true))
            {
                if (state.Enabled != enabled)
                {
                    state.Enabled = enabled;

                    changed = true;
                }
            }

            if (changed)
            {
                this.SaveManifest(manifest);

                this.logger.LogInformation("[ModuleRegistrar] {Action} all descriptors of module {ModuleId}.", enabled ? "Enabled" : "Disabled", moduleId);
            }
        }

        public ModuleManifest GetManifest()
        {
            ModuleManifest manifest = this.manifestAccessor.Get();

            if (manifest?.DescriptorStates == null)
            {
                this.logger.LogError("[ModuleRegistrar] Module manifest is unavailable or invalid (null or missing descriptor states).");

                throw new InvalidOperationException("Module manifest is unavailable or invalid.");
            }
            return manifest;
        }
        public void SaveManifest(ModuleManifest manifest)
        {
            Task.Run(() => this.SaveManifestAsync(manifest)).GetAwaiter().GetResult();
        }
        public async Task SaveManifestAsync(ModuleManifest manifest)
        {
            if (manifest?.DescriptorStates == null)
            {
                this.logger.LogError("[ModuleRegistrar] Attempted to save an invalid module manifest (null or missing descriptor states).");

                throw new ArgumentException("Manifest is invalid.", nameof(manifest));
            }

            try
            {
                await this.manifestAccessor.SaveAsync(manifest);

                this.logger.LogInformation("[ModuleRegistrar] Module manifest saved with {Count} descriptor states.", manifest.DescriptorStates.Count);
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "[ModuleRegistrar] Failed to save module manifest with {Count} descriptor states.", manifest.DescriptorStates?.Count ?? 0);

                throw;
            }
        }

        public List<Descriptor> GetDescriptorsForProvider(Type providerType)
        {
            ArgumentNullException.ThrowIfNull(providerType);

            List<Descriptor> allDescriptors = [];

            foreach (ModuleReference moduleReference in this.moduleCache.Modules)
            {
                IEnumerable<ProviderInterface> matchingProviders = moduleReference.Providers
                    .Where(pi => providerType.IsAssignableFrom(pi.InterfaceType));

                foreach (ProviderInterface? provider in matchingProviders)
                {
                    object? moduleInstance = this.moduleService.GetLoadedProviders<object>(provider);

                    if (moduleInstance == null)
                    {
                        this.logger.LogWarning("[ModuleRegistrar] Failed to instantiate provider {Provider} from module {Module}.", provider.InterfaceType.FullName, moduleReference.AssemblyName);

                        continue;
                    }

                    DescriptorProviderAttribute? attr = null;
                    Type[] allProviders = [provider.InterfaceType, .. provider.InterfaceType.GetInterfaces()];

                    foreach (Type pt in allProviders)
                    {
                        attr = pt.GetCustomAttribute<DescriptorProviderAttribute>(inherit: false);

                        if (attr != null) break;
                    }

                    if (attr == null)
                    {
                        this.logger.LogWarning("[ModuleRegistrar] Module provider {Provider} is missing ProvidesDescriptor attribute.", provider.InterfaceType.FullName);

                        continue;
                    }

                    string descriptorMethodName = attr.DescriptorAccessorName;

                    MethodInfo? descriptorMethod = provider.InterfaceType.GetMethod(descriptorMethodName, BindingFlags.Public | BindingFlags.Instance);

                    if (descriptorMethod == null)
                    {
                        this.logger.LogWarning("[ModuleRegistrar] Descriptor method {Method} not found on provider {Provider}.", descriptorMethodName, provider.InterfaceType.FullName);

                        continue;
                    }

                    object? descriptorsObj = descriptorMethod.Invoke(moduleInstance, null);

                    if (descriptorsObj is IEnumerable<Descriptor> descriptors)
                    {
                        allDescriptors.AddRange(descriptors);
                    }
                    else
                    {
                        this.logger.LogWarning("[ModuleRegistrar] Descriptor method {Method} on module provider {Provider} did not return expected descriptors.", descriptorMethodName, provider.InterfaceType.FullName);
                    }
                }
            }

            return allDescriptors;
        }
    }
}