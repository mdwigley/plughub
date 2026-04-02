using NucleusAF.Interfaces.Models.Configuration;
using NucleusAF.Interfaces.Services.Configuration;
using NucleusAF.Services.Capabilities;

namespace NucleusAF.Models.Configuration
{
    public class ConfigSource(IConfigService configService, string sourceLocation, Type sourceType, Dictionary<string, object?> values, CapabilityValue? read, CapabilityValue? write) : IConfigSource
    {
        public IConfigService ConfigService { get; init; } = configService;

        #region ConfigSource: Source Information

        public Type SourceType { get; init; } = sourceType;
        public string SourceLocation { get; init; } = sourceLocation;
        public Dictionary<string, object?> Values { get; set; } = values;

        #endregion

        #region ConfigSource: Access Information

        public CapabilityValue Read { get; init; } = read ?? CapabilityValue.Public;
        public CapabilityValue Write { get; init; } = write ?? CapabilityValue.Blocked;

        #endregion

        public virtual void Dispose()
        {
            GC.SuppressFinalize(this);
        }
    }
}