using Microsoft.Extensions.Logging;
using NucleusAF.Interfaces.Abstractions.CompositeRegistry;
using NucleusAF.Interfaces.Models;
using NucleusAF.Interfaces.Models.Configuration;
using NucleusAF.Interfaces.Services.Capabilities;
using NucleusAF.Interfaces.Services.Capabilities.Handlers;
using NucleusAF.Interfaces.Services.Configuration;
using NucleusAF.Models.Capabilities;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

namespace NucleusAF.Services.Configuration
{
    public abstract class ConfigHandler : IDisposable,
        IConfigRegistrar,
        IConfigHandler,
        IConfigHandler<object>,
        ICompositeRegistryHandlerFor<IConfigAccessor>
    {
        public virtual Type Key => typeof(IConfigHandler);

        public enum ConfigCapabilities
        {
            Read = 1,
            Write = 2
        }
        public record ConfigResourceKey(Type Type) : IResourceKey
        {
            public bool Matches(object? other)
                => this.Type == other?.GetType();
        }

        protected readonly ILogger<IConfigHandler> Logger;
        protected readonly ICapabilityService CapabilityService;
        protected readonly ICapabilityAccessorFor<IMinimalCapabilityHandler> CapabilityAccessor;
        protected readonly ConcurrentDictionary<Type, object?> Sources = [];
        protected readonly ConcurrentDictionary<Type, ReaderWriterLockSlim> ConfigLock = [];

        protected bool IsDisposed = false;

        public ConfigHandler(ILogger<IConfigHandler> logger, ICapabilityService capabilityService)
        {
            ArgumentNullException.ThrowIfNull(logger);
            ArgumentNullException.ThrowIfNull(capabilityService);

            this.Logger = logger;

            this.CapabilityService = capabilityService;
            this.CapabilityAccessor = this.CapabilityService.GetAccessor<IMinimalCapabilityHandler>();

            this.Logger.LogInformation("[ConfigHandler] Initialized with capability service and minimal capability accessor");
        }

        #region ConfigService: Predicate Operations

        public virtual bool IsRegistered(Type configType)
            => this.Sources.ContainsKey(configType);

        #endregion

        #region ConfigHandler: Registration Operations

        public abstract ICapabilityToken Register(Type configType, IConfigParams configParams, IConfigService configService, ICapabilityToken? token = null);
        public abstract void Unregister(Type configType, ICapabilityToken token);

        #endregion

        #region ConfigHandler: Value Operations

        [return: MaybeNull]
        public abstract T GetValue<T>(Type configType, string key, ICapabilityToken? token = null);
        public abstract void SetValue<T>(Type configType, string key, T newValue, ICapabilityToken? token = null);
        public abstract void SaveValues(Type configType, ICapabilityToken? token = null);
        public abstract Task SaveValuesAsync(Type configType, ICapabilityToken? token = null, CancellationToken cancellationToken = default);

        #endregion

        #region ConfigHandler: Instance Operations

        public abstract object GetConfigInstance(Type configType, ICapabilityToken? token = null);
        public abstract void SaveConfigInstance(Type configType, object updatedConfig, ICapabilityToken? token = null);
        public abstract Task SaveConfigInstanceAsync(Type configType, object updatedConfig, ICapabilityToken? token = null, CancellationToken cancellationToken = default);

        #endregion

        #region ConfigHandler: Resource Management

        public virtual void Dispose()
        {
            if (this.IsDisposed)
                return;

            try
            {
                List<ReaderWriterLockSlim> locks = this.AcquireAllConfigLocks();

                try
                {
                    this.IsDisposed = true;
                }
                finally
                {
                    this.ReleaseAllConfigLocks(locks);
                    this.DisposeConfigLocks();
                }
            }
            catch (Exception ex)
            {
                this.Logger.LogError(ex, "[ConfigHandler] Error during ConfigHandler disposal");
            }

            GC.SuppressFinalize(this);

            this.Logger.LogDebug("[{HandlerType}] disposed", this.GetType().Name);
        }

        protected virtual List<ReaderWriterLockSlim> AcquireAllConfigLocks()
        {
            List<ReaderWriterLockSlim> locks = [.. this.ConfigLock.Values];

            foreach (ReaderWriterLockSlim configLock in locks)
            {
                try
                {
                    configLock.EnterWriteLock();

                    this.Logger.LogDebug("[ConfigHandler] Acquired write lock instance");
                }
                catch (Exception ex)
                {
                    this.Logger.LogWarning(ex, "[ConfigHandler] Failed to acquire write lock during disposal");
                }
            }

            return locks;
        }
        protected virtual void ReleaseAllConfigLocks(List<ReaderWriterLockSlim> locks)
        {
            foreach (ReaderWriterLockSlim configLock in locks)
            {
                try
                {
                    if (configLock.IsWriteLockHeld)
                    {
                        configLock.ExitWriteLock();

                        this.Logger.LogDebug("[ConfigHandler] Released write lock instance");
                    }
                }
                catch (Exception ex)
                {
                    this.Logger.LogWarning(ex, "[ConfigHandler] Failed to release write lock during disposal");
                }
            }
        }
        protected virtual void DisposeConfigLocks()
        {
            foreach (ReaderWriterLockSlim configLock in this.ConfigLock.Values)
            {
                try
                {
                    configLock.Dispose();

                    this.Logger.LogDebug("[ConfigHandler] Disposed configuration lock instance");
                }
                catch (Exception ex)
                {
                    this.Logger.LogWarning(ex, "[ConfigHandler] Error disposing configuration lock");
                }
            }
        }

        #endregion

        #region ConfigHandler: Validation and Helpers

        [return: MaybeNull]
        protected virtual T CastStoredValue<T>(object? raw)
        {
            if (raw is null)
                return default;

            bool isCorrectType = raw is T;

            if (isCorrectType)
                return (T)raw;

            try
            {
                bool isConvertible = raw is IConvertible;
                if (isConvertible) return (T)Convert.ChangeType(raw, typeof(T));
            }
            catch (InvalidCastException ex)
            {
                this.Logger.LogError(ex, "[ConfigHandler] Invalid cast when converting stored value to {TargetType}", typeof(T).Name);
            }
            catch (FormatException ex)
            {
                this.Logger.LogError(ex, "[ConfigHandler] Format error when converting stored value to {TargetType}", typeof(T).Name);
            }
            catch (OverflowException ex)
            {
                this.Logger.LogError(ex, "[ConfigHandler] Overflow error when converting stored value to {TargetType}", typeof(T).Name);
            }

            return default;
        }

        protected virtual ReaderWriterLockSlim GetConfigTypeLock(Type configType)
        {
            ArgumentNullException.ThrowIfNull(configType);

            return this.ConfigLock.GetOrAdd(configType, _ => new ReaderWriterLockSlim());
        }

        protected virtual IResourceKey CreateResourceKey(Type type)
            => new ConfigResourceKey(type);
        protected virtual CapabilitySet CreateCapabilitySet(object? configParams)
        {
            return configParams is IConfigParams memParams
                ? new() {
                    { (int)ConfigCapabilities.Read, memParams.Read },
                    { (int)ConfigCapabilities.Write, memParams.Write }
                }
                : (CapabilitySet)[];
        }

        protected virtual bool HasAccess(Type configType, ICapabilityToken? token = null, ConfigCapabilities validationType = default)
        {
            ArgumentNullException.ThrowIfNull(configType);

            if (!this.Sources.ContainsKey(configType))
            {
                this.Logger.LogWarning("[ConfigHandler] No configuration registered for {ConfigType}", configType.Name);

                return false;
            }

            IResourceKey resourceKey = this.CreateResourceKey(configType);

            bool isOwner = token != null && this.CapabilityAccessor.IsOwner(resourceKey, token);
            bool hasAccess = this.CapabilityAccessor.IsAccessible(resourceKey, (int)validationType, token);

            if (!isOwner && !hasAccess)
            {
                this.Logger.LogWarning("[ConfigHandler] Access denied for {ConfigType}", configType.Name);

                this.Logger.LogDebug("[ConfigHandler] Access check result: denied for {ConfigType}", configType.Name);

                return false;
            }

            this.Logger.LogDebug("[ConfigHandler] Access check result: granted for {ConfigType}", configType.Name);

            return true;
        }

        #endregion
    }
}