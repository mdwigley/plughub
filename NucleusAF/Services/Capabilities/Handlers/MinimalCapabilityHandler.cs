using Microsoft.Extensions.Logging;
using NucleusAF.Interfaces.Abstractions.CompositeRegistry;
using NucleusAF.Interfaces.Models;
using NucleusAF.Interfaces.Services.Capabilities;
using NucleusAF.Interfaces.Services.Capabilities.Accessors;
using NucleusAF.Interfaces.Services.Capabilities.Handlers;
using NucleusAF.Models.Capabilities;

namespace NucleusAF.Services.Capabilities.Handlers
{
    public class MinimalCapabilityHandler : CapabilityHandler,
        IMinimalCapabilityHandler,
        ICapabilityHandler<IMinimalCapabilityHandler>,
        ICompositeRegistryHandlerFor<IMinimalCapabilityAccessor>
    {
        public override Type Key => typeof(IMinimalCapabilityHandler);

        public MinimalCapabilityHandler(ILogger<ICapabilityHandler> logger) : base(logger) => this.Logger.LogInformation("[MinimalCapabilityHandler] Initialized");

        #region MinimalCapabilityHandler: Registration Operations

        public override ICapabilityToken Register(IResourceKey resource, CapabilitySet capabilities, ICapabilityToken? token = null)
        {
            ArgumentNullException.ThrowIfNull(resource);
            ArgumentNullException.ThrowIfNull(capabilities);

            ICapabilityToken t = token ?? new CapabilityToken(Guid.NewGuid());
            CapabilityEntry newEntry = new(resource, new CapabilitySet(capabilities), t);

            if (!this.Capabilities.TryAdd(resource, newEntry))
            {
                this.Logger.LogWarning("[MinimalCapabilityHandler] Duplicate capability registration attempt for resource '{ResourceKey}'", resource);
                throw new InvalidOperationException($"A capability for resource '{resource}' is already registered. Duplicate registration is not allowed.");
            }

            return t;
        }

        public override void Unregister(IResourceKey resource, ICapabilityToken token)
        {
            ArgumentNullException.ThrowIfNull(resource);

            if (this.Capabilities.TryGetValue(resource, out CapabilityEntry entry))
            {
                if (entry.Token.Equals(token))
                {
                    this.Capabilities.TryRemove(resource, out _);

                    this.Logger.LogDebug("[MinimalCapabilityHandler] Unregistered capability for resource {Resource} with token {Token}", resource, token);
                }
                else
                {
                    this.Logger.LogWarning("[MinimalCapabilityHandler] Attempted to unregister capability for resource {Resource} with token {Token}, but the token did not match the registered token ({ActualToken}).", resource, token, entry.Token);
                }
            }
            else
            {
                this.Logger.LogWarning("[MinimalCapabilityHandler] Attempted to unregister capability for resource {Resource} with token {Token}, but no such resource was found.", resource, token);
            }
        }

        #endregion

        #region MinimalCapabilityHandler: Verifcation Operations

        public override bool IsAccessible(IResourceKey resource, int slot, ICapabilityToken? token = null)
        {
            ArgumentNullException.ThrowIfNull(resource);

            if (!this.Capabilities.TryGetValue(resource, out CapabilityEntry entry))
            {
                this.Logger.LogWarning("[MinimalCapabilityHandler] Unauthorized access attempt for resource '{ResourceKey}' slot {Slot}", resource, slot);
                return false;
            }

            if (entry.Slots.TryGetValue(slot, out CapabilityValue publicValue) && publicValue == CapabilityValue.Public)
                return true;

            if (token != null && entry.Token.Equals(token.Value))
                return entry.Slots.TryGetValue(slot, out CapabilityValue value) && value != CapabilityValue.Blocked;

            this.Logger.LogWarning("[MinimalCapabilityHandler] Unauthorized access attempt for resource '{ResourceKey}' slot {Slot}", resource, slot);
            return false;
        }
        public override bool IsOwner(IResourceKey resource, ICapabilityToken token)
        {
            ArgumentNullException.ThrowIfNull(resource);

            if (this.Capabilities.TryGetValue(resource, out CapabilityEntry entry))
                return entry.Token.Equals(token);

            this.Logger.LogWarning("[MinimalCapabilityHandler] Unauthorized access attempt for resource '{ResourceKey}'", resource);
            return false;
        }

        #endregion
    }
}