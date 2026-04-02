using NucleusAF.Interfaces.Abstractions.CompositeRegistry;
using NucleusAF.Interfaces.Models;
using NucleusAF.Interfaces.Models.Configuration;
using System.Diagnostics.CodeAnalysis;

namespace NucleusAF.Interfaces.Services.Configuration
{
    /// <summary>
    /// Defines the contract for a configuration registrar that manages the registration
    /// and unregistration of configuration types within the configuration service.
    /// Provides APIs for verifying registration state and associating capability tokens
    /// with configuration types.
    /// </summary>
    public interface IConfigRegistrar : ICompositeRegistryHandler
    {
        #region IConfigRegistrar: Predicate Operations

        /// <summary>
        /// Determines whether the specified configuration type is currently registered.
        /// </summary>
        /// <param name="configType">The configuration type to check.</param>
        /// <returns><c>true</c> if the configuration type is registered; otherwise, <c>false</c>.</returns>
        bool IsRegistered(Type configType);

        #endregion

        #region IConfigRegistrar: Registration

        /// <summary>
        /// Registers a configuration type with the specified parameters and optional capability token,
        /// using the provided configuration service.
        /// </summary>
        /// <param name="configType">The configuration type to register.</param>
        /// <param name="configParams">The parameters used to configure persistence for the type.</param>
        /// <param name="configService">The configuration service that manages the registration.</param>
        /// <param name="token">An optional capability token used to authorize registration.</param>
        /// <returns>A capability token representing the registration context.</returns>
        ICapabilityToken Register(Type configType, IConfigParams configParams, IConfigService configService, ICapabilityToken? token = null);

        /// <summary>
        /// Unregisters a configuration type using the specified capability token.
        /// </summary>
        /// <param name="configType">The configuration type to unregister.</param>
        /// <param name="token">The capability token used to authorize unregistration.</param>
        void Unregister(Type configType, ICapabilityToken token);

        #endregion
    }

    /// <summary>
    /// Defines the contract for a configuration handler that manages persistence
    /// and mutation of configuration values and instances. Provides APIs for
    /// reading, writing, and saving configuration data, scoped by capability tokens.
    /// </summary>
    public interface IConfigHandler : ICompositeRegistryHandler
    {
        #region IConfigHandler: Value Accessors and Mutators

        /// <summary>
        /// Retrieves the value of a specific configuration setting for the given configuration type.
        /// </summary>
        /// <typeparam name="T">The expected type of the configuration value.</typeparam>
        /// <param name="configType">The configuration type containing the setting.</param>
        /// <param name="key">The key identifying the configuration setting.</param>
        /// <param name="token">An optional capability token used to authorize access.</param>
        /// <returns>The value of the configuration setting, or <c>null</c> if not found.</returns>
        [return: MaybeNull]
        T GetValue<T>(Type configType, string key, ICapabilityToken? token = null);

        /// <summary>
        /// Sets the value of a specific configuration setting for the given configuration type.
        /// </summary>
        /// <typeparam name="T">The type of the configuration value being set.</typeparam>
        /// <param name="configType">The configuration type containing the setting.</param>
        /// <param name="key">The key identifying the configuration setting.</param>
        /// <param name="value">The new value to assign to the setting.</param>
        /// <param name="token">An optional capability token used to authorize mutation.</param>
        void SetValue<T>(Type configType, string key, T value, ICapabilityToken? token = null);

        /// <summary>
        /// Saves all configuration values for the specified configuration type synchronously.
        /// </summary>
        /// <param name="configType">The configuration type whose values should be saved.</param>
        /// <param name="token">An optional capability token used to authorize the save operation.</param>
        void SaveValues(Type configType, ICapabilityToken? token = null);

        /// <summary>
        /// Saves all configuration values for the specified configuration type asynchronously.
        /// </summary>
        /// <param name="configType">The configuration type whose values should be saved.</param>
        /// <param name="token">An optional capability token used to authorize the save operation.</param>
        /// <param name="cancellationToken">A cancellation token to cancel the asynchronous operation.</param>
        /// <returns>A task representing the asynchronous save operation.</returns>
        Task SaveValuesAsync(Type configType, ICapabilityToken? token = null, CancellationToken cancellationToken = default);

        #endregion

        #region IConfigHandler: Instance Accessors and Mutators

        /// <summary>
        /// Retrieves the configuration instance for the specified configuration type.
        /// </summary>
        /// <param name="configType">The configuration type to retrieve.</param>
        /// <param name="token">An optional capability token used to authorize access.</param>
        /// <returns>The configuration instance associated with the given type.</returns>
        object GetConfigInstance(Type configType, ICapabilityToken? token = null);

        /// <summary>
        /// Saves the updated configuration instance for the specified configuration type asynchronously.
        /// </summary>
        /// <param name="configType">The configuration type to save.</param>
        /// <param name="updatedConfig">The updated configuration instance to persist.</param>
        /// <param name="token">An optional capability token used to authorize the save operation.</param>
        /// <param name="cancellationToken">A cancellation token to cancel the asynchronous operation.</param>
        /// <returns>A task representing the asynchronous save operation.</returns>
        Task SaveConfigInstanceAsync(Type configType, object updatedConfig, ICapabilityToken? token = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Saves the updated configuration instance for the specified configuration type synchronously.
        /// </summary>
        /// <param name="configType">The configuration type to save.</param>
        /// <param name="updatedConfig">The updated configuration instance to persist.</param>
        /// <param name="token">An optional capability token used to authorize the save operation.</param>
        void SaveConfigInstance(Type configType, object updatedConfig, ICapabilityToken? token = null);

        #endregion
    }

    /// <summary>
    /// Defines a typed configuration handler associated with a specific handled type.
    /// Extends <see cref="IConfigHandler"/> to provide type-safe specialization.
    /// </summary>
    /// <typeparam name="THandled">The configuration type that this handler manages.</typeparam>
    public interface IConfigHandler<THandled> : IConfigHandler { }
}