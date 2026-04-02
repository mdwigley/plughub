using NucleusAF.Attributes;
using NucleusAF.Avalonia.Models.Descriptors;
using NucleusAF.Interfaces.Providers;
using System.Collections.Generic;

namespace NucleusAF.Avalonia.Interfaces.Providers
{
    /// <summary>
    /// Provider for descriptors that supply Avalonia resource dictionaries.    
    /// </summary>
    [DescriptorProvider("GetResourceIncludeDescriptors", false)]
    public interface IProviderResourceInclusion : IProvider
    {
        /// <summary>
        /// Returns a collection of descriptors defining Avalonia resource dictionaries
        /// (AXAML files containing styles, themes, or other resources) offered by this module.
        /// </summary>
        IEnumerable<DescriptorResourceInclude> GetResourceIncludeDescriptors();
    }
}