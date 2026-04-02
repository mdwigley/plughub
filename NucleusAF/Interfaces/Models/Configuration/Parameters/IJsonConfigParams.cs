using System.Text.Json;

namespace NucleusAF.Interfaces.Models.Configuration.Parameters
{
    public interface IJsonConfigParams : IFileConfigParams
    {
        JsonSerializerOptions? JsonSerializerOptions { get; }
    }
}