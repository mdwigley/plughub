using Avalonia.Controls;
using NucleusAF.Avalonia.ViewModels;
using NucleusAF.Models;
using NucleusAF.Models.Descriptors;
using System;
using System.Collections.Generic;

namespace NucleusAF.Avalonia.Models.Descriptors
{
    /// <summary>
    /// Descriptor for a module-provided main view.
    /// Defines the view, view model, and optional factories,
    /// along with dependency, conflict, and ordering metadata.
    /// </summary>
    /// <param name="ModuleId">Unique identifier of the module providing this descriptor.</param>
    /// <param name="DescriptorId">Unique identifier of this descriptor.</param>
    /// <param name="Version">Version string of the descriptor.</param>
    /// <param name="Key">Identifier for this main view; combined with its ViewType to form a unique config key.</param>
    /// <param name="ViewType">Concrete Avalonia <see cref="UserControl"/> type for the view.</param>
    /// <param name="ViewModelType">Associated <see cref="BaseViewModel"/> type for the view.</param>
    /// <param name="ViewFactory">Optional factory to create the view instance.</param>
    /// <param name="ViewModelFactory">Optional factory to create the view model instance.</param>
    /// <param name="LoadBefore">Descriptors that should load after this one.</param>
    /// <param name="LoadAfter">Descriptors that should load before this one.</param>
    /// <param name="DependsOn">Descriptors this one depends on.</param>
    /// <param name="ConflictsWith">Descriptors that cannot coexist with this one.</param>
    public record DescriptorMainView(
        Guid ModuleId,
        Guid DescriptorId,
        string Version,
        string Key,
        Type ViewType,
        Type ViewModelType,
        Func<IServiceProvider, UserControl>? ViewFactory = null,
        Func<IServiceProvider, BaseViewModel>? ViewModelFactory = null,
        IEnumerable<DescriptorReference>? LoadBefore = null,
        IEnumerable<DescriptorReference>? LoadAfter = null,
        IEnumerable<DescriptorReference>? DependsOn = null,
        IEnumerable<DescriptorReference>? ConflictsWith = null
    ) : Descriptor(ModuleId, DescriptorId, Version, LoadBefore, LoadAfter, DependsOn, ConflictsWith);
}