using Microsoft.Extensions.Logging;
using NucleusAF.Interfaces.Services.Capabilities;
using NucleusAF.Interfaces.Services.Capabilities.Accessors;

namespace NucleusAF.Services.Capabilities.Accessors
{
    public class MinimalCapabilityAccessor(ILogger<ICapabilityAccessor> logger)
        : CapabilityAccessor(logger), IMinimalCapabilityAccessor
    {
        public override Type Key { get; } = typeof(IMinimalCapabilityAccessor);

        #region MinimalCapabilityAccessor: Fluent Configuration API

        public new virtual IMinimalCapabilityAccessor SetCapabilityService(ICapabilityService service)
            => (IMinimalCapabilityAccessor)base.SetCapabilityService(service);
        public new virtual IMinimalCapabilityAccessor SetCapabilityHandler(ICapabilityHandler handler)
            => (IMinimalCapabilityAccessor)base.SetCapabilityHandler(handler);

        #endregion

        #region MinimalCapabilityAccessor: Factory Methods

        public override ICapabilityAccessorFor<THandler> For<THandler>() where THandler : class
            => base.For<THandler>();
        public override ICapabilityAccessorFor<THandler> For<THandler>(ICapabilityService service, ICapabilityHandler handler) where THandler : class
            => new MinimalCapabilityAccessorFor<THandler>(this.Logger, service, handler);

        #endregion
    }
    public class MinimalCapabilityAccessorFor<THandler>(ILogger<ICapabilityAccessor> logger, ICapabilityService service, ICapabilityHandler handler)
        : CapabilityAccessorFor<THandler>(logger, service, handler) where THandler : class
    { }
}