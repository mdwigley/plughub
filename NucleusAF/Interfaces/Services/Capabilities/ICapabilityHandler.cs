using NucleusAF.Interfaces.Abstractions.CompositeRegistry;
using NucleusAF.Interfaces.Models;
using NucleusAF.Models.Capabilities;

namespace NucleusAF.Interfaces.Services.Capabilities
{
    /// <summary>
    /// Defines the contract for a capability registrar that can register and unregister capabilities
    /// for specific resources. Only handlers implementing this interface are permitted to mutate
    /// capability state within the capability service.
    /// </summary>
    public interface ICapabilityRegistrar : ICompositeRegistryHandler
    {

        #region ICapabilityRegistrar: Predicate Operations

        /// <summary>
        /// Determines whether a capability set is currently registered for the specified resource.
        /// </summary>
        /// <param name="resource">The resource key to check for an existing capability registration.</param>
        /// <returns><c>true</c> if a capability set is registered for the resource; otherwise, <c>false</c>.</returns>
        bool IsRegistered(IResourceKey resource);

        #endregion

        #region ICapabilityRegistrar: Registration Operations

        /// <summary>
        /// Registers a capability set for the specified resource, optionally using a capability token
        /// to authorize the operation.
        /// </summary>
        /// <param name="resource">The resource key to associate with the capability set.</param>
        /// <param name="capabilities">The set of capabilities to register for the resource.</param>
        /// <param name="token">An optional capability token used to authorize the registration.</param>
        /// <returns>A capability token representing the registration context.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the registration cannot be completed.</exception>
        ICapabilityToken Register(IResourceKey resource, CapabilitySet capabilities, ICapabilityToken? token = null);

        /// <summary>
        /// Unregisters a capability set for the specified resource using the provided capability token.
        /// </summary>
        /// <param name="resource">The resource key whose capability set should be unregistered.</param>
        /// <param name="token">The capability token used to authorize the unregistration.</param>
        /// <exception cref="InvalidOperationException">Thrown if the unregistration cannot be completed.</exception>
        void Unregister(IResourceKey resource, ICapabilityToken token);

        #endregion
    }

    /// <summary>
    /// Defines the contract for a capability handler that can verify accessibility and ownership
    /// of resources based on registered capabilities and tokens.
    /// </summary>
    public interface ICapabilityHandler : ICompositeRegistryHandler
    {
        #region ICapabilityHandler: Verification Operations

        /// <summary>
        /// Determines whether the specified resource is accessible with the required capability level,
        /// optionally using a capability token for authorization.
        /// </summary>
        /// <param name="resource">The resource key to check for accessibility.</param>
        /// <param name="required">The required capability level or slot index.</param>
        /// <param name="token">An optional capability token used to authorize the check.</param>
        /// <returns><c>true</c> if the resource is accessible; otherwise, <c>false</c>.</returns>
        bool IsAccessible(IResourceKey resource, int required, ICapabilityToken? token = null);

        /// <summary>
        /// Determines whether the specified capability token identifies ownership of the given resource.
        /// </summary>
        /// <param name="resource">The resource key to check for ownership.</param>
        /// <param name="token">The capability token to verify ownership against.</param>
        /// <returns><c>true</c> if the token represents ownership of the resource; otherwise, <c>false</c>.</returns>
        bool IsOwner(IResourceKey resource, ICapabilityToken token);

        #endregion
    }

    /// <summary>
    /// Defines a typed capability handler associated with a specific handled type.
    /// Extends <see cref="ICapabilityHandler"/> to provide type-safe specialization.
    /// </summary>
    /// <typeparam name="THandled">The type that this capability handler manages.</typeparam>
    public interface ICapabilityHandler<THandled> : ICapabilityHandler { }
}