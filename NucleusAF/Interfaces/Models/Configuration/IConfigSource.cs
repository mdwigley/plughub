using NucleusAF.Interfaces.Services.Configuration;
using NucleusAF.Services.Capabilities;

namespace NucleusAF.Interfaces.Models.Configuration
{
    public interface IConfigSource : IDisposable
    {
        /// <summary>
        /// The configuration service that owns or manages this source.
        /// </summary>
        IConfigService ConfigService { get; init; }

        #region IConfigSource: Source Information

        /// <summary>
        /// The type of the configuration object this source provides values for.
        /// </summary>
        Type SourceType { get; init; }

        /// <summary>
        /// The location or identifier of this configuration source, e.g., a file path or URI.
        /// </summary>
        string SourceLocation { get; init; }

        /// <summary>
        /// The cached dictionary of configuration values keyed by setting name.
        /// The values are raw deserialized objects representing the config data.
        /// </summary>
        Dictionary<string, object?> Values { get; set; }

        #endregion

        #region IConfigSource: Access Information

        /// <summary>
        /// Security token for read access.
        /// </summary>
        CapabilityValue Read { get; init; }

        /// <summary>
        /// Security token for write access.
        /// </summary>
        CapabilityValue Write { get; init; }

        #endregion
    }
}