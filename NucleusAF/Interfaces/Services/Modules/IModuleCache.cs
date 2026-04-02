using NucleusAF.Models.Modules;

namespace NucleusAF.Interfaces.Services.Modules
{
    public interface IModuleCache
    {
        IEnumerable<ModuleReference> Modules { get; }
    }
}