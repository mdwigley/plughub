using NucleusAF.Attributes;
using NucleusAF.Avalonia.Models.Descriptors;
using NucleusAF.Interfaces.Providers;
using System.Collections.Generic;

namespace NucleusAF.Avalonia.Interfaces.Providers
{
    /// <summary>
    /// Contract for modules that contribute main views (top-level pages) to the host.
    /// Provides descriptors describing the module's views and related metadata.
    /// </summary>
    [DescriptorProvider("GetMainViewDescriptors", false)]
    public interface IProviderMainView : IProvider
    {
        /// <summary>
        /// Returns descriptors for the main views provided by this module.
        /// </summary>
        IEnumerable<DescriptorPage> GetMainViewDescriptors();
    }
}