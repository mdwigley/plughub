using Microsoft.Extensions.Logging.Abstractions;
using NucleusAF.Interfaces.Platform.Storage;
using NucleusAF.Interfaces.Services.Capabilities;
using NucleusAF.Interfaces.Services.Configuration;
using NucleusAF.Interfaces.Services.Encryption;
using NucleusAF.Models.Capabilities;
using NucleusAF.Models.Configuration.Parameters;
using NucleusAF.Platform.Storage;
using NucleusAF.Services.Capabilities;
using NucleusAF.Services.Capabilities.Accessors;
using NucleusAF.Services.Capabilities.Handlers;
using NucleusAF.Services.Configuration;
using NucleusAF.Services.Configuration.Accessors;
using NucleusAF.Services.Configuration.Handlers;
using NucleusAF.Services.Encryption;

namespace NucleusAF.UnitTests.Accessors.Configuration
{
    [TestClass]
    public class JsonConfigAccessorTests
    {
        private readonly MSTestHelpers msTestHelpers = new();
        private InsecureStorage? secureStorage;
        private CapabilityService? capabilityService;
        private JsonConfigHandler? configHandler;
        private ConfigService? configService;
        private JsonConfigParams fileParams = new();
        private EncryptionService? encryptionService;
        private readonly CapabilityToken capabilityToken = new(Guid.NewGuid());

        internal class UnitTestAConfig
        {
            public int FieldA { get; set; } = 50;
            public bool FieldB { get; set; } = false;
            public float FieldC { get; } = 2.71828f;
        }
        internal class UnitTestBConfig
        {
            public string FieldA { get; set; } = "NucleusAF";
            public int FieldB { get; set; } = 100;
            public float FieldC { get; } = 3.14f;
        }

        [TestInitialize]
        public void Setup()
        {
            this.capabilityService =
                new CapabilityService(
                    [new MinimalCapabilityAccessor(new NullLogger<ICapabilityAccessor>())],
                    [new MinimalCapabilityHandler(new NullLogger<ICapabilityHandler>())],
                    new NullLogger<ICapabilityService>());

            this.fileParams = new JsonConfigParams(Read: CapabilityValue.Limited, Write: CapabilityValue.Limited);

            this.secureStorage = new InsecureStorage(new NullLogger<ISecureStorage>());
            this.secureStorage.Initialize(this.msTestHelpers.TempDirectory, false, false);
            this.encryptionService = new EncryptionService(new NullLogger<IEncryptionService>(), this.secureStorage);

            this.configHandler = new JsonConfigHandler(new NullLogger<IConfigHandler>(), this.capabilityService);

            this.configService = new ConfigService(
            [
                new JsonConfigAccessor(new NullLogger<IConfigAccessor>(), this.capabilityService),
            ],
            [
                this.configHandler,
            ],
            new NullLogger<IConfigService>(),
            this.capabilityService,
            this.msTestHelpers.TempDirectory);
        }

        [TestCleanup]
        public void Cleanup()
        {
            (this.configService as IDisposable)?.Dispose();

            this.msTestHelpers.Dispose();
        }

        #region JsonConfigAccessorTests: Registration

        [TestMethod]
        [TestCategory("Registration")]
        public void For_UnregisteredType_Throw()
        {
            // Arrange & Act
            IConfigAccessor accessor = new JsonConfigAccessor(new NullLogger<IConfigAccessor>(), this.capabilityService!)
                .SetConfigService(this.configService!)
                .SetAccess(this.capabilityToken);

            // Assert
            Assert.ThrowsException<InvalidOperationException>(() => accessor.For<UnitTestBConfig>());
        }

        [TestMethod]
        [TestCategory("Registration")]
        public void For_RegisteredType_Succeeds()
        {
            // Arrange
            this.configService!.Register(typeof(UnitTestAConfig), this.fileParams, this.capabilityToken);

            IConfigAccessor accessor = new JsonConfigAccessor(new NullLogger<IConfigAccessor>(), this.capabilityService!)
                .SetConfigService(this.configService!)
                .SetConfigHandler(this.configHandler!)
                .SetAccess(this.capabilityToken);

            // Act
            IConfigAccessorFor<UnitTestAConfig> stronglyTyped = accessor.For<UnitTestAConfig>();

            // Assert
            Assert.IsInstanceOfType<IConfigAccessorFor<UnitTestAConfig>>(stronglyTyped);
        }

        #endregion

        #region JsonConfigAccessorTests: Accessors

        [TestMethod]
        [TestCategory("Accessors")]
        public void Get_ReturnsDefaultValue()
        {
            //Arrange
            this.configService!.Register(typeof(UnitTestAConfig), this.fileParams, this.capabilityToken);

            IConfigAccessor accessor = new JsonConfigAccessor(new NullLogger<IConfigAccessor>(), this.capabilityService!)
                .SetConfigService(this.configService!)
                .SetConfigHandler(this.configHandler!)
                .SetAccess(this.capabilityToken);

            IConfigAccessorFor<UnitTestAConfig> a = accessor.For<UnitTestAConfig>();

            // Act
            int value = a.Get<int>(nameof(UnitTestAConfig.FieldA));

            // Assert
            Assert.AreEqual(50, value);
        }

        [TestMethod]
        [TestCategory("Accessors")]
        public void Get_ReturnsMergedInstanceWithUserOverrides()
        {
            // Arrange
            this.configService!.Register(typeof(UnitTestAConfig), this.fileParams, this.capabilityToken);
            this.configHandler!.SetValue(typeof(UnitTestAConfig), nameof(UnitTestAConfig.FieldA), 99, this.capabilityToken);

            IConfigAccessor accessor = new JsonConfigAccessor(new NullLogger<IConfigAccessor>(), this.capabilityService!)
                .SetConfigService(this.configService!)
                .SetConfigHandler(this.configHandler)
                .SetAccess(this.capabilityToken);

            IConfigAccessorFor<UnitTestAConfig> aConfig = accessor.For<UnitTestAConfig>();

            // Act
            UnitTestAConfig instance = aConfig.Get();

            // Assert
            Assert.AreEqual(99, instance.FieldA);
            Assert.IsFalse(instance.FieldB);
            Assert.AreEqual(2.71828f, instance.FieldC);
        }

        [TestMethod]
        [TestCategory("Validation")]
        public void Get_WithNullKey_Throw()
        {
            // Arrange
            this.configService!.Register(typeof(UnitTestAConfig), this.fileParams, this.capabilityToken);

            // Act
            IConfigAccessor accessor = new JsonConfigAccessor(new NullLogger<IConfigAccessor>(), this.capabilityService!)
                .SetConfigService(this.configService!)
                .SetConfigHandler(this.configHandler!)
                .SetAccess(this.capabilityToken);

            IConfigAccessorFor<UnitTestAConfig> config = accessor.For<UnitTestAConfig>();

            // Assert
            Assert.ThrowsException<ArgumentNullException>(() => config.Get<int>(null!));
        }

        #endregion

        #region JsonConfigAccessorTests: Mutators

        [TestMethod]
        [TestCategory("Mutators")]
        public void Set_ThenGet_ReturnsUpdatedValue()
        {
            // Arrange
            this.configService!.Register(typeof(UnitTestAConfig), this.fileParams, this.capabilityToken);

            IConfigAccessor accessor = new JsonConfigAccessor(new NullLogger<IConfigAccessor>(), this.capabilityService!)
                .SetConfigService(this.configService!)
                .SetConfigHandler(this.configHandler!)
                .SetAccess(this.capabilityToken);

            IConfigAccessorFor<UnitTestAConfig> a = accessor.For<UnitTestAConfig>();

            // Act
            a.Set(nameof(UnitTestAConfig.FieldA), 123);

            int value = a.Get<int>(nameof(UnitTestAConfig.FieldA));

            // Assert
            Assert.AreEqual(123, value);
        }

        [TestMethod]
        [TestCategory("Mutators")]
        public async Task Save_ModifiedInstance_PersistsChanges()
        {
            // Arrange
            this.configService!.Register(typeof(UnitTestAConfig), this.fileParams, this.capabilityToken);

            IConfigAccessor accessor = new JsonConfigAccessor(new NullLogger<IConfigAccessor>(), this.capabilityService!)
                .SetConfigService(this.configService!)
                .SetConfigHandler(this.configHandler!)
                .SetAccess(this.capabilityToken);

            IConfigAccessorFor<UnitTestAConfig> aConfig = accessor.For<UnitTestAConfig>();

            UnitTestAConfig editable = aConfig.Get();
            editable.FieldA = 123;
            editable.FieldB = true;

            // Act
            await aConfig.SaveAsync(editable);

            // Assert
            int storedA = this.configHandler!.GetValue<int>(typeof(UnitTestAConfig), nameof(UnitTestAConfig.FieldA), this.capabilityToken);
            bool storedB = this.configHandler!.GetValue<bool>(typeof(UnitTestAConfig), nameof(UnitTestAConfig.FieldB), this.capabilityToken);

            Assert.AreEqual(123, storedA);
            Assert.IsTrue(storedB);

            UnitTestAConfig reloaded = aConfig.Get();
            Assert.AreEqual(123, reloaded.FieldA);
            Assert.IsTrue(reloaded.FieldB);
        }

        [TestMethod]
        [TestCategory("Mutators")]
        public async Task SaveAsync_WithInvalidToken_Throw()
        {
            // Arrange
            this.configService!.Register(typeof(UnitTestAConfig), this.fileParams, this.capabilityToken);

            // Act
            IConfigAccessor accessor = new JsonConfigAccessor(new NullLogger<IConfigAccessor>(), this.capabilityService!)
                .SetConfigService(this.configService!)
                .SetConfigHandler(this.configHandler!)
                .SetAccess(this.capabilityToken);

            IConfigAccessorFor<UnitTestAConfig> aConfig = accessor.For<UnitTestAConfig>();

            UnitTestAConfig edited = aConfig.Get();
            edited.FieldA = 42;

            accessor.SetAccess(new CapabilityToken(Guid.NewGuid()));

            IConfigAccessorFor<UnitTestAConfig> a2Config = accessor.For<UnitTestAConfig>();

            // Aassert
            await Assert.ThrowsExceptionAsync<UnauthorizedAccessException>(async () => await a2Config.SaveAsync(edited));
        }

        [TestMethod]
        [TestCategory("Validation")]
        public void Set_WithUnknownProperty_Throw()
        {
            // Arrange
            this.configService!.Register(typeof(UnitTestAConfig), this.fileParams, this.capabilityToken);

            // Act
            IConfigAccessor accessor = new JsonConfigAccessor(new NullLogger<IConfigAccessor>(), this.capabilityService!)
                .SetConfigService(this.configService!)
                .SetConfigHandler(this.configHandler!)
                .SetAccess(this.capabilityToken);

            IConfigAccessorFor<UnitTestAConfig> config = accessor.For<UnitTestAConfig>();

            // Assert
            Assert.ThrowsException<KeyNotFoundException>(() => config.Set("DoesNotExist", 42));
        }

        #endregion

        #region JsonConfigAccessorTests: Persistence

        [TestMethod]
        [TestCategory("Persistence")]
        public async Task SaveAsync_PersistsToConfigService()
        {
            // Arrange
            this.configService!.Register(typeof(UnitTestAConfig), this.fileParams, this.capabilityToken);

            IConfigAccessor accessor = new JsonConfigAccessor(new NullLogger<IConfigAccessor>(), this.capabilityService!)
                .SetConfigService(this.configService!)
                .SetConfigHandler(this.configHandler!)
                .SetAccess(this.capabilityToken);

            // Act
            IConfigAccessorFor<UnitTestAConfig> a = accessor.For<UnitTestAConfig>();

            a.Set(nameof(UnitTestAConfig.FieldA), 777);

            await a.SaveAsync(CancellationToken.None);

            // Assert
            int stored = this.configHandler!.GetValue<int>(typeof(UnitTestAConfig), nameof(UnitTestAConfig.FieldA), this.capabilityToken);

            Assert.AreEqual(777, stored);
        }

        [TestMethod]
        [TestCategory("Persistence")]
        public async Task SaveAsync_WithCancelledToken_DoesNotPersistTo_Disk()
        {
            // Arrange
            this.configService!.Register(typeof(UnitTestAConfig), this.fileParams, this.capabilityToken);

            IConfigAccessor accessor = new JsonConfigAccessor(new NullLogger<IConfigAccessor>(), this.capabilityService!)
                .SetConfigService(this.configService!)
                .SetConfigHandler(this.configHandler!)
                .SetAccess(this.capabilityToken);

            IConfigAccessorFor<UnitTestAConfig> a = accessor.For<UnitTestAConfig>();

            a.Set(nameof(UnitTestAConfig.FieldA), 9001);

            string filePath = Path.Combine(this.msTestHelpers.TempDirectory, $"{nameof(UnitTestAConfig)}.json");

            // Act & Assert
            using (CancellationTokenSource cts = new())
            {
                cts.Cancel();
                await Assert.ThrowsExceptionAsync<OperationCanceledException>(
                    () => a.SaveAsync(cts.Token),
                    "SaveAsync should propagate a pre-cancelled token.");
            }

            this.configService!.Unregister(typeof(UnitTestAConfig), this.capabilityToken);
            this.configService!.Register(typeof(UnitTestAConfig), this.fileParams, this.capabilityToken);

            accessor = new JsonConfigAccessor(new NullLogger<IConfigAccessor>(), this.capabilityService!)
                .SetConfigService(this.configService!)
                .SetConfigHandler(this.configHandler!)
                .SetAccess(this.capabilityToken);

            a = accessor.For<UnitTestAConfig>();

            Assert.AreEqual(50, a.Get<int>(nameof(UnitTestAConfig.FieldA)),
                "Cancelled SaveAsync must not create or update the settings file on disk.");
        }

        [TestMethod]
        [TestCategory("Persistence")]
        public async Task SaveSettingsAsync_RoundTripsThroughFreshConfigService()
        {
            // Arrange
            this.configService!.Register(typeof(UnitTestAConfig), this.fileParams, this.capabilityToken);

            IConfigAccessor accessor = new JsonConfigAccessor(new NullLogger<IConfigAccessor>(), this.capabilityService!)
                .SetConfigService(this.configService!)
                .SetConfigHandler(this.configHandler!)
                .SetAccess(this.capabilityToken);

            IConfigAccessorFor<UnitTestAConfig> a = accessor.For<UnitTestAConfig>();
            a.Set(nameof(UnitTestAConfig.FieldA), 424242);

            // Act
            await a.SaveAsync(CancellationToken.None);

            // Assert
            string filePath = Path.Combine(this.msTestHelpers.TempDirectory, $"{nameof(UnitTestAConfig)}.json");
            string json = await File.ReadAllTextAsync(filePath);

            Assert.IsTrue(File.Exists(filePath), "Settings file should be written to disk.");

            StringAssert.Contains(json, "424242", "Persisted JSON should include the updated value.");

            this.configService!.Unregister(typeof(UnitTestAConfig), this.capabilityToken);

            using (JsonConfigHandler newConfigService = new(new NullLogger<IConfigHandler>(), this.capabilityService!))
            {
                newConfigService.Register(typeof(UnitTestAConfig), this.fileParams, this.configService, this.capabilityToken);

                int roundTripped = newConfigService.GetValue<int>(
                    typeof(UnitTestAConfig),
                    nameof(UnitTestAConfig.FieldA),
                    this.capabilityToken);

                Assert.AreEqual(424242, roundTripped,
                    "A fresh ConfigService should read back the value that was flushed to disk.");
            }
        }

        #endregion

        #region JsonConfigAccessorTests: Security

        [TestMethod]
        [TestCategory("Security")]
        public void Set_WithInvalidWriteToken_Throw()
        {
            // Arrange
            this.configService!.Register(typeof(UnitTestAConfig), this.fileParams, this.capabilityToken);

            // Act
            IConfigAccessor accessor = new JsonConfigAccessor(new NullLogger<IConfigAccessor>(), this.capabilityService!)

                .SetConfigService(this.configService!)
                .SetConfigHandler(this.configHandler!)
                .SetAccess(new CapabilityToken(Guid.NewGuid()));

            IConfigAccessorFor<UnitTestAConfig> a = accessor.For<UnitTestAConfig>();

            // Assert
            Assert.ThrowsException<UnauthorizedAccessException>(() => a.Set(nameof(UnitTestAConfig.FieldA), 999));
        }

        #endregion

        #region JsonConfigAccessorTests: Concurrency

        [TestMethod]
        [TestCategory("Concurrency")]
        public async Task Concurrent_SetGet_DoesNotThrowAndValueRemainsValid()
        {
            // Arrange
            this.configService!.Register(typeof(UnitTestAConfig), this.fileParams, this.capabilityToken);

            IConfigAccessor accessor = new JsonConfigAccessor(new NullLogger<IConfigAccessor>(), this.capabilityService!)
                .SetConfigService(this.configService!)
                .SetConfigHandler(this.configHandler!)
                .SetAccess(this.capabilityToken);

            IConfigAccessorFor<UnitTestAConfig> config = accessor.For<UnitTestAConfig>();

            // Act
            const int iterations = 50;
            IEnumerable<Task> tasks = Enumerable.Range(0, iterations).Select(i =>
                Task.Run(() =>
                {
                    config.Set(nameof(UnitTestAConfig.FieldA), i);
                    _ = config.Get<int>(nameof(UnitTestAConfig.FieldA));
                }));

            await Task.WhenAll(tasks);

            // Assert
            int final = config.Get<int>(nameof(UnitTestAConfig.FieldA));

            Assert.IsTrue(final >= 0 && final < iterations, "Final value should be one of the written values.");
        }

        #endregion
    }
}