namespace NucleusAF.Interfaces.Services.Capabilities.Handlers
{
    /// <summary>
    /// Represents the minimal capability handler contract.
    /// Extends <see cref="ICapabilityHandler"/> without adding new members,
    /// serving as a lightweight specialization for scenarios where only
    /// basic capability verification is required.
    /// </summary>
    public interface IMinimalCapabilityHandler : ICapabilityHandler { }
}