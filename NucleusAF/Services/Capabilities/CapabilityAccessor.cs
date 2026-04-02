using Microsoft.Extensions.Logging;
using NucleusAF.Interfaces.Models;
using NucleusAF.Interfaces.Services.Capabilities;
using NucleusAF.Models.Capabilities;

namespace NucleusAF.Services.Capabilities
{
    public abstract class CapabilityAccessor(ILogger<ICapabilityAccessor> logger)
        : ICapabilityAccessor
    {
        protected readonly ILogger<ICapabilityAccessor> Logger = logger;

        public virtual Type Key { get; } = typeof(ICapabilityAccessor);

        public ICapabilityService? CapabilityService { get; protected set; } = null;
        public ICapabilityHandler? CapabilityHandler { get; protected set; } = null;

        #region CapabilityAccessor: Fluent Configuration API

        public virtual ICapabilityAccessor SetCapabilityService(ICapabilityService service)
        {
            this.CapabilityService = service;
            this.Logger?.LogDebug("[CapabilityAccessor] CapabilityService set");
            return this;
        }
        public virtual ICapabilityAccessor SetCapabilityHandler(ICapabilityHandler handler)
        {
            this.CapabilityHandler = handler;
            this.Logger?.LogDebug("[CapabilityAccessor] CapabilityHandler set");
            return this;
        }

        #endregion

        #region CapabilityAccessor: Factory Methods

        public virtual ICapabilityAccessorFor<THandler> For<THandler>() where THandler : class
        {
            if (this.CapabilityService == null)
            {
                this.Logger?.LogError("[CapabilityAccessor] CapabilityService is null when attempting to create accessor for {Type}", typeof(THandler).FullName);
                throw new InvalidOperationException("CapabilityService must be set before creating typed accessors");
            }

            if (this.CapabilityHandler == null)
            {
                this.Logger?.LogError("[CapabilityAccessor] CapabilityHandler is null when attempting to create accessor for {Type}", typeof(THandler).FullName);
                throw new InvalidOperationException("CapabilityHandler must be set before creating typed accessors");
            }

            this.Logger?.LogDebug("[CapabilityAccessor] Delegating creation of accessor for {Type}", typeof(THandler).FullName);

            return this.For<THandler>(this.CapabilityService, this.CapabilityHandler);
        }
        public abstract ICapabilityAccessorFor<THandler> For<THandler>(ICapabilityService service, ICapabilityHandler handler) where THandler : class;

        #endregion
    }

    public abstract class CapabilityAccessorFor<THandler>(ILogger<ICapabilityAccessor> logger, ICapabilityService service, ICapabilityHandler handler)
        : ICapabilityAccessorFor<THandler> where THandler : class
    {
        protected readonly ILogger<ICapabilityAccessor> Logger = logger;
        protected readonly ICapabilityService CapabilityService = service;
        protected readonly ICapabilityHandler CapabilityHandler = handler;

        #region CapabilityAccessorFor: Registration Operations

        public virtual ICapabilityToken Register(IResourceKey resource, CapabilitySet capabilities, ICapabilityToken? token = null)
        {
            this.Logger.LogDebug("[CapabilityAccessorFor] Register called for resource {Resource} with capabilities {Capabilities}", resource, capabilities);
            return this.CapabilityService.Register(typeof(THandler), resource, capabilities, token);
        }
        public virtual void Unregister(IResourceKey resource, ICapabilityToken token)
        {
            this.Logger.LogDebug("[CapabilityAccessorFor] Unregister called for resource {Resource}", resource);
            this.CapabilityService.Unregister(typeof(THandler), resource, token);
        }

        #endregion

        #region CapabilityAccessorFor: Verification Operations

        public virtual bool IsAccessible(IResourceKey resource, int slot, ICapabilityToken? token = null)
        {
            this.Logger.LogDebug("[CapabilityAccessorFor] IsAccessible called for resource {Resource} and slot {Slot}", resource, slot);
            return this.CapabilityHandler.IsAccessible(resource, slot, token);
        }
        public virtual bool IsOwner(IResourceKey resource, ICapabilityToken token)
        {
            this.Logger.LogDebug("[CapabilityAccessorFor] IsOwner called for resource {Resource}", resource);
            return this.CapabilityHandler.IsOwner(resource, token);
        }

        #endregion

        #region CapabilityAccessorFor: Access Checks

        public virtual bool CanAccess(IResourceKey resource, int slot, ICapabilityToken? token = null)
        {
            try
            {
                return this.IsAccessible(resource, slot, token);
            }
            catch (Exception ex)
            {
                this.Logger.LogError(ex, "[CapabilityAccessorFor] Capability validation failed for resource {Resource}", resource);
            }

            return false;
        }
        public virtual void AssertAccess(IResourceKey resource, int slot, ICapabilityToken? token = null)
        {
            if (!this.IsAccessible(resource, slot, token))
            {
                this.Logger.LogWarning("[CapabilityAccessorFor] Capability denied for handler {Handler} on resource {Resource} at slot {Slot} with token {Token}", typeof(THandler).Name, resource, slot, token);
                throw new UnauthorizedAccessException($"Capability denied for handler {typeof(THandler).Name} on resource {resource} at slot {slot}");
            }
        }

        #endregion
    }
}