using Avalonia.Controls;
using NucleusAF.Models;
using NucleusAF.Models.Descriptors;
using System;
using System.Collections.Generic;

namespace NucleusAF.Avalonia.Models.Descriptors
{
    /// <summary>
    /// Describes a module component that provides Avalonia resource dictionaries (AXAML files)
    /// that must be loaded before the main UI initializes.
    /// Declares all dependency and ordering relationships for conflict-free, deterministic resource loading.
    /// </summary>
    /// <param name="ModuleId">Unique identifier for the module providing this descriptor.</param>
    /// <param name="DescriptorId">Unique identifier for the descriptor.</param>
    /// <param name="Version">Version of the descriptor.</param>
    /// <param name="ResourceUri">URI of the AXAML resource to be loaded as a ResourceInclude.</param>
    /// <param name="BaseUri">Base URI for resolving relative resource paths (defaults to module's base URI).</param>
    /// <param name="Factory">Optional delegate that creates one or more <see cref="IResourceDictionary"/> or <see cref="IResourceProvider"/> instances at runtime.</param>
    /// <param name="LoadBefore">Descriptors that should be applied after this one to maintain order.</param>
    /// <param name="LoadAfter">Descriptors that should be applied before this one to maintain order.</param>
    /// <param name="DependsOn">Descriptors that this descriptor explicitly depends on.</param>
    /// <param name="ConflictsWith">Descriptors with which this descriptor cannot coexist.</param>
    public record DescriptorResourceInclude(
        Guid ModuleId,
        Guid DescriptorId,
        string Version,
        string? ResourceUri = null,
        string? BaseUri = null,
        Func<IResourceDictionary>? Factory = null,
        IEnumerable<DescriptorReference>? LoadBefore = null,
        IEnumerable<DescriptorReference>? LoadAfter = null,
        IEnumerable<DescriptorReference>? DependsOn = null,
        IEnumerable<DescriptorReference>? ConflictsWith = null
    ) : Descriptor(ModuleId, DescriptorId, Version, LoadBefore, LoadAfter, DependsOn, ConflictsWith);
}