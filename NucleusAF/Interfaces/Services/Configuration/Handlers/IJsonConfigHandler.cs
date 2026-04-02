namespace NucleusAF.Interfaces.Services.Configuration.Handlers
{
    /// <summary>
    /// Represents a JSON-based configuration handler.
    /// Extends <see cref="IConfigHandler"/> without adding new members,
    /// serving as a specialization for scenarios where configuration
    /// values and instances are persisted to and retrieved from JSON files.
    /// </summary>
    public interface IJsonConfigHandler : IConfigHandler { }
}