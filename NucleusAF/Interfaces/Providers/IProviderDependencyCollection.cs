using Microsoft.Extensions.DependencyInjection;
using NucleusAF.Attributes;
using NucleusAF.Models.Descriptors;

namespace NucleusAF.Interfaces.Providers
{
    /// <summary>
    /// Provider for descriptors that contribute service registrations directly to the host's <see cref="IServiceCollection"/> during materialization.
    /// </summary>
    [DescriptorProvider("GetCollectionDescriptors", false)]
    public interface IProviderDependencyCollection : IProvider
    {
        /// <summary>
        /// Returns one or more <see cref="DescriptorDependencyCollection"/> instances describing how this module wishes to configure the host's
        /// <see cref="IServiceCollection"/>.
        /// </summary>
        /// <remarks>Each descriptor encapsulates an action that mutates the service collection. The host will aggregate, order, and apply these descriptors before the final <see cref="IServiceProvider"/> is built.</remarks>
        IEnumerable<DescriptorDependencyCollection> GetCollectionDescriptors();
    }
}