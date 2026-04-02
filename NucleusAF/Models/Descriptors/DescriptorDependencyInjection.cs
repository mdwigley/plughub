using Microsoft.Extensions.DependencyInjection;

namespace NucleusAF.Models.Descriptors
{
    /// <summary>
    /// Describes a module component that supports dependency injection.
    /// Contains type and lifetime information required to register services with a dependency injection container,
    /// as well as dependency, conflict, and ordering relationships relative to other injectors.
    /// </summary>
    /// <param name="ModuleId">Unique identifier for the module providing this injector.</param>
    /// <param name="DescriptorId">Unique identifier for the descriptor.</param>
    /// <param name="Version">Version of the descriptor.</param>
    /// <param name="InterfaceType">The interface type to be injected.</param>
    /// <param name="ImplementationType">Optional explicit implementation type; if null, Instance is used.</param>
    /// <param name="ImplementationFactory">Optional factory delegate to create your service with full DI support.</param>
    /// <param name="Lifetime">Registration lifetime (singleton, scoped, transient) for this injector.</param>
    /// <param name="LoadBefore">Descriptors that should be applied after this one to maintain order.</param>
    /// <param name="LoadAfter">Descriptors that should be applied before this one to maintain order.</param>
    /// <param name="DependsOn">Descriptors that this descriptor explicitly depends on.</param>
    /// <param name="ConflictsWith">Descriptors with which this descriptor cannot coexist.</param>
    public record DescriptorDependencyInjection(
        Guid ModuleId,
        Guid DescriptorId,
        string Version,
        Type InterfaceType,
        Type? ImplementationType = null,
        Func<IServiceProvider, object?>? ImplementationFactory = null,
        ServiceLifetime Lifetime = ServiceLifetime.Singleton,
        IEnumerable<DescriptorReference>? LoadBefore = null,
        IEnumerable<DescriptorReference>? LoadAfter = null,
        IEnumerable<DescriptorReference>? DependsOn = null,
        IEnumerable<DescriptorReference>? ConflictsWith = null) :
            Descriptor(ModuleId, DescriptorId, Version, LoadBefore, LoadAfter, DependsOn, ConflictsWith);
}