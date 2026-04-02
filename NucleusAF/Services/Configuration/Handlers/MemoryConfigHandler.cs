using Microsoft.Extensions.Logging;
using NucleusAF.Interfaces.Abstractions.CompositeRegistry;
using NucleusAF.Interfaces.Models;
using NucleusAF.Interfaces.Models.Configuration;
using NucleusAF.Interfaces.Services.Capabilities;
using NucleusAF.Interfaces.Services.Configuration;
using NucleusAF.Interfaces.Services.Configuration.Accessors;
using NucleusAF.Interfaces.Services.Configuration.Handlers;
using NucleusAF.Models.Capabilities;
using NucleusAF.Models.Configuration;
using NucleusAF.Models.Configuration.Parameters;
using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace NucleusAF.Services.Configuration.Handlers
{
    public class MemoryConfigHandler : ConfigHandler,
        IMemoryConfigHandler,
        IConfigHandler<MemoryConfigParams>,
        ICompositeRegistryHandlerFor<IMemoryConfigAccessor>
    {
        public override Type Key => typeof(MemoryConfigParams);

        public MemoryConfigHandler(ILogger<IConfigHandler> logger, ICapabilityService capabilityServce)
            : base(logger, capabilityServce) => this.Logger.LogInformation("[MemoryConfigHandler] Initialized.");

        #region MemoryConfigHandler: Registration

        public override ICapabilityToken Register(Type configType, IConfigParams configParams, IConfigService configService, ICapabilityToken? token = null)
        {
            ArgumentNullException.ThrowIfNull(configType);
            ArgumentNullException.ThrowIfNull(configParams);
            ArgumentNullException.ThrowIfNull(configService);

            this.Logger.LogTrace("[MemoryConfigHandler] Attempting to register configuration: {ConfigType}", configType.Name);

            if (configParams is not MemoryConfigParams)
            {
                this.Logger.LogError("[MemoryConfigHandler] Invalid config params for {ConfigType}. Expected MemoryConfigParams, got {ParamType}", configType.Name, configParams.GetType().Name);
                return CapabilityToken.None;
            }

            ReaderWriterLockSlim rwLock = this.GetConfigTypeLock(configType);
            ICapabilityToken t;

            rwLock.EnterWriteLock();

            try
            {
                if (this.Sources.ContainsKey(configType))
                {
                    this.Logger.LogError("[MemoryConfigHandler] Configuration for {ConfigType} is already registered", configType.Name);
                    return CapabilityToken.None;
                }

                this.Logger.LogTrace("[MemoryConfigHandler] Registering capability accessor for {ConfigType}", configType.Name);

                t = this.CapabilityAccessor.Register(
                    this.CreateResourceKey(configType),
                    this.CreateCapabilitySet(configParams),
                    token);

                this.Logger.LogTrace("[MemoryConfigHandler] Building configuration source for {ConfigType}", configType.Name);

                IConfigSource source =
                    new ConfigSource(
                        configService: configService,
                        sourceLocation: string.Empty,
                        sourceType: configType,
                        values: this.BuildSettings(configType),
                        configParams.Read,
                        configParams.Write);

                this.Sources[configType] = source;

                this.Logger.LogDebug("[MemoryConfigHandler] Successfully registered configuration: {ConfigType}", configType.Name);
            }
            finally { if (rwLock.IsWriteLockHeld) rwLock.ExitWriteLock(); }

            return t;
        }
        public override void Unregister(Type configType, ICapabilityToken token)
        {
            ArgumentNullException.ThrowIfNull(configType);

            ReaderWriterLockSlim rwLock = this.GetConfigTypeLock(configType);

            rwLock.EnterWriteLock();

            try
            {
                IResourceKey resourceKey = this.CreateResourceKey(configType);

                this.Logger.LogTrace("[MemoryConfigHandler] Attempting to unregister configuration: {ConfigType}", configType.Name);

                if (this.CapabilityAccessor.IsOwner(resourceKey, token))
                {
                    bool removed = this.Sources.TryRemove(configType, out _);
                    if (removed)
                        this.Logger.LogDebug("[MemoryConfigHandler] Removed source for {ConfigType}", configType.Name);
                    else
                        this.Logger.LogWarning("[MemoryConfigHandler] No source entry found to remove for {ConfigType}", configType.Name);

                    this.CapabilityAccessor.Unregister(resourceKey, token);
                    this.Logger.LogDebug("[MemoryConfigHandler] Unregistered capability accessor for {ConfigType}", configType.Name);
                }
                else
                {
                    this.Logger.LogWarning("[MemoryConfigHandler] Token is not owner of {ConfigType}, unregister skipped", configType.Name);
                }
            }
            finally { if (rwLock.IsWriteLockHeld) rwLock.ExitWriteLock(); }

            if (this.ConfigLock.TryRemove(configType, out ReaderWriterLockSlim? configLock))
            {
                configLock.Dispose();
                this.Logger.LogDebug("[MemoryConfigHandler] Disposed lock for {ConfigType}", configType.Name);
            }
            else
            {
                this.Logger.LogTrace("[MemoryConfigHandler] No lock found to dispose for {ConfigType}", configType.Name);
            }
        }

        #endregion

        #region MemoryConfigHandler: Value Operations

        [return: MaybeNull]
        public override T GetValue<T>(Type configType, string key, ICapabilityToken? token = null)
        {
            ArgumentNullException.ThrowIfNull(configType);
            ArgumentNullException.ThrowIfNull(key);

            ReaderWriterLockSlim rwLock = this.GetConfigTypeLock(configType);
            rwLock.EnterReadLock();

            try
            {
                if (!this.HasAccess(configType, token, ConfigCapabilities.Read))
                {
                    this.Logger.LogWarning("[MemoryConfigHandler] Access denied when reading '{Key}' from {ConfigType}", key, configType.Name);
                    throw new UnauthorizedAccessException($"Access denied when reading '{key}' from {configType.Name}");
                }

                if (!this.Sources.TryGetValue(configType, out object? raw) || raw is not IConfigSource source)
                {
                    this.Logger.LogWarning("[MemoryConfigHandler] No configuration source found for {ConfigType}", configType.Name);
                    throw new KeyNotFoundException($"No configuration source found for {configType.Name}");
                }

                if (!source.Values.TryGetValue(key, out object? rawSetting) || rawSetting is not ConfigValue setting)
                {
                    this.Logger.LogInformation("[MemoryConfigHandler] Setting '{Key}' not found in {ConfigType}", key, configType.Name);
                    throw new KeyNotFoundException($"Setting '{key}' not found in {configType.Name}");
                }

                try
                {
                    T? value = this.CastStoredValue<T>(setting.Value);
                    this.Logger.LogDebug("[MemoryConfigHandler] Successfully retrieved setting '{Key}' from {ConfigType} as {TargetType}", key, configType.Name, typeof(T).Name);
                    return value;
                }
                catch (Exception ex)
                {
                    this.Logger.LogError(ex, "[MemoryConfigHandler] Failed to cast setting '{Key}' in {ConfigType} to {TargetType}", key, configType.Name, typeof(T).Name);
                    throw;
                }
            }
            finally { if (rwLock.IsReadLockHeld) rwLock.ExitReadLock(); }
        }
        public override void SetValue<T>(Type configType, string key, T newValue, ICapabilityToken? token = null)
        {
            ArgumentNullException.ThrowIfNull(configType);
            ArgumentNullException.ThrowIfNull(key);

            IConfigService? configService = null;
            ConfigServiceSettingChangeEventArgs? args = null;
            ReaderWriterLockSlim rwLock = this.GetConfigTypeLock(configType);

            rwLock.EnterWriteLock();

            try
            {
                if (!this.HasAccess(configType, token, ConfigCapabilities.Write))
                {
                    this.Logger.LogWarning("[MemoryConfigHandler] Access denied when writing '{Key}' to {ConfigType}", key, configType.Name);
                    throw new UnauthorizedAccessException($"Access denied when writing '{key}' to {configType.Name}");
                }

                if (!this.Sources.TryGetValue(configType, out object? raw) || raw is not IConfigSource source)
                {
                    this.Logger.LogWarning("[MemoryConfigHandler] No configuration source found for {ConfigType}", configType.Name);
                    throw new KeyNotFoundException($"No configuration source found for {configType.Name}");
                }

                if (!source.Values.TryGetValue(key, out object? rawSetting) || rawSetting is not ConfigValue setting)
                {
                    this.Logger.LogInformation("[MemoryConfigHandler] Setting '{Key}' not found in {ConfigType}", key, configType.Name);
                    throw new KeyNotFoundException($"Setting '{key}' not found in {configType.Name}");
                }

                if (!setting.CanWrite)
                {
                    this.Logger.LogWarning("[MemoryConfigHandler] Write denied for setting '{Key}' in {ConfigType}", key, configType.Name);
                    throw new InvalidOperationException($"Write denied for setting '{key}' in {configType.Name}");
                }

                object? oldValue = setting.Value;

                try
                {
                    setting.Value = (T)Convert.ChangeType(newValue, typeof(T))!;
                }
                catch (Exception ex)
                {
                    this.Logger.LogWarning("[MemoryConfigHandler] Failed to convert new value for '{Key}' in {ConfigType} to {TargetType}", key, configType.Name, typeof(T).Name);
                    throw new InvalidCastException($"Failed to convert new value for '{key}' in {configType.Name} to {typeof(T).Name}", ex);
                }

                args = new ConfigServiceSettingChangeEventArgs(configType, key, oldValue, newValue);

                configService = source.ConfigService;
            }
            finally { if (rwLock.IsWriteLockHeld) rwLock.ExitWriteLock(); }

            if (configService != null && args != null)
            {
                configService.OnSettingChanged(this, args.ConfigType, args.Key, args.OldValue, args.NewValue);

                this.Logger.LogDebug("[MemoryConfigHandler] Successfully updated setting '{Key}' in {ConfigType} from {OldValue} to {NewValue}", key, configType.Name, args.OldValue, args.NewValue);
            }
        }
        public override void SaveValues(Type configType, ICapabilityToken? token = null)
        {
            ArgumentNullException.ThrowIfNull(configType);

            this.Logger.LogInformation("[MemoryConfigHandler] SaveValues called for {ConfigType}, but this handler is memory-only and does not persist values.", configType.Name);
        }
        public override async Task SaveValuesAsync(Type configType, ICapabilityToken? token = null, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(configType);

            this.Logger.LogInformation("[MemoryConfigHandler] SaveValuesAsync called for {ConfigType}, but this handler is memory-only and does not persist values.", configType.Name);

            await Task.CompletedTask;
        }

        #endregion

        #region MemoryConfigHandler: Instance Operations

        public override object GetConfigInstance(Type configType, ICapabilityToken? token = null)
        {
            ArgumentNullException.ThrowIfNull(configType);

            ReaderWriterLockSlim rwLock = this.GetConfigTypeLock(configType);
            rwLock.EnterReadLock();

            try
            {
                if (!this.HasAccess(configType, token, ConfigCapabilities.Read))
                {
                    this.Logger.LogWarning("[MemoryConfigHandler] Access denied when creating instance of {ConfigType}", configType.Name);
                    throw new UnauthorizedAccessException($"Access denied when creating instance of {configType.Name}");
                }

                if (!this.Sources.TryGetValue(configType, out object? raw) || raw is not IConfigSource source)
                {
                    this.Logger.LogWarning("[MemoryConfigHandler] No configuration source found for {ConfigType}", configType.Name);
                    throw new KeyNotFoundException($"No configuration source found for {configType.Name}");
                }

                object? instance = Activator.CreateInstance(configType);

                if (instance == null)
                {
                    this.Logger.LogError("[MemoryConfigHandler] Failed to create instance of {ConfigType}", configType.Name);
                    throw new InvalidOperationException($"Failed to create instance of {configType.Name}");
                }

                this.PopulateInstanceProperties(configType, source, instance);

                this.Logger.LogDebug("[MemoryConfigHandler] Successfully created and populated instance of {ConfigType}", configType.Name);

                return instance;
            }
            finally { if (rwLock.IsReadLockHeld) rwLock.ExitReadLock(); }
        }
        public override void SaveConfigInstance(Type configType, object updatedConfig, ICapabilityToken? token = null)
        {
            ArgumentNullException.ThrowIfNull(configType);
            ArgumentNullException.ThrowIfNull(updatedConfig);

            this.Logger.LogInformation("[MemoryConfigHandler] SaveConfigInstance called for {ConfigType}, but this handler is memory-only and does not persist instances.", configType.Name);
        }
        public override async Task SaveConfigInstanceAsync(Type configType, object updatedConfig, ICapabilityToken? token = null, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(configType);
            ArgumentNullException.ThrowIfNull(updatedConfig);

            this.Logger.LogInformation("[MemoryConfigHandler] SaveConfigInstanceAsync called for {ConfigType}, but this handler is memory-only and does not persist instances.", configType.Name);

            await Task.CompletedTask;
        }

        #endregion

        #region MemoryConfigHandler: Configuration Management

        protected virtual Dictionary<string, object?> BuildSettings(Type configType)
        {
            ArgumentNullException.ThrowIfNull(configType);

            object? instance = Activator.CreateInstance(configType);

            if (instance == null)
            {
                this.Logger.LogError("[MemoryConfigHandler] Failed to create instance of {ConfigType}", configType.Name);

                return [];
            }

            Dictionary<string, object?> settings = [];
            PropertyInfo[] properties = configType.GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (PropertyInfo prop in properties)
            {
                string key = prop.Name;
                object? value = null;

                if (prop.CanRead)
                {
                    try
                    {
                        value = prop.GetValue(instance);
                    }
                    catch { /* nothing to see here */ }
                }

                ConfigValue configValue = new()
                {
                    ValueType = prop.PropertyType,
                    Value = value,
                    CanRead = prop.CanRead,
                    CanWrite = prop.CanWrite
                };

                settings[key] = configValue;
            }

            return settings;
        }

        protected virtual void PopulateInstanceProperties(Type configType, IConfigSource? source, object instance)
        {
            PropertyInfo[] properties = configType.GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (PropertyInfo prop in properties)
            {
                bool canWriteProperty = prop.CanWrite;

                if (!canWriteProperty)
                    continue;

                object? rawSetting = null;
                bool hasSettingValue = source != null && source.Values.TryGetValue(prop.Name, out rawSetting);
                bool isCorrectSettingType = rawSetting != null && hasSettingValue && rawSetting is ConfigValue;

                if (hasSettingValue && isCorrectSettingType)
                {
                    ConfigValue setting = (ConfigValue)rawSetting!;

                    this.SetPropertyValue(instance, prop, setting.Value, configType);
                }
            }
        }
        protected virtual void SetPropertyValue(object instance, PropertyInfo prop, object? value, Type configType)
        {
            try
            {
                if (value == null)
                {
                    bool canAcceptNull = !prop.PropertyType.IsValueType || Nullable.GetUnderlyingType(prop.PropertyType) != null;
                    bool isCollection = typeof(IEnumerable).IsAssignableFrom(prop.PropertyType) && prop.PropertyType != typeof(string);

                    if (canAcceptNull && !isCollection)
                        prop.SetValue(instance, null);

                    return;
                }

                if (prop.PropertyType.IsEnum)
                {
                    bool enumValueIsValid = Enum.IsDefined(prop.PropertyType, value);
                    object enumValue = enumValueIsValid
                        ? Enum.ToObject(prop.PropertyType, value)
                        : Activator.CreateInstance(prop.PropertyType)!;

                    prop.SetValue(instance, enumValue);
                }
                else if (prop.PropertyType.IsInstanceOfType(value))
                {
                    prop.SetValue(instance, value);
                }
                else
                {
                    Type targetType = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;
                    object convertedValue = Convert.ChangeType(value, targetType);
                    prop.SetValue(instance, convertedValue);
                }
            }
            catch (Exception ex) when (ex is InvalidCastException or FormatException or OverflowException)
            {
                this.Logger.LogError(ex, "[MemoryConfigHandler] Failed to set property {PropertyName} for type {ConfigType}", prop.Name, configType.Name);
            }
        }

        #endregion
    }
}