using NucleusAF.Interfaces.Services.Modules;
using NucleusAF.Models.Modules;

namespace NucleusAF.Services.Modules
{
    public sealed class ModuleCache(IEnumerable<ModuleReference> modules) : IModuleCache
    {
        private readonly List<ModuleReference> modules = [.. modules];

        public IEnumerable<ModuleReference> Modules
            => this.modules.AsReadOnly();
    }
}