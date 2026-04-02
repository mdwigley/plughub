using NucleusAF.Attributes;
using NucleusAF.Avalonia.Models.Descriptors;
using NucleusAF.Interfaces.Providers;
using System.Collections.Generic;

namespace NucleusAF.Avalonia.Interfaces.Providers
{
    /// <summary>
    /// Provider for descriptors that supply runtime environment configuration.
    /// </summary>
    [DescriptorProvider("GetAppEnvDescriptors")]
    public interface IProviderAppEnv : IProvider
    {
        /// <summary>
        /// Returns a collection of descriptors defining environment configuration and initialization steps (such as environment variables, runtime options, or configuration adjustments) offered by this module.
        /// </summary>
        IEnumerable<DescriptorAppEnv> GetAppEnvDescriptors();
    }
}