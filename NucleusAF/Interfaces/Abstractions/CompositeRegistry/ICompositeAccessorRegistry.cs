namespace NucleusAF.Interfaces.Abstractions.CompositeRegistry
{
    /// <summary>
    /// Represents a registry accessor component that participates in a composite registry.
    /// </summary>
    public interface ICompositeRegistryAccessor : ICompositeRegistryComponent { }

    /// <summary>
    /// Represents a registry accessor associated with a specific identifier type.
    /// </summary>
    /// <typeparam name="TIdent">The identifier type associated with this accessor.</typeparam>
    public interface ICompositeRegistryAccessor<TIdent> : ICompositeRegistryAccessor { }

    /// <summary>
    /// Represents a handler that is associated with a specific registry accessor type.
    /// </summary>
    /// <typeparam name="TIdent">The accessor type that this handler supports.</typeparam>
    public interface ICompositeRegistryHandlerFor<TIdent> : ICompositeRegistryHandler
        where TIdent : ICompositeRegistryAccessor
    { }

    /// <summary>
    /// Represents an accessor that is associated with a specific handler type.
    /// </summary>
    /// <typeparam name="TIdent">The handler type that this accessor supports.</typeparam>
    public interface ICompositeRegistryAccessorFor<TIdent>
        where TIdent : class
    { }

    /// <summary>
    /// Defines the contract for a composite accessor registry that manages accessors and handlers keyed by <see cref="Type"/>.
    /// Provides APIs for querying, retrieving, and attempting to resolve both accessors and handlers.
    /// </summary>
    /// <typeparam name="TAccessor">The type of accessor managed by the registry.</typeparam>
    /// <typeparam name="THandler">The type of handler managed by the registry.</typeparam>
    public interface ICompositeAccessorRegistry<TAccessor, THandler>
        where TAccessor : ICompositeRegistryAccessor
        where THandler : ICompositeRegistryHandler
    {
        #region ICompositeAccessorRegistry: Predicates

        /// <summary>
        /// Determines whether the specified accessor is registered for the given identifier type.
        /// </summary>
        /// <param name="id">The identifier type to check.</param>
        /// <param name="accessor">The accessor instance to verify.</param>
        /// <returns><c>true</c> if the accessor is registered for the given type; otherwise, <c>false</c>.</returns>
        bool DoesRegistryAccessorExist(Type id, TAccessor accessor);

        /// <summary>
        /// Determines whether the specified handler is registered for the given identifier type.
        /// </summary>
        /// <param name="id">The identifier type to check.</param>
        /// <param name="handler">The handler instance to verify.</param>
        /// <returns><c>true</c> if the handler is registered for the given type; otherwise, <c>false</c>.</returns>
        bool DoesRegistryHandlerExist(Type id, THandler handler);

        /// <summary>
        /// Determines whether the specified accessor is associated with the handler identified by the given type.
        /// </summary>
        /// <param name="handlerId">The identifier type of the handler.</param>
        /// <param name="accessor">The accessor instance to verify.</param>
        /// <returns><c>true</c> if the accessor is associated with the handler; otherwise, <c>false</c>.</returns>
        bool IsRegistryAccessorForHandler(Type handlerId, TAccessor accessor);

        /// <summary>
        /// Determines whether any accessors are available for the handler identified by the given type.
        /// </summary>
        /// <param name="handlerId">The identifier type of the handler.</param>
        /// <returns><c>true</c> if one or more accessors are available; otherwise, <c>false</c>.</returns>
        bool IsRegistryAccessorAvailableFor(Type handlerId);

        /// <summary>
        /// Determines whether the specified accessor is associated with the given handler instance.
        /// </summary>
        /// <param name="handler">The handler instance to check.</param>
        /// <param name="accessor">The accessor instance to verify.</param>
        /// <returns><c>true</c> if the accessor is associated with the handler; otherwise, <c>false</c>.</returns>
        bool IsRegistryAccessorForHandler(THandler handler, TAccessor accessor);

        /// <summary>
        /// Determines whether any accessors are available for the given handler instance.
        /// </summary>
        /// <param name="handler">The handler instance to check.</param>
        /// <returns><c>true</c> if one or more accessors are available; otherwise, <c>false</c>.</returns>
        bool IsRegistryAccessorAvailableFor(THandler handler);

        #endregion

        #region ICompositeAccessorRegistry: Accessor Operations

        /// <summary>
        /// Retrieves the single accessor registered for the specified identifier type.
        /// </summary>
        /// <param name="id">The identifier type whose accessor should be retrieved.</param>
        /// <returns>The accessor instance associated with the given type.</returns>
        TAccessor GetRegistryAccessor(Type id);

        /// <summary>
        /// Retrieves all accessors registered for the specified identifier type.
        /// </summary>
        /// <param name="id">The identifier type whose accessors should be retrieved.</param>
        /// <returns>A read-only list of accessors associated with the given type.</returns>
        IReadOnlyList<TAccessor> GetRegistryAccessors(Type id);

        /// <summary>
        /// Retrieves the single accessor associated with the handler identified by the given type.
        /// </summary>
        /// <param name="handlerId">The identifier type of the handler.</param>
        /// <returns>The accessor instance associated with the handler.</returns>
        TAccessor GetRegistryAccessorFor(Type handlerId);

        /// <summary>
        /// Retrieves all accessors associated with the handler identified by the given type.
        /// </summary>
        /// <param name="handlerId">The identifier type of the handler.</param>
        /// <returns>A read-only list of accessors associated with the handler.</returns>
        IReadOnlyList<TAccessor> GetRegistryAccessorsFor(Type handlerId);

        /// <summary>
        /// Retrieves the single accessor associated with the specified handler instance.
        /// </summary>
        /// <param name="handler">The handler instance whose accessor should be retrieved.</param>
        /// <returns>The accessor instance associated with the handler.</returns>
        TAccessor GetRegistryAccessorFor(THandler handler);

        /// <summary>
        /// Retrieves all accessors associated with the specified handler instance.
        /// </summary>
        /// <param name="handler">The handler instance whose accessors should be retrieved.</param>
        /// <returns>A read-only list of accessors associated with the handler.</returns>
        IReadOnlyList<TAccessor> GetRegistryAccessorsFor(THandler handler);

        #endregion

        #region ICompositeAccessorRegistry: Accessor Try Operations

        /// <summary>
        /// Attempts to retrieve a single accessor registered for the specified identifier type.
        /// </summary>
        /// <param name="id">The identifier type whose accessor should be retrieved.</param>
        /// <param name="accessor">When this method returns, contains the accessor instance if found; otherwise <c>null</c>.</param>
        /// <returns><c>true</c> if an accessor was found; otherwise, <c>false</c>.</returns>
        bool TryGetRegistryAccessor(Type id, out TAccessor? accessor);

        /// <summary>
        /// Attempts to retrieve all accessors registered for the specified identifier type.
        /// </summary>
        /// <param name="id">The identifier type whose accessors should be retrieved.</param>
        /// <param name="accessors">When this method returns, contains the list of accessors if found; otherwise <c>null</c>.</param>
        /// <returns><c>true</c> if one or more accessors were found; otherwise, <c>false</c>.</returns>
        bool TryGetRegistryAccessors(Type id, out IReadOnlyList<TAccessor>? accessors);

        /// <summary>
        /// Attempts to retrieve a single accessor associated with the handler identified by the given type.
        /// </summary>
        /// <param name="handlerId">The identifier type of the handler.</param>
        /// <param name="accessor">When this method returns, contains the accessor instance if found; otherwise <c>null</c>.</param>
        /// <returns><c>true</c> if an accessor was found; otherwise, <c>false</c>.</returns>
        bool TryGetRegistryAccessorFor(Type handlerId, out TAccessor? accessor);

        /// <summary>
        /// Attempts to retrieve all accessors associated with the handler identified by the given type.
        /// </summary>
        /// <param name="handlerId">The identifier type of the handler.</param>
        /// <param name="accessors">When this method returns, contains the list of accessors if found; otherwise <c>null</c>.</param>
        /// <returns><c>true</c> if one or more accessors were found; otherwise, <c>false</c>.</returns>
        bool TryGetRegistryAccessorsFor(Type handlerId, out IReadOnlyList<TAccessor>? accessors);

        /// <summary>
        /// Attempts to retrieve a single accessor associated with the specified handler instance.
        /// </summary>
        /// <param name="handler">The handler instance whose accessor should be retrieved.</param>
        /// <param name="accessor">When this method returns, contains the accessor instance if found; otherwise <c>null</c>.</param>
        /// <returns><c>true</c> if an accessor was found; otherwise, <c>false</c>.</returns>
        bool TryGetRegistryAccessorFor(THandler handler, out TAccessor? accessor);

        /// <summary>
        /// Attempts to retrieve all accessors associated with the specified handler instance.
        /// </summary>
        /// <param name="handler">The handler instance whose accessors should be retrieved.</param>
        /// <param name="accessors">When this method returns, contains the list of accessors if found; otherwise <c>null</c>.</param>
        /// <returns><c>true</c> if one or more accessors were found; otherwise, <c>false</c>.</returns>
        bool TryGetRegistryAccessorsFor(THandler handler, out IReadOnlyList<TAccessor>? accessors);

        #endregion

        #region ICompositeAccessorRegistry: Handler Operations

        /// <summary>
        /// Retrieves the single handler registered for the specified identifier type.
        /// </summary>
        /// <param name="id">The identifier type whose handler should be retrieved.</param>
        /// <returns>The handler instance associated with the given type.</returns>
        THandler GetRegistryHandler(Type id);

        /// <summary>
        /// Retrieves all handlers registered for the specified identifier type.
        /// </summary>
        /// <param name="id">The identifier type whose handlers should be retrieved.</param>
        /// <returns>A read-only list of handlers associated with the given type.</returns>
        IReadOnlyList<THandler> GetRegistryHandlers(Type id);

        #endregion

        #region ICompositeAccessorRegistry: Handler Try Operations

        /// <summary>
        /// Attempts to retrieve a single handler registered for the specified identifier type.
        /// </summary>
        /// <param name="id">The identifier type whose handler should be retrieved.</param>
        /// <param name="handler">When this method returns, contains the handler instance if found; otherwise <c>null</c>.</param>
        /// <returns><c>true</c> if a handler was found; otherwise, <c>false</c>.</returns>
        bool TryGetRegistryHandler(Type id, out THandler? handler);

        /// <summary>
        /// Attempts to retrieve all handlers registered for the specified identifier type.
        /// </summary>
        /// <param name="id">The identifier type whose handlers should be retrieved.</param>
        /// <param name="handlers">When this method returns, contains the list of handlers if found; otherwise <c>null</c>.</param>
        /// <returns><c>true</c> if one or more handlers were found; otherwise, <c>false</c>.</returns>
        bool TryGetRegistryHandlers(Type id, out IReadOnlyList<THandler>? handlers);

        #endregion
    }
}