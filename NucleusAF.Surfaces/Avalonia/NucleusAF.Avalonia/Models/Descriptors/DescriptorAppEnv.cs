using NucleusAF.Models;
using NucleusAF.Models.Descriptors;
using System;
using System.Collections.Generic;

namespace NucleusAF.Avalonia.Models.Descriptors
{
    /// <summary>
    /// Describes a module component that provides environment configuration settings (such as runtime options, environment-specific variables, or initialization logic).
    /// Declares all dependency and ordering relationships for conflict-free, deterministic environment setup.
    /// </summary>
    /// <param name="ModuleId">Unique identifier for the module providing this injector.</param>
    /// <param name="DescriptorId">Unique identifier for the descriptor.</param>
    /// <param name="Version">Version of the descriptor.</param>
    /// <param name="AppEnv">Optional action to apply modifications to the runtime environment configuration.</param>
    /// <param name="LoadBefore">Descriptors that should be applied after this one to maintain order.</param>
    /// <param name="LoadAfter">Descriptors that should be applied before this one to maintain order.</param>
    /// <param name="DependsOn">Descriptors that this descriptor explicitly depends on.</param>
    /// <param name="ConflictsWith">Descriptors with which this descriptor cannot coexist.</param>
    public record DescriptorAppEnv(
        Guid ModuleId,
        Guid DescriptorId,
        string Version,
        Action<AppEnv>? AppEnv = null,
        IEnumerable<DescriptorReference>? LoadBefore = null,
        IEnumerable<DescriptorReference>? LoadAfter = null,
        IEnumerable<DescriptorReference>? DependsOn = null,
        IEnumerable<DescriptorReference>? ConflictsWith = null) :
            Descriptor(ModuleId, DescriptorId, Version, LoadBefore, LoadAfter, DependsOn, ConflictsWith);
}