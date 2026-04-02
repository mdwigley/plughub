namespace NucleusAF.Models.Descriptors
{
    /// <summary>
    /// Base descriptor that defines an extension point within NucleusAF.
    /// </summary>
    public abstract record Descriptor(
        Guid ModuleId,
        Guid DescriptorId,
        string Version,
        IEnumerable<DescriptorReference>? LoadBefore = null,
        IEnumerable<DescriptorReference>? LoadAfter = null,
        IEnumerable<DescriptorReference>? DependsOn = null,
        IEnumerable<DescriptorReference>? ConflictsWith = null);
}