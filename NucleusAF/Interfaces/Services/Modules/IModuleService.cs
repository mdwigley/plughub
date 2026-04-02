using NucleusAF.Attributes;
using NucleusAF.Models.Modules;

namespace NucleusAF.Interfaces.Services.Modules
{
    /// <summary>
    /// Provides core module management operations for discovery and dynamic loading in the NucleusAF system.
    /// </summary>
    public interface IModuleService
    {
        /// <summary>
        /// Scans the specified directory for eligible module assemblies, discovers available modules, and returns their descriptors.
        /// This method loads metadata only and does not instantiate module types.
        /// </summary>
        /// <param name="moduleDirectory">The file system directory to scan for module assemblies.</param>
        /// <returns>A collection of <see cref="ModuleReference"/> objects representing each discovered module and its providers.</returns>
        IEnumerable<ModuleReference> Discover(string moduleDirectory);

        /// <summary>
        /// Retrieves the <see cref="DescriptorProviderAttribute"/> applied to the specified interface by its full name.
        /// Searches all loaded assemblies for the interface type and returns the attribute instance if found; otherwise, returns null.
        /// </summary>
        /// <param name="interfaceFullName">The full name (namespace + interface name) of the interface to inspect.</param>
        /// <returns>The <see cref="DescriptorProviderAttribute"/> instance if found on the interface; otherwise, null.</returns>
        DescriptorProviderAttribute? GetDescriptorProviderAttribute(string interfaceFullName);

        /// <summary>
        /// Loads and returns an implementation instance for a given provider, cast to the specified type.
        /// Searches for and loads the concrete module, then attempts to cast the result to the desired interface.
        /// Returns <c>null</c> if the module cannot be loaded or does not implement the requested interface.
        /// </summary>
        /// <typeparam name="TInterface">The target interface to retrieve; must be a class interface.</typeparam>
        /// <param name="provider">Descriptor of the provider to instantiate.</param>
        /// <returns>An instance implementing <typeparamref name="TInterface"/>, or <c>null</c> if not available.</returns>
        TInterface? GetLoadedProviders<TInterface>(ProviderInterface provider) where TInterface : class;

        /// <summary>
        /// Loads and returns a provider for the specified interface, cast to the provided strongly-typed module base class.
        /// Returns <c>null</c> if the module cannot be loaded or does not derive from <typeparamref name="TModule"/>.
        /// </summary>
        /// <typeparam name="TModule">The expected module type.</typeparam>
        /// <param name="provider">The provider to load the module from</param>
        /// <returns>An instance of <typeparamref name="TModule"/>, or <c>null</c> if not available.</returns>
        TModule? GetLoadedModule<TModule>(ProviderInterface provider) where TModule : ModuleBase;
    }
}
