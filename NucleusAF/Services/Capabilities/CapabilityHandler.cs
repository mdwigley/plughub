using Microsoft.Extensions.Logging;
using NucleusAF.Interfaces.Abstractions.CompositeRegistry;
using NucleusAF.Interfaces.Models;
using NucleusAF.Interfaces.Services.Capabilities;
using NucleusAF.Models.Capabilities;
using System.Collections.Concurrent;

namespace NucleusAF.Services.Capabilities
{
    public enum CapabilityValue
    {
        Blocked,
        Limited,
        Public
    }

    public abstract class CapabilityHandler(ILogger<ICapabilityHandler> logger) : IDisposable,
        ICompositeRegistryHandlerFor<ICapabilityAccessor>,
        ICapabilityRegistrar,
        ICapabilityHandler,
        ICapabilityHandler<ICapabilityHandler>
    {
        public readonly record struct CapabilityEntry(IResourceKey Resource, CapabilitySet Slots, ICapabilityToken Token);

        public virtual Type Key => typeof(ICapabilityHandler);

        protected readonly ILogger<ICapabilityHandler> Logger = logger;
        protected readonly ConcurrentDictionary<IResourceKey, CapabilityEntry> Capabilities = [];

        protected bool IsDisposed = false;

        #region CapabilityHandler: Predicate Operations

        public virtual bool IsRegistered(IResourceKey resource)
            => this.Capabilities.ContainsKey(resource);

        #endregion

        #region CapabilityHandler: Registration Operations

        public abstract ICapabilityToken Register(IResourceKey resource, CapabilitySet capabilities, ICapabilityToken? token = null);
        public abstract void Unregister(IResourceKey resource, ICapabilityToken token);

        #endregion

        #region CapabilityHandler: Verification Operations

        public abstract bool IsAccessible(IResourceKey resource, int slot, ICapabilityToken? token = null);
        public abstract bool IsOwner(IResourceKey resource, ICapabilityToken token);

        #endregion

        #region CapabilityHandler: Resource Management

        public virtual void Dispose()
        {
            if (this.IsDisposed)
                return;

            this.IsDisposed = true;

            GC.SuppressFinalize(this);

            this.Logger.LogDebug("[{HandlerType}] disposed", this.GetType().Name);
        }

        #endregion
    }
}