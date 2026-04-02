using Microsoft.Extensions.Configuration;
using System.Text.Json;

namespace NucleusAF.Interfaces.Models.Configuration.Sources
{
    public interface IJsonConfigSource : IConfigSource
    {
        #region IJsonConfigSource: Configuration Information

        /// <summary>
        /// The underlying hierarchical configuration interface representing the data.
        /// May be null if configuration is not hierarchical or unavailable.
        /// </summary>
        IConfiguration Configuration { get; init; }

        /// <summary>
        /// The JSON serialization options used when serializing or deserializing this configuration source.
        /// </summary>
        JsonSerializerOptions JsonSerializerOptions { get; init; }

        /// <summary>
        /// Whether this configuration source supports automatic reload on changes.
        /// </summary>
        bool ReloadOnChanged { get; init; }

        /// <summary>
        /// Disposable token representing subscription to change notifications on this source.
        /// Setting or disposing this will manage change callback registrations.
        /// </summary>
        IDisposable? OnChanged { get; set; }

        #endregion
    }
}