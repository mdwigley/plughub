using Microsoft.Extensions.DependencyInjection;

namespace NucleusAF.Models.Descriptors
{
    /// <summary>
    /// Describes a module component that contributes directly to the host's <see cref="IServiceCollection"/>.
    /// Contains information required to apply service collection mutations,
    /// as well as dependency, conflict, and ordering relationships relative to other configurators.
    /// </summary>
    /// <param name="ModuleId">Unique identifier for the module providing this configurator.</param>
    /// <param name="DescriptorId">Unique identifier for the descriptor.</param>
    /// <param name="Version">Version of the descriptor.</param>
    /// <param name="ConfigureAction">Action that applies service registrations to the <see cref="IServiceCollection"/>.</param>
    /// <param name="LoadBefore">Descriptors that should be applied after this one to maintain order.</param>
    /// <param name="LoadAfter">Descriptors that should be applied before this one to maintain order.</param>
    /// <param name="DependsOn">Descriptors that this descriptor explicitly depends on.</param>
    /// <param name="ConflictsWith">Descriptors with which this descriptor cannot coexist.</param>
    public record DescriptorDependencyCollection(
        Guid ModuleId,
        Guid DescriptorId,
        string Version,
        Action<IServiceCollection> ConfigureAction,
        IEnumerable<DescriptorReference>? LoadBefore = null,
        IEnumerable<DescriptorReference>? LoadAfter = null,
        IEnumerable<DescriptorReference>? DependsOn = null,
        IEnumerable<DescriptorReference>? ConflictsWith = null) :
            Descriptor(ModuleId, DescriptorId, Version, LoadBefore, LoadAfter, DependsOn, ConflictsWith);
}