using System.Reflection;

namespace NucleusAF.Models.Modules
{
    /// <summary>
    /// Represents a specific implementation of a descriptor provider. Encapsulates the runtime assembly, the concrete class implementing the interface, and the interface type itself.
    /// </summary>
    /// <param name="Assembly">The assembly in which the implementation resides.</param>
    /// <param name="InterfaceType">The interface type being implemented by the module.</param>
    /// <param name="ImplementationType">The concrete type implementing the interface.</param>
    /// <remarks>
    /// Exposes properties to access descriptive metadata related to the assembly and types, supporting high-fidelity module discovery, enablement, and diagnostics.
    /// </remarks>
    public record ProviderInterface(Assembly Assembly, Type InterfaceType, Type ImplementationType)
    {
        /// <summary>
        /// The file system path of the assembly containing the implementation.
        /// </summary>
        public string AssemblyLocation => this.Assembly.Location;

        /// <summary>
        /// The simple name of the assembly containing the implementation.
        /// </summary>
        public string AssemblyName => this.Assembly.GetName().Name ?? string.Empty;

        /// <summary>
        /// The fully qualified name of the interface being implemented. Facilitates interface-based resolution and registration.
        /// </summary>
        public string InterfaceName => this.InterfaceType.FullName ?? this.InterfaceType.Name;

        /// <summary>
        /// The fully qualified name of the module's concrete implementation type. Used for instantiation and reflection-based operations.
        /// </summary>
        public string ImplementationName => this.ImplementationType.FullName ?? this.ImplementationType.Name;
    }

    /// <summary>
    /// Represents a fully discovered module, including its source assembly, primary type, binding metadata, and the set of its distinct interfaces.
    /// </summary>
    /// <param name="Assembly">The assembly from which the module was loaded.</param>
    /// <param name="Type">The primary class or entry-point type implementing the module.</param>
    /// <param name="Metadata">Strongly-typed metadata describing module identity and attributes.</param>
    /// <param name="Providers">A collection of descriptor providers offered by this module.</param>
    /// <remarks>
    /// Used throughout NucleusAF to persist module state, resolve manifests, determine enablement, and to drive loading and registration workflows.
    /// </remarks>
    public record ModuleReference(Assembly Assembly, Type Type, ModuleMetadata Metadata, IEnumerable<ProviderInterface> Providers)
    {
        /// <summary>
        /// The file system path of the assembly containing the module's entry point.
        /// </summary>
        public string AssemblyLocation => this.Assembly.Location;

        /// <summary>
        /// The simple name of the assembly containing the module's entry point.
        /// </summary>
        public string AssemblyName => this.Assembly.GetName().Name ?? string.Empty;

        /// <summary>
        /// The fully qualified name of the module's primary class or entry type. Used for loading, discovery, and manifest recording.
        /// </summary>
        public string TypeName => this.Type.FullName ?? this.Type.Name;
    }
}