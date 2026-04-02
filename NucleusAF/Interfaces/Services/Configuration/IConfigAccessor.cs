using NucleusAF.Interfaces.Abstractions.CompositeRegistry;
using NucleusAF.Interfaces.Models;
using System.Diagnostics.CodeAnalysis;

namespace NucleusAF.Interfaces.Services.Configuration
{
    /// <summary>
    /// Defines the contract for a configuration accessor within the composite registry.
    /// A configuration accessor provides fluent configuration methods to bind services,
    /// handlers, capability tokens, and configuration types, as well as factory methods
    /// to create typed accessors for specific configuration objects.
    /// </summary>
    public interface IConfigAccessor : ICompositeRegistryAccessor
    {
        #region IConfigAccessor: Fluent Configuration API

        /// <summary>
        /// Sets the configuration service to be used by this accessor.
        /// This allows fluent configuration of the accessor with a specific service instance.
        /// </summary>
        /// <param name="service">The configuration service to associate with this accessor.</param>
        /// <returns>The current configuration accessor instance for fluent chaining.</returns>
        IConfigAccessor SetConfigService(IConfigService service);

        /// <summary>
        /// Sets the configuration handler to be used by this accessor.
        /// This allows fluent configuration of the accessor with a specific handler instance.
        /// </summary>
        /// <param name="handler">The configuration handler to associate with this accessor.</param>
        /// <returns>The current configuration accessor instance for fluent chaining.</returns>
        IConfigAccessor SetConfigHandler(IConfigHandler handler);

        /// <summary>
        /// Sets the capability token to be used by this accessor.
        /// This allows fluent configuration of the accessor with a specific access context.
        /// </summary>
        /// <param name="token">An optional capability token used to authorize configuration access.</param>
        /// <returns>The current configuration accessor instance for fluent chaining.</returns>
        IConfigAccessor SetAccess(ICapabilityToken? token = null);

        #endregion

        #region IConfigAccessor: Factory Methods

        /// <summary>
        /// Creates a typed configuration accessor for the specified configuration type.
        /// </summary>
        /// <typeparam name="TConfig">The configuration type for which the accessor is created.</typeparam>
        /// <returns>A typed configuration accessor for the given configuration type.</returns>
        IConfigAccessorFor<TConfig> For<TConfig>() where TConfig : class;

        /// <summary>
        /// Creates a typed configuration accessor for the specified configuration type,
        /// using the provided configuration service, handler, and optional capability token.
        /// </summary>
        /// <typeparam name="TConfig">The configuration type for which the accessor is created.</typeparam>
        /// <param name="configService">The configuration service to associate with the accessor.</param>
        /// <param name="configHandler">The configuration handler to associate with the accessor.</param>
        /// <param name="token">An optional capability token used to authorize configuration access.</param>
        /// <returns>A typed configuration accessor for the given configuration type.</returns>
        IConfigAccessorFor<TConfig> For<TConfig>(IConfigService configService, IConfigHandler configHandler, ICapabilityToken? token = null) where TConfig : class;

        #endregion
    }

    /// <summary>
    /// Defines the contract for a strongly typed configuration accessor associated with a specific configuration type.
    /// Provides property-level and instance-level operations for retrieving, setting, and persisting configuration values,
    /// along with "try" variants that avoid exceptions by returning success indicators.
    /// </summary>
    /// <typeparam name="TConfig">The configuration type that this accessor manages.</typeparam>
    public interface IConfigAccessorFor<TConfig> where TConfig : class
    {
        #region IConfigAccessorFor: Property Access

        /// <summary>
        /// Retrieves the value of a configuration property by key.
        /// Returns <c>null</c> if the property does not exist.
        /// </summary>
        /// <typeparam name="T">The type of the property value.</typeparam>
        /// <param name="key">The key identifying the configuration property.</param>
        /// <returns>The value of the configuration property, or <c>null</c> if not found.</returns>
        [return: MaybeNull]
        T Get<T>(string key);

        /// <summary>
        /// Sets the value of a configuration property by key.
        /// </summary>
        /// <typeparam name="T">The type of the property value.</typeparam>
        /// <param name="key">The key identifying the configuration property.</param>
        /// <param name="value">The value to assign to the property.</param>
        void Set<T>(string key, T value);

        /// <summary>
        /// Persists the current configuration state synchronously.
        /// </summary>
        void Save();

        /// <summary>
        /// Persists the current configuration state asynchronously.
        /// </summary>
        /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
        Task SaveAsync(CancellationToken cancellationToken = default);

        #endregion

        #region IConfigAccessorFor: Try Property Access

        /// <summary>
        /// Attempts to retrieve the value of a configuration property by key.
        /// </summary>
        /// <typeparam name="T">The type of the property value.</typeparam>
        /// <param name="key">The key identifying the configuration property.</param>
        /// <param name="value">When this method returns, contains the property value if found; otherwise <c>null</c>.</param>
        /// <returns><c>true</c> if the property was found; otherwise, <c>false</c>.</returns>
        bool TryGet<T>(string key, out T? value);

        /// <summary>
        /// Attempts to set the value of a configuration property by key.
        /// </summary>
        /// <typeparam name="T">The type of the property value.</typeparam>
        /// <param name="key">The key identifying the configuration property.</param>
        /// <param name="value">The value to assign to the property.</param>
        /// <returns><c>true</c> if the property was successfully set; otherwise, <c>false</c>.</returns>
        bool TrySet<T>(string key, T value);

        /// <summary>
        /// Attempts to persist the current configuration state synchronously.
        /// </summary>
        /// <returns><c>true</c> if the configuration was successfully saved; otherwise, <c>false</c>.</returns>
        bool TrySave();

        /// <summary>
        /// Attempts to persist the current configuration state asynchronously.
        /// </summary>
        /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
        /// <returns><c>true</c> if the configuration was successfully saved; otherwise, <c>false</c>.</returns>
        Task<bool> TrySaveAsync(CancellationToken cancellationToken = default);

        #endregion

        #region IConfigAccessorFor: Instance Operations

        /// <summary>
        /// Retrieves the entire configuration instance.
        /// </summary>
        /// <returns>The configuration object managed by this accessor.</returns>
        TConfig Get();

        /// <summary>
        /// Persists the specified configuration instance synchronously.
        /// </summary>
        /// <param name="config">The configuration object to save.</param>
        void Save(TConfig config);

        /// <summary>
        /// Persists the specified configuration instance asynchronously.
        /// </summary>
        /// <param name="config">The configuration object to save.</param>
        /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
        Task SaveAsync(TConfig config, CancellationToken cancellationToken = default);

        #endregion

        #region IConfigAccessorFor: Try Instance Operations

        /// <summary>
        /// Attempts to retrieve the entire configuration instance.
        /// </summary>
        /// <param name="config">When this method returns, contains the configuration object if found; otherwise <c>null</c>.</param>
        /// <returns><c>true</c> if the configuration was found; otherwise, <c>false</c>.</returns>
        bool TryGet(out TConfig? config);

        /// <summary>
        /// Attempts to persist the specified configuration instance synchronously.
        /// </summary>
        /// <param name="config">The configuration object to save.</param>
        /// <returns><c>true</c> if the configuration was successfully saved; otherwise, <c>false</c>.</returns>
        bool TrySave(TConfig config);

        /// <summary>
        /// Attempts to persist the specified configuration instance asynchronously.
        /// </summary>
        /// <param name="config">The configuration object to save.</param>
        /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
        /// <returns><c>true</c> if the configuration was successfully saved; otherwise, <c>false</c>.</returns>
        Task<bool> TrySaveAsync(TConfig config, CancellationToken cancellationToken = default);

        #endregion
    }
}