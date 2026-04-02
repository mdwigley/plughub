using Microsoft.Extensions.DependencyInjection;
using NucleusAF.Interfaces.Providers;
using NucleusAF.Models;
using NucleusAF.Models.Descriptors;
using NucleusAF.Models.Modules;
using System.Collections;

namespace NucleusAF.Mock
{
    /// <summary>
    /// Demonstrates a module that declares a dependency on another module's service interface.
    /// This is used to test dependency resolution and loading order within the module system.
    /// </summary>
    /// <remarks>
    /// The dependency is expressed via the <see cref="DependsOn"/> collection, indicating that this module requires another module to be loaded first.
    /// If the dependency is missing or incompatible, this module should not load.
    /// </remarks>
    public class NucleusMockDepend : ModuleBase, IProviderDependencyInjection
    {
        #region NucleusMockDepend: Key Fields

        public new static Guid ModuleId => new("ed35d473-f898-4efb-85d1-0a5d2d03fc01");
        public new static string IconSource => "resm://NucleusAF.Mock/Assets/ic_fluent_link_24_filled.png";
        public new static string Name => "NucleusAF: Mock Depend";
        public new static string Description => "";
        public new static string Version => "0.2.0";
        public new static string Author => "Enterlucent";
        public new static List<string> Categories { get; } = [
            "Diagnostics",
        ];

        #endregion

        #region NucleusMockDepend: Metadata

        public new static string DocsLink => "https://enterlucent.com";
        public new static string SupportLink => "https://enterlucent.com";
        public new static string SupportContact => "contact@enterlucent.com";
        public new static string ChangeLog => "https://enterlucent.com";

        #endregion

        #region NucleusMockDepend: IProviderDependencyInjection

        public IEnumerable<DescriptorDependencyInjection> GetInjectionDescriptors()
        {
            return [
                new DescriptorDependencyInjection(
                    ModuleId: ModuleId,
                    DescriptorId: Guid.Parse("f1ab6f62-74c2-4be2-af05-62e3fe54f349"),
                    Version: Version,
                    InterfaceType: typeof(IList),
                    ImplementationType: typeof(object),
                    Lifetime: ServiceLifetime.Singleton,
                    LoadBefore: [],
                    LoadAfter: [],
                    ConflictsWith: [],
                    DependsOn: [
                        new DescriptorReference(
                            Guid.Parse("8557da82-b6fc-4be1-8731-e32e449285ab"),
                            Guid.Parse("d018bc99-6156-46e5-ae7c-cf1f2c0255f0"),
                            "0.2.0",
                            "1.0.0"
                        )
                    ]
                )
            ];
        }

        #endregion
    }

    /// <summary>
    /// Demonstrates a module that explicitly conflicts with another module’s implementation.
    /// </summary>
    /// <remarks>
    /// The conflict is declared in the <see cref="ConflictsWith"/> collection to prevent incompatible modules from loading together.
    /// If conflicting modules are detected, the system avoids loading this module to maintain stability.
    /// </remarks>
    public class NucleusMockConflict : ModuleBase, IProviderDependencyInjection
    {
        #region NucleusMockConflict: Key Fields

        public new static Guid ModuleId => Guid.Parse("026e0a9b-8b53-4e7f-837d-026dd60d8882");
        public new static string IconSource => "resm://NucleusAF.Mock/Assets/ic_fluent_link_dismiss_24_filled.png";
        public new static string Name => "NucleusAF: Mock Conflict";
        public new static string Description => "";
        public new static string Version => "0.2.0";
        public new static string Author => "Enterlucent";
        public new static List<string> Categories { get; } = [
            "Diagnostics",
        ];

        #endregion

        #region NucleusMockConflict: Metadata

        public new static string DocsLink => "https://enterlucent.com";
        public new static string SupportLink => "https://enterlucent.com";
        public new static string SupportContact => "contact@enterlucent.com";
        public new static string ChangeLog => "https://enterlucent.com";

        #endregion

        #region NucleusMockConflict: IProviderDependencyInjection

        public IEnumerable<DescriptorDependencyInjection> GetInjectionDescriptors()
        {
            return [
                new DescriptorDependencyInjection(
                    ModuleId: ModuleId,
                    DescriptorId: Guid.Parse("41c8c019-3b8e-436a-bfe2-5682913b0ce8"),
                    Version: Version,
                    InterfaceType: typeof(IList),
                    ImplementationType: typeof(object),
                    Lifetime: ServiceLifetime.Singleton,
                    LoadBefore: [],
                    LoadAfter: [],
                    ConflictsWith: [
                        new DescriptorReference(
                            Guid.Parse("ed35d473-f898-4efb-85d1-0a5d2d03fc01"),
                            Guid.Parse("f1ab6f62-74c2-4be2-af05-62e3fe54f349"),
                            "0.0.0",
                            "1.0.0"
                        )
                    ],
                    DependsOn: [])
            ];
        }

        #endregion
    }

    /// <summary>
    /// Represents an abstract module class that should never be loaded or instantiated as a module.
    /// </summary>
    /// <remarks>
    /// Abstract base classes are excluded from loading since they cannot provide concrete implementations or descriptors.
    /// This serves as an example of how non-instantiable modules are handled by the system.
    /// </remarks>
    public abstract class NucleusMockAbstract : ModuleBase
    {
        #region NucleusMockDepend: Key Fields

        public new static Guid ModuleId => Guid.Parse("ae270d96-3f99-4b19-94ec-ecd61d2ecd83");
        public new static string IconSource => "";
        public new static string Name => "NucleusAF: Mock Abstract";
        public new static string Description => "This module should never load as it's an abstract class.";
        public new static string Version => "0.2.0";
        public new static string Author => "Enterlucent";
        public new static List<string> Categories { get; } = [
            "Diagnostics",
        ];

        #endregion

        #region NucleusMockDepend: Metadata

        public new static string DocsLink => "https://enterlucent.com";
        public new static string SupportLink => "https://enterlucent.com";
        public new static string SupportContact => "contact@enterlucent.com";
        public new static string ChangeLog => "https://enterlucent.com";

        #endregion
    }

    /// <summary>
    /// Module that implements no providers and thus provides no extension points or contributions.
    /// </summary>
    /// <remarks>
    /// Modules without implemented providers are considered invalid or inert and should not be loaded by the system.
    /// This demonstrates the requirement that a module must expose at least one extension interface to be active.
    /// </remarks>
    public class NucleusMockNoInterfaces : ModuleBase
    {
        #region NucleusMockNoInterfaces: Key Fields

        public new static Guid ModuleId => Guid.Parse("45bc53be-bff0-4f46-ad13-d483004cd8c8");
        public new static string IconSource => "";
        public new static string Name => "NucleusAF: Mock No Interfaces";
        public new static string Description => "This module should never load as it implements no providers.";
        public new static string Version => "0.2.0";
        public new static string Author => "Enterlucent";
        public new static List<string> Categories { get; } = [
            "Diagnostics",
        ];

        #endregion

        #region NucleusMockNoInterfaces: Metadata

        public new static string DocsLink => "https://enterlucent.com";
        public new static string SupportLink => "https://enterlucent.com";
        public new static string SupportContact => "contact@enterlucent.com";
        public new static string ChangeLog => "https://enterlucent.com";

        #endregion
    }

    /// <summary>
    /// Demonstrates a module with a duplicate ModuleId matching another module.
    /// </summary>
    /// <remarks>
    /// This module shares its ModuleId with NucleusMockNoInterfaces, which causes identity conflicts.
    /// The system requires each module to have a unique ModuleId to avoid ambiguity during loading and management.
    /// </remarks>
    public class NucleusMockDuplicateA : ModuleBase, IProviderDependencyInjection
    {
        #region NucleusMockDuplicate: Key Fields

        public new static Guid ModuleId => Guid.Parse("45bc53be-bff0-4f46-ad13-d483004cd8c8");
        public new static string IconSource => "";
        public new static string Name => "NucleusAF: Mock Duplicate A";
        public new static string Description => "This module is a duplicate of no interfaces.";
        public new static string Version => "0.2.0";
        public new static string Author => "Enterlucent";
        public new static List<string> Categories { get; } = [
            "Diagnostics",
        ];

        #endregion

        #region NucleusMockDuplicate: Metadata

        public new static string DocsLink => "https://enterlucent.com";
        public new static string SupportLink => "https://enterlucent.com";
        public new static string SupportContact => "contact@enterlucent.com";
        public new static string ChangeLog => "https://enterlucent.com";

        #endregion

        #region NucleusMockDuplicate: IProviderDependencyInjection

        public IEnumerable<DescriptorDependencyInjection> GetInjectionDescriptors()
        {
            return [
                new DescriptorDependencyInjection(
                    ModuleId: ModuleId,
                    DescriptorId: Guid.Parse("416c0a5a-1d0c-41f6-9703-be0263003278"),
                    Version: Version,
                    InterfaceType: typeof(IList),
                    ImplementationType: typeof(object),
                    Lifetime: ServiceLifetime.Singleton,
                    LoadBefore: [],
                    LoadAfter: [],
                    ConflictsWith: [],
                    DependsOn: []
                )
            ];
        }

        #endregion
    }

    /// <summary>
    /// Demonstrates a module with a duplicate ModuleId matching NucleusMockDuplicateA but with a unique descriptor.
    /// </summary>
    /// <remarks>
    /// Although this module has a unique descriptor, its ModuleId duplicates NucleusMockDuplicateA.
    /// Duplicate ModuleId must be avoided for consistent module identification and conflict-free loading.
    /// </remarks>
    public class NucleusMockDuplicateB : ModuleBase, IProviderDependencyInjection
    {
        #region NucleusMockDuplicate: Key Fields

        public new static Guid ModuleId => Guid.Parse("45bc53be-bff0-4f46-ad13-d483004cd8c8");
        public new static string IconSource => "";
        public new static string Name => "NucleusAF: Mock Duplicate B";
        public new static string Description => "This module is a duplicate of NucleusMockDuplicateA module with unique descriptor.";
        public new static string Version => "0.2.0";
        public new static string Author => "Enterlucent";
        public new static List<string> Categories { get; } = [
            "Diagnostics",
        ];

        #endregion

        #region NucleusMockDuplicate: Metadata

        public new static string DocsLink => "https://enterlucent.com";
        public new static string SupportLink => "https://enterlucent.com";
        public new static string SupportContact => "contact@enterlucent.com";
        public new static string ChangeLog => "https://enterlucent.com";

        #endregion

        #region NucleusMockDuplicate: IProviderDependencyInjection

        public IEnumerable<DescriptorDependencyInjection> GetInjectionDescriptors()
        {
            return [
                new DescriptorDependencyInjection(
                    ModuleId: ModuleId,
                    DescriptorId: Guid.Parse("f1ab6f62-74c2-4be2-af05-62e3fe54f349"),
                    Version: Version,
                    InterfaceType: typeof(IList),
                    ImplementationType: typeof(object),
                    Lifetime: ServiceLifetime.Singleton,
                    LoadBefore: [],
                    LoadAfter: [],
                    ConflictsWith: [],
                    DependsOn: []
                )
            ];
        }

        #endregion
    }

    /// <summary>
    /// Demonstrates a module with a unique ModuleId but a duplicate DescriptorId matching NucleusMockDuplicateB.
    /// </summary>
    /// <remarks>
    /// This module’s descriptor shares the same DescriptorId as NucleusMockDuplicateB, causing conflicts in service registration.
    /// DescriptorIds must be unique to ensure proper management of module services and extensions.
    /// </remarks>
    public class NucleusMockDuplicateC : ModuleBase, IProviderDependencyInjection
    {
        #region NucleusMockDuplicate: Key Fields

        public new static Guid ModuleId => Guid.Parse("018ed126-faf0-4298-91c8-97dca8ad597f");
        public new static string IconSource => "";
        public new static string Name => "NucleusAF: Mock Duplicate C";
        public new static string Description => "This module is a duplicate of NucleusMockDuplicateB's descriptor.";
        public new static string Version => "0.2.0";
        public new static string Author => "Enterlucent";
        public new static List<string> Categories { get; } = [
            "Diagnostics",
        ];

        #endregion

        #region NucleusMockDuplicate: Metadata

        public new static string DocsLink => "https://enterlucent.com";
        public new static string SupportLink => "https://enterlucent.com";
        public new static string SupportContact => "contact@enterlucent.com";
        public new static string ChangeLog => "https://enterlucent.com";

        #endregion

        #region NucleusMockDuplicate: IProviderDependencyInjection

        public IEnumerable<DescriptorDependencyInjection> GetInjectionDescriptors()
        {
            return [
                new DescriptorDependencyInjection(
                    ModuleId: ModuleId,
                    DescriptorId: Guid.Parse("f1ab6f62-74c2-4be2-af05-62e3fe54f349"),
                    Version: Version,
                    InterfaceType: typeof(IList),
                    ImplementationType: typeof(object),
                    Lifetime: ServiceLifetime.Singleton,
                    LoadBefore: [],
                    LoadAfter: [],
                    ConflictsWith: [],
                    DependsOn: []
                )
            ];
        }

        #endregion
    }
}
