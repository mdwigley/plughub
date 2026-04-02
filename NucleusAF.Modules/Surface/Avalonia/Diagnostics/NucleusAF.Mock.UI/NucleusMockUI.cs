using Microsoft.Extensions.DependencyInjection;
using NucleusAF.Avalonia.Interfaces.Providers;
using NucleusAF.Avalonia.Models;
using NucleusAF.Avalonia.Models.Descriptors;
using NucleusAF.Interfaces.Models;
using NucleusAF.Interfaces.Providers;
using NucleusAF.Interfaces.Services.Configuration;
using NucleusAF.Mock.UI.ViewModels;
using NucleusAF.Mock.UI.Views;
using NucleusAF.Mock.ViewModels;
using NucleusAF.Mock.Views;
using NucleusAF.Models;
using NucleusAF.Models.Capabilities;
using NucleusAF.Models.Descriptors;
using NucleusAF.Models.Modules;

namespace NucleusAF.Mock.UI
{
    /// <summary>
    /// Demonstration module showcasing NucleusAF's multi-interface architecture.
    /// Implements branding (application identity override), configuration (token-secured configuration), and dependency injection (service provision to other modules) simultaneously.
    /// </summary>
    public class NucleusMockUI : ModuleBase, IProviderDependencyInjection, IProviderAppEnv, IProviderResourceInclusion, IProviderStyleInclusion, IProviderPages, IProviderSettingsPages
    {
        private readonly ICapabilityToken token =
            new CapabilityToken(Guid.Parse("eaba88d4-4d46-4e95-94df-3b8a6f7ef750"));

        private readonly DescriptorReference mockConfig =
            new(Guid.Parse("8e4c7077-e57a-46fc-b4fa-15ebba04ee65"),
                Guid.Parse("ddf7eb3a-6223-4016-bfb8-b4e0bba5a1c9"),
                "0.1.0",
                "0.9.0");

        private readonly DescriptorReference mockService =
            new(Guid.Parse("8e4c7077-e57a-46fc-b4fa-15ebba04ee65"),
                Guid.Parse("34834c3e-313f-40b7-a8a1-ea021b74daa1"),
                "0.1.0",
                "0.9.0");

        #region NucleusMock: Key Fields

        public new static Guid ModuleId { get; } = Guid.Parse("9c5ad98e-bfb5-461c-9f1d-b7dae9365baf");
        public new static string IconSource { get; } = "avares://NucleusAF.Mock.UI/Assets/ic_fluent_chat_24_regular.png";
        public new static string Name { get; } = "NucleusAF: Mock Service UI";
        public new static string Description { get; } = "A mock module for nucleus avalonia diangostics.";
        public new static string Version { get; } = "0.2.0";
        public new static string Author { get; } = "Enterlucent";
        public new static List<string> Categories { get; } =
        [
            "Pages",
        ];

        #endregion

        #region NucleusMock: Metadata

        public new static List<string> Tags { get; } =
        [
            "Module:MockUI",
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
                    DescriptorId: Guid.Parse("f3be5c56-1d3e-45a7-a104-487f8484d115"),
                    Version: Version,
                    InterfaceType: typeof(MockPageView),
                    ImplementationType: typeof(MockPageView),
                    Lifetime: ServiceLifetime.Singleton,
                    LoadBefore: [],
                    LoadAfter: [],
                    ConflictsWith: [],
                    DependsOn: []),
                new DescriptorDependencyInjection(
                    ModuleId: ModuleId,
                    DescriptorId: Guid.Parse("8ad501c2-8171-47cb-b861-535307cf6754"),
                    Version: Version,
                    InterfaceType: typeof(MockPageViewModel),
                    ImplementationType: typeof(MockPageViewModel),
                    Lifetime: ServiceLifetime.Singleton,
                    LoadBefore: [],
                    LoadAfter: [],
                    ConflictsWith: [],
                    DependsOn: []),
                new DescriptorDependencyInjection(
                    ModuleId: ModuleId,
                    DescriptorId: Guid.Parse("b89aa1d6-9415-4194-98ee-4dcb8f608757"),
                    Version: Version,
                    InterfaceType: typeof(MockSettingsView),
                    ImplementationType: typeof(MockSettingsView),
                    Lifetime: ServiceLifetime.Singleton,
                    LoadBefore: [],
                    LoadAfter: [],
                    ConflictsWith: [],
                    DependsOn: []),
            ];
        }

        #endregion

        #region NucleusMock: IProviderAppEnv

        /// <summary>
        /// Demonstrates application environment changes see: <see cref="AppEnv"/>.
        /// </summary>
        public IEnumerable<DescriptorAppEnv> GetAppEnvDescriptors()
        {
            return [
                new DescriptorAppEnv(
                    ModuleId: ModuleId,
                    DescriptorId: Guid.Parse("ba543295-88e8-474a-b370-74594dcc76a8"),
                    Version: Version,
                    AppEnv: liveAppEnv => {
                        liveAppEnv.WindowTitle = "MockAF";
                        liveAppEnv.WindowIconPath = "avares://NucleusAF.Mock.UI/Assets/alien-bob.png";
                        liveAppEnv.AppName = "MockAF";
                        liveAppEnv.AppIconPath = "avares://NucleusAF.Mock.UI/Assets/alien-bob.png";
                    },
                    LoadBefore: [],
                    LoadAfter: [],
                    ConflictsWith: [],
                    DependsOn: [])
            ];
        }

        #endregion

        #region NucleusMock: IProviderResourceInclusion

        /// <summary>
        /// Demonstrates how a module can contribute XAML resources (icons, themes, control templates) that are merged into the host application.
        /// This allows modules to visually extend or customize the UI consistently.
        /// </summary>
        public IEnumerable<DescriptorResourceInclude> GetResourceIncludeDescriptors()
        {
            return [
                new DescriptorResourceInclude(
                    ModuleId: ModuleId,
                    DescriptorId: Guid.Parse("f9e88050-b8c3-43b3-b76e-79f16595acff"),
                    Version: Version,
                    "avares://NucleusAF.Mock.UI/Themes/FluentAvalonia/Theme.axaml",
                    LoadBefore: [],
                    LoadAfter: [],
                    ConflictsWith: [],
                    DependsOn: []
                ),
            ];
        }

        #endregion

        #region NucleusMock: IProviderStyleInclusion

        /// <summary>
        /// Demonstrates how a module can contribute XAML styles that are merged into the host application.
        /// This allows modules to visually extend or customize the UI consistently.
        /// </summary>
        public IEnumerable<DescriptorStyleInclude> GetStyleIncludeDescriptors()
        {
            return [
                new DescriptorStyleInclude(
                    ModuleId: ModuleId,
                    DescriptorId: Guid.Parse("f9e88050-b8c3-43b3-b76e-79f16595acff"),
                    Version: Version,
                    "avares://NucleusAF.Mock.UI/Themes/FluentAvalonia/Style.axaml",
                    LoadBefore: [],
                    LoadAfter: [],
                    ConflictsWith: [],
                    DependsOn: []
                ),
            ];
        }

        #endregion

        #region NucleusMock: IProviderPages

        /// <summary>
        /// Describes a module-provided page.
        /// </summary>
        /// <remarks>
        /// - Module authors may provide explicit <see cref="ViewFactory"/> and <see cref="ViewModelFactory"/> delegates.  <br/>
        /// - If a factory is <c>null</c>, the Bootstrapper will attempt to resolve <see cref="ViewType"/> or <see cref="ViewModelType"/> from
        ///   the DI container.  <br/>
        /// - To support DI resolution, types must be registered via <see cref="DescriptorDependencyInjection"/>.  <br/>
        /// - If neither a factory nor a DI registration exists, the page will be skipped and an error logged.<br/>
        /// This dual-path model preserves backward compatibility while supporting DI-based resolution.<br/>
        /// </remarks>
        public IEnumerable<DescriptorPage> GetPageDescriptors()
        {
            return [
                new DescriptorPage(
                    ModuleId: ModuleId,
                    DescriptorId: Guid.Parse("f9e88050-b8c3-43b3-b76e-79f16595acff"),
                    Version: Version,
                    ViewType: typeof(MockPageView),
                    ViewModelType: typeof(MockPageViewModel),
                    Name: "Mock Page",
                    IconSource: "tab_desktop_new_page_regular",
                    ViewFactory: null,
                    ViewModelFactory: null!,
                    LoadBefore: [],
                    LoadAfter: [],
                    ConflictsWith: [],
                    DependsOn: []
                )
            ];
        }

        #endregion

        #region NucleusMock: IProviderSettingsPages

        /// <summary>
        /// Integrates module‑specific configuration views into the global settings experience of the host application. 
        /// This enables end users to edit and persist module configuration (e.g., <see cref="NucleusMockConfig"/>) through a familiar settings UI rather than editing files manually.
        /// </summary>
        public List<DescriptorSettingsPage> GetSettingsPageDescriptors()
        {
            return [
                new DescriptorSettingsPage(
                    ModuleId: ModuleId,
                    DescriptorId: Guid.Parse("d9d8b10c-3bb2-42ca-9fd3-853f2d631e0d"),
                    Version: Version,
                    ViewType: typeof(MockSettingsView),
                    ViewModelType: typeof(MockSettingsViewModel),
                    Group: "Mock Settings",
                    Name: "General",
                    IconSource: "book_question_mark_regular",
                    ViewFactory: null,
                    ViewModelFactory: provider => {

                        IConfigService configService = provider.GetRequiredService<IConfigService>();
                        IConfigAccessorFor<NucleusMockConfig> accessor = configService.GetConfigAccessor<NucleusMockConfig>(this.token);

                        return new MockSettingsViewModel(accessor);
                    },
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