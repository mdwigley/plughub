using Avalonia.Controls;
using NucleusAF.Avalonia.ViewModels;
using NucleusAF.Avalonia.ViewModels.Components;
using NucleusAF.Models;
using System;
using System.Collections.Generic;

namespace NucleusAF.Avalonia.Models.Descriptors
{
    /// <summary>
    /// Describes a module-provided page, including its view, view model, group, icon, 
    /// resource dictionaries, and optional menu bar configuration.
    /// Inherits from <see cref="ControlDescsriptor"/>.
    /// </summary>
    /// <param name="ModuleId">Unique identifier for the module providing this injector.</param>
    /// <param name="DescriptorId">Unique identifier for the descriptor.</param>
    /// <param name="Version">Version of the descriptor.</param>
    /// <param name="ViewType">The concrete type of the Avalonia <see cref="UserControl"/> representing the page's view.</param>
    /// <param name="ViewModelType">The type of the associated view model, typically derived from <see cref="ViewModelBase"/>.</param>
    /// <param name="Group">The logical group for this support page.</param>
    /// <param name="Name">The display name for the support page, as shown in the UI.</param>
    /// <param name="IconSource">A resource key or file path for the icon representing this page.</param>
    /// <param name="ViewFactory">A factory function that creates an instance of the page's <see cref="UserControl"/>. Receives an <see cref="IServiceProvider"/> for dependency injection.</param>
    /// <param name="ViewModelFactory">A factory function that creates an instance of the page's <see cref="ViewModelBase"/>. Receives an <see cref="IServiceProvider"/> for dependency injection.</param>
    /// <param name="LoadBefore">Descriptors that should be applied after this one to maintain order.</param>
    /// <param name="LoadAfter">Descriptors that should be applied before this one to maintain order.</param>
    /// <param name="DependsOn">Descriptors that this descriptor explicitly depends on.</param>
    /// <param name="ConflictsWith">Descriptors with which this descriptor cannot coexist.</param>
    public record DescriptorSettingsPage(
        Guid ModuleId,
        Guid DescriptorId,
        string Version,
        Type ViewType,
        Type ViewModelType,
        string Group,
        string Name,
        string IconSource,
        Func<IServiceProvider, UserControl>? ViewFactory = null,
        Func<IServiceProvider, BaseViewModel>? ViewModelFactory = null,
        IEnumerable<DescriptorReference>? LoadBefore = null,
        IEnumerable<DescriptorReference>? LoadAfter = null,
        IEnumerable<DescriptorReference>? DependsOn = null,
        IEnumerable<DescriptorReference>? ConflictsWith = null) : DescriptorPage(ModuleId, DescriptorId, Version, ViewType, ViewModelType, Name, IconSource, ViewFactory, ViewModelFactory, LoadBefore, LoadAfter, DependsOn, ConflictsWith)
    {
        /// <summary>
        /// Creates a <see cref="ContentItemViewModel"/> from the specified
        /// <see cref="DescriptorSettingsPage"/> using the provided <see cref="IServiceProvider"/>.
        /// </summary>
        /// <param name="provider">The service provider used to resolve the view, view model, and their dependencies.</param>
        /// <param name="descriptor">The descriptor containing type and factory information for the settings page.</param>
        /// <returns>
        /// A fully constructed <see cref="ContentItemViewModel"/> with its view and view model instantiated and bound, or <c>null</c> if either cannot be resolved.
        /// </returns>
        public static ContentItemViewModel? GetItemViewModel(IServiceProvider provider, DescriptorSettingsPage descriptor)
        {
            ArgumentNullException.ThrowIfNull(provider);
            ArgumentNullException.ThrowIfNull(descriptor);

            return DescriptorPage.GetItemViewModel(provider, descriptor);
        }
    }
}