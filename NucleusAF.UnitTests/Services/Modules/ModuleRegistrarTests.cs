using Microsoft.Extensions.Logging.Abstractions;
using NucleusAF.Interfaces.Services.Capabilities;
using NucleusAF.Interfaces.Services.Configuration;
using NucleusAF.Interfaces.Services.Modules;
using NucleusAF.Models.Capabilities;
using NucleusAF.Models.Configuration.Parameters;
using NucleusAF.Models.Descriptors;
using NucleusAF.Models.Modules;
using NucleusAF.Services.Capabilities;
using NucleusAF.Services.Capabilities.Accessors;
using NucleusAF.Services.Capabilities.Handlers;
using NucleusAF.Services.Configuration;
using NucleusAF.Services.Configuration.Accessors;
using NucleusAF.Services.Configuration.Handlers;
using NucleusAF.Services.Modules;
using System.Collections.Concurrent;
using System.Reflection;

namespace NucleusAF.UnitTests.Services.Modules
{
    [TestClass]
    public sealed class ModuleRegistrarTests
    {
        private readonly MSTestHelpers msTestHelpers = new();
        private CapabilityService? capabilityService;
        private ConfigService? configService;
        private JsonConfigParams fileParams = new();
        private IConfigAccessorFor<ModuleManifest>? manifest;
        private ModuleRegistrar? moduleRegistrar;
        private ModuleService? moduleService;
        private ModuleCache? moduleCache;
        private readonly CapabilityToken capabilityToken = new(Guid.NewGuid());

        private readonly Assembly enabledAssembly = typeof(List<>).Assembly;
        private readonly Type enabledImplementationType = typeof(List<string>);
        private readonly Type enabledProviderType = typeof(IList<string>);
        private readonly Assembly disabledAssembly = typeof(ConcurrentStack<>).Assembly;
        private readonly Type disabledImplementationType = typeof(Dictionary<int, string>);
        private readonly Type disabledProviderType = typeof(IDictionary<int, string>);

        [TestInitialize]
        public async Task Setup()
        {
            this.capabilityService =
                new CapabilityService(
                    [new MinimalCapabilityAccessor(new NullLogger<ICapabilityAccessor>())],
                    [new MinimalCapabilityHandler(new NullLogger<ICapabilityHandler>())],
                    new NullLogger<ICapabilityService>());

            this.fileParams = new JsonConfigParams(Read: CapabilityValue.Limited, Write: CapabilityValue.Limited);

            this.configService = new ConfigService(
                [new JsonConfigAccessor(new NullLogger<IConfigAccessor>(), this.capabilityService)],
                [new JsonConfigHandler(new NullLogger<IConfigHandler>(), this.capabilityService)],
                new NullLogger<IConfigService>(),
                this.capabilityService,
                this.msTestHelpers.TempDirectory);

            this.fileParams = new JsonConfigParams();

            this.moduleService = new ModuleService(new NullLogger<IModuleService>());

            this.configService.Register<ModuleManifest>(this.fileParams, this.capabilityToken, out this.manifest);

            ModuleManifest moduleManifest = new()
            {
                DescriptorStates =
                [
                    new(
                        moduleId: Guid.Parse("12345678-1234-1234-1234-123456789abc"),
                        assemblyName: this.enabledAssembly.GetName().Name!,
                        providerName: this.enabledProviderType.FullName!,
                        className: this.enabledImplementationType.FullName!,
                        enabled: true,
                        loadOrder: 1),
                    new(
                        moduleId: Guid.Parse("87654321-4321-4321-4321-cba987654321"),
                        assemblyName: this.disabledAssembly.GetName().Name!,
                        providerName: this.disabledProviderType.FullName!,
                        className: this.disabledImplementationType.FullName!,
                        enabled: false,
                        loadOrder: 2)
                ]
            };

            this.moduleCache = new ModuleCache([]);

            await this.manifest.SaveAsync(moduleManifest);

            this.moduleRegistrar =
                new ModuleRegistrar(
                    new NullLogger<IModuleRegistrar>(),
                    this.manifest,
                    this.moduleService,
                    this.moduleCache);
        }

        [TestCleanup]
        public void Cleanup()
        {
            Thread.Sleep(100);
            Serilog.Log.CloseAndFlush();
            GC.Collect();
            GC.WaitForPendingFinalizers();
            this.msTestHelpers.Dispose();
        }

        #region ModuleRegistrarTests: Registration

        [TestMethod]
        [TestCategory("Constructor")]
        public void Constructor_ValidParametersCreatesInstance()
        {
            // Arrange & Act
            ModuleRegistrar registrar = new(
                new NullLogger<IModuleRegistrar>(),
                this.manifest!,
                this.moduleService!,
                this.moduleCache!);

            // Assert
            Assert.IsInstanceOfType<ModuleRegistrar>(registrar, "ModuleRegistrar should be created successfully");
        }

        [TestMethod]
        [TestCategory("Constructor")]
        public void Constructor_NullLoggerThrowsArgumentNullException()
        {
            // Arrange & Act & Assert
            Assert.ThrowsException<ArgumentNullException>(() =>
                new ModuleRegistrar(null!, this.manifest!, this.moduleService!, this.moduleCache!));
        }

        [TestMethod]
        [TestCategory("Constructor")]
        public void Constructor_NullModuleManifestThrowsArgumentNullException()
        {
            // Arrange & Act & Assert
            Assert.ThrowsException<ArgumentNullException>(() =>
                new ModuleRegistrar(new NullLogger<IModuleRegistrar>(), null!, this.moduleService!, this.moduleCache!));
        }

        [TestMethod]
        [TestCategory("Constructor")]
        public void Constructor_NullModuleServiceThrowsArgumentNullException()
        {
            // Arrange & Act & Assert
            Assert.ThrowsException<ArgumentNullException>(() =>
                new ModuleRegistrar(new NullLogger<IModuleRegistrar>(), this.manifest!, null!, this.moduleCache!));
        }

        #endregion

        #region ModuleRegistrarTests: Accessors

        [TestMethod]
        [TestCategory("GetEnabled")]
        public void GetEnabled_ExistingEnabledModuleReturnsTrue()
        {
            // Arrange
            Guid moduleId = Guid.Parse("12345678-1234-1234-1234-123456789abc");

            // Act
            bool result = this.moduleRegistrar!.IsEnabled(moduleId, this.enabledProviderType);

            // Assert
            Assert.IsTrue(result, "Should return true for enabled module");
        }

        [TestMethod]
        [TestCategory("GetEnabled")]
        public void GetEnabled_ExistingDisabledModuleReturnsFalse()
        {
            // Arrange
            Guid moduleId = Guid.Parse("87654321-4321-4321-4321-cba987654321");

            // Act
            bool result = this.moduleRegistrar!.IsEnabled(moduleId, this.enabledProviderType);

            // Assert
            Assert.IsFalse(result, "Should return false for disabled module");
        }

        [TestMethod]
        [TestCategory("GetEnabled")]
        public void GetEnabled_NonExistentModuleReturnsFalse()
        {
            // Arrange
            Guid moduleId = Guid.NewGuid();

            // Act
            bool result = this.moduleRegistrar!.IsEnabled(moduleId, typeof(ISet<int>));

            // Assert
            Assert.IsFalse(result, "Should return false for non-existent module");
        }

        [TestMethod]
        [TestCategory("GetEnabled")]
        public async Task GetEnabled_EmptyProviderStatesReturnsFalse()
        {
            // Arrange
            ModuleManifest manifestWithEmptyStates = new() { DescriptorStates = [] };

            await this.manifest!.SaveAsync(manifestWithEmptyStates);

            // Act
            bool result = this.moduleRegistrar!.IsEnabled(Guid.NewGuid(), typeof(object));

            // Assert
            Assert.IsFalse(result, "Should return false when descriptor states is empty");
        }


        [TestMethod]
        [TestCategory("GetManifest")]
        public void GetManifest_ValidManifestReturnsNonNull()
        {
            // Act
            ModuleManifest manifest = this.moduleRegistrar!.GetManifest();

            // Assert
            Assert.IsTrue(manifest.DescriptorStates.Count > 0, "Manifest should contain descriptor states.");
        }

        #endregion

        #region ModuleRegistrarTests: Provider Mutators

        [TestMethod]
        [TestCategory("SetEnabled")]
        public void SetEnabled_ValidModuleEnablesModule()
        {
            // Arrange
            Guid moduleId = Guid.Parse("87654321-4321-4321-4321-cba987654321");

            // Act
            this.moduleRegistrar!.SetEnabled(moduleId, this.disabledProviderType);

            // Assert            
            DescriptorLoadState updatedState = this.manifest!.Get().DescriptorStates.First(x => x.ModuleId == moduleId);

            Assert.IsTrue(updatedState.Enabled, "Module should be enabled after SetEnabled call");
        }

        [TestMethod]
        [TestCategory("SetDisabled")]
        public void SetDisabled_ValidModuleDisablesModule()
        {
            // Arrange
            Guid moduleId = Guid.Parse("12345678-1234-1234-1234-123456789abc");

            // Act
            this.moduleRegistrar!.SetEnabled(moduleId, this.enabledProviderType, false);

            // Assert
            DescriptorLoadState updatedState = this.manifest!.Get().DescriptorStates.First(x => x.ModuleId == moduleId);

            Assert.IsFalse(updatedState.Enabled, "Module should be disabled after SetDisabled call");
        }

        #endregion

        #region ModuleRegistrarTests: Module Mutators

        [TestMethod]
        [TestCategory("SetAllEnabled")]
        public void SetAllEnabled_DisabledModuleGetsEnabled()
        {
            // Arrange
            Guid moduleId = Guid.Parse("87654321-4321-4321-4321-cba987654321");

            // Act
            this.moduleRegistrar!.SetAllEnabled(moduleId, true);

            // Assert
            DescriptorLoadState updatedState = this.manifest!.Get().DescriptorStates.First(x => x.ModuleId == moduleId);

            Assert.IsTrue(updatedState.Enabled, "All descriptors of the module should be enabled.");
        }

        [TestMethod]
        [TestCategory("SetAllEnabled")]
        public void SetAllEnabled_EnabledModuleGetsDisabled()
        {
            // Arrange
            Guid moduleId = Guid.Parse("12345678-1234-1234-1234-123456789abc");

            // Act
            this.moduleRegistrar!.SetAllEnabled(moduleId, false);

            // Assert
            DescriptorLoadState updatedState = this.manifest!.Get().DescriptorStates.First(x => x.ModuleId == moduleId);

            Assert.IsFalse(updatedState.Enabled, "All descriptors of the module should be disabled.");
        }

        [TestMethod]
        [TestCategory("SetAllEnabled")]
        public void SetAllEnabled_NoChangeDoesNotModifyManifest()
        {
            // Arrange
            Guid moduleId = Guid.Parse("12345678-1234-1234-1234-123456789abc");
            ModuleManifest before = this.manifest!.Get();

            // Act
            this.moduleRegistrar!.SetAllEnabled(moduleId, true);

            // Assert
            ModuleManifest after = this.manifest!.Get();

            Assert.AreEqual(before.DescriptorStates.Count, after.DescriptorStates.Count, "Manifest should remain unchanged if descriptor states already match.");
        }

        #endregion

        #region ModuleRegistrarTests: Manifest Mutators

        [TestMethod]
        [TestCategory("SaveManifest")]
        public async Task SaveManifest_ValidManifestPersistsToAccessor()
        {
            // Arrange
            ModuleManifest newManifest = new()
            {
                DescriptorStates =
                [
                    new(moduleId: Guid.NewGuid(),
                        assemblyName: "TestAssembly",
                        providerName: typeof(IDisposable).FullName!,
                        className: "TestClass",
                        enabled: true,
                        loadOrder: 1)
                ]
            };

            // Act
            await this.moduleRegistrar!.SaveManifestAsync(newManifest);

            // Assert
            ModuleManifest persisted = this.manifest!.Get();

            Assert.AreEqual(newManifest.DescriptorStates.Count, persisted.DescriptorStates.Count,
                "Persisted manifest should match saved manifest.");
        }

        [TestMethod]
        [TestCategory("SaveManifest")]
        public void SaveManifest_InvalidManifestThrowsArgumentException()
        {
            // Arrange
            ModuleManifest invalidManifest = new() { DescriptorStates = null! };

            // Act & Assert
            Assert.ThrowsException<ArgumentException>(() =>
                this.moduleRegistrar!.SaveManifest(invalidManifest));
        }

        #endregion

        #region ModuleRegistrarTests: Decriptor Traversal

        [TestMethod]
        [TestCategory("GetDescriptorsForProvider")]
        public void GetDescriptorsForProvider_NoModulesReturnsEmptyList()
        {
            // Arrange & Act
            List<Descriptor> result = this.moduleRegistrar!.GetDescriptorsForProvider(typeof(IDisposable));

            // Assert
            Assert.AreEqual(0, result.Count, "Should return empty list when no module match.");
        }

        [TestMethod]
        [TestCategory("GetDescriptorsForProvider")]
        public void GetDescriptorsForProvider_WithValidModuleReturnsDescriptors()
        {
            //TODO: Determine if should be removed
            /*
            // Arrange
            // Assumes a test module assembly has been built and copied into MSTestHelpers.ModuleDirectory
            // that implements IFakeProvider decorated with [DescriptorProvider].
            Type fakeProvider = typeof(IFakeProvider); // Placeholder provider test

            this.moduleCache!.Reload(); // Ensure cache discovers modules in ModuleDirectory

            // Act
            List<Descriptor> result = this.moduleRegistrar!.GetDescriptorsForInterface(fakeProvider);

            // Assert
            Assert.IsTrue(result.Count > 0, "Expected at least one module descriptor to be returned.");
            */
        }

        [TestMethod]
        [TestCategory("GetDescriptorsForProvider")]
        public void GetDescriptorsForProvider_MissingAttributeLogsWarningAndSkips()
        {
            //TODO: Determine if should be removed
            /*
            // Arrange
            // Requires module without [DescriptorProvider] attribute, placed in ModuleDirectory
            Type fakeProvider = typeof(IEnumerable<int>); // Example provider with no descriptor

            this.moduleCache!.Reload();

            // Act
            List<Descriptor> result = this.moduleRegistrar!.GetDescriptorsForProvider(fakeProvider);

            // Assert
            Assert.IsTrue(result.Count == 0, "Modules missing DescriptorProvider attribute should not yield descriptors.");
            */
        }

        #endregion
    }
}