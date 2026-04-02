namespace NucleusAF.Models.Modules
{
    public class DescriptorLoadState(Guid moduleId = default, string assemblyName = "Unknown", string providerName = "Unknown", string className = "Unknown", bool enabled = false, bool system = false, int loadOrder = int.MaxValue)
    {
        public DescriptorLoadState()
            : this(default, "Unknown", "Unknown", "Unknown", false, false, int.MaxValue) { }

        public Guid ModuleId { get; set; } = moduleId;
        public string AssemblyName { get; set; } = assemblyName;
        public string ProviderName { get; set; } = providerName;
        public string ClassName { get; set; } = className;
        public bool System { get; set; } = system;
        public int LoadOrder { get; set; } = loadOrder;
        public bool Enabled { get; set; } = enabled;
    }
    public class ModuleManifest
    {
        public List<DescriptorLoadState> DescriptorStates { get; set; } = [];
    }
}