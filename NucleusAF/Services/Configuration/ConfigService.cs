using Microsoft.Extensions.Logging;
using NucleusAF.Abstractions.CompositeRegistry;
using NucleusAF.Interfaces.Models;
using NucleusAF.Interfaces.Models.Configuration;
using NucleusAF.Interfaces.Services.Capabilities;
using NucleusAF.Interfaces.Services.Capabilities.Handlers;
using NucleusAF.Interfaces.Services.Configuration;
using NucleusAF.Models.Capabilities;
using System.Collections.Concurrent;

namespace NucleusAF.Services.Configuration
{
    public class ConfigService : CompositeAccessorRegistryBase<IConfigAccessor, IConfigHandler>, IConfigService, IDisposable
    {
        public event EventHandler<ConfigServiceSaveCompletedEventArgs>? SyncSaveCompleted;
        public event EventHandler<ConfigServiceSaveErrorEventArgs>? SyncSaveErrors;
        public event EventHandler<ConfigServiceConfigReloadedEventArgs>? ConfigReloaded;
        public event EventHandler<ConfigServiceSettingChangeEventArgs>? SettingChanged;

        protected readonly ILogger<IConfigService> Logger;
        protected readonly ICapabilityService CapabilityService;
        protected readonly ICapabilityAccessorFor<IMinimalCapabilityHandler> CapabilityAccessor;
        protected readonly ConcurrentDictionary<Type, IConfigHandler> HandlersByConfigType;

        protected bool IsDisposed = false;

        public string ConfigDataDirectory { get; init; }

        public ConfigService(IEnumerable<IConfigAccessor> accessors, IEnumerable<IConfigHandler> providers, ILogger<IConfigService> logger, ICapabilityService capabilityService, string configDataDirectory)
            : base(accessors, providers)
        {
            ArgumentNullException.ThrowIfNull(providers);
            ArgumentNullException.ThrowIfNull(accessors);
            ArgumentNullException.ThrowIfNull(logger);
            ArgumentNullException.ThrowIfNull(capabilityService);
            ArgumentNullException.ThrowIfNull(configDataDirectory);

            this.Logger = logger;
            this.HandlersByConfigType = new ConcurrentDictionary<Type, IConfigHandler>();
            this.CapabilityService = capabilityService;
            this.CapabilityAccessor = this.CapabilityService.GetAccessor<IMinimalCapabilityHandler>();
            this.ConfigDataDirectory = configDataDirectory;

            this.Logger.LogInformation("[ConfigService] Initialized with {AccessorCount} accessors, {ProviderCount} providers, config data directory '{ConfigDir}'", accessors.Count(), providers.Count(), configDataDirectory);
        }

        #region ConfigService: Accessor Management

        public virtual IConfigAccessorFor<TConfig> GetConfigAccessor<TConfig>(ICapabilityToken? token = null) where TConfig : class
        {
            Type configType = typeof(TConfig);

            IConfigHandler handler = this.GetHandlerForConfigType(configType);
            IConfigAccessor accessor = this.GetRegistryAccessorFor(handler);

            this.Logger.LogDebug("[ConfigService] Retrieved config accessor for configuration type {ConfigType}", configType.Name);

            return accessor.For<TConfig>(this, handler, token);
        }
        public virtual TAccessor GetConfigAccessor<TAccessor, TConfig>(ICapabilityToken? token = null) where TConfig : class where TAccessor : IConfigAccessorFor<TConfig>
        {
            IConfigAccessorFor<TConfig> defaultAccessor = this.GetConfigAccessor<TConfig>(token);

            bool accessorIsCorrectType = defaultAccessor is TAccessor;

            if (accessorIsCorrectType)
            {
                this.Logger.LogDebug("[ConfigService] Retrieved config accessor {AccessorType} for configuration {ConfigType}", typeof(TAccessor).Name, typeof(TConfig).Name);

                return (TAccessor)defaultAccessor;
            }
            else
            {
                this.Logger.LogError("[ConfigService] Accessor type mismatch: expected {ExpectedType}, got {ActualType}", typeof(TAccessor).Name, defaultAccessor.GetType().Name);
                throw new InvalidCastException($"Accessor for {typeof(TConfig).Name} is a {defaultAccessor.GetType().Name} and cannot be cast to {typeof(TAccessor).Name}");
            }
        }
        public virtual IConfigAccessor GetConfigAccessor(Type accessorType, ICapabilityToken? token = null)
        {
            ArgumentNullException.ThrowIfNull(accessorType);

            IConfigAccessor accessorProvider = this.GetAccessorProviderByInterface(accessorType);
            IConfigAccessor accessor = accessorProvider.SetAccess(token);

            this.Logger.LogDebug("[ConfigService] Retrieved config accessor for type {AccessorType}", accessorType.Name);

            return accessor;
        }

        #endregion

        #region ConfigService: Predicate Operations

        public virtual bool IsRegistered(Type configType)
        {
            return this.HandlersByConfigType.ContainsKey(configType);
        }

        #endregion

        #region ConfigService: Registration Operations

        public virtual ICapabilityToken Register(Type configType, IConfigParams configParams, ICapabilityToken? token = null)
        {
            ArgumentNullException.ThrowIfNull(configType);
            ArgumentNullException.ThrowIfNull(configParams);

            Type paramType = configParams.GetType();

            if (!this.TryGetRegistryHandler(paramType, out IConfigHandler? handler) || handler is not IConfigRegistrar registrar)
            {
                this.Logger.LogError("[ConfigService] No config implementation registered for parameter type: {ParamType}", paramType.Name);
                throw new InvalidOperationException($"No config implementation registered for parameter type {paramType.Name}");
            }

            if (!this.HandlersByConfigType.TryAdd(configType, handler))
            {
                this.Logger.LogError("[ConfigService] Type already registered: {ConfigType}", configType.Name);
                throw new InvalidOperationException($"{configType.Name} was already registered.");
            }

            ICapabilityToken t = registrar.Register(configType, configParams, this, token);

            this.Logger.LogDebug("[ConfigService] Registered configuration: {ConfigType} with {ParamType}", configType.Name, paramType.Name);

            return t;
        }
        public virtual void Register<TConfig>(IConfigParams configParams, ICapabilityToken? token, out IConfigAccessorFor<TConfig> accessor) where TConfig : class
        {
            ArgumentNullException.ThrowIfNull(configParams);

            ICapabilityToken t = this.Register(typeof(TConfig), configParams, token);
            accessor = this.GetConfigAccessor<TConfig>(t);

            this.Logger.LogDebug("[ConfigService] Registered configuration type {ConfigType}", typeof(TConfig).Name);
        }
        public virtual IDictionary<Type, ICapabilityToken> Register(IEnumerable<Type> configTypes, IConfigParams configParams, ICapabilityToken? token = null)
        {
            ArgumentNullException.ThrowIfNull(configTypes);
            ArgumentNullException.ThrowIfNull(configParams);

            Dictionary<Type, ICapabilityToken> result = [];

            foreach (Type configType in configTypes)
            {
                this.Logger.LogTrace("[ConfigService] Attempting to register configuration type {ConfigType}", configType.Name);

                result.Add(configType, this.Register(configType, configParams, token));
            }

            this.Logger.LogInformation("[ConfigService] Bulk registered {Count} configuration types", this.HandlersByConfigType.Count);

            return result;
        }
        public virtual void Register(IEnumerable<Type> configTypes, IConfigParams configParams, out IConfigAccessor accessor, ICapabilityToken? token = null)
        {
            ArgumentNullException.ThrowIfNull(configTypes);
            ArgumentNullException.ThrowIfNull(configParams);

            ICapabilityToken t = token ?? new CapabilityToken(Guid.NewGuid());

            foreach (Type configType in configTypes)
            {
                this.Logger.LogTrace("[ConfigService] Attempting to register configuration type {ConfigType}", configType.Name);

                this.Register(configType, configParams, t);
            }

            IConfigHandler handler = this.GetProviderForParamsType(configParams.GetType());
            accessor = this.GetRegistryAccessorFor(handler);

            this.Logger.LogDebug("[ConfigService] Bulk registered configuration types with params {ParamType}", configParams.GetType().Name);
        }

        public virtual bool TryRegister(Type configType, IConfigParams configParams, ICapabilityToken? token, out ICapabilityToken? registered)
        {
            ArgumentNullException.ThrowIfNull(configType);
            ArgumentNullException.ThrowIfNull(configParams);

            try
            {
                registered = this.Register(configType, configParams, token);

                this.Logger.LogDebug("[ConfigService] Registered configuration type {ConfigType}", configType.Name);

                return true;
            }
            catch (Exception ex)
            {
                registered = default;

                this.Logger.LogWarning(ex, "[ConfigService] Failed to register configuration type {ConfigType} with params {ParamType}", configType.Name, configParams.GetType().Name);

                return false;
            }
        }
        public virtual bool TryRegister<TConfig>(IConfigParams configParams, ICapabilityToken? token, out IConfigAccessorFor<TConfig> accessor) where TConfig : class
        {
            ArgumentNullException.ThrowIfNull(configParams);

            try
            {
                this.Register(configParams, token, out accessor);

                this.Logger.LogDebug("[ConfigService] Registered configuration type {ConfigType}", typeof(TConfig).Name);

                return true;
            }
            catch (Exception ex)
            {
                accessor = default!;

                this.Logger.LogWarning(ex, "[ConfigService] Failed to register configuration type {ConfigType} with params {ParamType}", typeof(TConfig).Name, configParams.GetType().Name);

                return false;
            }
        }
        public virtual bool TryRegister(IEnumerable<Type> configTypes, IConfigParams configParams, out IDictionary<Type, ICapabilityToken> results, ICapabilityToken? token = null)
        {
            ArgumentNullException.ThrowIfNull(configTypes);
            ArgumentNullException.ThrowIfNull(configParams);

            results = new Dictionary<Type, ICapabilityToken>();
            bool allSucceeded = true;

            foreach (Type configType in configTypes)
            {
                this.Logger.LogTrace("[ConfigService] Attempting to register configuration type {ConfigType}", configType.Name);

                try
                {
                    ICapabilityToken regToken = this.Register(configType, configParams, token);

                    results.Add(configType, regToken);
                }
                catch (Exception ex)
                {
                    this.Logger.LogWarning(ex, "[ConfigService] Failed to register configuration type {ConfigType}", configType.Name);

                    allSucceeded = false;
                }
            }

            this.Logger.LogDebug("[ConfigService] Completed bulk configuration registration");

            return allSucceeded;
        }
        public virtual bool TryRegister(IEnumerable<Type> configTypes, IConfigParams configParams, out IConfigAccessor? accessor, ICapabilityToken? token = null)
        {
            ArgumentNullException.ThrowIfNull(configTypes);
            ArgumentNullException.ThrowIfNull(configParams);

            ICapabilityToken t = token ?? new CapabilityToken(Guid.NewGuid());
            bool allSucceeded = true;

            foreach (Type configType in configTypes)
            {
                this.Logger.LogTrace("[ConfigService] Attempting to register configuration type {ConfigType}", configType.Name);

                try
                {
                    this.Register(configType, configParams, t);
                }
                catch (Exception ex)
                {
                    this.Logger.LogWarning(ex, "[ConfigService] Failed to register configuration type {ConfigType}", configType.Name);
                    allSucceeded = false;
                }
            }

            if (allSucceeded)
            {
                IConfigHandler handler = this.GetProviderForParamsType(configParams.GetType());
                accessor = this.GetRegistryAccessorFor(handler);

                this.Logger.LogDebug("[ConfigService] Completed bulk configuration registration");
                return true;
            }

            accessor = default;
            this.Logger.LogDebug("[ConfigService] Bulk configuration registration incomplete due to errors");
            return false;
        }

        public virtual void Unregister(Type configType, ICapabilityToken token)
        {
            ArgumentNullException.ThrowIfNull(configType);

            if (!this.HandlersByConfigType.TryGetValue(configType, out IConfigHandler? handler) || handler is not IConfigRegistrar registrar)
            {
                this.Logger.LogWarning("[ConfigService] Attempted to unregister non-existent or invalid configuration type: {ConfigType}", configType.Name);
                return;
            }

            registrar.Unregister(configType, token);

            if (!registrar.IsRegistered(configType))
            {
                if (this.HandlersByConfigType.TryRemove(configType, out _))
                    this.Logger.LogDebug("[ConfigService] Unregistered configuration: {ConfigType}", configType.Name);
                else
                    this.Logger.LogWarning("[ConfigService] Failed to remove configuration mapping for {ConfigType}", configType.Name);
            }
            else
            {
                this.Logger.LogWarning("[ConfigService] Registrar still reports {ConfigType} as registered after unregister attempt.", configType.Name);
            }
        }
        public virtual void Unregister(IEnumerable<Type> configTypes, ICapabilityToken token)
        {
            ArgumentNullException.ThrowIfNull(configTypes);

            foreach (Type configType in configTypes)
            {
                this.Logger.LogTrace("[ConfigService] Attempting to unregister configuration type {ConfigType}", configType.Name);

                this.Unregister(configType, token);
            }

            this.Logger.LogInformation("[ConfigService] Bulk unregistered configuration types");
        }

        public virtual bool TryUnregister(Type configType, ICapabilityToken token)
        {
            ArgumentNullException.ThrowIfNull(configType);

            this.Logger.LogTrace("[ConfigService] Attempting to unregister configuration type {ConfigType}", configType.Name);

            try
            {
                this.Unregister(configType, token);

                this.Logger.LogDebug("[ConfigService] Completed configuration unregistration");

                return true;
            }
            catch (Exception ex)
            {
                this.Logger.LogWarning(ex, "[ConfigService] Failed to unregister configuration type {ConfigType}", configType.Name);

                return false;
            }
        }
        public virtual bool TryUnregister(IEnumerable<Type> configTypes, ICapabilityToken token)
        {
            ArgumentNullException.ThrowIfNull(configTypes);

            bool allSucceeded = true;

            foreach (Type configType in configTypes)
            {
                this.Logger.LogTrace("[ConfigService] Attempting to unregister configuration type {ConfigType}", configType.Name);

                try
                {
                    this.Unregister(configType, token);
                }
                catch (Exception ex)
                {
                    this.Logger.LogWarning(ex, "[ConfigService] Failed to unregister configuration type {ConfigType}", configType.Name);
                    allSucceeded = false;
                }
            }

            this.Logger.LogDebug("[ConfigService] Completed bulk configuration unregistration");

            return allSucceeded;
        }

        #endregion

        #region ConfigService: Event Handlers

        public virtual void OnSaveOperationComplete(object sender, Type configType)
        {
            ArgumentNullException.ThrowIfNull(sender);
            ArgumentNullException.ThrowIfNull(configType);

            this.SyncSaveCompleted?.Invoke(sender, new ConfigServiceSaveCompletedEventArgs(configType));

            this.Logger.LogDebug("[ConfigService] Save operation complete for configuration type {ConfigType}", configType.Name);
        }
        public virtual void OnSaveOperationError(object sender, Exception ex, ConfigSaveOperation operation, Type configType)
        {
            ArgumentNullException.ThrowIfNull(sender);
            ArgumentNullException.ThrowIfNull(ex);
            ArgumentNullException.ThrowIfNull(configType);

            this.Logger.LogError(ex, "[ConfigService] Save operation failed: {Operation} for {ConfigType}", operation, configType.Name);

            this.SyncSaveErrors?.Invoke(sender, new ConfigServiceSaveErrorEventArgs(ex, operation, configType));
        }

        public virtual void OnSettingChanged(object sender, Type configType, string key, object? oldValue, object? newValue)
        {
            ArgumentNullException.ThrowIfNull(sender);
            ArgumentNullException.ThrowIfNull(configType);
            ArgumentNullException.ThrowIfNull(key);

            this.Logger.LogDebug("[ConfigService] Setting changed: {ConfigType}.{Key} from {OldValue} to {NewValue}", configType.Name, key, oldValue, newValue);

            this.SettingChanged?.Invoke(sender, new ConfigServiceSettingChangeEventArgs(configType, key, oldValue, newValue));
        }

        public virtual void OnConfigReloaded(object sender, Type configType)
        {
            ArgumentNullException.ThrowIfNull(sender);
            ArgumentNullException.ThrowIfNull(configType);

            this.Logger.LogDebug("[ConfigService] Configuration reloaded: {ConfigType}", configType.Name);

            this.ConfigReloaded?.Invoke(sender, new ConfigServiceConfigReloadedEventArgs(configType));
        }

        #endregion

        #region ConfigService: Resource Management

        public void Dispose()
        {
            if (this.IsDisposed)
                return;

            this.IsDisposed = true;

            GC.SuppressFinalize(this);

            this.Logger.LogDebug("[ConfigService] Disposed");
        }

        #endregion

        #region ConfigService: Validation and Helpers

        protected virtual IConfigHandler GetProviderForParamsType(Type paramType)
        {
            ArgumentNullException.ThrowIfNull(paramType);

            if (!this.TryGetRegistryHandler(paramType, out IConfigHandler? handler))
            {
                this.Logger.LogError("[ConfigService] No provider registered for parameter type: {ParamType}", paramType.Name);
                throw new KeyNotFoundException($"Parameters for {paramType.Name} is not registered.");
            }

            return handler!;
        }
        protected virtual IConfigHandler GetHandlerForConfigType(Type configType)
        {
            ArgumentNullException.ThrowIfNull(configType);

            if (!this.HandlersByConfigType.TryGetValue(configType, out IConfigHandler? handler))
            {
                this.Logger.LogError("[ConfigService] No provider registered for configuration type: {ConfigType}", configType.Name);
                throw new KeyNotFoundException($"Configuration for {configType.Name} is not registered.");
            }

            return handler!;
        }

        protected virtual IConfigAccessor GetAccessorProviderByInterface(Type accessorType)
        {
            if (!this.TryGetRegistryAccessor(accessorType, out IConfigAccessor? handler))
            {
                this.Logger.LogError("[ConfigService] No accessor registered for interface: {AccessorType}", accessorType.Name);
                throw new InvalidOperationException($"No accessor registered for interface {accessorType.Name}");
            }

            return handler!;
        }


        #endregion
    }
}