using NucleusAF.Models.Descriptors;
using NucleusAF.Models.Modules;

namespace NucleusAF.Interfaces.Services.Modules
{
    /// <summary>
    /// Manages the enabled/disabled state of modules and their descriptors within the NucleusAF system.
    /// Responsible for persisting state changes to the module manifest, and providing metadata about module extension points.
    /// </summary>
    public interface IModuleRegistrar
    {
        /// <summary>
        /// Retrieves the list of module descriptors associated with a specified provider type.
        /// These descriptors define how various modules implement or extend the given provider.
        /// </summary>
        /// <param name="providerType">The <see cref="Type"/> representing a module's descriptor provider. Must be assignable to <see cref="IProvider"/>.</param>
        /// <returns>A <see cref="List{Descriptor}"/> containing metadata descriptors for modules that interact with the specified provider type.</returns>
        public List<Descriptor> GetDescriptorsForProvider(Type providerType);

        /// <summary>
        /// Retrieves the current module manifest representing the global descriptors enable state.
        /// </summary>
        /// <returns>The <see cref="ModuleManifest"/> instance that records descriptors enable status.</returns>
        public ModuleManifest GetManifest();

        /// <summary>
        /// Persists the given <see cref="ModuleManifest"/> to the underlying storage.
        /// </summary>
        /// <param name="manifest">The <see cref="ModuleManifest"/> instance containing updated descriptors states to write.</param>
        /// <remarks>
        /// This is the synchronous entry point.  
        /// It delegates to <see cref="SaveManifestAsync(ModuleManifest)"/> but blocks on completion.  
        /// Use this method only when calling code cannot be async (e.g., constructors, legacy APIs).  
        /// For new code paths, prefer the async version to avoid blocking the thread.
        /// </remarks>
        public void SaveManifest(ModuleManifest manifest);

        /// <summary>
        /// Asynchronously persists the given <see cref="ModuleManifest"/> to the underlying storage.
        /// </summary>
        /// <param name="manifest">The <see cref="ModuleManifest"/> instance containing updated descriptors states to write.</param>
        /// <remarks>This is the async-first implementation. It should be preferred in most scenarios as it does not block the calling thread.</remarks>
        public Task SaveManifestAsync(ModuleManifest manifest);

        /// <summary>
        /// Determines whether a specific provider is currently enabled for a given moduleId.
        /// </summary>
        /// <param name="moduleId">The <see cref="Guid"/> uniquely identifying the module.</param>
        /// <param name="providerType">The <see cref="Type"/> of the provider.</param>
        /// <returns><c>true</c> if the specified provider on the module is enabled; otherwise, <c>false</c>.</returns>
        public bool IsEnabled(Guid moduleId, Type providerType);

        /// <summary>
        /// Sets the enabled or disabled state of a specific provider for a specified module.
        /// </summary>
        /// <param name="moduleId">The unique <see cref="Guid"/> of the module.</param>
        /// <param name="providerType">The <see cref="Type"/> of the provider to modify.</param>
        /// <param name="enabled"><c>true</c> to enable the provider; <c>false</c> to disable it. Defaults to <c>true</c>.
        /// </param>
        public void SetEnabled(Guid moduleId, Type providerType, bool enabled = true);

        /// <summary>
        /// Sets all providers of a given module to an enabled or disabled state.
        /// </summary>
        /// <param name="moduleId">The unique <see cref="Guid"/> of the module.</param>
        /// <param name="enabled"><c>true</c> to enable all providers; <c>false</c> to disable them. Defaults to <c>true</c>.
        /// </param>
        public void SetAllEnabled(Guid moduleId, bool enabled = true);
    }
}
