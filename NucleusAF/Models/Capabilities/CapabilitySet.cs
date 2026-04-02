using NucleusAF.Services.Capabilities;

namespace NucleusAF.Models.Capabilities
{
    public class CapabilitySet : Dictionary<int, CapabilityValue>
    {
        public CapabilitySet() : base(new Dictionary<int, CapabilityValue>()) { }
        public CapabilitySet(Dictionary<int, CapabilityValue> values) : base(values) { }

        public void AddCapability(int id, CapabilityValue value) => this[id] = value;
    }
}