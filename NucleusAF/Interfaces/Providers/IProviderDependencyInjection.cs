using NucleusAF.Attributes;
using NucleusAF.Models.Descriptors;

namespace NucleusAF.Interfaces.Providers
{
    /// <summary>
    /// Provider for descriptors that participate in dependency injection.
    /// </summary>
    [DescriptorProvider("GetInjectionDescriptors", false)]
    public interface IProviderDependencyInjection : IProvider
    {
        /// <summary>
        /// Returns a collection of descriptors detailing the dependency injection points offered by this module.
        /// </summary>
        IEnumerable<DescriptorDependencyInjection> GetInjectionDescriptors();
    }
}