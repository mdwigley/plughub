namespace NucleusAF.Models.Descriptors
{
    /// <summary>
    /// Describes a module component that provides access to read/modify <see cref="NecluesAF.AppConfig"/> at startup..
    /// </summary>
    /// <param name="ModuleId">Unique identifier for the module providing this injector.</param>
    /// <param name="DescriptorId">Unique identifier for the descriptor.</param>
    /// <param name="Version">Version of the descriptor.</param>
    /// <param name="LoadBefore">Descriptors that should be applied after this one to maintain order.</param>
    /// <param name="LoadAfter">Descriptors that should be applied before this one to maintain order.</param>
    /// <param name="DependsOn">Descriptors that this descriptor explicitly depends on.</param>
    /// <param name="ConflictsWith">Descriptors with which this descriptor cannot coexist.</param>
    public record DescriptorAppConfig(
        Guid ModuleId,
        Guid DescriptorId,
        string Version,
        Action<AppConfig>? AppConfig = null,
        IEnumerable<DescriptorReference>? LoadBefore = null,
        IEnumerable<DescriptorReference>? LoadAfter = null,
        IEnumerable<DescriptorReference>? DependsOn = null,
        IEnumerable<DescriptorReference>? ConflictsWith = null) :
            Descriptor(ModuleId, DescriptorId, Version, LoadBefore, LoadAfter, DependsOn, ConflictsWith);
}