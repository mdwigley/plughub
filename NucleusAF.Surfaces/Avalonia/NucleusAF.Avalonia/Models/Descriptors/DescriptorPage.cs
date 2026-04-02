using Avalonia.Controls;
using Microsoft.Extensions.DependencyInjection;
using NucleusAF.Avalonia.Interfaces.Providers;
using NucleusAF.Avalonia.ViewModels;
using NucleusAF.Avalonia.ViewModels.Components;
using NucleusAF.Interfaces.Services.Modules;
using NucleusAF.Models;
using NucleusAF.Models.Descriptors;
using System;
using System.Collections.Generic;

namespace NucleusAF.Avalonia.Models.Descriptors
{
    /// <summary>
    /// Describes a module-provided page, including its view, view model, icon, 
    /// and dependency relationships relative to other pages.
    /// Contains type and factory information required to create page instances,
    /// as well as dependency, conflict, and ordering relationships relative to other page providers.
    /// </summary>
    /// <param name="ModuleId">Unique identifier for the module providing this injector.</param>
    /// <param name="DescriptorId">Unique identifier for the descriptor.</param>
    /// <param name="Version">Version of the descriptor.</param>
    /// <param name="ViewType">The concrete type of the Avalonia UserControl for the page.</param>
    /// <param name="ViewModelType">The type of the associated ViewModel.</param>
    /// <param name="Name">Display name for the page.</param>
    /// <param name="IconSource">Resource key or path for the page icon.</param>
    /// <param name="ViewFactory">Factory function to create the UserControl instance.</param>
    /// <param name="ViewModelFactory">Factory function to create the ViewModel instance.</param>
    /// <param name="LoadBefore">Descriptors that should be applied after this one to maintain order.</param>
    /// <param name="LoadAfter">Descriptors that should be applied before this one to maintain order.</param>
    /// <param name="DependsOn">Descriptors that this descriptor explicitly depends on.</param>
    /// <param name="ConflictsWith">Descriptors with which this descriptor cannot coexist.</param>
    public record DescriptorPage(
            Guid ModuleId,
            Guid DescriptorId,
            string Version,

            Type ViewType,
            Type ViewModelType,
            string Name,
            string IconSource,
            Func<IServiceProvider, UserControl>? ViewFactory = null,
            Func<IServiceProvider, BaseViewModel>? ViewModelFactory = null,

            IEnumerable<DescriptorReference>? LoadBefore = null,
            IEnumerable<DescriptorReference>? LoadAfter = null,
            IEnumerable<DescriptorReference>? DependsOn = null,
            IEnumerable<DescriptorReference>? ConflictsWith = null) : Descriptor(ModuleId, DescriptorId, Version, LoadBefore, LoadAfter, DependsOn, ConflictsWith)
    {
        /// <summary>
        /// Creates a <see cref="ContentItemViewModel"/> instance from the given <see cref="DescriptorPage"/> using the provided <see cref="IServiceProvider"/>.
        /// </summary>
        /// <param name="provider">The service provider used to resolve the view, view model, and any dependencies.</param>
        /// <param name="descriptor">The descriptor containing type and factory information for the page.</param>
        /// <returns>
        /// A fully constructed <see cref="ContentItemViewModel"/> with its <see cref="Controls.UserControl"/> and <see cref="BaseViewModel"/> instantiated and bound together, or <c>null</c> if either cannot be resolved.
        /// </returns>
        public static ContentItemViewModel? GetItemViewModel(IServiceProvider provider, DescriptorPage descriptor)
        {
            ArgumentNullException.ThrowIfNull(provider);
            ArgumentNullException.ThrowIfNull(descriptor);

            IModuleResolver moduleResolver = provider.GetRequiredService<IModuleResolver>();
            IEnumerable<IProviderPages> pageProviders = provider.GetServices<IProviderPages>();

            IReadOnlyList<DescriptorPage> orderedDescriptors =
                moduleResolver.ResolveAndOrder<IProviderPages, DescriptorPage>(pageProviders);

            UserControl? view;
            BaseViewModel? viewModel;

            #region DescriptorPage: Resolve View

            if (descriptor.ViewFactory != null)
            {
                view = descriptor.ViewFactory(provider);
            }
            else if (descriptor.ViewType != null)
            {
                view = provider.GetService(descriptor.ViewType) as UserControl;
                if (view is null) return null;
            }
            else return null;

            #endregion

            #region DescriptorPage: Resolve ViewModel

            if (descriptor.ViewModelFactory != null)
            {
                viewModel = descriptor.ViewModelFactory(provider);
            }
            else if (descriptor.ViewModelType != null)
            {
                viewModel = provider.GetService(descriptor.ViewModelType) as BaseViewModel;
                if (viewModel is null) return null;
            }
            else return null;

            #endregion

            ContentItemViewModel item = new(descriptor.ViewType!, descriptor.ViewModelType!, descriptor.Name, descriptor.IconSource)
            {
                Control = view,
                ViewModel = viewModel
            };
            view.DataContext = viewModel;

            return item;
        }
    }
}