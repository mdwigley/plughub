using NucleusAF.Interfaces.Abstractions.CompositeRegistry;
using NucleusAF.Interfaces.Models;
using NucleusAF.Models.Capabilities;

namespace NucleusAF.Interfaces.Services.Capabilities
{
    /// <summary>
    /// Defines the contract for a capability accessor within the composite registry.
    /// A capability accessor provides fluent configuration methods to bind services and handlers,
    /// and factory methods to create typed accessors for specific identifiers.
    /// </summary>
    public interface ICapabilityAccessor : ICompositeRegistryAccessor
    {
        #region ICapabilityAccessor: Fluent Configuration API

        /// <summary>
        /// Sets the capability service to be used by this accessor.
        /// This allows fluent configuration of the accessor with a specific service instance.
        /// </summary>
        /// <param name="service">The capability service to associate with this accessor.</param>
        /// <returns>The current accessor instance for fluent chaining.</returns>
        ICapabilityAccessor SetCapabilityService(ICapabilityService service);

        /// <summary>
        /// Sets the capability handler to be used by this accessor.
        /// This allows fluent configuration of the accessor with a specific handler instance.
        /// </summary>
        /// <param name="handler">The capability handler to associate with this accessor.</param>
        /// <returns>The current accessor instance for fluent chaining.</returns>
        ICapabilityAccessor SetCapabilityHandler(ICapabilityHandler handler);

        #endregion

        #region ICapabilityAccessor: Factory Methods

        /// <summary>
        /// Creates a typed capability accessor for the specified identifier type.
        /// </summary>
        /// <typeparam name="THandler">The identifier type for which the accessor is created.</typeparam>
        /// <returns>A typed capability accessor for the given identifier type.</returns>
        ICapabilityAccessorFor<THandler> For<THandler>() where THandler : class;

        /// <summary>
        /// Creates a typed capability accessor for the specified identifier type,
        /// using the provided capability service and handler.
        /// </summary>
        /// <typeparam name="THandler">The identifier type for which the accessor is created.</typeparam>
        /// <param name="service">The capability service to associate with the accessor.</param>
        /// <param name="handler">The capability handler to associate with the accessor.</param>
        /// <returns>A typed capability accessor for the given identifier type.</returns>
        ICapabilityAccessorFor<THandler> For<THandler>(ICapabilityService service, ICapabilityHandler handler) where THandler : class;

        #endregion
    }

    /// <summary>
    /// Defines the contract for a typed capability accessor associated with a specific identifier type.
    /// Provides operations for registering and unregistering capabilities,
    /// verifying ownership and accessibility, and performing access checks.
    /// </summary>
    /// <typeparam name="THandler">The identifier type that this accessor is associated with.</typeparam>
    public interface ICapabilityAccessorFor<THandler> : ICompositeRegistryAccessorFor<THandler> where THandler : class
    {
        #region ICapabilityAccessorFor: Registration Operations

        /// <summary>
        /// Registers a capability for the specified resource with the given capability set.
        /// </summary>
        /// <param name="resource">The resource key to associate with the capability.</param>
        /// <param name="capabilities">The set of capabilities to register for the resource.</param>
        /// <param name="token">An optional capability token used to authorize the registration.</param>
        /// <returns>A capability token representing the registration context.</returns>
        ICapabilityToken Register(IResourceKey resource, CapabilitySet capabilities, ICapabilityToken? token = null);

        /// <summary>
        /// Unregisters a capability for the specified resource using the provided token.
        /// </summary>
        /// <param name="resource">The resource key whose capability should be unregistered.</param>
        /// <param name="token">The capability token used to authorize the unregistration.</param>
        void Unregister(IResourceKey resource, ICapabilityToken token);

        #endregion

        #region ICapabilityAccessorFor: Verification Operations

        /// <summary>
        /// Determines whether the specified resource is accessible at the given slot,
        /// optionally using a capability token for authorization.
        /// </summary>
        /// <param name="resource">The resource key to check for accessibility.</param>
        /// <param name="slot">The slot index to verify access against.</param>
        /// <param name="token">An optional capability token used to authorize the check.</param>
        /// <returns><c>true</c> if the resource is accessible; otherwise, <c>false</c>.</returns>
        bool IsAccessible(IResourceKey resource, int slot, ICapabilityToken? token = null);

        /// <summary>
        /// Determines whether the specified capability token identifies ownership of the given resource.
        /// </summary>
        /// <param name="resource">The resource key to check for ownership.</param>
        /// <param name="token">The capability token to verify ownership against.</param>
        /// <returns><c>true</c> if the token represents ownership of the resource; otherwise, <c>false</c>.</returns>
        bool IsOwner(IResourceKey resource, ICapabilityToken token);

        #endregion

        #region ICapabilityAccessorFor: Access Checks

        /// <summary>
        /// Determines whether the specified resource can be accessed at the given slot,
        /// optionally using a capability token for authorization.
        /// </summary>
        /// <param name="resource">The resource key to check for access.</param>
        /// <param name="slot">The slot index to verify access against.</param>
        /// <param name="token">An optional capability token used to authorize the check.</param>
        /// <returns><c>true</c> if the resource can be accessed; otherwise, <c>false</c>.</returns>
        bool CanAccess(IResourceKey resource, int slot, ICapabilityToken? token = null);

        /// <summary>
        /// Asserts that the specified resource can be accessed at the given slot,
        /// optionally using a capability token for authorization.
        /// Throws an exception if access is not permitted.
        /// </summary>
        /// <param name="resource">The resource key to assert access for.</param>
        /// <param name="slot">The slot index to verify access against.</param>
        /// <param name="token">An optional capability token used to authorize the check.</param>
        /// <exception cref="UnauthorizedAccessException">Thrown when the resource cannot be accessed with the provided token or slot.</exception>
        void AssertAccess(IResourceKey resource, int slot, ICapabilityToken? token = null);

        #endregion
    }
}