using NucleusAF.Attributes;
using NucleusAF.Models.Descriptors;

namespace NucleusAF.Interfaces.Providers
{
    /// <summary>
    /// Provider for descriptors that register configuration options.
    /// </summary>
    [DescriptorProvider("GetConfigurationDescriptors", false)]
    public interface IProviderConfiguration : IProvider
    {
        /// <summary>
        /// Returns a collection of descriptors representing the configuration settings exposed by this module.
        /// </summary>
        IEnumerable<DescriptorConfiguration> GetConfigurationDescriptors();
    }
}