using Microsoft.Extensions.DependencyInjection;
using NucleusAF.Interfaces.Models;
using NucleusAF.Interfaces.Providers;
using NucleusAF.Mock.Interfaces.Services;
using NucleusAF.Mock.Services;
using NucleusAF.Models;
using NucleusAF.Models.Capabilities;
using NucleusAF.Models.Configuration.Parameters;
using NucleusAF.Models.Descriptors;
using NucleusAF.Models.Modules;
using NucleusAF.Services.Capabilities;

namespace NucleusAF.Mock
{
    /// <summary>
    /// Configuration model for the mock module demonstrating type-safe module configuration.
    /// </summary>
    public class NucleusMockConfig
    {
        public int AnswerToEverything { get; init; } = 42;
    }

    /// <summary>
    /// Demonstration module showcasing NucleusAF's multi-interface architecture.
    /// Implements branding (application identity override), configuration (token-secured configuration), and dependency injection (service provision to other modules) simultaneously.
    /// </summary>
    public class NucleusMock : ModuleBase, IProviderDependencyInjection, IProviderDependencyCollection, IProviderAppConfig, IProviderConfiguration
    {
        private readonly ICapabilityToken token =
            new CapabilityToken(Guid.Parse("eaba88d4-4d46-4e95-94df-3b8a6f7ef750"));

        #region NucleusMock: Key Fields

        public new static Guid ModuleId { get; } = Guid.Parse("8e4c7077-e57a-46fc-b4fa-15ebba04ee65");
        public new static string IconSource { get; } = "resm://NucleusAF.Mock/Assets/ic_fluent_chat_24_regular.png";
        public new static string Name { get; } = "NucleusAF: Mock Service";
        public new static string Description { get; } = "A mock module for nucleus diangostics.";
        public new static string Version { get; } = "0.2.0";
        public new static string Author { get; } = "Enterlucent";
        public new static List<string> Categories { get; } =
        [
            "Services",
            "Configuration",
        ];

        #endregion

        #region NucleusMock: Metadata

        public new static List<string> Tags { get; } =
        [
            "Module:Mock",
            "EchoService",
        ];
        public new static string DocsLink { get; } = "https://github.com/enterlucent/NucleusAF/wiki/";
        public new static string SupportLink { get; } = "https://support.enterlucent.com/NucleusAF/";
        public new static string SupportContact { get; } = "contact@enterlucent.com";
        public new static string License { get; } = "GNU Lesser General Public License v3";
        public new static string ChangeLog { get; } = "https://github.com/enterlucent/NucleusAF/releases/";

        #endregion

        #region NucleusMock: IProviderDependencyInjection

        /// <summary>
        /// Provides IEchoService with handler-based extensibility - other modules can implement 
        /// IEchoSuccessHandler or IEchoErrorHandler to extend service behavior.
        /// </summary>
        public IEnumerable<DescriptorDependencyInjection> GetInjectionDescriptors()
        {
            return [
                new DescriptorDependencyInjection(
                    ModuleId: ModuleId,
                    DescriptorId: Guid.Parse("34834c3e-313f-40b7-a8a1-ea021b74daa1"),
                    Version: Version,
                    InterfaceType: typeof(IEchoService),
                    ImplementationType: typeof(EchoService),
                    Lifetime: ServiceLifetime.Singleton,
                    LoadBefore: [],
                    LoadAfter: [],
                    ConflictsWith: [],
                    DependsOn: []),
            ];
        }

        #endregion

        #region NucleusMock: IProviderDependencyCollection

        public IEnumerable<DescriptorDependencyCollection> GetCollectionDescriptors()
        {
            return [
                new DescriptorDependencyCollection(
                    ModuleId: ModuleId,
                    DescriptorId: Guid.Parse("52890f6c-d7ef-46dd-8048-3c9a058f6c8c"),
                    Version: Version,
                    ConfigureAction: services =>
                    {
                        // Do work on the service collection directly
                    })
                ];
        }

        #endregion

        #region NucleusMock: IProviderAppConfig

        /// <summary>
        /// Demonstrates application configuration changes see: <see cref="AppConfig"/>.
        /// </summary>
        /// <remarks>
        /// Only applies during the **system-level load phase**
        /// </remarks>
        public IEnumerable<DescriptorAppConfig> GetAppConfigDescriptors()
        {
            return [
                new DescriptorAppConfig(
                    ModuleId: ModuleId,
                    DescriptorId: Guid.Parse("f26d3290-c059-4641-8280-d229ee2c2c32"),
                    Version: Version,
                    AppConfig: liveAppConfig => {
                        liveAppConfig.LoggingDirectory =
                            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MockAF", "Logging");
                        liveAppConfig.ConfigDirectory =
                            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MockAF", "Config");
                        liveAppConfig.StorageDirectory =
                            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MockAF", "Storage");
                    },
                    LoadBefore: [],
                    LoadAfter: [],
                    ConflictsWith: [],
                    DependsOn: [])
            ];
        }

        #endregion

        #region NucleusMock: IProviderConfiguration

        /// <summary>
        /// Demonstrates token-secured configuration where only the Owner token has write access, while others have read or no permissions.
        /// </summary>
        /// <remarks>
        /// The Owner token is created and cached in the module constructor for use in configuration access.
        /// </remarks>
        public IEnumerable<DescriptorConfiguration> GetConfigurationDescriptors()
        {
            return [
                new DescriptorConfiguration(
                    ModuleId: ModuleId,
                    DescriptorId: Guid.Parse("ddf7eb3a-6223-4016-bfb8-b4e0bba5a1c9"),
                    Version: Version,
                    ConfigType: typeof(NucleusMockConfig),
                    ConfigParams: new JsonConfigParams(
                        Read: CapabilityValue.Public,
                        Write: CapabilityValue.Blocked),
                    CapabilityToken: this.token,
                    LoadBefore: [],
                    LoadAfter: [],
                    ConflictsWith: [],
                    DependsOn: [])
            ];
        }

        #endregion
    }
}