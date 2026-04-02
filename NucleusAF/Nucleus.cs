using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NucleusAF.Attributes;
using NucleusAF.Interfaces.Platform.Storage;
using NucleusAF.Interfaces.Providers;
using NucleusAF.Interfaces.Services.Capabilities;
using NucleusAF.Interfaces.Services.Configuration;
using NucleusAF.Interfaces.Services.Encryption;
using NucleusAF.Interfaces.Services.Modules;
using NucleusAF.Models;
using NucleusAF.Models.Capabilities;
using NucleusAF.Models.Configuration.Parameters;
using NucleusAF.Models.Descriptors;
using NucleusAF.Models.Modules;
using NucleusAF.Platform.Storage;
using NucleusAF.Services.Capabilities;
using NucleusAF.Services.Capabilities.Accessors;
using NucleusAF.Services.Capabilities.Handlers;
using NucleusAF.Services.Configuration;
using NucleusAF.Services.Configuration.Accessors;
using NucleusAF.Services.Configuration.Handlers;
using NucleusAF.Services.Encryption;
using NucleusAF.Services.Modules;
using NucleusAF.Utility;
using Serilog;
using System.Reflection;
using System.Text.Json;

namespace NucleusAF
{
    public class Nucleus
    {
        public static IServiceProvider Materialize(IServiceCollection serviceCollection, CapabilityToken? token = null)
        {
            ArgumentNullException.ThrowIfNull(serviceCollection);

            CapabilityToken t = token ?? new CapabilityToken(Guid.NewGuid());

            AppConfig appConfig = new();
            ModuleManifest baseManifest;
            AppConfig sysAppConfig;
            ModuleManifest moduleManifest;

            serviceCollection.AddLogging(builder =>
            {
                string temp = Path.GetTempPath();

                Log.Logger = new LoggerConfiguration()
                    .MinimumLevel.Information()
                    .WriteTo.File(Path.Combine(temp, $"NucleusAF.log"), rollingInterval: RollingInterval.Infinite)
                    .CreateLogger();

                builder.ClearProviders();
                builder.AddSerilog(Log.Logger, dispose: false);
            });

            CollectServices(serviceCollection, t);

            IConfigService baseConfigService = GetJsonConfigHandler(serviceCollection, appConfig);

            AppConfig baseConfig = GetBaseAppConfig(baseConfigService, appConfig, t);

            GetEnvConfig().Bind(baseConfig);

            // Initialize an empty module reference collection
            IEnumerable<ModuleReference> modules = [];

            // Build a temporary DI provider (to resolve things like logging, services, etc.)
            using (ServiceProvider tempProvider = serviceCollection.BuildServiceProvider())
            {
                // Load the *base ModuleManifest* (system-level module config) from disk if it exists in AppContext.BaseDirectory
                baseManifest = GetModuleManifest(baseConfigService, t, AppContext.BaseDirectory);

                // If the base AppConfig specifies a module folder, discover & register those modules
                if (!string.IsNullOrWhiteSpace(baseConfig.ModuleDirectory))
                    modules = RegisterModules(tempProvider, serviceCollection, baseManifest, baseConfig.ModuleDirectory, modules);
            }

            // Build a temporary DI provider including system modules
            using (ServiceProvider tempProvider = serviceCollection.BuildServiceProvider())
            {
                // Apply system-module-provided AppConfig mutations 
                sysAppConfig = ModulesAppConfig(tempProvider, baseConfig);

                // Merge user manifest with the base manifest and discovered module states
                moduleManifest = ResolveModuleManifest(tempProvider, baseConfigService, t, sysAppConfig, baseManifest);

                if (!string.IsNullOrWhiteSpace(sysAppConfig.ModuleDirectory))
                    modules = RegisterModules(tempProvider, serviceCollection, moduleManifest, sysAppConfig.ModuleDirectory, modules);

                // Persist a cache of the loaded module references
                serviceCollection.AddSingleton<IModuleCache>(new ModuleCache(modules));

                SaveAppConfig(baseConfigService, t, sysAppConfig);
                SaveModuleManifest(baseConfigService, t, sysAppConfig, moduleManifest);

                // Register a ConfigService instance bound to the module AppConfig
                serviceCollection.AddSingleton(GetJsonConfigHandler(serviceCollection, sysAppConfig));
            }

            // Build last temporary DI provider to include collection mutations
            using (ServiceProvider tempProvider = serviceCollection.BuildServiceProvider())
            {
                ModulesDependencyCollection(serviceCollection, tempProvider);

                ConfigureSystemLogs(serviceCollection, sysAppConfig);
            }

            // Build the *final* DI provider including modules and configs
            IServiceProvider serviceProvider = serviceCollection.BuildServiceProvider();

            // Register base configs
            IConfigService configService = serviceProvider.GetRequiredService<IConfigService>();
            configService.Register(typeof(AppConfig), new JsonConfigParams(Read: CapabilityValue.Public, Write: CapabilityValue.Public), t);

            ConfigureStorageLocation(serviceProvider, configService, t);

            ModuleConfigs(serviceProvider);
            ModuleAppServices(serviceProvider);

            return serviceProvider;
        }

        #region Bootstrapper: Service Registration

        private static void CollectServices(IServiceCollection services, CapabilityToken token)
        {
            ArgumentNullException.ThrowIfNull(services);

            services.AddSingleton<ISecureStorage, InsecureStorage>();
            services.AddSingleton<IEncryptionService, EncryptionService>();

            services.AddSingleton<ICapabilityHandler, MinimalCapabilityHandler>();
            services.AddSingleton<ICapabilityAccessor, MinimalCapabilityAccessor>();
            services.AddSingleton<ICapabilityService, CapabilityService>();

            services.AddSingleton<IConfigHandler, JsonConfigHandler>();
            services.AddSingleton<IConfigHandler, SecureJsonConfigHandler>();
            services.AddTransient<IConfigAccessor, JsonConfigAccessor>();
            services.AddTransient<IConfigAccessor, SecureJsonConfigAccessor>();

            services.AddSingleton<IModuleResolver, ModuleResolver>();
            services.AddSingleton<IModuleService, ModuleService>();
            services.AddSingleton<IModuleRegistrar>(provider =>
            {
                ILogger<IModuleRegistrar> logger = provider.GetRequiredService<ILogger<IModuleRegistrar>>();
                IModuleService moduleService = provider.GetRequiredService<IModuleService>();
                IModuleCache moduleCache = provider.GetRequiredService<IModuleCache>();
                IConfigService configService = provider.GetRequiredService<IConfigService>();

                configService.Register(new JsonConfigParams(), token, out IConfigAccessorFor<ModuleManifest>? accessor);

                return accessor == null
                    ? throw new InvalidOperationException("Failed to retrieve persisted ModuleManifest from the configuration service.")
                    : (IModuleRegistrar)new ModuleRegistrar(logger, accessor, moduleService, moduleCache);
            });

            Log.Information("[Nucleus] Module Services Added");
        }

        private static AppConfig GetBaseAppConfig(IConfigService configService, AppConfig config, CapabilityToken token)
        {
            ArgumentNullException.ThrowIfNull(configService);
            ArgumentNullException.ThrowIfNull(config);

            string configFilePath = Path.Combine(AppContext.BaseDirectory, "AppConfig.json");

            if (PlatformPath.Exists(configFilePath))
            {
                configService.Register(new JsonConfigParams(configFilePath), token, out IConfigAccessorFor<AppConfig>? accessor);

                AppConfig loadedConfig = accessor?.Get() ?? new AppConfig();

                foreach (PropertyInfo prop in typeof(AppConfig).GetProperties(BindingFlags.Public | BindingFlags.Instance))
                {
                    if (prop.CanRead && prop.CanWrite)
                    {
                        object? loadedValue = prop.GetValue(loadedConfig);
                        if (loadedValue != null)
                        {
                            prop.SetValue(config, loadedValue);
                        }
                    }
                }

                // Unregister when done, using the same token
                configService.Unregister(typeof(AppConfig), token);
            }

            return config;
        }

        #endregion

        #region Bootstrapper: Config Management

        public static IConfiguration GetEnvConfig()
        {
            return new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddEnvironmentVariables()
                .AddCommandLine(Environment.GetCommandLineArgs())
                .Build();
        }
        public static IConfigService GetJsonConfigHandler(IServiceCollection services, AppConfig appConfig)
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentNullException.ThrowIfNull(appConfig);

            using (ServiceProvider provider = services.BuildServiceProvider())
            {
                IEnumerable<IConfigHandler> providers = provider.GetRequiredService<IEnumerable<IConfigHandler>>();
                IEnumerable<IConfigAccessor> accessors = provider.GetRequiredService<IEnumerable<IConfigAccessor>>();
                ILogger<IConfigService> logger = provider.GetRequiredService<ILogger<IConfigService>>();
                ICapabilityService capabilityService = provider.GetRequiredService<ICapabilityService>();

                return new ConfigService(
                        accessors,
                        providers,
                        logger,
                        capabilityService,
                        appConfig.ConfigDirectory ?? AppContext.BaseDirectory);
            }
        }

        private static ModuleManifest GetModuleManifest(IConfigService configService, CapabilityToken token, string directory)
        {
            ArgumentNullException.ThrowIfNull(configService);

            ModuleManifest moduleManifest = new();

            string moduleFilePath = Path.Combine(directory, "ModuleManifest.json");

            if (PlatformPath.Exists(moduleFilePath))
            {
                Log.Information("[Nucleus] Module manifest found at {ModuleManifestPath}, loading.", moduleFilePath);

                configService.Register(
                    new JsonConfigParams(moduleFilePath),
                    token,
                    out IConfigAccessorFor<ModuleManifest>? accessor);

                moduleManifest = accessor?.Get() ?? new ModuleManifest();

                configService.Unregister(typeof(ModuleManifest), token);
            }

            return moduleManifest;
        }

        private static ModuleManifest ResolveModuleManifest(IServiceProvider provider, IConfigService configService, CapabilityToken token, AppConfig appConfig, ModuleManifest baseManifest)
        {
            ArgumentNullException.ThrowIfNull(configService);
            ArgumentNullException.ThrowIfNull(provider);
            ArgumentNullException.ThrowIfNull(appConfig);
            ArgumentNullException.ThrowIfNull(baseManifest);

            IModuleService moduleService = provider.GetRequiredService<IModuleService>();

            ModuleManifest userManifest = !string.IsNullOrWhiteSpace(appConfig.ConfigDirectory)
                ? GetModuleManifest(configService, token, appConfig.ConfigDirectory)
                : new ModuleManifest();

            IEnumerable<ModuleReference> discoveredModules = !string.IsNullOrWhiteSpace(appConfig.ModuleDirectory)
                ? moduleService.Discover(appConfig.ModuleDirectory)
                : [];

            List<DescriptorLoadState> mergedStates = MergeModuleManifest(baseManifest, userManifest);

            userManifest.DescriptorStates = mergedStates;

            ModuleManifest syncedManifest = SynchronizeModuleConfig(userManifest, discoveredModules);

            NormalizeModuleManifest(syncedManifest, baseManifest, moduleService);

            Log.Information("[Nucleus] Resolved and synchronized module manifest with {ModuleCount} descriptor states and {DiscoveredCount} discovered modules.", syncedManifest.DescriptorStates?.Count ?? 0, discoveredModules.Count());

            return syncedManifest;
        }
        private static List<DescriptorLoadState> MergeModuleManifest(ModuleManifest baseManifest, ModuleManifest userManifest)
        {
            List<DescriptorLoadState> mergedStates = [];

            Dictionary<(Guid ModuleId, string ProviderName), DescriptorLoadState> userStatesByKey = userManifest.DescriptorStates.ToDictionary(s => (s.ModuleId, s.ProviderName), s => s);

            HashSet<(Guid, string)> mergedKeys = [];

            foreach (DescriptorLoadState systemState in baseManifest.DescriptorStates)
            {
                (Guid ModuleId, string ProviderName) key = (systemState.ModuleId, systemState.ProviderName);

                if (userStatesByKey.TryGetValue(key, out DescriptorLoadState? userState))
                {
                    userState.AssemblyName = systemState.AssemblyName;
                    userState.ClassName = systemState.ClassName;
                    userState.Enabled = systemState.Enabled;
                    userState.LoadOrder = systemState.LoadOrder;
                    userState.System = true;
                    mergedStates.Add(userState);
                }
                else
                {
                    mergedStates.Add(new DescriptorLoadState(
                        systemState.ModuleId,
                        systemState.AssemblyName,
                        systemState.ProviderName,
                        systemState.ClassName,
                        true,
                        systemState.Enabled,
                        systemState.LoadOrder));

                    Log.Information("[Nucleus] Added missing default module state for {ProviderName}", systemState.ProviderName);
                }

                mergedKeys.Add(key);
            }

            foreach (KeyValuePair<(Guid ModuleId, string ProviderName), DescriptorLoadState> kvp in userStatesByKey)
                if (!mergedKeys.Contains(kvp.Key))
                    mergedStates.Add(kvp.Value);

            return mergedStates;
        }
        private static void NormalizeModuleManifest(ModuleManifest syncedManifest, ModuleManifest baseManifest, IModuleService moduleService)
        {
            Dictionary<string, DescriptorProviderAttribute?> attributeCache = [];

            foreach (DescriptorLoadState state in syncedManifest.DescriptorStates)
            {
                if (!attributeCache.TryGetValue(state.ProviderName, out DescriptorProviderAttribute? dpa))
                {
                    dpa = moduleService.GetDescriptorProviderAttribute(state.ProviderName);
                    attributeCache[state.ProviderName] = dpa;
                }

                if (dpa != null)
                {
                    bool isInBaseManifest = baseManifest.DescriptorStates.Any(
                        m => m.ModuleId == state.ModuleId && m.ProviderName == state.ProviderName);

                    if (!isInBaseManifest && dpa.DescriptorIsSystemOnly)
                    {
                        state.Enabled = false;
                        state.System = true;
                    }
                }
            }
        }
        private static ModuleManifest SynchronizeModuleConfig(ModuleManifest moduleManifest, IEnumerable<ModuleReference> modules)
        {
            ArgumentNullException.ThrowIfNull(moduleManifest);

            modules ??= [];
            moduleManifest.DescriptorStates ??= [];

            Log.Information("[Nucleus] Synchronizing module config. Current entries: {EntryCount}", moduleManifest.DescriptorStates.Count);

            HashSet<(string AssemblyName, string ImplementationName, string InterfaceName)> discoveredSet =
                new(modules.SelectMany(module =>
                        module.Providers.Select(provider =>
                            (provider.AssemblyName, provider.ImplementationName, provider.InterfaceName))),
                EqualityComparer<(string, string, string)>.Default);

            bool newEntriesAdded = AddNewModuleEntries(moduleManifest, modules);
            bool staleEntriesRemoved = RemoveStaleModuleEntries(moduleManifest, discoveredSet, ignoreDefault: true);

            if (newEntriesAdded || staleEntriesRemoved)
                Log.Information("[Nucleus] Module config changes detected and saved.");
            else
                Log.Information("[Nucleus] No module config changes detected.");

            return moduleManifest;
        }

        private static void SaveAppConfig(IConfigService configService, CapabilityToken token, AppConfig appConfig)
        {
            ArgumentNullException.ThrowIfNull(configService);
            ArgumentNullException.ThrowIfNull(appConfig);

            JsonSerializerOptions options = new() { WriteIndented = false };

            string configPath = Path.Combine(appConfig.ConfigDirectory ?? AppContext.BaseDirectory, "AppConfig.json");

            configService.Register(
                new JsonConfigParams(configPath),
                token,
                out IConfigAccessorFor<AppConfig>? accessor);

            if (accessor == null)
                throw new InvalidOperationException("Failed to retrieve accessor for AppConfig from the configuration service.");

            AppConfig persistAppConfig = accessor.Get();

            string appLocalJson = JsonSerializer.Serialize(appConfig, options);
            string appPersistJson = JsonSerializer.Serialize(persistAppConfig, options);

            try
            {
                if (appLocalJson != appPersistJson)
                {
                    // Persist changes only if the serialized configs differ
                    Task.Run(() => accessor.SaveAsync(appConfig)).GetAwaiter().GetResult();
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "[Nucleus] Failed to save AppConfig to {ConfigPath}", configPath);
            }
        }
        private static void SaveModuleManifest(IConfigService configService, CapabilityToken token, AppConfig appConfig, ModuleManifest moduleManifest)
        {
            ArgumentNullException.ThrowIfNull(configService);
            ArgumentNullException.ThrowIfNull(appConfig);
            ArgumentNullException.ThrowIfNull(moduleManifest);

            JsonSerializerOptions options = new() { WriteIndented = false };

            string configPath = Path.Combine(appConfig.ConfigDirectory ?? AppContext.BaseDirectory, "ModuleManifest.json");

            // Register the ModuleManifest config using the provided capability token
            configService.Register(
                new JsonConfigParams(configPath),
                token,
                out IConfigAccessorFor<ModuleManifest>? accessor);

            if (accessor == null)
                throw new InvalidOperationException("Failed to retrieve accessor for AppConfig from the configuration service.");

            ModuleManifest persistManifest = accessor.Get();

            string manifestLocalJson = JsonSerializer.Serialize(moduleManifest, options);
            string manifestPersistJson = JsonSerializer.Serialize(persistManifest, options);

            try
            {
                if (manifestLocalJson != manifestPersistJson)
                {
                    // Persist changes only if the serialized configs differ
                    Task.Run(() => accessor.SaveAsync(moduleManifest)).GetAwaiter().GetResult();
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "[Nucleus] Failed to save ModuleManifest to {ConfigPath}", configPath);
            }
        }

        private static bool AddNewModuleEntries(ModuleManifest moduleData, IEnumerable<ModuleReference> modules)
        {
            bool configChanged = false;

            foreach (ModuleReference module in modules)
            {
                foreach (ProviderInterface provider in module.Providers)
                {
                    bool entryExists = moduleData.DescriptorStates.Any(
                        loadState =>
                            loadState.ModuleId == module.Metadata.ModuleId &&
                            loadState.AssemblyName == provider.AssemblyName &&
                            loadState.ClassName == provider.ImplementationName &&
                            loadState.ProviderName == provider.InterfaceName
                    );

                    if (!entryExists)
                    {
                        DescriptorLoadState newEntry = new(
                            module.Metadata.ModuleId,
                            provider.AssemblyName,
                            provider.InterfaceName,
                            provider.ImplementationName,
                            false,
                            false,
                            int.MaxValue);

                        moduleData.DescriptorStates.Add(newEntry);

                        configChanged = true;

                        Log.Debug("[Nucleus] Added new config entry for {AssemblyName}:{TypeName}:{ImplementationName}", provider.AssemblyName, provider.ImplementationName, provider.InterfaceName);
                    }
                }
            }

            return configChanged;
        }
        private static bool RemoveStaleModuleEntries(ModuleManifest moduleManifest, HashSet<(string AssemblyName, string ImplementationName, string InterfaceName)> discoveredSet, bool ignoreDefault)
        {
            bool removedAny = false;

            for (int i = moduleManifest.DescriptorStates.Count - 1; i >= 0; i--)
            {
                DescriptorLoadState state = moduleManifest.DescriptorStates[i];

                if (ignoreDefault && state.System)
                    continue;

                (string AssemblyName, string ImplementationName, string InterfaceName) key =
                    (state.AssemblyName, state.ClassName, state.ProviderName);

                if (!discoveredSet.Contains(key))
                {
                    Log.Information("[Nucleus] Removing stale module entry {ProviderName}", state.ProviderName);

                    moduleManifest.DescriptorStates.RemoveAt(i);

                    removedAny = true;
                }
            }
            return removedAny;
        }

        #endregion

        #region Bootstrapper: Module Registration

        private static IEnumerable<ModuleReference> RegisterModules(IServiceProvider provider, IServiceCollection services, ModuleManifest? moduleManifest, string moduleFolderPath, IEnumerable<ModuleReference> moduleReferences)
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentNullException.ThrowIfNull(moduleFolderPath);

            IEnumerable<ModuleReference> enabled = [];

            IModuleService moduleService = provider.GetRequiredService<IModuleService>();
            IModuleResolver moduleResolver = provider.GetRequiredService<IModuleResolver>();

            if (!moduleReferences.Any())
                moduleReferences = moduleService.Discover(moduleFolderPath);

            if (moduleManifest != null)
                enabled = GetEnabledProviders(moduleManifest, moduleReferences);

            RegisterInjectors(moduleService, moduleResolver, services, enabled);
            RegisterModules(services, enabled);

            return moduleReferences;
        }
        private static List<ModuleReference> GetEnabledProviders(ModuleManifest moduleData, IEnumerable<ModuleReference> modules)
        {
            List<ModuleReference> result = [];

            foreach (ModuleReference module in modules)
            {
                Log.Debug("[Nucleus] Processing module {AssemblyName}:{TypeName}", module.AssemblyName, module.TypeName);

                List<ProviderInterface> enabledImplementations = FindEnabledProviders(moduleData, module);

                bool hasEnabledImplementations = enabledImplementations.Count > 0;

                if (hasEnabledImplementations)
                {
                    Log.Information("[Nucleus] Module {AssemblyName}:{TypeName} has {EnabledCount} enabled implementations.", module.AssemblyName, module.TypeName, enabledImplementations.Count);

                    result.Add(new ModuleReference(module.Assembly, module.Type, module.Metadata, enabledImplementations));
                }
            }

            if (result.Count > 0)
                Log.Information("[Nucleus] Found {ModuleCount} modules with enabled implementations.", result.Count);

            return result;
        }
        private static List<ProviderInterface> FindEnabledProviders(ModuleManifest moduleData, ModuleReference module)
        {
            List<ProviderInterface> enabledImplementations = [];

            foreach (ProviderInterface provider in module.Providers)
            {
                foreach (DescriptorLoadState configEntry in moduleData.DescriptorStates)
                {
                    bool assemblyMatch = string.Equals(configEntry.AssemblyName, provider.AssemblyName, StringComparison.OrdinalIgnoreCase);
                    bool implementationMatch = string.Equals(configEntry.ClassName, provider.ImplementationName, StringComparison.OrdinalIgnoreCase);
                    bool interfaceMatch = string.Equals(configEntry.ProviderName, provider.InterfaceName, StringComparison.OrdinalIgnoreCase);

                    if (assemblyMatch && implementationMatch && interfaceMatch && configEntry.Enabled)
                    {
                        enabledImplementations.Add(provider);

                        Log.Debug("[Nucleus] Enabled implementation {AssemblyName}:{TypeName}:{ImplementationName}", provider.AssemblyName, provider.ImplementationName, provider.InterfaceName);

                        break;
                    }
                }
            }

            return enabledImplementations;
        }

        private static void RegisterInjectors(IModuleService moduleService, IModuleResolver moduleResolver, IServiceCollection serviceCollection, IEnumerable<ModuleReference> enabledModules)
        {
            List<IProviderDependencyInjection> injectors = [];

            foreach (ModuleReference module in enabledModules)
            {
                foreach (ProviderInterface implementation in module.Providers)
                {
                    if (implementation.InterfaceType != typeof(IProviderDependencyInjection))
                        continue;

                    IProviderDependencyInjection? injector =
                        moduleService.GetLoadedProviders<IProviderDependencyInjection>(implementation);

                    if (injector == null)
                    {
                        Log.Error("[Nucleus] Failed to create injector instance for {TypeName} in {AssemblyName}.", implementation.ImplementationName, implementation.AssemblyName);

                        continue;
                    }

                    injectors.Add(injector);
                }
            }

            IReadOnlyList<DescriptorDependencyInjection> orderedDescriptors =
                moduleResolver.ResolveAndOrder<IProviderDependencyInjection, DescriptorDependencyInjection>(injectors);

            foreach (DescriptorDependencyInjection descriptor in orderedDescriptors)
            {
                if (descriptor == null)
                    continue;

                bool hasImplementationType = descriptor.ImplementationType != null;
                bool hasImplementationFactory = descriptor.ImplementationFactory != null;

                if (!hasImplementationType && !hasImplementationFactory)
                {
                    Log.Warning("[Nucleus] Descriptor for {InterfaceType} must specify either ImplementationType or Factory; skipping malformed registration.", descriptor.InterfaceType.Name);

                    continue;
                }
                else if (hasImplementationType && hasImplementationFactory)
                {
                    Log.Information("[Nucleus] Descriptor for {InterfaceType} specifies both ImplementationType and Factory; must specify only one; skipping ambiguous registration.", descriptor.InterfaceType.Name);

                    continue;
                }
                else if (hasImplementationFactory)
                {
                    serviceCollection.Add(new ServiceDescriptor(
                        descriptor.InterfaceType,
                        provider => descriptor.ImplementationFactory!(provider)!,
                        descriptor.Lifetime));
                }
                else
                {
                    serviceCollection.Add(new ServiceDescriptor(
                        descriptor.InterfaceType,
                        descriptor.ImplementationType!,
                        descriptor.Lifetime));
                }
            }

            Log.Information("[Nucleus] Module DI injection complete: processed {Count} injectors across enabled modules.", injectors.Count);
        }
        private static void RegisterModules(IServiceCollection serviceCollection, IEnumerable<ModuleReference> enabledModules)
        {
            foreach (ModuleReference module in enabledModules)
            {
                Log.Debug("[Nucleus] Registering general module {AssemblyName}:{TypeName}", module.AssemblyName, module.TypeName);

                foreach (ProviderInterface provider in module.Providers)
                {
                    Log.Debug("[Nucleus] Registered {AssemblyName}:{TypeName}:{Interface}.", provider.AssemblyName, provider.ImplementationName, provider.InterfaceName);

                    serviceCollection.AddSingleton(provider.InterfaceType, provider.ImplementationType);
                }
            }
        }

        #endregion

        #region Bootstrapper: Module Integration

        private static AppConfig ModulesAppConfig(IServiceProvider provider, AppConfig appConfig)
        {
            ArgumentNullException.ThrowIfNull(provider);
            ArgumentNullException.ThrowIfNull(appConfig);

            IModuleResolver moduleResolver = provider.GetRequiredService<IModuleResolver>();
            IEnumerable<IProviderAppConfig> appConfigModules = provider.GetServices<IProviderAppConfig>();

            IReadOnlyList<DescriptorAppConfig> orderedDescriptors =
                moduleResolver.ResolveAndOrder<IProviderAppConfig, DescriptorAppConfig>(appConfigModules);

            foreach (DescriptorAppConfig descriptor in orderedDescriptors)
            {
                try
                {
                    descriptor.AppConfig?.Invoke(appConfig);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "[Nucleus] Failed to apply configuration mutation for module {ModuleId}", descriptor.ModuleId);
                }
            }

            Log.Information("[Nucleus] ModulesAppConfig completed: applied {ModuleCount} config mutation descriptors.", orderedDescriptors.Count);

            return appConfig;
        }

        private static void ModulesDependencyCollection(IServiceCollection services, IServiceProvider provider)
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentNullException.ThrowIfNull(provider);

            IModuleResolver moduleResolver = provider.GetRequiredService<IModuleResolver>();
            IEnumerable<IProviderDependencyCollection> collectionModules = provider.GetServices<IProviderDependencyCollection>();

            IReadOnlyList<DescriptorDependencyCollection> orderedDescriptors =
                moduleResolver.ResolveAndOrder<IProviderDependencyCollection, DescriptorDependencyCollection>(collectionModules);

            foreach (DescriptorDependencyCollection descriptor in orderedDescriptors)
            {
                try
                {
                    descriptor.ConfigureAction?.Invoke(services);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "[Nucleus] Failed to apply service collection mutation for module {ModuleId}", descriptor.ModuleId);
                }
            }

            Log.Information("[Nucleus] ModulesDependencyCollection completed: applied {DescriptorCount} dependency collection descriptors.", orderedDescriptors.Count);
        }

        private static void ModuleConfigs(IServiceProvider provider)
        {
            ArgumentNullException.ThrowIfNull(provider);

            IConfigService configService = provider.GetRequiredService<IConfigService>();
            IModuleResolver moduleResolver = provider.GetRequiredService<IModuleResolver>();
            IEnumerable<IProviderConfiguration> configurationModules = provider.GetServices<IProviderConfiguration>();

            IReadOnlyList<DescriptorConfiguration> orderedDescriptors =
                moduleResolver.ResolveAndOrder<IProviderConfiguration, DescriptorConfiguration>(configurationModules);

            foreach (DescriptorConfiguration descriptor in orderedDescriptors)
            {
                if (descriptor.ConfigServiceAction is not null)
                {
                    descriptor.ConfigServiceAction(configService);
                }
                else
                {
                    if (descriptor.ConfigType == null || descriptor.ConfigParams == null || descriptor.CapabilityToken == null)
                        throw new InvalidOperationException($"Descriptor {descriptor.DescriptorId} must define all static values (ConfigType, ConfigParams, CapabilityToken) when no ConfigServiceAction is provided.");

                    configService.Register(descriptor.ConfigType, descriptor.ConfigParams, descriptor.CapabilityToken);
                }
            }

            Log.Information("[Nucleus] ModulesConfigs completed: Added {ConfigCount} configuration descriptors from modules.", orderedDescriptors.Count);
        }
        private static void ModuleAppServices(IServiceProvider provider)
        {
            ArgumentNullException.ThrowIfNull(provider);

            IModuleResolver moduleResolver = provider.GetRequiredService<IModuleResolver>();
            IEnumerable<IProviderAppSetup> appSetupModules = provider.GetServices<IProviderAppSetup>();

            IReadOnlyList<DescriptorAppSetup> orderedDescriptors =
                moduleResolver.ResolveAndOrder<IProviderAppSetup, DescriptorAppSetup>(appSetupModules);

            foreach (DescriptorAppSetup descriptor in orderedDescriptors)
            {
                try
                {
                    descriptor.AppSetup?.Invoke(provider);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "[Nucleus] Failed to apply service setup for module {ModuleId}", descriptor.ModuleId);
                }
            }

            Log.Information("[Nucleus] ModuleAppServices completed: applied {SetupCount} service mutation descriptors.",
                orderedDescriptors.Count);
        }

        #endregion

        #region Bootstrapper: Framework Configuration

        private static void ConfigureSystemLogs(IServiceCollection serviceCollection, AppConfig appConfig)
        {
            ArgumentNullException.ThrowIfNull(serviceCollection);
            ArgumentNullException.ThrowIfNull(appConfig);

            RollingInterval rolloverInterval = (RollingInterval)(appConfig.LoggingRolloverInterval ?? LoggingRollingInterval.Day);

            string? defaultFileName = new AppConfig().LoggingFileName;

            if (string.IsNullOrWhiteSpace(defaultFileName))
            {
                Log.Warning("[App] Default log file name is missing or empty. Exiting log configuration.");

                return;
            }

            string? logFileName = appConfig.LoggingFileName;

            if (string.IsNullOrWhiteSpace(logFileName))
            {
                Log.Warning("[App] Runtime log file name is missing or empty. Exiting log configuration.");

                return;
            }

            string? defaultDir = new AppConfig().LoggingDirectory;

            if (string.IsNullOrWhiteSpace(defaultDir))
            {
                Log.Warning("[App] Default log directory is missing or empty. Exiting log configuration.");

                return;
            }

            string? runtimeDir = appConfig.LoggingDirectory;

            if (string.IsNullOrWhiteSpace(runtimeDir))
            {
                Log.Warning("[App] Runtime log directory is missing or empty. Exiting log configuration.");

                return;
            }

            Log.Information("[App] Boot log transitioning to runtime log.");

            string configLogPath =
                Path.GetFullPath(Path.Combine(runtimeDir, logFileName))
                    .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

            Log.CloseAndFlush();

            Directory.CreateDirectory(runtimeDir);

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.File(configLogPath, rollingInterval: rolloverInterval)
                .CreateLogger();

            serviceCollection.AddLogging(builder =>
            {
                builder.ClearProviders();
                builder.AddSerilog(Log.Logger, dispose: true);
            });

            Log.Information("[App] Boot log hand off.");
        }
        private static void ConfigureStorageLocation(IServiceProvider provider, IConfigService configService, CapabilityToken token)
        {
            ArgumentNullException.ThrowIfNull(provider);
            ArgumentNullException.ThrowIfNull(configService);

            IConfigAccessorFor<AppConfig> configAccessor = configService.GetConfigAccessor<AppConfig>(token);
            ISecureStorage secureStorage = provider.GetRequiredService<ISecureStorage>();

            string? storageFolder = configAccessor.Get().StorageDirectory;

            if (storageFolder != null)
            {
                secureStorage.Initialize(storageFolder);
            }
        }

        #endregion
    }
}