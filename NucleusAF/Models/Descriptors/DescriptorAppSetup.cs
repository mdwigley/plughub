namespace NucleusAF.Models.Descriptors
{
    /// <summary>
    /// Describes a module component that provides runtime initialization or setup logic during application startup using the built service provider.
    /// Declares all dependency and ordering relationships for conflict-free, deterministic integration of runtime setup tasks.
    /// </summary>
    /// <param name="ModuleId">Unique identifier for the module providing this injector.</param>
    /// <param name="DescriptorId">Unique identifier for the descriptor.</param>
    /// <param name="Version">Version of the descriptor.</param>
    /// <param name="AppSetup">Action invoked during application startup to perform runtime initialization or setup tasks using the provided <see cref="IServiceProvider"/>. This allows the module to resolve services and configure runtime behavior after DI container construction.</param>
    /// <param name="LoadBefore">Descriptors that should be applied after this one to maintain order.</param>
    /// <param name="LoadAfter">Descriptors that should be applied before this one to maintain order.</param>
    /// <param name="DependsOn">Descriptors that this descriptor explicitly depends on.</param>
    /// <param name="ConflictsWith">Descriptors with which this descriptor cannot coexist.</param>
    public record DescriptorAppSetup(
        Guid ModuleId,
        Guid DescriptorId,
        string Version,
        Action<IServiceProvider>? AppSetup = null,
        IEnumerable<DescriptorReference>? LoadBefore = null,
        IEnumerable<DescriptorReference>? LoadAfter = null,
        IEnumerable<DescriptorReference>? DependsOn = null,
        IEnumerable<DescriptorReference>? ConflictsWith = null) :
            Descriptor(ModuleId, DescriptorId, Version, LoadBefore, LoadAfter, DependsOn, ConflictsWith);
}