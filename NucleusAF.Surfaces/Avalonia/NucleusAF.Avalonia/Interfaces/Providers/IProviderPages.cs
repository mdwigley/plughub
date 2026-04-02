using NucleusAF.Attributes;
using NucleusAF.Avalonia.Models.Descriptors;
using NucleusAF.Interfaces.Providers;
using System.Collections.Generic;

namespace NucleusAF.Avalonia.Interfaces.Providers
{
    /// <summary>
    /// Provider for descriptors that provide pages to the host application.
    /// </summary>
    [DescriptorProvider("GetPageDescriptors", false)]
    public interface IProviderPages : IProvider
    {
        /// <summary>
        /// Returns a collection of descriptors detailing the pages provided by this module.
        /// </summary>
        public IEnumerable<DescriptorPage> GetPageDescriptors();
    }
}