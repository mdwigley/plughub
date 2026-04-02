using NucleusAF.Interfaces.Models;
using NucleusAF.Interfaces.Models.Configuration;

namespace NucleusAF.Interfaces.Services.Configuration
{
    /// <summary>
    /// Enumerates the types of configuration operations that can trigger a fire-and-forget save error.
    /// </summary>
    public enum ConfigSaveOperation
    {
        /// <summary>
        /// The operation is unknown or unspecified.
        /// </summary>
        Unknown,

        /// <summary>
        /// Represents a save operation for configuration settings.
        /// </summary>
        SaveSettings,

        /// <summary>
        /// Represents a save operation for an entire configuration instance.
        /// </summary>
        SaveConfigInstance,

        /// <summary>
        /// Represents a save operation for default configuration file contents.
        /// </summary>
        SaveDefaultConfigFileContents
    }

    /// <summary>
    /// Provides data for the <c>ConfigService.SyncSaveOpErrors</c> event,
    /// containing details about a fire-and-forget save error in the configuration service.
    /// </summary>
    /// <param name="ex">The exception that was thrown during the operation.</param>
    /// <param name="operation">The type of configuration operation that failed.</param>
    /// <param name="configType">The configuration type involved in the operation, if applicable.</param>
    public class ConfigServiceSaveErrorEventArgs(Exception ex, ConfigSaveOperation operation = ConfigSaveOperation.Unknown, Type? configType = null) : EventArgs
    {
        /// <summary>
        /// Gets the exception that was thrown during the configuration save operation.
        /// </summary>
        public Exception Exception = ex;

        /// <summary>
        /// Gets the type of configuration operation that failed.
        /// </summary>
        public ConfigSaveOperation Operation = operation;

        /// <summary>
        /// Gets the configuration type involved in the operation, if applicable.
        /// </summary>
        public Type? ConfigType = configType;
    }

    /// <summary>
    /// Event args for the SaveCompleted event.
    /// Carries info about what was saved, where, and how.
    /// </summary>
    /// <remarks>
    /// Create a new event args for a completed save.
    /// </remarks>
    public class ConfigServiceSaveCompletedEventArgs(Type configType) : EventArgs
    {
        /// <summary>
        /// The type of config that was saved (e.g., AppConfig).
        /// </summary>
        public Type ConfigType { get; } = configType;
    }

    /// <summary>
    /// Event arguments for when a configuration file is reloaded in the <see cref="ConfigService"/>.
    /// Provides the type of the configuration that was reloaded.
    /// </summary>
    /// <param name="configType">The configuration type that was reloaded.</param>
    public class ConfigServiceConfigReloadedEventArgs(Type configType) : EventArgs
    {
        /// <summary>
        /// Gets the configuration type that was reloaded.
        /// </summary>
        public Type ConfigType { get; } = configType;
    }

    /// <summary>
    /// Event arguments for when a default or user setting changes in the <see cref="ConfigService"/>.
    /// Provides the configuration type, key, and both the old and new values of the setting.
    /// </summary>
    /// <param name="configType">The configuration type where the setting changed.</param>
    /// <param name="key">The key of the setting that changed.</param>
    /// <param name="oldValue">The previous value of the setting.</param>
    /// <param name="newValue">The new value of the setting.</param>
    public class ConfigServiceSettingChangeEventArgs(Type configType, string key, object? oldValue, object? newValue) : EventArgs
    {
        /// <summary>
        /// Gets the configuration type where the setting changed.
        /// </summary>
        public Type ConfigType { get; } = configType;

        /// <summary>
        /// Gets the key of the setting that changed.
        /// </summary>
        public string Key { get; } = key;

        /// <summary>
        /// Gets the previous value of the setting.
        /// </summary>
        public object? OldValue { get; } = oldValue;

        /// <summary>
        /// Gets the new value of the setting.
        /// </summary>
        public object? NewValue { get; } = newValue;
    }

    /// <summary>
    /// Defines the contract for the configuration service, which orchestrates
    /// configuration handlers and accessors. Provides APIs for registering,
    /// unregistering, and retrieving configuration objects, as well as events
    /// for save operations, reloads, and setting changes.
    /// </summary>
    public interface IConfigService
    {
        #region IConfigService: Events

        /// <summary>
        /// Occurs when a synchronous save operation completes successfully.
        /// </summary>
        event EventHandler<ConfigServiceSaveCompletedEventArgs>? SyncSaveCompleted;

        /// <summary>
        /// Occurs when a synchronous save operation encounters an error.
        /// </summary>
        event EventHandler<ConfigServiceSaveErrorEventArgs>? SyncSaveErrors;

        /// <summary>
        /// Occurs when a configuration object is reloaded.
        /// </summary>
        event EventHandler<ConfigServiceConfigReloadedEventArgs>? ConfigReloaded;

        /// <summary>
        /// Occurs when a configuration setting changes.
        /// </summary>
        event EventHandler<ConfigServiceSettingChangeEventArgs>? SettingChanged;

        #endregion

        /// <summary>
        /// Gets the directory path where configuration data is stored.
        /// </summary>
        string ConfigDataDirectory { get; }

        #region IConfigService: Predicates

        /// <summary>
        /// Determines whether a handler is registered for the specified configuration type.
        /// </summary>
        /// <param name="configType">The configuration type to check.</param>
        /// <returns><c>true</c> if a handler is registered; otherwise, <c>false</c>.</returns>
        bool IsRegistered(Type configType);

        #endregion

        #region IConfigService: Accessors

        /// <summary>
        /// Retrieves a typed configuration accessor for the specified configuration type.
        /// </summary>
        /// <typeparam name="TConfig">The configuration type to retrieve.</typeparam>
        /// <param name="token">An optional capability token used to authorize access.</param>
        /// <returns>A typed configuration accessor for the given configuration type.</returns>
        IConfigAccessorFor<TConfig> GetConfigAccessor<TConfig>(ICapabilityToken? token = null) where TConfig : class;

        /// <summary>
        /// Retrieves a specific typed configuration accessor for the given configuration type.
        /// </summary>
        /// <typeparam name="TAccessor">The specific accessor type to retrieve.</typeparam>
        /// <typeparam name="TConfig">The configuration type managed by the accessor.</typeparam>
        /// <param name="token">An optional capability token used to authorize access.</param>
        /// <returns>The specific typed configuration accessor instance.</returns>
        TAccessor GetConfigAccessor<TAccessor, TConfig>(ICapabilityToken? token = null) where TAccessor : IConfigAccessorFor<TConfig> where TConfig : class;

        /// <summary>
        /// Retrieves a configuration accessor for the specified accessor type and configuration types.
        /// </summary>
        /// <param name="accessorType">The accessor type to retrieve.</param>
        /// <param name="token">An optional capability token used to authorize access.</param>
        /// <returns>The configuration accessor instance.</returns>
        IConfigAccessor GetConfigAccessor(Type accessorType, ICapabilityToken? token = null);

        #endregion

        #region IConfigService: Registration

        /// <summary>
        /// Registers a configuration type with the specified parameters and optional capability token.
        /// </summary>
        /// <param name="configType">The configuration type to register.</param>
        /// <param name="configParams">The parameters used to configure persistence.</param>
        /// <param name="token">An optional capability token used to authorize registration.</param>
        /// <returns>A capability token representing the registration context.</returns>
        ICapabilityToken Register(Type configType, IConfigParams configParams, ICapabilityToken? token = null);

        /// <summary>
        /// Registers a typed configuration with the specified parameters and optional capability token.
        /// </summary>
        /// <typeparam name="TConfig">The configuration type to register.</typeparam>
        /// <param name="configParams">The parameters used to configure persistence.</param>
        /// <param name="token">An optional capability token used to authorize registration.</param>
        /// <param name="accessor">When this method returns, contains the typed accessor if registration succeeded.</param>
        void Register<TConfig>(IConfigParams configParams, ICapabilityToken? token, out IConfigAccessorFor<TConfig>? accessor) where TConfig : class;

        /// <summary>
        /// Registers multiple configuration types with the specified parameters and optional capability token.
        /// </summary>
        /// <param name="configTypes">The configuration types to register.</param>
        /// <param name="configParams">The parameters used to configure persistence.</param>
        /// <param name="token">An optional capability token used to authorize registration.</param>
        /// <returns>A dictionary mapping configuration types to their capability tokens.</returns>
        IDictionary<Type, ICapabilityToken> Register(IEnumerable<Type> configTypes, IConfigParams configParams, ICapabilityToken? token = null);

        /// <summary>
        /// Registers multiple configuration types with the specified parameters and optional capability token.
        /// </summary>
        /// <param name="configTypes">The configuration types to register.</param>
        /// <param name="configParams">The parameters used to configure persistence.</param>
        /// <param name="accessor">When this method returns, contains the accessor if registration succeeded.</param>
        /// <param name="token">An optional capability token used to authorize registration.</param>
        void Register(IEnumerable<Type> configTypes, IConfigParams configParams, out IConfigAccessor? accessor, ICapabilityToken? token);


        /// <summary>
        /// Attempts to register a configuration type with the specified parameters and optional capability token.
        /// </summary>
        /// <param name="configType">The configuration type to register.</param>
        /// <param name="configParams">The parameters used to configure persistence.</param>
        /// <param name="token">An optional capability token used to authorize registration.</param>
        /// <param name="registered">When this method returns, contains the capability token if registration succeeded.</param>
        /// <returns><c>true</c> if registration succeeded; otherwise, <c>false</c>.</returns>
        bool TryRegister(Type configType, IConfigParams configParams, ICapabilityToken? token, out ICapabilityToken? registered);

        /// <summary>
        /// Attempts to register a typed configuration with the specified parameters and optional capability token.
        /// </summary>
        /// <typeparam name="TConfig">The configuration type to register.</typeparam>
        /// <param name="configParams">The parameters used to configure persistence.</param>
        /// <param name="token">An optional capability token used to authorize registration.</param>
        /// <param name="accessor">When this method returns, contains the typed accessor if registration succeeded.</param>
        /// <returns><c>true</c> if registration succeeded; otherwise, <c>false</c>.</returns>
        bool TryRegister<TConfig>(IConfigParams configParams, ICapabilityToken? token, out IConfigAccessorFor<TConfig>? accessor) where TConfig : class;

        /// <summary>
        /// Attempts to bulk register the specified configuration types using the provided parameters.
        /// </summary>
        /// <param name="configTypes">The configuration types to register.</param>
        /// <param name="configParams">The configuration parameters used during registration.</param>
        /// <param name="results">Outputs a dictionary mapping each configuration type to its capability token if registration succeeds.</param>
        /// <param name="token">An optional capability token to associate with the registrations. If not provided, a new token may be created.</param>
        /// <returns>
        /// True if all configuration types were successfully registered; false if any registration failed.
        /// </returns>
        bool TryRegister(IEnumerable<Type> configTypes, IConfigParams configParams, out IDictionary<Type, ICapabilityToken> results, ICapabilityToken? token = null);

        /// <summary>
        /// Attempts to bulk register the specified configuration types and returns an accessor for the registered configuration set.
        /// </summary>
        /// <param name="configTypes">The configuration types to register.</param>
        /// <param name="configParams">The configuration parameters used during registration.</param>
        /// <param name="accessor">Outputs the configuration accessor if all registrations succeed; otherwise null.</param>
        /// <param name="token">An optional capability token to associate with the registrations. If not provided, a new token may be created.</param>
        /// <returns>
        /// True if all configuration types were successfully registered and an accessor was created; false if any registration failed.
        /// </returns>
        bool TryRegister(IEnumerable<Type> configTypes, IConfigParams configParams, out IConfigAccessor? accessor, ICapabilityToken? token = null);


        /// <summary>
        /// Unregisters a configuration type using the specified capability token.
        /// </summary>
        /// <param name="configType">The configuration type to unregister.</param>
        /// <param name="token">The capability token used to authorize unregistration.</param>
        void Unregister(Type configType, ICapabilityToken token);

        /// <summary>
        /// Unregisters multiple configuration types using the specified capability token.
        /// </summary>
        /// <param name="configTypes">The configuration types to unregister.</param>
        /// <param name="token">The capability token used to authorize unregistration.</param>
        void Unregister(IEnumerable<Type> configTypes, ICapabilityToken token);


        /// <summary>
        /// Attempts to unregister a configuration type using the specified capability token.
        /// </summary>
        /// <param name="configType">The configuration type to unregister.</param>
        /// <param name="token">The capability token used to authorize unregistration.</param>
        /// <returns><c>true</c> if unregistration succeeded; otherwise, <c>false</c>.</returns>
        bool TryUnregister(Type configType, ICapabilityToken token);

        /// <summary>
        /// Attempts to bulk unregister the specified configuration types using the provided capability token.
        /// </summary>
        /// <param name="configTypes">The configuration types to unregister.</param>
        /// <param name="token">The capability token associated with the registrations to be removed.</param>
        /// <returns>
        /// True if all configuration types were successfully unregistered; false if any unregistration failed.
        /// </returns>
        bool TryUnregister(IEnumerable<Type> configTypes, ICapabilityToken token);

        #endregion

        #region IConfigService: Event Handlers

        /// <summary>
        /// Raises the <see cref="SyncSaveCompleted"/> event for the specified configuration type.
        /// </summary>
        /// <param name="sender">The sender of the event.</param>
        /// <param name="configType">The configuration type that was saved.</param>
        void OnSaveOperationComplete(object sender, Type configType);

        /// <summary>
        /// Raises the <see cref="SyncSaveErrors"/> event for the specified configuration type.
        /// </summary>
        /// <param name="sender">The sender of the event.</param>
        /// <param name="ex">The exception that occurred during the save operation.</param>
        /// <param name="operation">The save operation that failed.</param>
        /// <param name="configType">The configuration type that was being saved.</param>
        void OnSaveOperationError(object sender, Exception ex, ConfigSaveOperation operation, Type configType);

        /// <summary>
        /// Raises the <see cref="ConfigReloaded"/> event for the specified configuration type.
        /// </summary>
        /// <param name="sender">The sender of the event.</param>
        /// <param name="configType">The configuration type that was reloaded.</param>
        void OnConfigReloaded(object sender, Type configType);

        /// <summary>
        /// Raises the <see cref="SettingChanged"/> event for the specified configuration type and setting key.
        /// </summary>
        /// <param name="sender">The sender of the event.</param>
        /// <param name="configType">The configuration type whose setting changed.</param>
        /// <param name="key">The key of the setting that changed.</param>
        /// <param name="oldValue">The previous value of the setting.</param>
        /// <param name="newValue">The new value of the setting.</param>
        void OnSettingChanged(object sender, Type configType, string key, object? oldValue, object? newValue);

        #endregion
    }
}