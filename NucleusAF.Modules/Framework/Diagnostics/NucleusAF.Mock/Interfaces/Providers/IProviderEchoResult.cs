using NucleusAF.Attributes;
using NucleusAF.Interfaces.Providers;
using NucleusAF.Mock.Models.Descriptors;

namespace NucleusAF.Mock.Interfaces.Providers
{

    /// <summary>
    /// Module-level handler invoked on both successful echo and echo error.
    /// Supports extensibility of EchoService behavior by contributing a combined success/error processing pipeline.
    /// </summary>
    [DescriptorProvider("GetEchoResultDescriptors")]
    public interface IProviderEchoResult : IProvider
    {
        /// <summary>
        /// Returns the descriptors containing success and error handling actions contributed by this handler.
        /// Aggregation across module creates an extensible processing pipeline for echo results.
        /// </summary>
        List<DescriptorEchoResult> GetEchoResultDescriptors();
    }
}