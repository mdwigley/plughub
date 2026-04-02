using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging.Abstractions;
using NucleusAF.Attributes;
using NucleusAF.Interfaces.Providers;
using NucleusAF.Interfaces.Services.Modules;
using NucleusAF.Mock.Interfaces.Services;
using NucleusAF.Models.Descriptors;
using NucleusAF.Models.Modules;
using NucleusAF.Services.Modules;

namespace NucleusAF.UnitTests.Services.Modules
{
    [TestClass]
    public sealed class ModuleServiceTests
    {
        private readonly MSTestHelpers msTestHelpers = new();
        private ModuleService? moduleService;
        private ModuleResolver? moduleResolver;
        private ServiceCollection? serviceCollection;
        private ServiceProvider? serviceProvider;

        [TestInitialize]
        public void Setup()
        {
            this.moduleService = new ModuleService(new NullLogger<IModuleService>());
            this.moduleResolver = new ModuleResolver(new NullLogger<IModuleResolver>());
        }

        [TestCleanup]
        public void Cleanup()
        {
            (this.serviceProvider as IDisposable)?.Dispose();

            Thread.Sleep(100);

            Serilog.Log.CloseAndFlush();

            GC.Collect();
            GC.WaitForPendingFinalizers();

            this.msTestHelpers!.Dispose();
        }


        #region ModuleServiceTests: Discovery

        [TestMethod]
        [TestCategory("Discovery")]
        public void Discovery_ShouldReturnAnyValidModules()
        {
            // Arrange & Act
            IEnumerable<ModuleReference> modules =
                this.moduleService!.Discover(this.msTestHelpers.ModuleDirectory);

            // Assert
            Assert.IsTrue(modules.Any(), "The mock module was not found: {" + this.msTestHelpers.ModuleDirectory + "}");
        }

        [TestMethod]
        [TestCategory("Discovery")]
        public void Discovery_ShouldFindModuleMock()
        {
            // Arrange
            IEnumerable<ModuleReference> modules =
                this.moduleService!.Discover(this.msTestHelpers.ModuleDirectory);

            ModuleReference? foundModule = null;

            // Act
            foreach (ModuleReference module in modules)
                if (module.AssemblyName == "NucleusAF.Mock")
                    foundModule = module;

            // Assert
            Assert.IsNotNull(foundModule, "The mock module was not found: {" + this.msTestHelpers.ModuleDirectory + "}");
            Assert.IsInstanceOfType<ModuleReference>(foundModule, "Instance was not of the mock module type");
        }

        [TestMethod]
        [TestCategory("Discovery")]
        public void Discovery_ShouldThrowOnNullInput()
        {
            // Arrange & Act & Assert
            Assert.ThrowsException<ArgumentNullException>(() =>
            {
                this.moduleService!.Discover(null!);
            });
        }

        [TestMethod]
        [TestCategory("Discovery")]
        public void Discovery_ShouldNotReturnModuleClassesWithoutProviders()
        {
            // Arrange && Act
            IEnumerable<ModuleReference> modules = this.moduleService!.Discover(this.msTestHelpers.ModuleDirectory);

            bool hasNoProviders = modules.Any(p => p.Type.FullName == "NucleusAF.Mock.ModuleMockNoProvider");

            // Assert
            Assert.IsFalse(hasNoProviders, "Modules missing provider should not be returned in discovery");
        }

        [TestMethod]
        [TestCategory("Discovery")]
        public void Discovery_ShouldRejectModulesWithDuplicateModuleIds()
        {
            // Arrange && Act
            IEnumerable<ModuleReference> modules = this.moduleService!.Discover(this.msTestHelpers.ModuleDirectory);

            Guid duplicateModuleId = Guid.Parse("45bc53be-bff0-4f46-ad13-d483004cd8c8");

            List<ModuleReference> groupedModules = [.. modules.Where(p => p.Metadata.ModuleId == duplicateModuleId)];

            // Assert
            Assert.AreEqual(1, groupedModules.Count, "Duplicate modules with same metadata should be filtered out");
        }

        #endregion

        #region ModuleServiceTests: Instantiate

        [TestMethod]
        [TestCategory("Instantiate")]
        public void Instantiate_ShouldReturnModuleBase()
        {
            // Arrange
            IEnumerable<ModuleReference> modules =
                this.moduleService!.Discover(this.msTestHelpers.ModuleDirectory);

            ModuleReference? foundModule = null;

            // Act
            foreach (ModuleReference module in modules)
                if (module.AssemblyName == "NucleusAF.Mock" && module.Providers.Any())
                    foundModule = module;

            ModuleBase? mockModule = this.moduleService.GetLoadedModule<ModuleBase>(foundModule!.Providers.FirstOrDefault()!);

            // Assert
            Assert.IsNotNull(mockModule, "The mock module was not found: {" + this.msTestHelpers.ModuleDirectory + "}");
            Assert.IsInstanceOfType<ModuleBase>(mockModule, "Instance was not of the module base");
        }

        [TestMethod]
        [TestCategory("Instantiate")]
        public void Instantiate_ShouldReturnConfigProvidere()
        {
            // Arrange
            IEnumerable<ModuleReference> modules =
                this.moduleService!.Discover(this.msTestHelpers.ModuleDirectory);
            ModuleReference? foundModule = null;
            ProviderInterface? providerToTest;

            // Act
            foundModule = modules
                .FirstOrDefault(module => module.AssemblyName == "NucleusAF.Mock" && module.Providers.Any());

            providerToTest = foundModule!.Providers
                .FirstOrDefault(i => i.InterfaceType == typeof(IProviderConfiguration));

            Assert.IsNotNull(providerToTest, "Expected interface IProviderConfiguration not found in provider");

            IProviderConfiguration? configProvider =
                this.moduleService.GetLoadedProviders<IProviderConfiguration>(providerToTest!);

            // Assert
            Assert.IsNotNull(configProvider, "The mock module's configuration descriptor provider was not found");
            Assert.IsInstanceOfType(configProvider, typeof(IProviderConfiguration), "Instance was not of the configuration descriptor interface");
        }


        [TestMethod]
        [TestCategory("Instantiate")]
        public void GetLoadedModule_ShouldReturnNullForUnknownProvider()
        {
            // Arrage
            ProviderInterface fakeProvider = new(
                Assembly: typeof(object).Assembly,
                InterfaceType: typeof(IDisposable),
                ImplementationType: typeof(object));

            // Act
            ModuleBase? module = this.moduleService!.GetLoadedModule<ModuleBase>(fakeProvider);

            // Assert
            Assert.IsNull(module, "Should return null if a provider is not present.");
        }

        [TestMethod]
        [TestCategory("Instantiate")]
        public void GetLoadedInterface_ShouldReturnNullIfProviderNotPresent()
        {
            // Arrange
            IEnumerable<ModuleReference> modules = this.moduleService!.Discover(this.msTestHelpers.ModuleDirectory);
            ModuleReference? foundModule = modules.FirstOrDefault();

            if (foundModule == null)
                Assert.Inconclusive("No modules available for negative test.");

            ProviderInterface fakeProvider = new(
                Assembly: typeof(object).Assembly,
                InterfaceType: typeof(IDisposable),
                ImplementationType: typeof(object));

            // Act
            IDisposable? providers = this.moduleService!.GetLoadedProviders<IDisposable>(fakeProvider);

            // Assert
            Assert.IsNull(providers, "Should return null for providers not present in module.");
        }

        #endregion

        #region ModuleServiceTests: Module Services

        [TestMethod]
        [TestCategory("ModuleServices")]
        public void DISetup_ShouldResolveIEchoService()
        {
            // Arrange
            string input = "Test DI Echo";

            // Act
            IEchoService echoService = this.GetEchoService();

            string output = echoService.Echo(input);

            // Assert
            Assert.AreEqual(input, output);
        }

        [TestMethod]
        [TestCategory("ModuleServices")]
        public void EchoService_ShouldRaiseMessageReceived_Event_OnEcho()
        {
            // Arrange
            IEchoService echoService = this.GetEchoService();
            string input = "Hello, events!";
            string? receivedMessage = null;

            echoService.MessageReceived += (sender, args) =>
            {
                receivedMessage = args.Message;
            };

            // Act
            echoService.Echo(input);

            // Assert
            Assert.IsNotNull(receivedMessage, "MessageReceived event did not fire.");
            Assert.AreEqual(input, receivedMessage, "MessageReceived event did not deliver the correct message.");
        }

        [TestMethod]
        [TestCategory("ModuleServices")]
        public void EchoService_ShouldRaiseMessageErrorEventOnEmptyInput()
        {
            // Arrange
            IEchoService echoService = this.GetEchoService();
            string input = "";
            string? errorMsg = null;
            string? errorValue = null;

            echoService.MessageError += (sender, args) =>
            {
                errorMsg = args.Message;
                errorValue = args.Error;
            };

            // Act
            echoService.Echo(input);

            // Assert
            Assert.IsNotNull(errorMsg, "MessageError event did not fire.");
            Assert.AreEqual(input, errorMsg, "MessageError event did not capture the empty input.");
            Assert.AreEqual("The message provided was emtpy.", errorValue, "MessageError event did not convey the expected error message.");
        }

        #endregion

        #region ModuleServiceTests: Descriptor Attributes

        [TestMethod]
        [TestCategory("DescriptorAttributes")]
        public void GetDescriptorAttribute_ShouldReturnAttribute_ForValidProvider()
        {
            // Arrange
            string interfaceName = "NucleusAF.Interfaces.Providers.IProviderAppConfig";

            // Act
            DescriptorProviderAttribute? attribute =
                this.moduleService!.GetDescriptorProviderAttribute(interfaceName);

            // Assert
            Assert.IsNotNull(attribute, "Expected to find DescriptorAttribute on the interface.");
            Assert.AreEqual("GetAppConfigDescriptors", attribute.DescriptorAccessorName);
        }

        [TestMethod]
        [TestCategory("DescriptorAttributes")]
        public void GetDescriptorAttribute_ShouldReturnNull_ForNonExistentProvider()
        {
            // Arrange
            string interfaceName = "Non.Existent.Namespace.INonExistentProvider";

            // Act
            DescriptorProviderAttribute? attribute = this.moduleService!.GetDescriptorProviderAttribute(interfaceName);

            // Assert
            Assert.IsNull(attribute, "Expected null for a non-existent provider.");
        }

        [TestMethod]
        [TestCategory("DescriptorAttributes")]
        public void GetDescriptorAttribute_ShouldThrow_OnNullOrEmptyInput()
        {
            // Arrange & Act & Assert
            Assert.ThrowsException<ArgumentNullException>(() =>
            {
                this.moduleService!.GetDescriptorProviderAttribute(string.Empty);
            });
        }

        #endregion


        private IEchoService GetEchoService(bool forceRebuild = false)
        {
            if (this.serviceProvider == null || this.serviceCollection == null || forceRebuild)
            {
                (this.serviceProvider as IDisposable)?.Dispose();
                this.serviceCollection = this.msTestHelpers.CreateTempServiceCollection();
                this.serviceCollection.AddSingleton<IModuleResolver>(this.moduleResolver!);

                IEnumerable<ModuleReference> modules =
                    this.moduleService!.Discover(this.msTestHelpers.ModuleDirectory);

                ProviderInterface mockProvider = modules
                    .First(p => p.AssemblyName == "NucleusAF.Mock")
                    .Providers
                    .First();

                IProviderDependencyInjection? injectgor =
                    this.moduleService.GetLoadedProviders<IProviderDependencyInjection>(mockProvider);

                foreach (DescriptorDependencyInjection descriptor in injectgor!.GetInjectionDescriptors())
                {
                    if (descriptor.ImplementationType != null)
                    {
                        this.serviceCollection.Add(
                            new ServiceDescriptor(
                                descriptor.InterfaceType,
                                descriptor.ImplementationType,
                                descriptor.Lifetime));
                    }
                    else if (descriptor.ImplementationFactory != null)
                    {
                        this.serviceCollection.Add(
                            new ServiceDescriptor(
                                descriptor.InterfaceType,
                                provider => descriptor.ImplementationFactory(provider)!,
                                descriptor.Lifetime));
                    }
                }

                this.serviceProvider = this.serviceCollection.BuildServiceProvider();
            }

            return this.serviceProvider.GetService<IEchoService>()!;
        }
    }
}