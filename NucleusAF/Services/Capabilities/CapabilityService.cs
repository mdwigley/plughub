using Microsoft.Extensions.Logging;
using NucleusAF.Abstractions.CompositeRegistry;
using NucleusAF.Interfaces.Models;
using NucleusAF.Interfaces.Services.Capabilities;
using NucleusAF.Models.Capabilities;
using System.Collections.Concurrent;

namespace NucleusAF.Services.Capabilities
{
    public class CapabilityService : CompositeAccessorRegistryBase<ICapabilityAccessor, ICapabilityHandler>, ICapabilityService, IDisposable
    {
        protected readonly ILogger<ICapabilityService> Logger;
        protected readonly ConcurrentDictionary<IResourceKey, ICapabilityHandler> HandlerTypeByResource = [];

        protected bool IsDisposed = false;

        public CapabilityService(IEnumerable<ICapabilityAccessor> accessors, IEnumerable<ICapabilityHandler> handler, ILogger<ICapabilityService> logger)
            : base(accessors, handler)
        {
            this.Logger = logger;
            this.Logger.LogInformation("[CapabilityService] Initialized.");
        }

        #region CapabilityService: Accessor Management

        public virtual ICapabilityAccessor GetAccessor(Type handlerType)
        {
            ArgumentNullException.ThrowIfNull(handlerType);

            if (!this.TryGetRegistryAccessorFor(handlerType, out ICapabilityAccessor? accessor))
            {
                this.Logger.LogError("[CapabilityService] No capability accessor found for provider type {HandlerType}", handlerType.Name);
                throw new KeyNotFoundException($"No capability accessor found for provider type {handlerType.Name}");
            }

            if (!this.TryGetRegistryHandler(handlerType, out ICapabilityHandler? handler))
            {
                this.Logger.LogError("[CapabilityService] No handler registered for accessor: {AccessorType}", handlerType.Name);
                throw new InvalidOperationException($"No handler registered for accessor {handlerType.Name}");
            }

            this.Logger.LogDebug("[CapabilityService] Retrieved capability accessor for type {AccessorType}", handlerType.Name);
            return accessor!.SetCapabilityService(this).SetCapabilityHandler(handler!);
        }
        public virtual ICapabilityAccessorFor<THandler> GetAccessor<THandler>() where THandler : class
        {
            if (!this.TryGetRegistryAccessorFor(typeof(THandler), out ICapabilityAccessor? accessor))
            {
                this.Logger.LogError("[CapabilityService] No capability accessor found for provider type {HandlerType}", typeof(THandler).Name);
                throw new KeyNotFoundException($"No capability accessor found for provider type {typeof(THandler).Name}");
            }

            if (!this.TryGetRegistryHandler(typeof(THandler), out ICapabilityHandler? handler))
            {
                this.Logger.LogError("[CapabilityService] No capability handler found for provider type {HandlerType}", typeof(THandler).Name);
                throw new KeyNotFoundException($"No capability handler found for provider type {typeof(THandler).Name}");
            }

            ICapabilityAccessorFor<THandler>? accountFor = accessor!.For<THandler>(this, handler!);

            if (accountFor == null)
            {
                this.Logger.LogError("[CapabilityService] Unable to create accessor for {HandlerType}", typeof(THandler).Name);
                throw new InvalidOperationException($"Unable to create accessor for {typeof(THandler).Name}");
            }

            this.Logger.LogDebug("[CapabilityService] Retrieved capability accessor for type {AccessorType}", typeof(THandler).Name);
            return accountFor;
        }
        public virtual TSpecific GetAccessor<TSpecific, THandler>()
            where TSpecific : ICapabilityAccessorFor<THandler>
            where THandler : class
        {
            ICapabilityAccessorFor<THandler> defaultAccessor = this.GetAccessor<THandler>();

            if (defaultAccessor is TSpecific typedAccessor)
            {
                this.Logger.LogDebug("[CapabilityService] Retrieved accessor {AccessorType} for identifier {IdentType}", typeof(TSpecific).Name, typeof(THandler).Name);
                return typedAccessor;
            }
            else
            {
                this.Logger.LogError("[CapabilityService] Accessor type mismatch: expected {ExpectedType}, got {ActualType}", typeof(TSpecific).Name, defaultAccessor.GetType().Name);
                throw new InvalidCastException($"Accessor for {typeof(THandler).Name} is a {defaultAccessor.GetType().Name} and cannot be cast to {typeof(TSpecific).Name}");
            }
        }

        #endregion

        #region CapabilityService: Registration Operations

        public virtual ICapabilityToken Register(Type handlerType, IResourceKey resource, CapabilitySet capabilities, ICapabilityToken? token = null)
        {
            ArgumentNullException.ThrowIfNull(handlerType);
            ArgumentNullException.ThrowIfNull(resource);
            ArgumentNullException.ThrowIfNull(capabilities);

            ICapabilityHandler? handler = this.TryGetRegistryHandler(handlerType, out ICapabilityHandler? resolved) ? resolved : null;

            if (handler is null)
            {
                this.Logger.LogWarning("[CapabilityService] No handler found for {HandlerType}", handlerType.Name);
                throw new InvalidOperationException($"No capability handler handler found for type '{handlerType.Name}'.");
            }

            if (!this.HandlerTypeByResource.TryAdd(resource, handler))
            {
                this.Logger.LogWarning("[CapabilityService] Resource key {Resource} already registered with handler {HandlerType}", resource, handlerType.Name);
                throw new InvalidOperationException($"Resource key '{resource}' is already registered with handler '{handlerType.Name}'.");
            }

            if (handler is ICapabilityRegistrar registrar)
            {
                this.Logger.LogInformation("[CapabilityService] Registering capability for resource {Resource} with handler {HandlerType}", resource, handlerType.Name);
                return registrar.Register(resource, capabilities, token);
            }

            this.Logger.LogError("[CapabilityService] Handler {HandlerType} does not implement ICapabilityRegistrar", handler.GetType().Name);
            throw new InvalidOperationException($"Handler type '{handler.GetType().Name}' does not implement ICapabilityRegistrar.");
        }
        public virtual IDictionary<IResourceKey, ICapabilityToken> Register(Type handlerType, IEnumerable<IResourceKey> resources, CapabilitySet capabilities, ICapabilityToken? token = null)
        {
            ArgumentNullException.ThrowIfNull(handlerType);
            ArgumentNullException.ThrowIfNull(resources);
            ArgumentNullException.ThrowIfNull(capabilities);

            Dictionary<IResourceKey, ICapabilityToken> results = [];

            foreach (IResourceKey resource in resources)
            {
                this.Logger.LogTrace("[CapabilityService] Registering capability for {Resource} with handler {HandlerType}", resource, handlerType.Name);

                ICapabilityToken regToken = this.Register(handlerType, resource, capabilities, token);

                results.Add(resource, regToken);
            }

            this.Logger.LogDebug("[CapabilityService] Completed batch capability registration for {Count} resources", results.Count);

            return results;
        }

        public virtual bool TryRegister(Type handlerType, IResourceKey resource, CapabilitySet capabilities, out ICapabilityToken? registered, ICapabilityToken? token = null)
        {
            ArgumentNullException.ThrowIfNull(handlerType);
            ArgumentNullException.ThrowIfNull(resource);
            ArgumentNullException.ThrowIfNull(capabilities);

            try
            {
                registered = this.Register(handlerType, resource, capabilities, token);

                this.Logger.LogDebug("[CapabilityService] Successfully registered capability for resource {Resource} with handler {HandlerType}", resource, handlerType.Name);

                return true;
            }
            catch (Exception ex)
            {
                registered = default;

                this.Logger.LogWarning(ex, "[CapabilityService] Failed to register capability for resource {Resource} with handler {HandlerType}", resource, handlerType.Name);

                return false;
            }
        }
        public virtual bool TryRegister(Type handlerType, IEnumerable<IResourceKey> resources, CapabilitySet capabilities, out IDictionary<IResourceKey, ICapabilityToken> results, ICapabilityToken? token = null)
        {
            ArgumentNullException.ThrowIfNull(handlerType);
            ArgumentNullException.ThrowIfNull(resources);
            ArgumentNullException.ThrowIfNull(capabilities);

            results = new Dictionary<IResourceKey, ICapabilityToken>();

            bool allSucceeded = true;

            foreach (IResourceKey resource in resources)
            {
                this.Logger.LogTrace("[CapabilityService] Attempting to register capability for resource {Resource} with handler {HandlerType}", resource, handlerType.Name);

                try
                {
                    ICapabilityToken regToken = this.Register(handlerType, resource, capabilities, token);

                    results.Add(resource, regToken);
                }
                catch (Exception ex)
                {
                    this.Logger.LogWarning(ex, "[CapabilityService] Failed to register capability for resource {Resource} with handler {HandlerType}", resource, handlerType.Name);

                    allSucceeded = false;
                }
            }

            this.Logger.LogDebug("[CapabilityService] Completed batch capability registration for handler {HandlerType}", handlerType.Name);

            return allSucceeded;
        }

        public virtual void Unregister(Type handlerType, IResourceKey resource, ICapabilityToken token)
        {
            ArgumentNullException.ThrowIfNull(handlerType);
            ArgumentNullException.ThrowIfNull(resource);
            ArgumentNullException.ThrowIfNull(token);

            if (!this.TryGetRegistryHandler(handlerType, out ICapabilityHandler? prov))
            {
                this.Logger.LogWarning("[CapabilityService] Unregister failed: no handler found for {HandlerType}", handlerType.Name);
                throw new KeyNotFoundException($"No handler found for {handlerType.Name}");
            }

            if (prov is not ICapabilityRegistrar registrar)
            {
                this.Logger.LogError("[CapabilityService] Handler {HandlerType} does not implement ICapabilityRegistrar", prov?.GetType().Name);
                throw new InvalidOperationException($"Handler {prov?.GetType().Name} does not implement ICapabilityRegistrar");
            }

            registrar.Unregister(resource, token);

            foreach (KeyValuePair<IResourceKey, ICapabilityHandler> kvp in this.HandlerTypeByResource)
            {
                if (ReferenceEquals(kvp.Value, prov))
                {
                    if (!this.HandlerTypeByResource.TryRemove(kvp.Key, out _))
                    {
                        this.Logger.LogError("[CapabilityService] Failed to remove resource mapping {Resource} for handler {HandlerType}", kvp.Key, handlerType.Name);
                        throw new InvalidOperationException($"Failed to remove resource mapping {kvp.Key} for handler {handlerType.Name}");
                    }

                    this.Logger.LogDebug("[CapabilityService] Unregistered resource mapping {Resource} for handler {HandlerType}", kvp.Key, handlerType.Name);
                }
            }
        }
        public virtual void Unregister(Type handlerType, IEnumerable<IResourceKey> resources, ICapabilityToken token)
        {
            ArgumentNullException.ThrowIfNull(handlerType);
            ArgumentNullException.ThrowIfNull(resources);
            ArgumentNullException.ThrowIfNull(token);

            foreach (IResourceKey resource in resources)
            {
                this.Logger.LogTrace("[CapabilityService] Attempting to unregister capability for resource {Resource} with handler {HandlerType}", resource, handlerType.Name);

                this.Unregister(handlerType, resource, token);
            }

            this.Logger.LogDebug("[CapabilityService] Completed bulk capability unregistration for handler {HandlerType}", handlerType.Name);
        }

        public virtual bool TryUnregister(Type handlerType, IResourceKey resource, ICapabilityToken token)
        {
            ArgumentNullException.ThrowIfNull(handlerType);
            ArgumentNullException.ThrowIfNull(resource);
            ArgumentNullException.ThrowIfNull(token);

            try
            {
                this.Unregister(handlerType, resource, token);

                this.Logger.LogDebug("[CapabilityService] Successfully unregistered capability for resource {Resource} with handler {HandlerType}", resource, handlerType.Name);

                return true;
            }
            catch (Exception ex)
            {
                this.Logger.LogWarning(ex, "[CapabilityService] Failed to unregister capability for resource {Resource} with handler {HandlerType}", resource, handlerType.Name);

                return false;
            }
        }
        public virtual bool TryUnregister(Type handlerType, IEnumerable<IResourceKey> resources, ICapabilityToken token)
        {
            ArgumentNullException.ThrowIfNull(handlerType);
            ArgumentNullException.ThrowIfNull(resources);
            ArgumentNullException.ThrowIfNull(token);

            bool allSucceeded = true;

            foreach (IResourceKey resource in resources)
            {
                this.Logger.LogTrace("[CapabilityService] Attempting to unregister capability for resource {Resource} with handler {HandlerType}", resource, handlerType.Name);

                try
                {
                    this.Unregister(handlerType, resource, token);
                }
                catch (Exception ex)
                {
                    this.Logger.LogWarning(ex, "[CapabilityService] Failed to unregister capability for resource {Resource} with handler {HandlerType}", resource, handlerType.Name);

                    allSucceeded = false;
                }
            }

            this.Logger.LogDebug("[CapabilityService] Completed bulk capability unregistration for handler {HandlerType}", handlerType.Name);

            return allSucceeded;
        }

        #endregion

        #region CapabilityService: Resource Management

        public void Dispose()
        {
            if (this.IsDisposed)
                return;

            this.IsDisposed = true;

            GC.SuppressFinalize(this);

            this.Logger.LogInformation("[CapabilityService] Disposed");
        }

        #endregion
    }
}