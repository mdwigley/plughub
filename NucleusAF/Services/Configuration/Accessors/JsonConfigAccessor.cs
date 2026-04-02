using Microsoft.Extensions.Logging;
using NucleusAF.Interfaces.Models;
using NucleusAF.Interfaces.Services.Capabilities;
using NucleusAF.Interfaces.Services.Configuration;
using NucleusAF.Interfaces.Services.Configuration.Accessors;

namespace NucleusAF.Services.Configuration.Accessors
{
    public class JsonConfigAccessor(ILogger<IConfigAccessor> logger, ICapabilityService capabilityService)
        : ConfigAccessor(logger, capabilityService), IJsonConfigAccessor
    {
        public override Type Key => typeof(IJsonConfigAccessor);

        #region JsonConfigAccessor: Fluent Configuration API

        public override IJsonConfigAccessor SetConfigService(IConfigService? service = null)
            => (IJsonConfigAccessor)base.SetConfigService(service);
        public override IJsonConfigAccessor SetConfigHandler(IConfigHandler? handler = null)
            => (IJsonConfigAccessor)base.SetConfigHandler(handler);
        public override IJsonConfigAccessor SetAccess(ICapabilityToken? token = null)
            => (IJsonConfigAccessor)base.SetAccess(token);

        #endregion

        #region JsonConfigAccessor: Factory Methods

        public override IJsonConfigAccessorFor<TConfig> For<TConfig>() where TConfig : class
            => (IJsonConfigAccessorFor<TConfig>)base.For<TConfig>();
        public override IJsonConfigAccessorFor<TConfig> For<TConfig>(IConfigService configService, IConfigHandler configHandler, ICapabilityToken? token = null) where TConfig : class
            => new JsonConfigAccessorFor<TConfig>(this.Logger, configService, configHandler, this.CapabilityService, token);

        #endregion
    }

    public class JsonConfigAccessorFor<TConfig>(ILogger<IConfigAccessor> logger, IConfigService configService, IConfigHandler configHandler, ICapabilityService capabilityService, ICapabilityToken? token = null)
        : ConfigAccessorFor<TConfig>(logger, configService, configHandler, capabilityService, token), IJsonConfigAccessorFor<TConfig> where TConfig : class
    { }
}