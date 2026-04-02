namespace NucleusAF.Interfaces.Abstractions.CompositeRegistry
{
    /// <summary>
    /// Defines the contract for a composite registry that manages keyed collections of registrants.
    /// A composite registry allows multiple values to be registered under the same key,
    /// supports FILO semantics for retrieval, and provides discoverability of all registrants.
    /// </summary>
    /// <typeparam name="TKey">The key type used to group registrants (e.g., a command type).</typeparam>
    /// <typeparam name="TValue">The registrant type stored in the registry (e.g., command metadata).</typeparam>
    public interface ICompositeRegistry<TKey, TValue>
    {
        #region ICompositeRegistry: Predicates

        /// <summary>
        /// Determines whether the specified registrant is currently registered under the given key.
        /// </summary>
        /// <param name="key">The key to check.</param>
        /// <param name="registrant">The registrant instance to verify.</param>
        /// <returns><c>true</c> if the registrant is registered; otherwise, <c>false</c>.</returns>
        bool IsRegistered(TKey key, TValue registrant);

        /// <summary>
        /// Determines whether the specified key is currently registered.
        /// </summary>
        /// <param name="key">The key to check.</param>
        /// <returns><c>true</c> if the key is registered; otherwise, <c>false</c>.</returns>
        bool IsRegistered(TKey key);

        #endregion

        #region ICompositeRegistry: Add/Remove

        /// <summary>
        /// Adds a registrant under the specified key.
        /// Multiple registrants may be added under the same key; the most recent is returned by <see cref="GetRegistrant"/>.
        /// </summary>
        /// <param name="key">The key under which to register the value.</param>
        /// <param name="registrant">The registrant instance to add.</param>
        void AddRegistrant(TKey key, TValue registrant);

        /// <summary>
        /// Removes a specific registrant from the registry under the given key.
        /// </summary>
        /// <param name="key">The key from which to remove the registrant.</param>
        /// <param name="registrant">The registrant instance to remove.</param>
        /// <returns><c>true</c> if the registrant was removed; otherwise, <c>false</c>.</returns>
        bool RemoveRegistrant(TKey key, TValue registrant);

        /// <summary>
        /// Removes all registrants associated with the specified key.
        /// </summary>
        /// <param name="key">The key whose registrants should be removed.</param>
        void RemoveAllRegistrants(TKey key);

        /// <summary>
        /// Clears all registrants across all keys in the registry.
        /// </summary>
        void ClearAllRegistrants();

        #endregion

        #region ICompositeRegistry: Get Operations

        /// <summary>
        /// Retrieves the most recently registered value for the specified key.
        /// </summary>
        /// <param name="key">The key to query.</param>
        /// <returns>The latest registrant associated with the key.</returns>
        TValue GetRegistrant(TKey key);

        /// <summary>
        /// Retrieves all registrants associated with the specified key, in registration order.
        /// </summary>
        /// <param name="key">The key to query.</param>
        /// <returns>A read-only list of registrants associated with the key.</returns>
        IReadOnlyList<TValue> GetRegistrants(TKey key);

        #endregion

        #region ICompositeRegistry: Try Get Operations

        /// <summary>
        /// Attempts to retrieve the most recently registered value for the specified key.
        /// </summary>
        /// <param name="key">The key to query.</param>
        /// <param name="registrant">When this method returns, contains the registrant if found; otherwise <c>null</c>.</param>
        /// <returns><c>true</c> if a registrant was found; otherwise, <c>false</c>.</returns>
        bool TryGetRegistrant(TKey key, out TValue? registrant);

        /// <summary>
        /// Attempts to retrieve all registrants associated with the specified key.
        /// </summary>
        /// <param name="key">The key to query.</param>
        /// <param name="registrants">When this method returns, contains the registrants if found; otherwise <c>null</c>.</param>
        /// <returns><c>true</c> if any registrants were found; otherwise, <c>false</c>.</returns>
        bool TryGetRegistrants(TKey key, out IReadOnlyList<TValue>? registrants);

        #endregion

        #region ICompositeRegistry: Get All Operations

        /// <summary>
        /// Retrieves the most recent registrant for each unique key in the registry.
        /// This method returns one registrant per key, representing the current "head" of each group.
        /// Historical entries are not included; only the latest registrant for each key is surfaced.
        /// </summary>
        /// <returns>
        /// A read-only list containing the most recent registrant for every key in the registry.
        /// </returns>
        IReadOnlyList<TValue> GetKeyRegistrants();

        /// <summary>
        /// Retrieves all registrants associated with the specified key, including historical entries.
        /// </summary>
        /// <param name="key">The key to query.</param>
        /// <returns>A read-only list of all registrants for the key.</returns>
        IReadOnlyList<TValue> GetAllRegistrants(TKey key);

        /// <summary>
        /// Retrieves all registrants across all keys in the registry.
        /// </summary>
        /// <returns>A read-only list of all registrants currently registered.</returns>
        IReadOnlyList<TValue> GetAllRegistrants();

        #endregion

        #region ICompositeRegistry: Try Get All Operations

        /// <summary>
        /// Attempts to retrieve all registrants associated with the specified key.
        /// Unlike <see cref="GetAllRegistrants(TKey)"/>, this method never throws and always returns a list (possibly empty).
        /// </summary>
        /// <param name="key">The key to query.</param>
        /// <returns>A read-only list of registrants, or an empty list if none are found.</returns>
        IReadOnlyList<TValue> TryGetAllRegistrants(TKey key);

        /// <summary>
        /// Attempts to retrieve all registrants across all keys in the registry.
        /// Unlike <see cref="GetAllRegistrants()"/>, this method never throws and always returns a list (possibly empty).
        /// </summary>
        /// <returns>A read-only list of registrants, or an empty list if none are found.</returns>
        IReadOnlyList<TValue> TryGetAllRegistrants();

        #endregion
    }
}