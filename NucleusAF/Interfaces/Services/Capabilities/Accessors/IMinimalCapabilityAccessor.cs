namespace NucleusAF.Interfaces.Services.Capabilities.Accessors
{
    /// <summary>
    /// Defines a minimal capability accessor that extends the base <see cref="ICapabilityAccessor"/>.
    /// Provides fluent configuration methods for associating a capability service and handler,
    /// returning the minimal accessor type for chaining.
    /// </summary>
    public interface IMinimalCapabilityAccessor : ICapabilityAccessor
    {
        #region IMinimalCapabilityAccessor: Fluent Configuration API

        /// <summary>
        /// Sets the capability service to be used by this minimal accessor.
        /// Returns the minimal accessor instance for fluent chaining.
        /// </summary>
        /// <param name="service">The capability service to associate with this accessor.</param>
        /// <returns>The current minimal capability accessor instance.</returns>
        new IMinimalCapabilityAccessor SetCapabilityService(ICapabilityService service);

        /// <summary>
        /// Sets the capability handler to be used by this minimal accessor.
        /// Returns the minimal accessor instance for fluent chaining.
        /// </summary>
        /// <param name="handler">The capability handler to associate with this accessor.</param>
        /// <returns>The current minimal capability accessor instance.</returns>
        new IMinimalCapabilityAccessor SetCapabilityHandler(ICapabilityHandler handler);

        #endregion
    }

    /// <summary>
    /// Defines a minimal typed capability accessor associated with a specific identifier type.
    /// Extends <see cref="ICapabilityAccessorFor{THandler}"/> to provide a lightweight accessor contract.
    /// </summary>
    /// <typeparam name="THandler">The identifier type that this minimal accessor is associated with.</typeparam>
    public interface IMinimalCapabilityAccessor<THandler> : ICapabilityAccessorFor<THandler> where THandler : class;
}