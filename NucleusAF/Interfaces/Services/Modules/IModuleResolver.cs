using NucleusAF.Attributes;
using NucleusAF.Models.Descriptors;
using NucleusAF.Services.Modules;

namespace NucleusAF.Interfaces.Services.Modules
{
    /// <summary>
    /// Resolves descriptors into a stable, dependency-valid, conflict-free load order.
    /// The resolver ensures that results are deterministic and safe for initialization, accounting for
    /// all declared dependencies, ordering constraints, and exclusions. Resolution is always performed
    /// at the provider level, supporting manifest-driven systems and pluggable load strategies.
    /// </summary>
    public interface IModuleResolver
    {
        /// <summary>
        /// Resolves descriptors for the given provider by:<br/>
        /// 1. Using the interface’s <see cref="DescriptorProviderAttribute"/> to locate the descriptor accessor.<br/>
        /// 2. Invoking the accessor on each provided module instance to collect descriptors.<br/>
        /// 3. Performing dependency resolution and topological sort.<br/>
        /// 4. Applying the interface’s declared <see cref="DescriptorSortContext"/>.
        /// </summary>
        /// <typeparam name="TInterface">The provider type annotated with <see cref="DescriptorProviderAttribute"/>.</typeparam>
        /// <typeparam name="TDescriptor">The descriptor type returned by the accessor; must inherit from <see cref="Descriptor"/>.</typeparam>
        /// <param name="modules">The module implementing <typeparamref name="TInterface"/>.</param>
        /// <returns>A deterministic, dependency-valid, conflict-free, and correctly ordered sequence of descriptors.</returns>
        IReadOnlyList<TDescriptor> ResolveAndOrder<TInterface, TDescriptor>(IEnumerable<TInterface> modules) where TInterface : class where TDescriptor : Descriptor;

        /// <summary>
        /// Resolves descriptors by:<br/>
        /// 1. Performing dependency resolution, conflict detection, and duplicate filtering.<br/>
        /// 2. Executing a deterministic topological sort over the valid descriptor set.<br/>
        /// 3. Applying the specified <see cref="DescriptorSortContext"/> to produce the final order.
        /// </summary>
        /// <typeparam name="TDescriptor">The descriptor type being resolved; must inherit from <see cref="Descriptor"/>.</typeparam>
        /// <param name="descriptors">The descriptors to resolve and sort.</param>
        /// <param name="sortContext">The ordering rule controlling whether the sorted output is used as-is, reversed, or left unordered.</param>
        /// <returns>A deterministic, dependency-valid, conflict-free, and correctly ordered sequence of descriptors.</returns>
        IReadOnlyList<TDescriptor> ResolveAndOrder<TDescriptor>(IEnumerable<TDescriptor> descriptors, DescriptorSortContext sortContext) where TDescriptor : Descriptor;

        /// <summary>
        /// Builds a full resolution context for the given set of module descriptors,
        /// preserving both valid order information and any rejection reasons.  
        /// </summary>
        /// <typeparam name="TDescriptor">The descriptor type being resolved; must inherit from <see cref="Descriptor"/>.</typeparam>
        /// <param name="descriptors">The collection of descriptors to evaluate for ordering and conflicts.</param>
        /// <returns>A <see cref="DescriptorResolutionContext{TDescriptor}"/> containing deterministic ordering results along with detailed state about rejected or disabled descriptors.</returns>
        DescriptorResolutionContext<TDescriptor> ResolveContext<TDescriptor>(IEnumerable<TDescriptor> descriptors) where TDescriptor : Descriptor;

        /// <summary>
        /// Returns a dependency- and conflict-aware, stable ordering of descriptors.
        /// Sorting operates on individual providers, assembling an order that respects each descriptor’s
        /// declared dependencies, ordering constraints, and conflict exclusions. Unsatisfiable or conflicting
        /// descriptors are filtered out. The resulting list preserves the same order on every run for the
        /// same input, ensuring predictability and safe module initialization.
        /// </summary>
        /// <typeparam name="TDescriptor">The descriptor type being resolved and ordered; must inherit from <see cref="Descriptor"/>.</typeparam>
        /// <param name="descriptors">The collection of descriptors to process and order.</param>
        /// <returns>A deterministic, dependency-valid, and conflict-free sequence of descriptors, sorted for correct load.</returns>
        IEnumerable<TDescriptor> ResolveDescriptors<TDescriptor>(IEnumerable<TDescriptor> descriptors) where TDescriptor : Descriptor;
    }
}