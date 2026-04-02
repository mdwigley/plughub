using Microsoft.Extensions.Logging;
using NucleusAF.Interfaces.Models;
using NucleusAF.Interfaces.Services.Capabilities;
using NucleusAF.Interfaces.Services.Configuration;
using NucleusAF.Interfaces.Services.Configuration.Accessors;

namespace NucleusAF.Services.Configuration.Accessors
{
    public class MemoryConfigAccessor(ILogger<IConfigAccessor> logger, ICapabilityService capabilityService)
        : ConfigAccessor(logger, capabilityService), IMemoryConfigAccessor
    {
        public override Type Key => typeof(IMemoryConfigAccessor);

        #region MemoryConfigAccessor: Fluent Configuration API

        public override IMemoryConfigAccessor SetConfigService(IConfigService? service = null)
            => (IMemoryConfigAccessor)base.SetConfigService(service);
        public override IMemoryConfigAccessor SetConfigHandler(IConfigHandler? handler = null)
            => (IMemoryConfigAccessor)base.SetConfigHandler(handler);
        public override IMemoryConfigAccessor SetAccess(ICapabilityToken? token = null)
            => (IMemoryConfigAccessor)base.SetAccess(token);

        #endregion

        #region MemoryConfigAccessor: Factory Methods

        public override IMemoryConfigAccessorFor<TConfig> For<TConfig>() where TConfig : class
            => (IMemoryConfigAccessorFor<TConfig>)base.For<TConfig>();
        public override IMemoryConfigAccessorFor<TConfig> For<TConfig>(IConfigService configService, IConfigHandler configHandler, ICapabilityToken? token = null) where TConfig : class
            => new MemoryConfigAccessorFor<TConfig>(this.Logger, configService, configHandler, this.CapabilityService, token);

        #endregion
    }

    public class MemoryConfigAccessorFor<TConfig>(ILogger<IConfigAccessor> logger, IConfigService configService, IConfigHandler configHandler, ICapabilityService capabilityService, ICapabilityToken? token = null)
    : ConfigAccessorFor<TConfig>(logger, configService, configHandler, capabilityService, token), IMemoryConfigAccessorFor<TConfig> where TConfig : class
    { }
}