using NucleusAF.Attributes;
using NucleusAF.Models.Descriptors;

namespace NucleusAF.Interfaces.Providers
{
    /// <summary>
    /// Provider for descriptors that supply runtime initialization or setup logic at application startup.
    /// </summary>
    [DescriptorProvider("GetAppSetupDescriptors", false)]
    public interface IProviderAppSetup : IProvider
    {
        /// <summary>
        /// Returns a collection of descriptors defining runtime setup actions that are invoked during application startup with the built <see cref="IServiceProvider"/>. These actions enable modules to perform initialization, configuration, or event wiring.
        /// </summary>
        IEnumerable<DescriptorAppSetup> GetAppSetupDescriptors();
    }
}