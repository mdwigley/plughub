using Microsoft.Extensions.Configuration;
using NucleusAF.Interfaces.Models.Configuration.Sources;
using NucleusAF.Interfaces.Services.Configuration;
using NucleusAF.Services.Capabilities;
using System.Text.Json;

namespace NucleusAF.Models.Configuration.Sources
{
    public class JsonConfigSource(IConfigService configService, string sourceLocation, Type sourceType, IConfiguration configuration, Dictionary<string, object?> values, JsonSerializerOptions? jsonOptions, CapabilityValue? read, CapabilityValue? write, bool reloadOnChanged)
        : ConfigSource(configService, sourceLocation, sourceType, values, read, write), IJsonConfigSource
    {
        #region ConfigSource: Configuration Information

        public IConfiguration Configuration { get; init; } = configuration;
        public JsonSerializerOptions JsonSerializerOptions { get; init; } = jsonOptions ?? new JsonSerializerOptions();
        public bool ReloadOnChanged { get; init; } = reloadOnChanged;
        public IDisposable? OnChanged { get; set; }

        #endregion

        public override void Dispose()
        {
            try
            {
                this.OnChanged?.Dispose();
                this.OnChanged = null;
            }
            catch { /* nothing to see here*/ }

            GC.SuppressFinalize(this);

            base.Dispose();
        }
    }
}