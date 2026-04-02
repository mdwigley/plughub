namespace NucleusAF.Interfaces.Services.Configuration.Handlers
{
    /// <summary>
    /// Represents a secure JSON-based configuration handler.
    /// Extends <see cref="IConfigHandler"/> without adding new members,
    /// serving as a specialization for scenarios where configuration
    /// values and instances must be persisted to JSON files with
    /// encryption applied for confidentiality and integrity.
    /// </summary>
    public interface ISecureJsonConfigHandler : IJsonConfigHandler { }
}