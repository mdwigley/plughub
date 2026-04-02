using NucleusAF.Interfaces.Models.Configuration;
using NucleusAF.Services.Capabilities;

namespace NucleusAF.Models.Configuration.Parameters
{
    /// <summary>
    /// Parameters used to configure a memory-based configuration source.
    /// </summary>
    /// <param name="Read">Optional token for read access permissions.</param>
    /// <param name="Write">Optional token for write access permissions.</param>
    public record MemoryConfigParams(
        CapabilityValue Read = CapabilityValue.Public,
        CapabilityValue Write = CapabilityValue.Blocked) : IConfigParams;
}