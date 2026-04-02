using NucleusAF.Interfaces.Models;

namespace NucleusAF.Interfaces.Services.Configuration.Accessors
{
    /// <summary>
    /// Defines a memory-based configuration accessor that extends the base <see cref="IConfigAccessor"/>.
    /// Provides fluent configuration methods for associating a configuration service, handler,
    /// capability token, and configuration types, returning the memory accessor type for chaining.
    /// </summary>
    public interface IMemoryConfigAccessor : IConfigAccessor
    {
        #region IMemoryConfigAccessor: Fluent Configuration API

        /// <summary>
        /// Sets the configuration service to be used by this memory accessor.
        /// Returns the memory accessor instance for fluent chaining.
        /// </summary>
        /// <param name="service">The configuration service to associate with this accessor.</param>
        /// <returns>The current memory configuration accessor instance.</returns>
        new IMemoryConfigAccessor SetConfigService(IConfigService service);

        /// <summary>
        /// Sets the configuration handler to be used by this memory accessor.
        /// Returns the memory accessor instance for fluent chaining.
        /// </summary>
        /// <param name="handler">The configuration handler to associate with this accessor.</param>
        /// <returns>The current memory configuration accessor instance.</returns>
        new IMemoryConfigAccessor SetConfigHandler(IConfigHandler handler);

        /// <summary>
        /// Sets the capability token to be used by this memory accessor.
        /// Returns the memory accessor instance for fluent chaining.
        /// </summary>
        /// <param name="token">An optional capability token used to authorize configuration access.</param>
        /// <returns>The current memory configuration accessor instance.</returns>
        new IMemoryConfigAccessor SetAccess(ICapabilityToken? token = null);

        #endregion

        #region IMemoryConfigAccessor: Factory Methods

        /// <summary>
        /// Creates a typed memory configuration accessor for the specified configuration type.
        /// </summary>
        /// <typeparam name="TConfig">The configuration type for which the accessor is created.</typeparam>
        /// <returns>A typed memory configuration accessor for the given configuration type.</returns>
        new IMemoryConfigAccessorFor<TConfig> For<TConfig>() where TConfig : class;

        /// <summary>
        /// Creates a typed memory configuration accessor for the specified configuration type,
        /// using the provided configuration service, handler, and optional capability token.
        /// </summary>
        /// <typeparam name="TConfig">The configuration type for which the accessor is created.</typeparam>
        /// <param name="configService">The configuration service to associate with the accessor.</param>
        /// <param name="configHandler">The configuration handler to associate with the accessor.</param>
        /// <param name="token">An optional capability token used to authorize configuration access.</param>
        /// <returns>A typed memory configuration accessor for the given configuration type.</returns>
        new IMemoryConfigAccessorFor<TConfig> For<TConfig>(IConfigService configService, IConfigHandler configHandler, ICapabilityToken? token = null) where TConfig : class;

        #endregion
    }

    /// <summary>
    /// Defines a strongly typed memory-based configuration accessor associated with a specific configuration type.
    /// Extends <see cref="IConfigAccessorFor{TConfig}"/> to provide a lightweight, in-memory accessor contract.
    /// </summary>
    /// <typeparam name="TConfig">The configuration type that this memory accessor is associated with.</typeparam>
    public interface IMemoryConfigAccessorFor<TConfig> : IConfigAccessorFor<TConfig> where TConfig : class;
}