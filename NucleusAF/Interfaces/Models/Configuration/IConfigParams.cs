using NucleusAF.Services.Capabilities;

namespace NucleusAF.Interfaces.Models.Configuration
{
    public interface IConfigParams
    {
        CapabilityValue Read { get; }
        CapabilityValue Write { get; }
    }
}