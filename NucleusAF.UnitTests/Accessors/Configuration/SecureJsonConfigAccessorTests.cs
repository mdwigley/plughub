using Microsoft.Extensions.Logging.Abstractions;
using NucleusAF.Interfaces.Models;
using NucleusAF.Interfaces.Models.Configuration;
using NucleusAF.Interfaces.Platform.Storage;
using NucleusAF.Interfaces.Services.Capabilities;
using NucleusAF.Interfaces.Services.Configuration;
using NucleusAF.Interfaces.Services.Configuration.Accessors;
using NucleusAF.Interfaces.Services.Encryption;
using NucleusAF.Models.Capabilities;
using NucleusAF.Models.Configuration.Parameters;
using NucleusAF.Models.Encryption;
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
    public class SecureJsonConfigAccessorTests
    {
        private readonly MSTestHelpers msTestHelpers = new();

        private CapabilityService capabilityService = null!;
        private ConfigService configService = null!;
        private IConfigHandler configHandler = null!;
        private InsecureStorage secureStorage = null!;
        private EncryptionService encryptionService = null!;
        private IEncryptionContext encryptionContext = null!;
        private IConfigParams? secureParams;

        private readonly CapabilityToken capabilityToken = new(Guid.NewGuid());


        internal class UnitTestSecureAConfig
        {
            public required Guid FieldA { get; set; } = Guid.Empty;

            [Secure]
            public string FieldB { get; set; } = "Secrets!";

            public SecureValue FieldC { get; set; } = new SecureValue("More Secrets!");
        }
        internal class UnitTestSecureBConfig
        {
            [Secure]
            public required string FieldA { get; set; } = "NucleusAF";

            public int FieldB { get; set; } = 100;

            [Secure]
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

            this.secureStorage = new InsecureStorage(new NullLogger<ISecureStorage>());
            this.secureStorage.Initialize(this.msTestHelpers.TempDirectory, false, false);
            this.encryptionService = new EncryptionService(new NullLogger<IEncryptionService>(), this.secureStorage);
            this.encryptionContext = this.encryptionService.GetEncryptionContext(typeof(SecureJsonConfigAccessorTests), new("EFFFFFFF-FFFF-EEEE-FFFF-FFFFFFFFFFFE"));

            this.configHandler = new SecureJsonConfigHandler(new NullLogger<IConfigHandler>(), this.capabilityService, this.encryptionService);
            this.configService = new ConfigService(
                [
                    new JsonConfigAccessor(new NullLogger<IConfigAccessor>(), this.capabilityService!),
                    new SecureJsonConfigAccessor(new NullLogger<IConfigAccessor>(), this.capabilityService!, this.encryptionService),
                ],
                [
                    new JsonConfigHandler(new NullLogger<IConfigHandler>(), this.capabilityService),
                    this.configHandler,
                ],
                new NullLogger<IConfigService>(),
                this.capabilityService,
                this.msTestHelpers.TempDirectory);

            this.secureParams = new SecureJsonConfigParams(EncryptionContext: this.encryptionContext!);
        }

        [TestCleanup]
        public void Cleanup()
        {
            this.msTestHelpers!.Dispose();
        }


        #region SecureAccessorTests: Registration

        [TestMethod]
        [TestCategory("Registration")]
        public void For_ValidType_ReturnsSecureAccessorFor()
        {
            // Arrange
            ISecureJsonConfigAccessorFor<UnitTestSecureAConfig> accessor =
                new SecureJsonConfigAccessor(new NullLogger<IConfigAccessor>(), this.capabilityService!, this.encryptionService)
                    .SetEncryptionContext(this.encryptionContext!)
                    .SetConfigService(this.configService)
                    .SetConfigHandler(this.configHandler)
                    .SetAccess(this.capabilityToken)
                    .For<UnitTestSecureAConfig>();

            // Act & Assert
            Assert.IsInstanceOfType(accessor, typeof(ISecureJsonConfigAccessorFor<UnitTestSecureAConfig>));
        }

        [TestMethod]
        [TestCategory("Registration")]
        public void For_InvalidType_Throw()
        {
            // Arrange
            ISecureJsonConfigAccessor accessor =
                new SecureJsonConfigAccessor(new NullLogger<IConfigAccessor>(), this.capabilityService!, this.encryptionService)
                    .SetEncryptionContext(this.encryptionContext!)
                    .SetConfigService(this.configService)
                    .SetAccess(this.capabilityToken);

            // Act & Assert
            Assert.ThrowsException<InvalidOperationException>(() => accessor.For<UnitTestSecureBConfig>());
        }

        #endregion

        #region SecureAccessorTests ‑ KeyHandling

        [TestMethod]
        [TestCategory("KeyHandling")]
        public void Set_WithUnknownKey_Throw()
        {
            ISecureJsonConfigAccessorFor<UnitTestSecureAConfig> accessor =
                new SecureJsonConfigAccessor(new NullLogger<IConfigAccessor>(), this.capabilityService!, this.encryptionService)
                    .SetEncryptionContext(this.encryptionContext!)
                    .SetConfigService(this.configService)
                    .SetConfigHandler(this.configHandler)
                    .SetAccess(this.capabilityToken)
                    .For<UnitTestSecureAConfig>();

            Assert.ThrowsException<UnauthorizedAccessException>(() => accessor.Set("DoesNotExist", 123));
        }

        [TestMethod]
        [TestCategory("KeyHandling")]
        public void Set_WithUnconvertibleString_PersistsButGetReturnsEmptyGuid()
        {
            // Arrange
            this.configService.Register(typeof(UnitTestSecureAConfig), this.secureParams!, this.capabilityToken);

            ISecureJsonConfigAccessorFor<UnitTestSecureAConfig> accessor =
                new SecureJsonConfigAccessor(new NullLogger<IConfigAccessor>(), this.capabilityService!, this.encryptionService)
                    .SetEncryptionContext(this.encryptionContext!)
                    .SetConfigService(this.configService)
                    .SetConfigHandler(this.configHandler)
                    .SetAccess(this.capabilityToken)
                    .For<UnitTestSecureAConfig>();

            // Act
            accessor.Set(nameof(UnitTestSecureAConfig.FieldA), "not-a-guid");

            Guid value = accessor.Get<Guid>(nameof(UnitTestSecureAConfig.FieldA));

            // Assert
            Assert.AreEqual(Guid.Empty, value, "Invalid GUID string should deserialize to Guid.Empty.");
        }

        #endregion

        #region SecureAccessorTests: Accessors

        [TestMethod]
        [TestCategory("Accessors")]
        public void Get_WithInvalidReadToken_Throw()
        {
            SecureJsonConfigParams secureParams = new(Read: CapabilityValue.Limited);

            // Arrange            
            this.configService.Register(typeof(UnitTestSecureAConfig), secureParams, this.capabilityToken);

            // Act
            ISecureJsonConfigAccessorFor<UnitTestSecureAConfig> accessor =
                new SecureJsonConfigAccessor(new NullLogger<IConfigAccessor>(), this.capabilityService!, this.encryptionService)
                    .SetEncryptionContext(this.encryptionContext!)
                    .SetConfigService(this.configService)
                    .SetConfigHandler(this.configHandler)
                    .SetAccess(new CapabilityToken(Guid.NewGuid()))
                    .For<UnitTestSecureAConfig>();

            // Assert
            Assert.ThrowsException<UnauthorizedAccessException>(() => accessor.Get<string>("FieldB"));
        }

        [TestMethod]
        [TestCategory("Accessors")]
        public void Get_WithUnknownProperty_Throw()
        {
            // Arrange
            this.configService.Register(typeof(UnitTestSecureAConfig), this.secureParams!, this.capabilityToken);

            ISecureJsonConfigAccessorFor<UnitTestSecureAConfig> accessor = new SecureJsonConfigAccessor(
                new NullLogger<IConfigAccessor>(), this.capabilityService!, this.encryptionService)
                    .SetEncryptionContext(this.encryptionContext!)
                    .SetConfigService(this.configService)
                    .SetConfigHandler(this.configHandler)
                    .SetAccess(this.capabilityToken)
                    .For<UnitTestSecureAConfig>();

            // Act & Assert
            Assert.ThrowsException<KeyNotFoundException>(() => accessor.Get<string>("DoesNotExist"));
        }

        #endregion

        #region SecureAccessorTests: Persistence

        [TestMethod]
        [TestCategory("Persistence")]
        public async Task Persistence_SetGet_NonSecureValue_RoundTrips()
        {
            // Arrange
            this.configService.Register(typeof(UnitTestSecureAConfig), this.secureParams!, this.capabilityToken);

            ISecureJsonConfigAccessorFor<UnitTestSecureAConfig> accessor = new SecureJsonConfigAccessor(
                new NullLogger<IConfigAccessor>(), this.capabilityService!, this.encryptionService)
                    .SetEncryptionContext(this.encryptionContext!)
                    .SetConfigService(this.configService)
                    .SetConfigHandler(this.configHandler)
                    .SetAccess(this.capabilityToken)
                    .For<UnitTestSecureAConfig>();

            // Act
            Guid testValue = Guid.NewGuid();
            accessor.Set("FieldA", testValue);
            Guid result = accessor.Get<Guid>("FieldA");

            await accessor.SaveAsync();

            // Assert
            string configFilePath = Path.Combine(this.msTestHelpers.TempDirectory, "UnitTestSecureAConfig.json");
            string fileContents = File.ReadAllText(configFilePath);

            Assert.AreEqual(testValue, result);
            Assert.IsTrue(fileContents.Contains(testValue.ToString()), "Config file should store the value in plaintext.");
        }

        [TestMethod]
        [TestCategory("Persistence")]
        public async Task Set_And_Get_SensitiveValue_RoundTrips()
        {
            // Arrange
            this.configService.Register(typeof(UnitTestSecureAConfig), this.secureParams!, this.capabilityToken);

            ISecureJsonConfigAccessorFor<UnitTestSecureAConfig> accessor = new SecureJsonConfigAccessor(
                new NullLogger<IConfigAccessor>(), this.capabilityService!, this.encryptionService)
                    .SetEncryptionContext(this.encryptionContext!)
                    .SetConfigService(this.configService)
                    .SetConfigHandler(this.configHandler)
                    .SetAccess(this.capabilityToken)
                    .For<UnitTestSecureAConfig>();

            // Act
            string testValue = "UltraSecret";
            accessor.Set("FieldB", testValue);
            string result = accessor.Get<string>("FieldB") ?? string.Empty;

            await accessor.SaveAsync();

            // Assert
            string configFilePath = Path.Combine(this.msTestHelpers.TempDirectory, "UnitTestSecureAConfig.json");
            string fileContents = File.ReadAllText(configFilePath);

            Assert.AreEqual(testValue, result);
            Assert.IsFalse(fileContents.Contains(testValue), "Config file should not store the sensitive value in plaintext.");
        }

        [TestMethod]
        [TestCategory("Persistence")]
        public async Task SaveAsync_WithModifiedInstance_PersistsChanges()
        {
            // Arrange
            this.configService.Register(typeof(UnitTestSecureAConfig), this.secureParams!, this.capabilityToken);

            ISecureJsonConfigAccessorFor<UnitTestSecureAConfig> accessor = new SecureJsonConfigAccessor(
                new NullLogger<IConfigAccessor>(), this.capabilityService!, this.encryptionService)
                    .SetEncryptionContext(this.encryptionContext!)
                    .SetConfigService(this.configService)
                    .SetConfigHandler(this.configHandler)
                    .SetAccess(this.capabilityToken)
                    .For<UnitTestSecureAConfig>();

            // Act
            UnitTestSecureAConfig config = accessor.Get();
            config.FieldB = "VerySecret";

            await accessor.SaveAsync(config);

            UnitTestSecureAConfig roundTrip = accessor.Get();

            // Assert
            Assert.AreEqual("VerySecret", roundTrip.FieldB);
        }

        [TestMethod]
        [TestCategory("Persistence")]
        public async Task SaveAsync_WithModifiedSettings_PersistsToDisk()
        {
            // Arrange
            this.configService.Register(typeof(UnitTestSecureAConfig), this.secureParams!, this.capabilityToken);

            ISecureJsonConfigAccessorFor<UnitTestSecureAConfig> accessor = new SecureJsonConfigAccessor(
                new NullLogger<IConfigAccessor>(), this.capabilityService!, this.encryptionService)
                    .SetEncryptionContext(this.encryptionContext!)
                    .SetConfigService(this.configService)
                    .SetConfigHandler(this.configHandler)
                    .SetAccess(this.capabilityToken)
                    .For<UnitTestSecureAConfig>();

            // Act
            Guid updated = Guid.NewGuid();
            accessor.Set(nameof(UnitTestSecureAConfig.FieldA), updated);

            await accessor.SaveAsync();

            // Assert
            string path = Path.Combine(this.msTestHelpers.TempDirectory, "UnitTestSecureAConfig.json");
            string json = File.ReadAllText(path);

            Assert.IsTrue(json.Contains(updated.ToString()),
                "User settings should include the updated FieldA value.");
        }

        #endregion
    }

}