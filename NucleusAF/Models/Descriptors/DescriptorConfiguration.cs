using NucleusAF.Interfaces.Models;
using NucleusAF.Interfaces.Models.Configuration;
using NucleusAF.Interfaces.Services.Configuration;

namespace NucleusAF.Models.Descriptors
{
    /// <summary>
    /// Describes a module component that registers configuration options, schema, or settings with the host system.
    /// Integrates full dependency graph metadata for controlled initialization order, and provides explicit service parameterization.
    /// If a <c>RegisterAction</c> is defined, it takes precedence over the static configuration values.
    /// </summary>
    /// <param name="ModuleId">Unique identifier for the module providing this descriptor.</param>
    /// <param name="DescriptorId">Unique identifier for the descriptor.</param>
    /// <param name="Version">Version of the descriptor.</param>
    /// <param name="ConfigType">Strongly typed POCO schema used for module configuration.</param>
    /// <param name="ConfigParams">Configuration parameters chosen by this module.</param>
    /// <param name="CapabilityToken">Capability token required for registration.</param>
    /// <param name="ConfigServiceAction">An action that, when provided an IConfigService, performs the registration logic for this descriptor.</param>
    /// <param name="LoadBefore">Descriptors that should be applied after this one to maintain order.</param>
    /// <param name="LoadAfter">Descriptors that should be applied before this one to maintain order.</param>
    /// <param name="DependsOn">Descriptors that this descriptor explicitly depends on.</param>
    /// <param name="ConflictsWith">Descriptors with which this descriptor cannot coexist.</param>
    public record DescriptorConfiguration(
        Guid ModuleId,
        Guid DescriptorId,
        string Version,
        Type? ConfigType = null,
        IConfigParams? ConfigParams = null,
        ICapabilityToken? CapabilityToken = null,
        Action<IConfigService>? ConfigServiceAction = null,
        IEnumerable<DescriptorReference>? LoadBefore = null,
        IEnumerable<DescriptorReference>? LoadAfter = null,
        IEnumerable<DescriptorReference>? DependsOn = null,
        IEnumerable<DescriptorReference>? ConflictsWith = null
    ) : Descriptor(ModuleId, DescriptorId, Version, LoadBefore, LoadAfter, DependsOn, ConflictsWith);
}