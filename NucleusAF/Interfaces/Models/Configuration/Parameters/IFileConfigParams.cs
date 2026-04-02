namespace NucleusAF.Interfaces.Models.Configuration.Parameters
{
    public interface IFileConfigParams : IConfigParams
    {
        string? ConfigUriOverride { get; }

        bool ReloadOnChange { get; }
    }
}