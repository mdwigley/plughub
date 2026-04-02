using NucleusAF.Interfaces.Models;
using NucleusAF.Models.Capabilities;

namespace NucleusAF.Interfaces.Services.Capabilities
{
    /// <summary>
    /// Defines the contract for the capability service, which orchestrates capability
    /// accessors and handlers. Provides APIs for retrieving accessors and for registering
    /// or unregistering capabilities against resources.
    /// </summary>
    public interface ICapabilityService
    {
        #region ICapabilityService: Accessor Management

        /// <summary>
        /// Retrieves a capability accessor instance for the specified handler type.
        /// </summary>
        /// <param name="handlerType">The handler type to retrieve.</param>
        /// <returns>The capability accessor instance associated with the given type.</returns>
        /// <exception cref="KeyNotFoundException">Thrown if no accessor is registered for the specified type.</exception>
        ICapabilityAccessor GetAccessor(Type handlerType);

        /// <summary>
        /// Retrieves a typed capability accessor for the specified identifier type.
        /// </summary>
        /// <typeparam name="THandler">The identifier type that the accessor manages.</typeparam>
        /// <returns>A typed capability accessor for the given identifier type.</returns>
        /// <exception cref="KeyNotFoundException">Thrown if no accessor is registered for the specified identifier type.</exception>
        ICapabilityAccessorFor<THandler> GetAccessor<THandler>() where THandler : class;

        /// <summary>
        /// Retrieves a specific typed capability accessor for the given identifier type.
        /// </summary>
        /// <typeparam name="TSpecific">The specific accessor type to retrieve.</typeparam>
        /// <typeparam name="THandler">The identifier type that the accessor manages.</typeparam>
        /// <returns>The specific typed capability accessor instance.</returns>
        /// <exception cref="KeyNotFoundException">Thrown if no accessor of the specified type is registered.</exception>
        TSpecific GetAccessor<TSpecific, THandler>() where TSpecific : ICapabilityAccessorFor<THandler> where THandler : class;

        #endregion

        #region ICapabilityService: Registration Operations

        /// <summary>
        /// Registers a capability set for the specified resource using the given handler type,
        /// optionally with a capability token to authorize the operation.
        /// </summary>
        /// <param name="handlerType">The handler type responsible for managing the capability.</param>
        /// <param name="resource">The resource key to associate with the capability set.</param>
        /// <param name="capabilities">The set of capabilities to register for the resource.</param>
        /// <param name="token">An optional capability token used to authorize the registration.</param>
        /// <returns>A capability token representing the registration context.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the handler type does not support registration or if the operation fails.</exception>
        ICapabilityToken Register(Type handlerType, IResourceKey resource, CapabilitySet capabilities, ICapabilityToken? token = null);

        /// <summary>
        /// Registers capabilities for a collection of resources using the specified handler type.
        /// </summary>
        /// <param name="handlerType">The capability handler type responsible for registration.</param>
        /// <param name="resources">The resources to register capabilities for.</param>
        /// <param name="capabilities">The capability set to apply to each resource.</param>
        /// <param name="token">An optional capability token to associate with the registrations.</param>
        /// <returns>A dictionary mapping each resource key to its associated capability token.</returns>
        IDictionary<IResourceKey, ICapabilityToken> Register(Type handlerType, IEnumerable<IResourceKey> resources, CapabilitySet capabilities, ICapabilityToken? token = null);


        /// <summary>
        /// Attempts to register a capability set for the specified resource using the given handler type,
        /// optionally with a capability token to authorize the operation.
        /// </summary>
        /// <param name="handlerType">The handler type responsible for managing the capability.</param>
        /// <param name="resource">The resource key to associate with the capability set.</param>
        /// <param name="capabilities">The set of capabilities to register for the resource.</param>
        /// <param name="registered">When this method returns, contains the capability token representing the registration context if successful; otherwise, null.</param>
        /// <param name="token">An optional capability token used to authorize the registration.</param>
        /// <returns><c>true</c> if the capability set was successfully registered; otherwise, <c>false</c>.</returns>
        bool TryRegister(Type handlerType, IResourceKey resource, CapabilitySet capabilities, out ICapabilityToken? registered, ICapabilityToken? token = null);

        /// <summary>
        /// Attempts to register capabilities for a collection of resources using the specified handler type.
        /// </summary>
        /// <param name="handlerType">The capability handler type responsible for registration.</param>
        /// <param name="resources">The resources to register capabilities for.</param>
        /// <param name="capabilities">The capability set to apply to each resource.</param>
        /// <param name="results">Outputs a dictionary mapping each resource key to its capability token if registration succeeds.</param>
        /// <param name="token">An optional capability token to associate with the registrations.</param>
        /// <returns>True if all resources were successfully registered; false if any registration failed.</returns>
        bool TryRegister(Type handlerType, IEnumerable<IResourceKey> resources, CapabilitySet capabilities, out IDictionary<IResourceKey, ICapabilityToken> results, ICapabilityToken? token = null);


        /// <summary>
        /// Unregisters a capability set for the specified resource using the given handler type
        /// and capability token.
        /// </summary>
        /// <param name="handlerType">The handler type responsible for managing the capability.</param>
        /// <param name="resource">The resource key whose capability set should be unregistered.</param>
        /// <param name="token">The capability token used to authorize the unregistration.</param>
        /// <exception cref="InvalidOperationException">Thrown if the handler type does not support unregistration or if the operation fails.</exception>
        void Unregister(Type handlerType, IResourceKey resource, ICapabilityToken token);

        /// <summary>
        /// Unregisters capabilities for a collection of resources using the specified handler type.
        /// </summary>
        /// <param name="handlerType">The capability handler type responsible for unregistration.</param>
        /// <param name="resources">The resources whose capabilities should be unregistered.</param>
        /// <param name="token">The capability token associated with the registrations to be removed.</param>
        void Unregister(Type handlerType, IEnumerable<IResourceKey> resources, ICapabilityToken token);


        /// <summary>
        /// Attempts to unregister a capability set for the specified resource using the given handler type
        /// and capability token.
        /// </summary>
        /// <param name="handlerType">The handler type responsible for managing the capability.</param>
        /// <param name="resource">The resource key whose capability set should be unregistered.</param>
        /// <param name="token">The capability token used to authorize the unregistration.</param>
        /// <returns><c>true</c> if the capability set was successfully unregistered; otherwise, <c>false</c>.</returns>
        bool TryUnregister(Type handlerType, IResourceKey resource, ICapabilityToken token);

        /// <summary>
        /// Attempts to unregister capabilities for a collection of resources using the specified handler type.
        /// </summary>
        /// <param name="handlerType">The capability handler type responsible for unregistration.</param>
        /// <param name="resources">The resources whose capabilities should be unregistered.</param>
        /// <param name="token">The capability token associated with the registrations to be removed.</param>
        /// <returns>
        /// True if all resources were successfully unregistered; false if any unregistration failed.
        /// </returns>
        bool TryUnregister(Type handlerType, IEnumerable<IResourceKey> resources, ICapabilityToken token);

        #endregion
    }
}