namespace NucleusAF.Interfaces.Abstractions.CompositeRegistry
{
    /// <summary>
    /// Represents a component that can participate in a composite registry.
    /// Each component is uniquely identified by a <see cref="Type"/> key.
    /// </summary>
    public interface ICompositeRegistryComponent
    {
        /// <summary>
        /// Gets the unique key (typically a <see cref="Type"/>) that identifies this component.
        /// </summary>
        Type Key { get; }
    }

    /// <summary>
    /// Represents a handler that can be stored in a composite registry.
    /// </summary>
    public interface ICompositeRegistryHandler : ICompositeRegistryComponent { }

    /// <summary>
    /// Represents a handler associated with a specific identifier type.
    /// </summary>
    /// <typeparam name="TId">The identifier type that this handler is associated with.</typeparam>
    public interface ICompositeRegistryHandler<TId> : ICompositeRegistryHandler { }

    /// <summary>
    /// Defines the contract for a composite registry that manages handlers keyed by <see cref="Type"/>.
    /// Provides APIs for querying, retrieving, and attempting to resolve handlers.
    /// </summary>
    /// <typeparam name="THandler">The type of handler managed by the registry.</typeparam>
    public interface ICompositeComponentRegistry<Type, THandler>
    {
        #region ICompositeComponentRegistry: Predicates

        /// <summary>
        /// Determines whether the specified handler is registered for the given identifier type.
        /// </summary>
        /// <param name="id">The identifier type to check.</param>
        /// <param name="handler">The handler instance to verify.</param>
        /// <returns><c>true</c> if the handler is registered for the given type; otherwise, <c>false</c>.</returns>
        bool IsRegistered(Type id, THandler handler);

        #endregion

        #region ICompositeComponentRegistry: Handler API

        /// <summary>
        /// Retrieves the single handler registered for the specified identifier type.
        /// </summary>
        /// <param name="id">The identifier type whose handler should be retrieved.</param>
        /// <returns>The handler instance associated with the given type.</returns>
        THandler GetComponent(Type id);

        /// <summary>
        /// Retrieves all handlers registered for the specified identifier type.
        /// </summary>
        /// <param name="id">The identifier type whose handlers should be retrieved.</param>
        /// <returns>A read-only list of handlers associated with the given type.</returns>
        IReadOnlyList<THandler> GetComponents(Type id);

        #endregion

        #region ICompositeComponentRegistry: Try API

        /// <summary>
        /// Attempts to retrieve a single handler registered for the specified identifier type.
        /// </summary>
        /// <param name="id">The identifier type whose handler should be retrieved.</param>
        /// <param name="handler">When this method returns, contains the handler instance if found; otherwise <c>null</c>.</param>
        /// <returns><c>true</c> if a handler was found; otherwise, <c>false</c>.</returns>
        bool TryGetComponent(Type id, out THandler? handler);

        /// <summary>
        /// Attempts to retrieve all handlers registered for the specified identifier type.
        /// </summary>
        /// <param name="id">The identifier type whose handlers should be retrieved.</param>
        /// <param name="handlers">When this method returns, contains the list of handlers if found; otherwise <c>null</c>.</param>
        /// <returns><c>true</c> if one or more handlers were found; otherwise, <c>false</c>.</returns>
        bool TryGetComponents(Type id, out IReadOnlyList<THandler>? handlers);

        #endregion
    }
}