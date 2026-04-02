using NucleusAF.Interfaces.Models;

namespace NucleusAF.Models.Capabilities
{
    public readonly record struct CapabilityToken(Guid Value) : ICapabilityToken
    {
        public static readonly CapabilityToken None = new(Guid.Empty);

        public bool IsNone => this.Value == Guid.Empty;
    }
}