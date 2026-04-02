using NucleusAF.Attributes;
using NucleusAF.Avalonia.Models.Descriptors;
using NucleusAF.Interfaces.Providers;
using System.Collections.Generic;

namespace NucleusAF.Avalonia.Interfaces.Providers
{
    /// <summary>
    /// Provider for descriptors that provide settings or configuration pages.
    /// </summary>
    [DescriptorProvider("GetSettingsPageDescriptors", false)]
    public interface IProviderSettingsPages : IProvider
    {
        /// <summary>
        /// Gets the list of settings pages provided by the module.
        /// Each <see cref="DescriptorSettingsPage"/> describes a single page, including its view, view model, and metadata.
        /// </summary>
        public List<DescriptorSettingsPage> GetSettingsPageDescriptors();
    }
}