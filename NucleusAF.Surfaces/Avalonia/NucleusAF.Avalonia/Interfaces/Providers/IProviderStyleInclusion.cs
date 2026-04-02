using NucleusAF.Attributes;
using NucleusAF.Avalonia.Models.Descriptors;
using NucleusAF.Interfaces.Providers;
using System.Collections.Generic;


namespace NucleusAF.Avalonia.Interfaces.Providers
{
    /// <summary>
    /// Provider for descriptors that supply Avalonia StyleInclude resources.
    /// Provides descriptors for AXAML style files that need to be loaded during application bootstrap.
    /// </summary>
    [DescriptorProvider("GetStyleIncludeDescriptors", false)]
    public interface IProviderStyleInclusion : IProvider
    {
        /// <summary>
        /// Returns a collection of descriptors defining StyleInclude resources
        /// (AXAML files containing styles, themes, or resource dictionaries) offered by this module.
        /// </summary>
        IEnumerable<DescriptorStyleInclude> GetStyleIncludeDescriptors();
    }
}