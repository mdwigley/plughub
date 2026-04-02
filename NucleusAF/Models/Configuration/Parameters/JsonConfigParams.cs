using NucleusAF.Interfaces.Models.Configuration.Parameters;
using NucleusAF.Services.Capabilities;
using System.Text.Json;

namespace NucleusAF.Models.Configuration.Parameters
{
    /// <summary>
    /// Parameters used to configure a file-based configuration source.
    /// </summary>
    /// <param name="ConfigUriOverride">Optional override for the configuration file URI or path. If null, default paths will be used. </param>
    /// <param name="Read">Optional token for read access permissions.</param>
    /// <param name="Write">Optional token for write access permissions.</param>
    /// <param name="JsonSerializerOptions">Optional JSON serialization options to customize serialization/deserialization behavior.</param>
    /// <param name="ReloadOnChange">Indicates whether the configuration source should reload automatically when underlying data changes. Defaults to false.</param>
    public record JsonConfigParams(
        string? ConfigUriOverride = null,
        CapabilityValue Read = CapabilityValue.Public,
        CapabilityValue Write = CapabilityValue.Blocked,
        JsonSerializerOptions? JsonSerializerOptions = null,
        bool ReloadOnChange = false)
        : IJsonConfigParams;
}