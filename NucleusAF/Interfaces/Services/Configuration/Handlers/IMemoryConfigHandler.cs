namespace NucleusAF.Interfaces.Services.Configuration.Handlers
{
    /// <summary>
    /// Represents a memory-based configuration handler.
    /// Extends <see cref="IConfigHandler"/> without adding new members,
    /// serving as a specialization for scenarios where configuration
    /// values and instances are managed entirely in memory rather than
    /// persisted to disk or external storage.
    /// </summary>
    public interface IMemoryConfigHandler : IConfigHandler { }
}