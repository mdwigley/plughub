using NucleusAF.Mock.Interfaces.Services;
using NucleusAF.Models;
using NucleusAF.Models.Descriptors;

namespace NucleusAF.Mock.Models.Descriptors
{
    /// <summary>
    /// Descriptor providing executable processing logic for both success and error events in EchoService.
    /// Bridges IEchoResultHandler interface contracts with actual Action delegates.
    /// </summary>
    public record DescriptorEchoResult(
        Guid ModuleId,
        Guid DescriptorId,
        string Version,
        Action<MessageReceivedEventArgs, IEchoService>? ProcessSuccess = null,
        Action<MessageErrorEventArgs, IEchoService>? ProcessError = null,
        IEnumerable<DescriptorReference>? LoadBefore = null,
        IEnumerable<DescriptorReference>? LoadAfter = null,
        IEnumerable<DescriptorReference>? DependsOn = null,
        IEnumerable<DescriptorReference>? ConflictsWith = null
    ) : Descriptor(ModuleId, DescriptorId, Version, LoadBefore, LoadAfter, DependsOn, ConflictsWith);
}