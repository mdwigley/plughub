using NucleusAF.Attributes;
using NucleusAF.Models.Descriptors;

namespace NucleusAF.Interfaces.Providers
{
    /// <summary>
    /// Provider for descriptors that supply base application (host) configuration.
    /// </summary>
    [DescriptorProvider("GetAppConfigDescriptors", true)]
    public interface IProviderAppConfig : IProvider
    {
        /// <summary>
        /// Returns a collection of descriptors defining application configuration.
        /// </summary>
        IEnumerable<DescriptorAppConfig> GetAppConfigDescriptors();
    }
}