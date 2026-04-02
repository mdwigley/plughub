using NucleusAF.Interfaces.Models;
using NucleusAF.Interfaces.Services.Encryption;

namespace NucleusAF.Interfaces.Services.Configuration.Accessors
{
    /// <summary>
    /// Defines a secure JSON-based configuration accessor that extends <see cref="IJsonConfigAccessor"/>.
    /// Provides fluent configuration methods for associating encryption context and services,
    /// as well as configuration service, handler, capability token, and configuration types.
    /// Returns the secure JSON accessor type for chaining.
    /// </summary>
    public interface ISecureJsonConfigAccessor : IJsonConfigAccessor
    {
        #region ISecureJsonConfigAccessor: Fluent Configuration API

        /// <summary>
        /// Sets the encryption context to be used by this secure JSON accessor.
        /// Returns the secure JSON accessor instance for fluent chaining.
        /// </summary>
        /// <param name="encryptionContext">The encryption context to associate with this accessor.</param>
        /// <returns>The current secure JSON configuration accessor instance.</returns>
        ISecureJsonConfigAccessor SetEncryptionContext(IEncryptionContext encryptionContext);

        /// <summary>
        /// Sets the encryption service to be used by this secure JSON accessor.
        /// Returns the secure JSON accessor instance for fluent chaining.
        /// </summary>
        /// <param name="encryptionService">The encryption service to associate with this accessor.</param>
        /// <returns>The current secure JSON configuration accessor instance.</returns>
        ISecureJsonConfigAccessor SetEncryptionService(IEncryptionService encryptionService);

        /// <summary>
        /// Sets the configuration service to be used by this secure JSON accessor.
        /// Returns the secure JSON accessor instance for fluent chaining.
        /// </summary>
        /// <param name="service">The configuration service to associate with this accessor.</param>
        /// <returns>The current secure JSON configuration accessor instance.</returns>
        new ISecureJsonConfigAccessor SetConfigService(IConfigService service);

        /// <summary>
        /// Sets the configuration handler to be used by this secure JSON accessor.
        /// Returns the secure JSON accessor instance for fluent chaining.
        /// </summary>
        /// <param name="handler">The configuration handler to associate with this accessor.</param>
        /// <returns>The current secure JSON configuration accessor instance.</returns>
        new ISecureJsonConfigAccessor SetConfigHandler(IConfigHandler handler);

        /// <summary>
        /// Sets the capability token to be used by this secure JSON accessor.
        /// Returns the secure JSON accessor instance for fluent chaining.
        /// </summary>
        /// <param name="token">An optional capability token used to authorize configuration access.</param>
        /// <returns>The current secure JSON configuration accessor instance.</returns>
        new ISecureJsonConfigAccessor SetAccess(ICapabilityToken? token = null);

        #endregion

        #region ISecureJsonConfigAccessor: Factory Methods

        /// <summary>
        /// Creates a typed secure JSON configuration accessor for the specified configuration type.
        /// </summary>
        /// <typeparam name="TConfig">The configuration type for which the accessor is created.</typeparam>
        /// <returns>A typed secure JSON configuration accessor for the given configuration type.</returns>
        new ISecureJsonConfigAccessorFor<TConfig> For<TConfig>() where TConfig : class;

        /// <summary>
        /// Creates a typed secure JSON configuration accessor for the specified configuration type,
        /// using the provided configuration service, handler, encryption context, and optional capability token.
        /// </summary>
        /// <typeparam name="TConfig">The configuration type for which the accessor is created.</typeparam>
        /// <param name="configService">The configuration service to associate with the accessor.</param>
        /// <param name="configHandler">The configuration handler to associate with the accessor.</param>
        /// <param name="encryptionContext">The encryption context to associate with the accessor.</param>
        /// <param name="token">An optional capability token used to authorize configuration access.</param>
        /// <returns>A typed secure JSON configuration accessor for the given configuration type.</returns>
        ISecureJsonConfigAccessorFor<TConfig> For<TConfig>(IConfigService configService, IConfigHandler configHandler, IEncryptionContext encryptionContext, ICapabilityToken? token = null) where TConfig : class;

        #endregion
    }

    /// <summary>
    /// Defines a strongly typed secure JSON-based configuration accessor associated with a specific configuration type.
    /// Extends <see cref="IJsonConfigAccessorFor{TConfig}"/> to provide a typed contract for encrypted JSON-backed configuration.
    /// </summary>
    /// <typeparam name="TConfig">The configuration type that this secure JSON accessor is associated with.</typeparam>
    public interface ISecureJsonConfigAccessorFor<TConfig> : IJsonConfigAccessorFor<TConfig> where TConfig : class { }
}