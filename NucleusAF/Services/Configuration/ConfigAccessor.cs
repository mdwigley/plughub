using Microsoft.Extensions.Logging;
using NucleusAF.Interfaces.Models;
using NucleusAF.Interfaces.Services.Capabilities;
using NucleusAF.Interfaces.Services.Configuration;
using NucleusAF.Interfaces.Services.Configuration.Accessors;
using System.Diagnostics.CodeAnalysis;

namespace NucleusAF.Services.Configuration
{
    public abstract class ConfigAccessor(ILogger<IConfigAccessor> logger, ICapabilityService capabilityService)
        : IConfigAccessor
    {
        public virtual Type Key { get; } = typeof(IConfigAccessor);

        protected readonly ILogger<IConfigAccessor> Logger = logger;
        protected readonly ICapabilityService CapabilityService = capabilityService;

        protected IConfigService? ConfigService = null;
        protected IConfigHandler? ConfigHandler = null;
        protected ICapabilityToken? Token = null;

        #region ConfigAccessor: Fluent Configuration API

        public virtual IConfigAccessor SetConfigService(IConfigService? service = null)
        {
            this.ConfigService = service;
            this.Logger.LogDebug("[ConfigAccessor] ConfigService set");
            return this;
        }
        public virtual IConfigAccessor SetConfigHandler(IConfigHandler? handler = null)
        {
            this.ConfigHandler = handler;
            this.Logger.LogDebug("[ConfigAccessor] ConfigHandler set");
            return this;
        }
        public virtual IConfigAccessor SetAccess(ICapabilityToken? token = null)
        {
            this.Token = token;
            this.Logger.LogDebug("[ConfigAccessor] CapabilityToken set");
            return this;
        }

        #endregion

        #region ConfigAccessor: Factory Methods

        public virtual IConfigAccessorFor<TConfig> For<TConfig>() where TConfig : class
        {
            if (this.ConfigService == null)
            {
                this.Logger.LogError("[ConfigAccessor] ConfigService must be set before creating typed accessors");
                throw new InvalidOperationException("ConfigService must be set before creating typed accessors");
            }

            if (this.ConfigHandler == null)
            {
                this.Logger.LogError("[ConfigAccessor] ConfigHandler must be set before creating typed accessors");
                throw new InvalidOperationException("ConfigHandler must be set before creating typed accessors");
            }

            this.Logger.LogDebug("[ConfigAccessor] Successfully created typed accessor for configuration type {ConfigType}", typeof(TConfig).Name);
            return this.For<TConfig>(this.ConfigService, this.ConfigHandler, this.Token);
        }
        public abstract IConfigAccessorFor<TConfig> For<TConfig>(IConfigService configService, IConfigHandler configHandler, ICapabilityToken? token = null) where TConfig : class;

        #endregion
    }

    public abstract class ConfigAccessorFor<TConfig>(ILogger<IConfigAccessor> logger, IConfigService configService, IConfigHandler configHandler, ICapabilityService capabilityService, ICapabilityToken? token = null)
        : IConfigAccessorFor<TConfig>, IJsonConfigAccessorFor<TConfig> where TConfig : class
    {
        protected readonly ILogger<IConfigAccessor> Logger = logger;
        protected readonly IConfigService ConfigService = configService;
        protected readonly IConfigHandler ConfigHandler = configHandler;
        protected readonly ICapabilityService CapabilityService = capabilityService;
        protected readonly ICapabilityToken? CapabilityToken = token;

        #region ConfigAccessorFor: Property Access

        [return: MaybeNull]
        public virtual T Get<T>(string key)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(key);

            this.Logger.LogDebug("[ConfigAccessorFor] Get<{ValueType}> called for key {Key} in configuration type {ConfigType}", typeof(T).Name, key, typeof(TConfig).Name);
            return this.ConfigHandler.GetValue<T>(typeof(TConfig), key, this.CapabilityToken)!;
        }
        public virtual void Set<T>(string key, T value)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(key);

            this.Logger.LogDebug("[ConfigAccessorFor] Set<{ValueType}> called for key {Key} in configuration type {ConfigType}", typeof(T).Name, key, typeof(TConfig).Name);
            this.ConfigHandler.SetValue(typeof(TConfig), key, value, this.CapabilityToken);
        }
        public virtual void Save()
        {
            this.Logger.LogDebug("[ConfigAccessorFor] Save called for configuration type {ConfigType}", typeof(TConfig).Name);
            this.ConfigHandler.SaveValues(typeof(TConfig), this.CapabilityToken);
        }
        public virtual async Task SaveAsync(CancellationToken cancellationToken = default)
        {
            this.Logger.LogDebug("[ConfigAccessorFor] SaveAsync called for configuration type {ConfigType}", typeof(TConfig).Name);
            await this.ConfigHandler.SaveValuesAsync(typeof(TConfig), this.CapabilityToken, cancellationToken);
        }

        #endregion

        #region ConfigAccessorFor: Try Property Access

        public virtual bool TryGet<T>(string key, out T? value)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(key);

            try
            {
                value = this.ConfigHandler.GetValue<T>(typeof(TConfig), key, this.CapabilityToken);
                this.Logger.LogDebug("[ConfigAccessorFor] Successfully retrieved value for key {Key} in configuration type {ConfigType}", key, typeof(TConfig).Name);
                return true;
            }
            catch (Exception ex)
            {
                this.Logger.LogError(ex, "[ConfigAccessorFor] Failed to get value for key {Key} in configuration type {ConfigType} with token {CapabilityToken}", key, typeof(TConfig).Name, this.CapabilityToken);
                value = default;
                return false;
            }
        }
        public virtual bool TrySet<T>(string key, T value)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(key);

            try
            {
                this.ConfigHandler.SetValue(typeof(TConfig), key, value, this.CapabilityToken);
                this.Logger.LogDebug("[ConfigAccessorFor] Successfully set value for key {Key} in configuration type {ConfigType}", key, typeof(TConfig).Name);
                return true;
            }
            catch (Exception ex)
            {
                this.Logger.LogError(ex, "[ConfigAccessorFor] Failed to set value for key {Key} in configuration type {ConfigType} with token {CapabilityToken}", key, typeof(TConfig).Name, this.CapabilityToken);
                return false;
            }
        }
        public virtual bool TrySave()
        {
            try
            {
                this.ConfigHandler.SaveValues(typeof(TConfig), this.CapabilityToken);
                this.Logger.LogDebug("[ConfigAccessorFor] Successfully saved values for configuration type {ConfigType}", typeof(TConfig).Name);
                return true;
            }
            catch (Exception ex)
            {
                this.Logger.LogError(ex, "[ConfigAccessorFor] Failed to save values for configuration type {ConfigType} with token {CapabilityToken}", typeof(TConfig).Name, this.CapabilityToken);
                return false;
            }
        }
        public virtual async Task<bool> TrySaveAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                await this.ConfigHandler.SaveValuesAsync(typeof(TConfig), this.CapabilityToken, cancellationToken);
                this.Logger.LogDebug("[ConfigAccessorFor] Successfully saved values for configuration type {ConfigType}", typeof(TConfig).Name);
                return true;
            }
            catch (Exception ex)
            {
                this.Logger.LogError(ex, "[ConfigAccessorFor] Failed to asynchronously save values for configuration type {ConfigType} with token {CapabilityToken}", typeof(TConfig).Name, this.CapabilityToken);
                return false;
            }
        }

        #endregion

        #region ConfigAccessorFor: Instance Operations

        public virtual TConfig Get()
        {
            this.Logger.LogDebug("[ConfigAccessorFor] Get called for configuration of type {ConfigType}", typeof(TConfig).Name);
            return (TConfig)this.ConfigHandler.GetConfigInstance(typeof(TConfig), this.CapabilityToken);
        }
        public virtual void Save(TConfig config)
        {
            ArgumentNullException.ThrowIfNull(config);

            this.Logger.LogDebug("[ConfigAccessorFor] Save called for configuration of type {ConfigType}", typeof(TConfig).Name);
            this.ConfigHandler.SaveConfigInstance(typeof(TConfig), config, this.CapabilityToken);
        }
        public virtual async Task SaveAsync(TConfig config, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(config);

            this.Logger.LogDebug("[ConfigAccessorFor] SaveAsync called for configuration of type {ConfigType}", typeof(TConfig).Name);

            await this.ConfigHandler.SaveConfigInstanceAsync(typeof(TConfig), config, this.CapabilityToken, cancellationToken);
        }

        #endregion

        #region ConfigAccessorFor: Try Instance Operations

        public virtual bool TryGet(out TConfig? config)
        {
            try
            {
                config = (TConfig)this.ConfigHandler.GetConfigInstance(typeof(TConfig), this.CapabilityToken);
                this.Logger.LogDebug("[ConfigAccessorFor] Successfully retrieved configuration of type {ConfigType}", typeof(TConfig).Name);
                return true;
            }
            catch (Exception ex)
            {
                this.Logger.LogError(ex, "[ConfigAccessorFor] Failed to get configuration instance of type {ConfigType} with token {CapabilityToken}", typeof(TConfig).Name, this.CapabilityToken);
                config = default;
                return false;
            }
        }
        public virtual bool TrySave(TConfig config)
        {
            if (config is null)
            {
                this.Logger.LogWarning("[ConfigAccessorFor] Attempted to save a null configuration of type {ConfigType}", typeof(TConfig).Name);
                return false;
            }

            try
            {
                this.ConfigHandler.SaveConfigInstance(typeof(TConfig), config, this.CapabilityToken);
                this.Logger.LogDebug("[ConfigAccessorFor] Successfully saved configuration of type {ConfigType}", typeof(TConfig).Name);
                return true;
            }
            catch (Exception ex)
            {
                this.Logger.LogError(ex, "[ConfigAccessorFor] Failed to save configuration instance of type {ConfigType} with token {CapabilityToken}", typeof(TConfig).Name, this.CapabilityToken);
                return false;
            }
        }
        public virtual async Task<bool> TrySaveAsync(TConfig config, CancellationToken cancellationToken = default)
        {
            if (config is null)
            {
                this.Logger.LogWarning("[ConfigAccessorFor] Attempted to asynchronously save a null configuration of type {ConfigType}", typeof(TConfig).Name);
                return false;
            }

            try
            {
                await this.ConfigHandler.SaveConfigInstanceAsync(typeof(TConfig), config, this.CapabilityToken, cancellationToken);
                this.Logger.LogDebug("[ConfigAccessorFor] Successfully saved configuration of type {ConfigType}", typeof(TConfig).Name);
                return true;
            }
            catch (Exception ex)
            {
                this.Logger.LogError(ex, "[ConfigAccessorFor] Failed to asynchronously save configuration instance of type {ConfigType} with token {CapabilityToken}", typeof(TConfig).Name, this.CapabilityToken);
                return false;
            }
        }

        #endregion
    }
}