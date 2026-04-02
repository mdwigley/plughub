using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NucleusAF.Extensions;
using NucleusAF.Interfaces.Abstractions.CompositeRegistry;
using NucleusAF.Interfaces.Models;
using NucleusAF.Interfaces.Models.Configuration;
using NucleusAF.Interfaces.Models.Configuration.Sources;
using NucleusAF.Interfaces.Services.Capabilities;
using NucleusAF.Interfaces.Services.Configuration;
using NucleusAF.Interfaces.Services.Configuration.Accessors;
using NucleusAF.Interfaces.Services.Configuration.Handlers;
using NucleusAF.Models.Capabilities;
using NucleusAF.Models.Configuration;
using NucleusAF.Models.Configuration.Parameters;
using NucleusAF.Models.Configuration.Sources;
using NucleusAF.Utility;
using System.Collections;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text.Json;

namespace NucleusAF.Services.Configuration.Handlers
{
    public class JsonConfigHandler : ConfigHandler,
        IJsonConfigHandler,
        IConfigHandler<JsonConfigParams>,
        ICompositeRegistryHandlerFor<IJsonConfigAccessor>
    {
        public override Type Key => typeof(JsonConfigParams);

        protected JsonSerializerOptions JsonOptions { get; init; } = new JsonSerializerOptions();
        protected readonly ConcurrentDictionary<Type, Timer> ReloadTimers = [];

        public JsonConfigHandler(ILogger<IConfigHandler> logger, ICapabilityService capabilityService)
            : base(logger, capabilityService) => this.Logger.LogInformation("[JsonConfigHandler] Initialized");

        #region JsonConfigHandler: Registration

        public override ICapabilityToken Register(Type configType, IConfigParams configParams, IConfigService configService, ICapabilityToken? token = null)
        {
            ArgumentNullException.ThrowIfNull(configType);
            ArgumentNullException.ThrowIfNull(configParams);
            ArgumentNullException.ThrowIfNull(configService);

            this.Logger.LogTrace("[JsonConfigHandler] Attempting to register configuration: {ConfigType}", configType.Name);

            if (configParams is not JsonConfigParams p)
            {
                this.Logger.LogError("[JsonConfigHandler] Invalid config params for {ConfigType}. Expected JsonConfigParams, got {ParamType}", configType.Name, configParams.GetType().Name);
                return CapabilityToken.None;
            }

            ICapabilityToken t;
            ReaderWriterLockSlim rwLock = this.GetConfigTypeLock(configType);
            rwLock.EnterWriteLock();

            try
            {
                if (this.Sources.ContainsKey(configType))
                {
                    this.Logger.LogError("[JsonConfigHandler] Configuration for {ConfigType} is already registered", configType.Name);
                    return CapabilityToken.None;
                }

                this.Logger.LogTrace("[JsonConfigHandler] Creating source object for {ConfigType}", configType.Name);
                JsonConfigSource source = this.CreateSourceObject(configType, p, configService);

                this.Logger.LogTrace("[JsonConfigHandler] Registering capability accessor for {ConfigType}", configType.Name);
                t = this.CapabilityAccessor.Register(
                    this.CreateResourceKey(configType),
                    this.CreateCapabilitySet(configParams),
                    token);

                if (source.ReloadOnChanged)
                {
                    this.Logger.LogTrace("[JsonConfigHandler] Setting up reload callback for {ConfigType}", configType.Name);
                    source.OnChanged = source.Configuration
                        .GetReloadToken()
                        .RegisterChangeCallback(this.OnConfigHasChanged, configType);
                }

                this.Sources[configType] = source;

                this.Logger.LogDebug("[JsonConfigHandler] Successfully registered configuration: {ConfigType}", configType.Name);
            }
            finally
            {
                if (rwLock.IsWriteLockHeld) rwLock.ExitWriteLock();
            }

            return t;
        }
        public override void Unregister(Type configType, ICapabilityToken token)
        {
            ArgumentNullException.ThrowIfNull(configType);

            ReaderWriterLockSlim rwLock = this.GetConfigTypeLock(configType);

            rwLock.EnterWriteLock();

            try
            {
                this.Logger.LogTrace("[JsonConfigHandler] Attempting to unregister configuration: {ConfigType}", configType.Name);

                if (!this.Sources.TryGetValue(configType, out object? raw) || raw is not IJsonConfigSource source)
                {
                    this.Logger.LogError("[JsonConfigHandler] No configuration source found for {ConfigType}", configType.Name);
                    throw new InvalidOperationException($"No configuration source found for {configType.Name}");
                }

                IResourceKey resourceKey = this.CreateResourceKey(configType);

                if (!this.CapabilityAccessor.IsOwner(resourceKey, token))
                {
                    this.Logger.LogError("[JsonConfigHandler] Unregister denied. Caller is not owner of {ConfigType}", configType.Name);
                    throw new UnauthorizedAccessException($"Unregister denied. Caller is not owner of {configType.Name}");
                }

                source.OnChanged?.Dispose();
                this.Logger.LogTrace("[JsonConfigHandler] Disposed change subscription for {ConfigType}", configType.Name);

                this.Sources.TryRemove(configType, out _);
                this.Logger.LogTrace("[JsonConfigHandler] Removed source entry for {ConfigType}", configType.Name);

                this.CapabilityAccessor.Unregister(resourceKey, token);
                this.Logger.LogTrace("[JsonConfigHandler] Unregistered capability accessor for {ConfigType}", configType.Name);

                this.Logger.LogDebug("[JsonConfigHandler] Successfully unregistered configuration: {ConfigType}", configType.Name);
            }
            finally { if (rwLock.IsWriteLockHeld) rwLock.ExitWriteLock(); }

            if (this.ConfigLock.TryRemove(configType, out ReaderWriterLockSlim? configLock))
            {
                configLock.Dispose();
                this.Logger.LogDebug("[JsonConfigHandler] Disposed lock for {ConfigType}", configType.Name);
            }
            else
            {
                this.Logger.LogTrace("[JsonConfigHandler] No lock found to dispose for {ConfigType}", configType.Name);
            }
        }

        #endregion

        #region JsonConfigHandler: Value Operations

        [return: MaybeNull]
        public override T GetValue<T>(Type configType, string key, ICapabilityToken? token = null)
        {
            ArgumentNullException.ThrowIfNull(configType);
            ArgumentNullException.ThrowIfNull(key);

            ReaderWriterLockSlim rwLock = this.GetConfigTypeLock(configType);
            rwLock.EnterReadLock();

            try
            {
                this.Logger.LogTrace("[JsonConfigHandler] Attempting to read setting '{Key}' from {ConfigType}", key, configType.Name);

                if (!this.HasAccess(configType, token, ConfigCapabilities.Read))
                {
                    this.Logger.LogWarning("[JsonConfigHandler] Access denied when reading '{Key}' from {ConfigType}", key, configType.Name);
                    throw new UnauthorizedAccessException($"Access denied when reading '{key}' from {configType.Name}.");
                }

                if (!this.Sources.TryGetValue(configType, out object? raw) || raw is not IJsonConfigSource source)
                {
                    this.Logger.LogError("[JsonConfigHandler] No configuration source found for {ConfigType}", configType.Name);
                    throw new InvalidOperationException($"No configuration source found for {configType.Name}.");
                }

                if (!source.Values.TryGetValue(key, out object? rawSetting) || rawSetting is not ConfigValue setting)
                {
                    this.Logger.LogInformation("[JsonConfigHandler] Setting '{Key}' not found in {ConfigType}", key, configType.Name);
                    throw new KeyNotFoundException($"Setting '{key}' not found in {configType.Name}.");
                }

                try
                {
                    T? value = this.CastStoredValue<T>(setting.Value);
                    this.Logger.LogDebug("[JsonConfigHandler] Successfully retrieved setting '{Key}' from {ConfigType} as {TargetType}", key, configType.Name, typeof(T).Name);
                    return value;
                }
                catch (Exception ex)
                {
                    this.Logger.LogError(ex, "[JsonConfigHandler] Failed to cast setting '{Key}' in {ConfigType} to {TargetType}", key, configType.Name, typeof(T).Name);
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
                this.Logger.LogTrace("[JsonConfigHandler] Attempting to write setting '{Key}' in {ConfigType}", key, configType.Name);

                if (!this.HasAccess(configType, token, ConfigCapabilities.Write))
                {
                    this.Logger.LogWarning("[JsonConfigHandler] Access denied when writing '{Key}' to {ConfigType}", key, configType.Name);
                    throw new UnauthorizedAccessException($"Access denied when writing '{key}' to {configType.Name}");
                }

                if (!this.Sources.TryGetValue(configType, out object? raw) || raw is not IJsonConfigSource source)
                {
                    this.Logger.LogError("[JsonConfigHandler] No configuration source found for {ConfigType}", configType.Name);
                    throw new InvalidOperationException($"No configuration source found for {configType.Name}");
                }

                if (!source.Values.TryGetValue(key, out object? rawSetting) || rawSetting is not ConfigValue setting)
                {
                    this.Logger.LogInformation("[JsonConfigHandler] Setting '{Key}' not found in {ConfigType}", key, configType.Name);
                    throw new KeyNotFoundException($"Setting '{key}' not found in {configType.Name}");
                }

                if (!setting.CanWrite)
                {
                    this.Logger.LogWarning("[JsonConfigHandler] Write denied for setting '{Key}' in {ConfigType}", key, configType.Name);
                    throw new UnauthorizedAccessException($"Write denied for setting '{key}' in {configType.Name}");
                }

                object? oldValue = setting.Value;

                try
                {
                    setting.Value = (T)Convert.ChangeType(newValue, typeof(T))!;
                }
                catch
                {
                    this.Logger.LogWarning("[JsonConfigHandler] Failed to convert new value for '{Key}' in {ConfigType} to {TargetType}", key, configType.Name, typeof(T).Name);
                    throw new InvalidCastException($"Failed to convert new value for '{key}' in {configType.Name} to {typeof(T).Name}");
                }

                args = new ConfigServiceSettingChangeEventArgs(configType, key, oldValue, newValue);
                configService = source.ConfigService;

                this.Logger.LogDebug("[JsonConfigHandler] Successfully updated setting '{Key}' in {ConfigType} from {OldValue} to {NewValue}", key, configType.Name, oldValue, newValue);
            }
            finally { if (rwLock.IsWriteLockHeld) rwLock.ExitWriteLock(); }

            if (configService != null && args != null)
                configService.OnSettingChanged(this, args.ConfigType, args.Key, args.OldValue, args.NewValue);
        }
        public override void SaveValues(Type configType, ICapabilityToken? token = null)
        {
            ArgumentNullException.ThrowIfNull(configType);

            IConfigService? configService = null;
            ConfigServiceSaveErrorEventArgs? errorArgs = null;

            Task.Run(async () =>
            {
                try
                {
                    this.Logger.LogTrace("[JsonConfigHandler] Attempting to save values for {ConfigType}", configType.Name);

                    if (!this.HasAccess(configType, token, ConfigCapabilities.Write))
                    {
                        this.Logger.LogWarning("[JsonConfigHandler] Access denied when saving values for {ConfigType}", configType.Name);

                        errorArgs = new ConfigServiceSaveErrorEventArgs(
                            new UnauthorizedAccessException("Write access denied."),
                            ConfigSaveOperation.SaveSettings,
                            configType);
                    }
                    else if (!this.Sources.TryGetValue(configType, out object? raw) || raw is not IJsonConfigSource source)
                    {
                        this.Logger.LogError("[JsonConfigHandler] No configuration source found for {ConfigType}", configType.Name);

                        errorArgs = new ConfigServiceSaveErrorEventArgs(
                            new KeyNotFoundException("Configuration source not found."),
                            ConfigSaveOperation.SaveSettings,
                            configType);
                    }
                    else
                    {
                        configService = source.ConfigService;

                        await this.SaveValuesAsync(configType, token).ConfigureAwait(false);

                        this.Logger.LogDebug("[JsonConfigHandler] Successfully saved values for {ConfigType}", configType.Name);
                    }
                }
                catch (Exception ex)
                {
                    this.Logger.LogError(ex, "[JsonConfigHandler] Exception occurred while saving values for {ConfigType}", configType.Name);

                    errorArgs = new ConfigServiceSaveErrorEventArgs(ex, ConfigSaveOperation.SaveSettings, configType);
                }

                if (configService != null)
                {
                    if (errorArgs == null)
                        configService.OnSaveOperationComplete(this, configType);
                    else
                        configService.OnSaveOperationError(this, errorArgs.Exception, errorArgs.Operation, configType);
                }
            });
        }
        public override async Task SaveValuesAsync(Type configType, ICapabilityToken? token = null, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(configType);

            cancellationToken.ThrowIfCancellationRequested();

            Dictionary<string, object?> configValues;
            string sourceLocation;
            JsonSerializerOptions jsonOpts;
            IJsonConfigSource? source = null;

            ReaderWriterLockSlim rw = this.GetConfigTypeLock(configType);
            rw.EnterWriteLock();

            try
            {
                this.Logger.LogTrace("[JsonConfigHandler] Attempting to save values for {ConfigType}", configType.Name);

                if (!this.HasAccess(configType, token, ConfigCapabilities.Write))
                {
                    this.Logger.LogWarning("[JsonConfigHandler] Access denied when saving values for {ConfigType}", configType.Name);
                    throw new UnauthorizedAccessException($"Access denied when saving values for {configType.Name}");
                }

                if (!this.Sources.TryGetValue(configType, out object? raw) || raw is not IJsonConfigSource s)
                {
                    this.Logger.LogError("[JsonConfigHandler] No configuration source found for {ConfigType}", configType.Name);
                    throw new InvalidOperationException($"No configuration source found for {configType.Name}");
                }

                source = s;

                configValues = source.Values
                    .Select(kvp => (kvp.Key, setting: kvp.Value as ConfigValue))
                    .Where(t => t.setting != null)
                    .ToDictionary(
                        t => t.Key,
                        t => t.setting!.Value);

                sourceLocation = source.SourceLocation;
                jsonOpts = source.JsonSerializerOptions;
            }
            finally { if (rw.IsWriteLockHeld) rw.ExitWriteLock(); }

            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                await this.SaveSettingsToFileAsync(sourceLocation, configValues, jsonOpts, cancellationToken).ConfigureAwait(false);

                source?.ConfigService.OnSaveOperationComplete(this, configType);

                this.Logger.LogDebug("[JsonConfigHandler] Successfully saved values for {ConfigType}", configType.Name);
            }
            catch (Exception ex)
            {
                this.Logger.LogError(ex, "[JsonConfigHandler] Failed to save settings for {ConfigType}", configType.Name);

                source?.ConfigService.OnSaveOperationError(this, ex, ConfigSaveOperation.SaveSettings, configType);

                throw new InvalidOperationException($"Failed to save config values for {configType.Name}", ex);
            }
        }

        #endregion

        #region JsonConfigHandler: Instance Operations

        public override object GetConfigInstance(Type configType, ICapabilityToken? token = null)
        {
            ArgumentNullException.ThrowIfNull(configType);

            ReaderWriterLockSlim rwLock = this.GetConfigTypeLock(configType);
            rwLock.EnterReadLock();

            try
            {
                this.Logger.LogTrace("[JsonConfigHandler] Attempting to create config instance for {ConfigType}", configType.Name);

                if (!this.HasAccess(configType, token, ConfigCapabilities.Read))
                {
                    this.Logger.LogWarning("[JsonConfigHandler] Access denied when creating instance of {ConfigType}", configType.Name);
                    throw new UnauthorizedAccessException($"Access denied when creating instance of {configType.Name}");
                }

                if (!this.Sources.TryGetValue(configType, out object? raw) || raw is not IJsonConfigSource source)
                {
                    this.Logger.LogWarning("[JsonConfigHandler] No configuration source found for {ConfigType}", configType.Name);
                    throw new InvalidOperationException($"No configuration source found for {configType.Name}");
                }

                object? instance = Activator.CreateInstance(configType);

                if (instance == null)
                {
                    this.Logger.LogError("[JsonConfigHandler] Failed to create instance of {ConfigType}", configType.Name);
                    throw new InvalidOperationException($"Failed to create instance of {configType.Name}");
                }

                this.PopulateInstanceProperties(instance, source, configType);

                this.Logger.LogDebug("[JsonConfigHandler] Successfully created and populated instance of {ConfigType}", configType.Name);

                return instance;
            }
            finally { if (rwLock.IsReadLockHeld) rwLock.ExitReadLock(); }
        }
        public override void SaveConfigInstance(Type configType, object updatedConfig, ICapabilityToken? token = null)
        {
            ArgumentNullException.ThrowIfNull(configType);
            ArgumentNullException.ThrowIfNull(updatedConfig);

            IConfigService? configService = null;
            ConfigServiceSaveErrorEventArgs? errorArgs = null;

            Task.Run(async () =>
            {
                try
                {
                    this.Logger.LogTrace("[JsonConfigHandler] Attempting to save config instance for {ConfigType}", configType.Name);

                    if (!this.HasAccess(configType, token, ConfigCapabilities.Write))
                    {
                        this.Logger.LogWarning("[JsonConfigHandler] Access denied when saving config instance for {ConfigType}", configType.Name);

                        errorArgs = new ConfigServiceSaveErrorEventArgs(
                            new UnauthorizedAccessException("Write access denied."),
                            ConfigSaveOperation.SaveConfigInstance,
                            configType);
                    }
                    else if (!this.Sources.TryGetValue(configType, out object? raw) || raw is not IJsonConfigSource source)
                    {
                        this.Logger.LogError("[JsonConfigHandler] No configuration source found for {ConfigType}", configType.Name);

                        errorArgs = new ConfigServiceSaveErrorEventArgs(
                            new KeyNotFoundException("Configuration source not found."),
                            ConfigSaveOperation.SaveConfigInstance,
                            configType);
                    }
                    else
                    {
                        configService = source.ConfigService;

                        await this.SaveConfigInstanceAsync(configType, updatedConfig, token).ConfigureAwait(false);

                        this.Logger.LogDebug("[JsonConfigHandler] Successfully saved config instance for {ConfigType}", configType.Name);
                    }
                }
                catch (Exception ex)
                {
                    this.Logger.LogError(ex, "[JsonConfigHandler] Exception occurred while saving config instance for {ConfigType}", configType.Name);

                    errorArgs = new ConfigServiceSaveErrorEventArgs(ex, ConfigSaveOperation.SaveConfigInstance, configType);
                }

                if (configService != null)
                {
                    if (errorArgs == null)
                        configService.OnSaveOperationComplete(this, configType);
                    else
                        configService.OnSaveOperationError(this, errorArgs.Exception, errorArgs.Operation, configType);
                }
            });
        }
        public override async Task SaveConfigInstanceAsync(Type configType, object updatedConfig, ICapabilityToken? token = null, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(configType);
            ArgumentNullException.ThrowIfNull(updatedConfig);

            cancellationToken.ThrowIfCancellationRequested();

            ReaderWriterLockSlim rwLock = this.GetConfigTypeLock(configType);
            IJsonConfigSource? source = null;

            rwLock.EnterWriteLock();

            try
            {
                if (!this.HasAccess(configType, token, ConfigCapabilities.Write))
                {
                    this.Logger.LogWarning("[JsonConfigHandler] Access denied when saving config instance for {ConfigType}", configType.Name);
                    throw new UnauthorizedAccessException($"Access denied when saving values for {configType.Name}");
                }

                if (!this.Sources.TryGetValue(configType, out object? raw) || raw is not IJsonConfigSource s)
                {
                    this.Logger.LogError("[JsonConfigHandler] No configuration source found for {ConfigType}", configType.Name);
                    throw new InvalidOperationException($"No configuration source found for {configType.Name}");
                }

                source = s;

                this.UpdateConfigurationFromInstance(updatedConfig, source, configType);
                this.Logger.LogTrace("[JsonConfigHandler] Updated configuration instance for {ConfigType}", configType.Name);
            }
            finally { if (rwLock.IsWriteLockHeld) rwLock.ExitWriteLock(); }

            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                await this.SaveValuesAsync(configType, token, cancellationToken).ConfigureAwait(false);

                source?.ConfigService.OnSaveOperationComplete(this, configType);

                this.Logger.LogDebug("[JsonConfigHandler] Successfully saved configuration instance for {ConfigType}", configType.Name);
            }
            catch (Exception ex)
            {
                this.Logger.LogError(ex, "[JsonConfigHandler] Failed to save config instance for {ConfigType}", configType.Name);

                source?.ConfigService.OnSaveOperationError(this, ex, ConfigSaveOperation.SaveConfigInstance, configType);

                throw new InvalidOperationException($"Failed to save config instance for {configType.Name}", ex);
            }
        }

        #endregion

        #region JsonConfigHandler: Configuration Building

        protected virtual object? GetBuildSettingsValue(IConfiguration config, PropertyInfo property)
        {
            IConfigurationSection section = config.GetSection(property.Name);

            bool sectionExists = section.Exists();

            return !sectionExists ? null : this.ConvertConfigurationValue(section, property.PropertyType);
        }
        protected virtual object? ConvertConfigurationValue(IConfigurationSection section, Type propertyType)
        {
            try
            {
                return section.Get(propertyType);
            }
            catch
            {
                try
                {
                    return Convert.ChangeType(section.Value, propertyType);
                }
                catch (Exception ex)
                {
                    this.Logger.LogWarning(ex, "[JsonConfigHandler] Failed to convert configuration value {Value} to type {Type}", section.Value, propertyType.Name);

                    return null;
                }
            }
        }
        protected virtual IConfiguration BuildConfig(string filePath, bool reloadOnChange = false)
        {
            ArgumentNullException.ThrowIfNull(filePath);

            try
            {
                return new ConfigurationBuilder()
                    .AddJsonFile(filePath, optional: true, reloadOnChange: reloadOnChange)
                    .Build();
            }
            catch (Exception ex)
            {
                this.Logger.LogError(ex, "[JsonConfigHandler] Failed to build configuration from file '{FilePath}'", filePath);

                return new ConfigurationBuilder().Build();
            }
        }

        #endregion

        #region JsonConfigHandler: Configuration Management

        protected virtual JsonConfigSource CreateSourceObject(Type configType, JsonConfigParams p, IConfigService configService)
        {
            JsonSerializerOptions jsonOptions = p.JsonSerializerOptions ?? this.JsonOptions;
            bool reloadOnChange = p.ReloadOnChange;

            string defaultConfigFilePath = this.ResolveLocalFilePath(
                p.ConfigUriOverride,
                configService.ConfigDataDirectory,
                configType,
                "json");

            this.EnsureFileExists(defaultConfigFilePath, jsonOptions, configType);

            IConfiguration defaultConfig = this.BuildConfig(defaultConfigFilePath, reloadOnChange);
            Dictionary<string, object?> values = this.BuildSettings(configType, [defaultConfig]);

            return new JsonConfigSource(
                configService,
                defaultConfigFilePath,
                configType,
                defaultConfig,
                values,
                jsonOptions,
                p.Read,
                p.Write,
                reloadOnChange);
        }
        protected virtual void PopulateInstanceProperties(object instance, IJsonConfigSource source, Type configType)
        {
            PropertyInfo[] properties = configType.GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (PropertyInfo prop in properties)
            {
                bool canWriteProperty = prop.CanWrite;

                if (!canWriteProperty)
                    continue;

                bool hasSettingValue = source.Values.TryGetValue(prop.Name, out object? rawSetting);
                bool isCorrectSettingType = hasSettingValue && rawSetting is ConfigValue;

                if (isCorrectSettingType)
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
                this.Logger.LogError(ex, "[JsonConfigHandler] Failed to set property {PropertyName} for type {ConfigType}", prop.Name, configType.Name);
            }
        }
        protected virtual void UpdateConfigurationFromInstance(object updatedConfig, IJsonConfigSource source, Type configType)
        {
            PropertyInfo[] properties = configType.GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (PropertyInfo prop in properties)
            {
                string key = prop.Name;
                object? newValue = prop.GetValue(updatedConfig);

                if (!source.Values.TryGetValue(key, out object? rawSetting) || rawSetting is not ConfigValue setting)
                {
                    this.Logger.LogInformation("[JsonConfigHandler] Property '{Key}' not found in settings for {ConfigType}", key, configType.Name);

                    continue;
                }

                try
                {
                    setting.Value = newValue;
                }
                catch (Exception ex)
                {
                    this.Logger.LogWarning(ex, "[JsonConfigHandler] Failed to update key '{Key}' on type {ConfigType}. Value: {Value}", key, configType.Name, newValue);
                }
            }
        }

        #endregion

        #region JsonConfigHandler: Resource Management

        public override void Dispose()
        {
            if (this.IsDisposed)
                return;

            base.Dispose();

            try
            {
                this.DisposeConfigurationObjects();
                this.DisposeReloadTimers();
            }
            catch { /* nothing to see here */}

            GC.SuppressFinalize(this);
        }

        protected virtual void DisposeConfigurationObjects()
        {
            foreach (object? configObject in this.Sources.Values)
            {
                if (configObject is IJsonConfigSource source)
                {
                    try
                    {
                        source.OnChanged?.Dispose();
                    }
                    catch (Exception ex)
                    {
                        this.Logger.LogWarning(ex, "[JsonConfigHandler] Error disposing configuration change tokens");
                    }
                }
            }

            this.Sources.Clear();
        }
        protected virtual void DisposeReloadTimers()
        {
            foreach (Timer timer in this.ReloadTimers.Values)
                timer.Dispose();

            this.ReloadTimers.Clear();
        }

        #endregion

        #region JsonConfigHandler: Configuration Change Handling

        protected virtual void HandleConfigHasChanged(Type configType)
        {
            ArgumentNullException.ThrowIfNull(configType);

            IJsonConfigSource? source = null;
            IConfigService? configService = null;

            bool found = false;
            bool reloaded = false;

            ReaderWriterLockSlim rw = this.GetConfigTypeLock(configType);
            rw.EnterWriteLock();

            try
            {
                if (!this.Sources.TryGetValue(configType, out object? raw) || raw is not IJsonConfigSource s)
                {
                    this.Logger.LogWarning("[JsonConfigHandler] No configuration source found for {ConfigType}", configType.Name);

                    return;
                }

                source = s;
                configService = source.ConfigService;

                try
                {
                    Dictionary<string, object?> newSettings = this.BuildSettings(configType, [source.Configuration]);
                    source.Values = newSettings;
                    found = true;

                    if (source.ReloadOnChanged)
                    {
                        source.OnChanged?.Dispose();

                        source.OnChanged = source.Configuration
                            .GetReloadToken()
                            .RegisterChangeCallback(this.OnConfigHasChanged, configType);
                    }

                    reloaded = true;
                }
                catch (FileNotFoundException ex)
                {
                    this.Logger.LogWarning(ex, "[JsonConfigHandler] Config directory missing – Type {Type}", configType.Name);
                }
                catch (Exception ex)
                {
                    this.Logger.LogError(ex, "[JsonConfigHandler] Config reload failed – Type {Type}", configType.Name);
                }
            }
            finally { if (rw.IsWriteLockHeld) rw.ExitWriteLock(); }

            if (configService != null && reloaded)
                configService.OnConfigReloaded(this, configType);

            if (!found)
                this.Logger.LogWarning("[JsonConfigHandler] Unregistered or not reloaded config type – Type {Type}", configType.Name);
        }

        protected virtual Dictionary<string, object?> BuildSettings(Type configType, params IConfiguration[] configurations)
        {
            ArgumentNullException.ThrowIfNull(configType);
            ArgumentNullException.ThrowIfNull(configurations);

            Dictionary<string, object?> settings = [];
            PropertyInfo[] properties = configType.GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (PropertyInfo prop in properties)
            {
                string key = prop.Name;
                object? value = null;

                foreach (IConfiguration configuration in configurations)
                {
                    object? v = this.ExtractConfigurationValue(configuration, prop);

                    if (v != null) value = v;
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
        protected virtual object? ExtractConfigurationValue(IConfiguration cfg, PropertyInfo prop)
        {
            IConfigurationSection section = cfg.GetSection(prop.Name);

            bool sectionExists = section.Exists();

            if (!sectionExists)
                return null;

            bool isGenericType = prop.PropertyType.IsGenericType;
            bool isEnumerable = typeof(IEnumerable).IsAssignableFrom(prop.PropertyType);

            return isGenericType && isEnumerable && prop.PropertyType.GetGenericTypeDefinition() == typeof(List<>)
                ? this.CreateListFromConfiguration(section, prop.PropertyType)
                : this.ConvertSectionValue(section, prop.PropertyType);
        }
        protected virtual object? CreateListFromConfiguration(IConfigurationSection section, Type propertyType)
        {
            Type elementType = propertyType.GetGenericArguments()[0];
            Type listType = typeof(List<>).MakeGenericType(elementType);

            IList list = (IList)Activator.CreateInstance(listType)!;

            foreach (IConfigurationSection child in section.GetChildren())
            {
                object? element = Activator.CreateInstance(elementType);

                child.Bind(element);

                list.Add(element);
            }

            return list;
        }
        protected virtual object? ConvertSectionValue(IConfigurationSection section, Type propertyType)
        {
            try
            {
                return section.Get(propertyType);
            }
            catch
            {
                try
                {
                    return Convert.ChangeType(section.Value, propertyType);
                }
                catch
                {
                    return null;
                }
            }
        }

        protected virtual void OnConfigHasChanged(object? state)
        {
            if (state is not Type configType)
                return;

            Timer debounceTimer = this.ReloadTimers.GetOrAdd(configType, this.CreateDebounceTimer);

            debounceTimer.Change(TimeSpan.FromMilliseconds(300), Timeout.InfiniteTimeSpan);
        }
        protected virtual Timer CreateDebounceTimer(Type configType)
        {
            return new Timer(
                _ => this.HandleConfigHasChanged(configType),
                null,
                TimeSpan.FromMilliseconds(300),
                Timeout.InfiniteTimeSpan);
        }

        #endregion

        #region JsonConfigHandler: File Operations

        protected virtual void EnsureDirectoryExists(string filePath)
        {
            ArgumentNullException.ThrowIfNull(filePath);

            string? directoryPath = Path.GetDirectoryName(filePath);

            if (string.IsNullOrEmpty(directoryPath))
            {
                this.Logger.LogDebug("[JsonConfigHandler] No directory component found in file path: {FilePath}", filePath);

                return;
            }

            if (!Directory.Exists(directoryPath))
            {
                try
                {
                    Directory.CreateDirectory(directoryPath);

                    this.Logger.LogDebug("[JsonConfigHandler] Created directory: {Directory}", directoryPath);
                }
                catch (Exception ex)
                {
                    this.Logger.LogError(ex, "[JsonConfigHandler] Failed to create directory: {Directory}", directoryPath);

                    return;
                }
            }
        }
        protected virtual void EnsureFileExists(string filePath, JsonSerializerOptions options, Type? configType = null)
        {
            ArgumentNullException.ThrowIfNull(filePath);
            ArgumentNullException.ThrowIfNull(options);

            this.EnsureDirectoryExists(filePath);

            if (!File.Exists(filePath))
            {
                try
                {
                    string fileContent = "{}";

                    if (configType != null)
                    {
                        try
                        {
                            fileContent = configType.SerializeToJson(options);
                        }
                        catch (Exception ex)
                        {
                            this.Logger.LogWarning(ex, "[JsonConfigHandler] Failed to serialize default configuration for type {ConfigType}", configType.Name);
                        }
                    }

                    Atomic.Write(filePath, fileContent);

                    this.Logger.LogDebug("[JsonConfigHandler] Created configuration file: {FilePath}", filePath);
                }
                catch (Exception ex)
                {
                    this.Logger.LogError(ex, "[JsonConfigHandler] Failed to create the required configuration file at '{FilePath}'", filePath);

                    return;
                }
            }
        }
        protected virtual string ResolveLocalFilePath(string? overridePath, string root, Type configType, string suffix)
        {
            ArgumentNullException.ThrowIfNull(root);
            ArgumentNullException.ThrowIfNull(configType);
            ArgumentNullException.ThrowIfNull(suffix);

            if (string.IsNullOrWhiteSpace(overridePath))
                return Path.Combine(root, $"{configType.Name}.{suffix}");

            bool isRootedPath = Path.IsPathRooted(overridePath);

            return isRootedPath ? overridePath : Path.Combine(root, overridePath);
        }
        protected virtual async Task SaveSettingsToFileAsync(string filePath, Dictionary<string, object?> settings, JsonSerializerOptions options, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(filePath);
            ArgumentNullException.ThrowIfNull(settings);
            ArgumentNullException.ThrowIfNull(options);

            cancellationToken.ThrowIfCancellationRequested();

            string serializedContent;

            try
            {
                serializedContent = await Task.Run(() => JsonSerializer.Serialize(settings, options), cancellationToken).ConfigureAwait(false);
            }
            catch (NotSupportedException ex)
            {
                this.Logger.LogError(ex, "[JsonConfigHandler] Failed to serialize the provided settings object for {FilePath}", filePath);

                return;
            }
            catch (Exception ex)
            {
                this.Logger.LogError(ex, "[JsonConfigHandler] Unexpected error during serialization for {FilePath}", filePath);

                return;
            }

            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                await Atomic.WriteAsync(filePath, serializedContent, cancellationToken: cancellationToken).ConfigureAwait(false);

                this.Logger.LogDebug("[JsonConfigHandler] Saved configuration to file: {FilePath}", filePath);
            }
            catch (OperationCanceledException)
            {
                this.Logger.LogInformation("[JsonConfigHandler] Save operation canceled for {FilePath}", filePath);

                return;
            }
            catch (Exception ex)
            {
                this.Logger.LogError(ex, "[JsonConfigHandler] Failed to save settings to file: {FilePath}", filePath);

                return;
            }
        }

        #endregion
    }
}