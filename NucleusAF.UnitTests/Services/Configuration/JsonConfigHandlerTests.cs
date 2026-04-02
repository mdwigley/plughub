using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using NucleusAF.Interfaces.Models;
using NucleusAF.Interfaces.Services.Capabilities;
using NucleusAF.Interfaces.Services.Configuration;
using NucleusAF.Interfaces.Services.Configuration.Accessors;
using NucleusAF.Models.Capabilities;
using NucleusAF.Models.Configuration.Parameters;
using NucleusAF.Services.Capabilities;
using NucleusAF.Services.Capabilities.Accessors;
using NucleusAF.Services.Capabilities.Handlers;
using NucleusAF.Services.Configuration;
using NucleusAF.Services.Configuration.Accessors;
using NucleusAF.Services.Configuration.Handlers;

namespace NucleusAF.UnitTests.Services.Configuration
{
    [TestClass]
    public sealed class JsonConfigHandlerTests
    {
        private readonly MSTestHelpers msTestHelpers = new();
        private CapabilityService? capabilityService;
        private ConfigService? configService;
        private ConfigHandler? configHandler;
        private JsonConfigParams fileParams = new();
        private readonly CapabilityToken capabilityToken = new(Guid.NewGuid());

        internal class UnitTestConfigItem(string name = "Unknown")
        {
            public UnitTestConfigItem() : this("Unknown") { }
            public string Name { get; set; } = name;
        }
        internal class UnitTestAConfig
        {
            public int FieldA { get; set; } = 50;
            public bool FieldB { get; set; } = false;
            public float FieldC { get; } = 2.71828f;
            public List<UnitTestConfigItem> SampleList { get; set; } =
            [
                new UnitTestConfigItem("TestValueA1"),
                new UnitTestConfigItem("TestValueA2"),
                new UnitTestConfigItem("TestValueA3")
            ];

        }
        internal class UnitTestBConfig
        {
            public required string FieldA { get; set; } = "NucleusAF";
            public int FieldB { get; set; } = 100;
            public float FieldC { get; } = 3.14f;

            public Dictionary<string, UnitTestConfigItem> SampleDictionary { get; set; } =
                new Dictionary<string, UnitTestConfigItem>
                {
                    { "Key1", new UnitTestConfigItem("DictValue1") },
                    { "Key2", new UnitTestConfigItem("DictValue2") },
                    { "Key3", new UnitTestConfigItem("DictValue3") }
                };
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
            this.configHandler = new JsonConfigHandler(new NullLogger<IConfigHandler>(), this.capabilityService);

            this.configService = new ConfigService(
                [new JsonConfigAccessor(new NullLogger<IConfigAccessor>(), this.capabilityService)],
                [this.configHandler],
                new NullLogger<IConfigService>(),
                this.capabilityService,
                this.msTestHelpers.TempDirectory);
        }

        [TestCleanup]
        public void Cleanup()
        {
            if (this.configService != null)
                ((IDisposable)this.configService).Dispose();

            this.msTestHelpers!.Dispose();
        }


        #region ConfigServiceTests: Registration

        //TODO: Check if this needs to be removed
        /*
        [TestMethod]
        [TestCategory("Registration")]
        public void RegisterConfigs_WithExistingRegistration_InvalidOperationThrows()
        {
            // Arrange
            UserConfigServiceParams attackerParams =
                new(Owner: this.tokenService!.CreateToken(),
                    Read: this.tokenService!.CreateToken(),
                    Write: this.tokenService!.CreateToken());

            this.configService!.RegisterConfigs([typeof(UnitTestAConfig)], this.fileParams, this.capabilityToken);

            // Act & Assert
            Assert.ThrowsException<InvalidOperationException>(() =>
                this.configService!.RegisterConfigs([typeof(UnitTestAConfig)], attackerParams));
        }
        */


        [TestMethod]
        [TestCategory("Registration")]
        public void RegisterConfigs_ValidTypes_RegistersSuccessfully()
        {
            // Arrange
            List<Type> configTypes = [typeof(UnitTestAConfig), typeof(UnitTestBConfig)];

            string typeAlphaConfigPath =
                Path.Combine(this.msTestHelpers.TempDirectory, "UnitTestAConfig.json");
            string typeBetaConfigPath =
                Path.Combine(this.msTestHelpers.TempDirectory, "UnitTestBConfig.json");

            // Act
            this.msTestHelpers.CreateTempFile("{\r\n  \"FieldA\": 80,\r\n  \"FieldB\": true\r\n}", typeAlphaConfigPath);
            this.msTestHelpers.CreateTempFile("{\r\n  \"FieldA\": \"Yazza!\",\r\n  \"FieldB\": 60\r\n}", typeBetaConfigPath);

            this.configService!.Register(configTypes, this.fileParams, this.capabilityToken);

            int value = this.configHandler!.GetValue<int>(typeof(UnitTestAConfig), "FieldA", this.capabilityToken);

            // Assert
            Assert.AreEqual(80, value);
            Assert.AreEqual("Yazza!", this.configHandler!.GetValue<string>(typeof(UnitTestBConfig), "FieldA", this.capabilityToken));
        }


        [TestMethod]
        [TestCategory("Registration")]
        public void RegisterConfigs_InvalidConfig_Throw()
        {
            // Arrnage
            List<Type> configTypes = [typeof(UnitTestAConfig)];

            string typeAlphaConfigPath = Path.Combine(this.msTestHelpers.TempDirectory, "UnitTestAConfig.json");

            // Act
            this.msTestHelpers.CreateTempFile("INVALID_JSON", typeAlphaConfigPath);

            // Assert
            IDictionary<Type, ICapabilityToken> result = this.configService!.Register(configTypes, this.fileParams, this.capabilityToken);

            // Assert
            Assert.IsTrue(result.ContainsKey(typeof(UnitTestAConfig)));

            IJsonConfigAccessorFor<UnitTestAConfig> accessor =
                new JsonConfigAccessor(new NullLogger<IConfigAccessor>(), this.capabilityService!)
                    .SetConfigService(this.configService!)
                    .SetConfigHandler(new JsonConfigHandler(new NullLogger<IConfigHandler>(), this.capabilityService!))
                    .SetAccess(this.capabilityToken)
                    .For<UnitTestAConfig>();

            UnitTestAConfig config;

            UnauthorizedAccessException ex = Assert.ThrowsException<UnauthorizedAccessException>(() =>
                config = accessor.Get());
        }


        [TestCategory("Registration")]
        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void RegisterConfig_InvalidJson_FallsBackToDefaultInstance(bool duringRegistration)
        {
            // Arrange
            string typeConfigPath =
                Path.Combine(this.msTestHelpers.TempDirectory!, "UnitTestAConfig.json");

            if (duringRegistration)
            {
                // Act
                this.msTestHelpers.CreateTempFile("INVALIDJSON", typeConfigPath);

                // Assert
                this.configService!.Register(typeof(UnitTestAConfig), this.fileParams, this.capabilityToken);

                UnitTestAConfig config =
                    (UnitTestAConfig)this.configHandler!.GetConfigInstance(typeof(UnitTestAConfig), this.capabilityToken);

                Assert.AreEqual(50, config.FieldA);
                Assert.IsFalse(config.FieldB);
            }
            else
            {
                // Act
                this.msTestHelpers.CreateTempFile("{ \"FieldA\": 100 }", typeConfigPath);
                this.configService!.Register(typeof(UnitTestAConfig), this.fileParams, this.capabilityToken);

                this.msTestHelpers.CreateTempFile("INVALIDJSON", typeConfigPath);

                // Assert
                UnitTestAConfig config =
                    (UnitTestAConfig)this.configHandler!.GetConfigInstance(typeof(UnitTestAConfig), this.capabilityToken);

                Assert.AreEqual(100, config.FieldA);
                Assert.IsFalse(config.FieldB);
            }
        }


        [TestMethod]
        [TestCategory("Registration")]
        public void UnregisterConfig_ValidType_RemovesConfigAndSettings()
        {
            // Arrange
            this.configService!.Register(typeof(UnitTestAConfig), this.fileParams, this.capabilityToken);

            // Act
            object obj = this.configHandler!.GetConfigInstance(typeof(UnitTestAConfig), this.capabilityToken);

            this.configService!.Unregister(typeof(UnitTestAConfig), this.capabilityToken);

            // Assert
            Assert.IsInstanceOfType<UnitTestAConfig>(obj);

            Assert.ThrowsException<UnauthorizedAccessException>(() =>
                this.configHandler!.GetConfigInstance(typeof(UnitTestAConfig), this.capabilityToken));
        }

        [TestMethod]
        [TestCategory("Registration")]
        public void UnregisterConfigs_MultipleTypes_RemovesAll()
        {
            // Arrange
            Type[] types = [typeof(UnitTestAConfig), typeof(UnitTestBConfig)];

            // Act
            this.configService!.Register(types, this.fileParams, this.capabilityToken);
            this.configService!.Unregister(types, this.capabilityToken);

            // Assert
            foreach (Type type in types)
                Assert.ThrowsException<UnauthorizedAccessException>(() =>
                        this.configHandler!.GetConfigInstance(type, this.capabilityToken));
        }

        [TestMethod]
        [TestCategory("Registration")]
        public void UnregisterConfig_InvalidToken_Throw()
        {
            // Arrange
            CapabilityToken attackerToken = new(Guid.NewGuid());

            this.configService!.Register(typeof(UnitTestAConfig), this.fileParams, this.capabilityToken);

            // Act & Assert
            UnauthorizedAccessException ex = Assert.ThrowsException<UnauthorizedAccessException>(() =>
                this.configService!.Unregister(typeof(UnitTestAConfig), attackerToken));

            StringAssert.Contains(ex.Message, "Unregister denied");
        }

        #endregion

        #region ConfigServiceTests: Accessors

        [TestMethod]
        [TestCategory("Accessors")]
        public void GetConfigInstance_UnregisteredType_Throw()
        {
            // Arrange & Act & Assert
            UnauthorizedAccessException ex = Assert.ThrowsException<UnauthorizedAccessException>(() =>
                this.configHandler!.GetConfigInstance(typeof(UnitTestAConfig), this.capabilityToken)
            );

            StringAssert.Contains(ex.Message, "Access denied");
        }

        [TestMethod]
        [TestCategory("Accessors")]
        public void GetEnvConfig_ValidEnvironmentVariable_ReturnsExpectedValue()
        {
            string testKey = "TEST_ENV_VAR_" + Guid.NewGuid().ToString("N");
            string expectedValue = "test_value_123";
            Environment.SetEnvironmentVariable(testKey, expectedValue);

            IConfiguration envConfig = Nucleus.GetEnvConfig();
            string? actualValue = envConfig[testKey];

            Environment.SetEnvironmentVariable(testKey, null);

            Assert.AreEqual(expectedValue, actualValue);
        }

        [TestMethod]
        [TestCategory("Accessors")]
        public void GetEnvConfig_EnvironmentVariableChangedAfterRebuild_ReflectsUpdatedValue()
        {
            string testKey = "TEST_ENV_VAR_" + Guid.NewGuid().ToString("N");
            string initialValue = "initial_value";
            string updatedValue = "updated_value";
            Environment.SetEnvironmentVariable(testKey, initialValue);

            IConfiguration envConfig1 = Nucleus.GetEnvConfig();
            string? actualValue1 = envConfig1[testKey];
            Assert.AreEqual(initialValue, actualValue1);

            Environment.SetEnvironmentVariable(testKey, updatedValue);

            IConfiguration envConfig2 = Nucleus.GetEnvConfig();
            string? actualValue2 = envConfig2[testKey];

            Environment.SetEnvironmentVariable(testKey, null);

            Assert.AreEqual(updatedValue, actualValue2);
        }

        [TestCategory("Accessors")]
        [TestMethod]
        [DataRow(true, "test-value")]
        [DataRow(false, null)]
        public void GetSetting_ValidAndInvalidToken_EnforcesAccess(bool useValidToken, string? expectedValue)
        {
            // Arrange
            CapabilityToken token = new(Guid.NewGuid());

            this.configService!.Register(typeof(UnitTestAConfig), this.fileParams, this.capabilityToken);
            this.configHandler!.SetValue(typeof(UnitTestAConfig), "FieldA", "test-value", this.capabilityToken);

            if (useValidToken)
            {
                // Act
                string? result = this.configHandler!.GetValue<string>(
                    typeof(UnitTestAConfig),
                    "FieldA",
                    this.capabilityToken);

                // Assert
                Assert.AreEqual(expectedValue, result);
            }
            else
            {
                // Act & Assert
                Assert.ThrowsException<UnauthorizedAccessException>(() =>
                    this.configHandler!.GetValue<string>(
                        typeof(UnitTestAConfig),
                        "FieldA",
                        token));
            }
        }

        #endregion

        #region ConfigServiceTests: Mutators

        [TestMethod]
        [TestCategory("Mutators")]
        public void SetSetting_RegisteredType_UpdatesValueAndFiresEvent()
        {
            // Arrange
            bool eventFired = false;
            this.configService!.SettingChanged += (s, e) => eventFired = true;

            // Act
            this.configService!.Register([typeof(UnitTestAConfig)], this.fileParams, this.capabilityToken);
            this.configHandler!.SetValue(typeof(UnitTestAConfig), "FieldA", 85, this.capabilityToken);

            int value = this.configHandler!.GetValue<int>(typeof(UnitTestAConfig), "FieldA", this.capabilityToken);

            // Assert
            Assert.AreEqual(85, value);
            Assert.IsTrue(eventFired, "Setting changes should fire events");
        }

        [TestMethod]
        [TestCategory("Mutators")]
        public void SetSetting_UnregisteredType_Throw()
        {
            UnauthorizedAccessException ex = Assert.ThrowsException<UnauthorizedAccessException>(() =>
                this.configHandler!.SetValue(typeof(UnitTestAConfig), "FieldA", 80));

            StringAssert.Contains(ex.Message, "Access denied");
        }

        [TestMethod]
        [TestCategory("Mutators")]
        public void SetSetting_TypeConversion_HandlesCorrectlyAndFiresEvent()
        {
            // Arrange
            bool eventFired = false;
            this.configService!.SettingChanged += (s, e) => eventFired = true;

            // Act
            this.configService!.Register([typeof(UnitTestAConfig)], this.fileParams, this.capabilityToken);
            this.configHandler!.SetValue(typeof(UnitTestAConfig), "FieldA", 80, this.capabilityToken);
            this.configHandler!.SetValue(typeof(UnitTestAConfig), "FieldA", "90", this.capabilityToken);

            // Assert
            string value = this.configHandler!.GetValue<string>(typeof(UnitTestAConfig), "FieldA", this.capabilityToken) ?? string.Empty;

            Assert.IsTrue(eventFired, "Type change should fire event");
            Assert.AreEqual("90", value);
        }

        //TODO: Remove or fix
        /*
        [TestMethod]
        [TestCategory("Mutators")]
        public void GetBaseConfigFileContents_FileNotFound_Throw()
        {
            // Arrnage
            Token token = this.tokenService!.CreateToken();

            // Act
            this.configService!.RegisterConfig(typeof(UnitTestBConfig), this.fileParams, this.capabilityToken);

            // Assert
            Assert.ThrowsException<KeyNotFoundException>(() =>
                this.configService!.GetDefaultConfigFileContents(typeof(UnitTestAConfig), this.capabilityToken));
        }
        */

        [TestMethod]
        [TestCategory("Mutators")]
        public void SetSetting_ListProperty_UpdatesListAndFiresEvent()
        {
            // Arrange
            bool eventFired = false;
            this.configService!.SettingChanged += (s, e) => eventFired = true;
            List<string> testList = ["one", "two", "three"];

            // Act
            this.configService!.Register([typeof(UnitTestAConfig)], this.fileParams, this.capabilityToken);
            this.configHandler!.SetValue(typeof(UnitTestAConfig), "SampleList", testList, this.capabilityToken);

            List<string> value = this.configHandler!.GetValue<List<string>>(typeof(UnitTestAConfig), "SampleList", this.capabilityToken) ?? [];

            // Assert
            CollectionAssert.AreEqual(testList, value);
            Assert.IsTrue(eventFired, "Setting changes should fire events");
        }

        [TestMethod]
        [TestCategory("Mutators")]
        public void SetSetting_BindsDictionaryPropertyCorrectly()
        {
            // Arrange
            Dictionary<string, UnitTestConfigItem> expectedDict = new()
            {
                { "Key1", new UnitTestConfigItem("NewValue1") },
                { "Key2", new UnitTestConfigItem("NewValue2") },
                { "Key3", new UnitTestConfigItem("NewValue3") }
            };

            this.configService!.Register([typeof(UnitTestBConfig)], this.fileParams, this.capabilityToken);

            // Act
            this.configHandler!.SetValue(typeof(UnitTestBConfig), "SampleDictionary", expectedDict, this.capabilityToken);

            Dictionary<string, UnitTestConfigItem> actualDict =
                this.configHandler!.GetValue<Dictionary<string, UnitTestConfigItem>>(typeof(UnitTestBConfig), "SampleDictionary", this.capabilityToken)
                ?? [];

            // Assert
            Assert.AreEqual(3, actualDict.Count);
            Assert.AreEqual("NewValue2", actualDict["Key2"].Name);
        }

        #endregion

        #region ConfigServiceTests: Persistence

        [TestMethod]
        [TestCategory("Persistence")]
        public async Task SaveConfigInstance_ValidToken_UpdatesSettings()
        {
            // Arrange
            this.configService!.Register(typeof(UnitTestAConfig), this.fileParams, this.capabilityToken);

            // Act
            UnitTestAConfig preSave = (UnitTestAConfig)this.configHandler!.GetConfigInstance(typeof(UnitTestAConfig), this.capabilityToken);
            preSave.FieldA = 200;
            preSave.FieldB = true;

            await this.configHandler!.SaveConfigInstanceAsync(typeof(UnitTestAConfig), preSave, this.capabilityToken);

            UnitTestAConfig postSave = (UnitTestAConfig)this.configHandler!.GetConfigInstance(typeof(UnitTestAConfig), this.capabilityToken);

            // Assert
            Assert.AreEqual(200, postSave.FieldA);
            Assert.IsTrue(postSave.FieldB);
        }

        [TestMethod]
        [TestCategory("Persistence")]
        public async Task SaveConfigInstance_NullConfig_Throw()
        {
            this.configService!.Register(typeof(UnitTestAConfig), this.fileParams, this.capabilityToken);

            // Act & Assert
            await Assert.ThrowsExceptionAsync<ArgumentNullException>(async () =>
                await this.configHandler!.SaveConfigInstanceAsync(typeof(UnitTestAConfig), null!, this.capabilityToken)
            );
        }

        [TestMethod]
        [TestCategory("Persistence")]
        public async Task SaveSettingsAsync_UnregisteredConfigType_Throw()
        {
            UnauthorizedAccessException ex = await Assert.ThrowsExceptionAsync<UnauthorizedAccessException>(() =>
                this.configHandler!.SaveValuesAsync(typeof(UnitTestAConfig)));

            StringAssert.Contains(ex.Message, "Access denied");
        }

        //TODO: Remove or Fix
        /*
        [TestMethod]
        [TestCategory("Persistence")]
        public async Task GetBaseConfigFileContents_GenericType_SavesAndRetrievesContentSuccessfully()
        {
            // Arrange
            string contents = "{ \"FieldA\": 99, \"FieldB\": true }";

            // Act
            this.configService!.RegisterConfig(typeof(UnitTestAConfig), this.fileParams, this.capabilityToken);

            await this.configService!.SaveDefaultConfigFileContentsAsync(typeof(UnitTestAConfig), contents, this.capabilityToken);

            string actual = this.configService!.GetDefaultConfigFileContents(typeof(UnitTestAConfig), this.capabilityToken);

            // Assert
            Assert.AreEqual(contents, actual);
        }
        */

        //TODO: Remove or Fix
        /*
        [TestCategory("Persistence")]
        [DataTestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public async Task SaveDefaultConfigFileContentsAsync_MissingDirectory_CreatesDirectoryAndSavesFile(bool deleteDirectory)
        {
            // Arrange
            this.configService!.RegisterConfig(typeof(UnitTestAConfig), this.fileParam, this.capabilityTokens);

            if (deleteDirectory && Directory.Exists(this.msTestHelpers.TempDirectory))
                Directory.Delete(this.msTestHelpers.TempDirectory, recursive: true);
            else
                Directory.CreateDirectory(this.msTestHelpers.TempDirectory);

            string contents = "{ \"FieldA\": 1, \"FieldB\": false }";
            string settingPath = Path.Combine(this.msTestHelpers.TempDirectory, "UnitTestAConfig.json");

            // Act
            await this.configService!.SaveDefaultConfigFileContentsAsync(typeof(UnitTestAConfig), contents, this.capabilityToken);

            // Assert
            Assert.IsTrue(Directory.Exists(Path.GetDirectoryName(settingPath)), "Config directory should exist");
            Assert.IsTrue(File.Exists(settingPath), "Config file should be created");
            Assert.AreEqual(contents, File.ReadAllText(settingPath), "File content should match");
        }
        */

        #endregion

        #region ConfigServiceTests: Events

        [TestMethod]
        [TestCategory("Events")]
        public async Task OnConfigChanged_ValidType_ReloadsSettings()
        {
            // Arrange
            List<Type> configTypes = [typeof(UnitTestAConfig)];

            JsonConfigParams userConfigServiceParams = new(ReloadOnChange: true);

            string configPath = Path.Combine(this.msTestHelpers.TempDirectory, "UnitTestAConfig.json");

            this.msTestHelpers.CreateTempFile("{\"FieldA\": 80, \"FieldB\": true}", configPath);
            this.configService!.Register(configTypes, userConfigServiceParams, this.capabilityToken);

            TaskCompletionSource<bool> reloadCompleted = new();

            this.configService!.ConfigReloaded += (sender, e) =>
            {
                if (e.ConfigType == typeof(UnitTestAConfig))
                    reloadCompleted.TrySetResult(true);
            };

            this.msTestHelpers.CreateTempFile("{\"FieldA\": 70, \"FieldB\": true}", configPath);

            // Act
            Task completedTask = await Task.WhenAny(reloadCompleted.Task, Task.Delay(2000));

            // Assert
            if (completedTask != reloadCompleted.Task)
                Assert.Fail("TypeConfigurationReloaded event not raised within timeout");

            Assert.AreEqual(70, this.configHandler!.GetValue<int>(typeof(UnitTestAConfig), "FieldA", this.capabilityToken));
        }

        [TestMethod]
        [TestCategory("Events")]
        public async Task OnConfigChanged_MultipleReloads_ProcessesSequentially()
        {
            // Arrange
            List<Type> configTypes = [typeof(UnitTestAConfig)];

            JsonConfigParams userConfigServiceParams = new(ReloadOnChange: true);

            string configPath = Path.Combine(this.msTestHelpers.TempDirectory, "UnitTestAConfig.json");
            this.msTestHelpers.CreateTempFile("{\"FieldA\": 80}", configPath);
            this.configService!.Register(configTypes, userConfigServiceParams, this.capabilityToken);

            int reloadCount = 0;
            List<int> data = [];

            Queue<TaskCompletionSource<bool>> reloadQueue = new();

            this.configService!.ConfigReloaded += (s, e) =>
            {
                if (e.ConfigType == typeof(UnitTestAConfig))
                {
                    reloadCount++;

                    if (reloadQueue.Count > 0)
                        reloadQueue.Dequeue().TrySetResult(true);
                }
            };

            // Act
            for (int i = 0; i < 3; i++)
            {
                TaskCompletionSource<bool> reloadCompletion = new();
                reloadQueue.Enqueue(reloadCompletion);

                int newFieldA = 70 + i;
                this.msTestHelpers.CreateTempFile($"{{\"FieldA\": {newFieldA}}}", configPath);

                await Task.WhenAny(reloadCompletion.Task, Task.Delay(2000));

                int value = this.configHandler!.GetValue<int>(typeof(UnitTestAConfig), "FieldA", this.capabilityToken);

                data.Add(value);
            }

            // Assert
            for (int i = 0; i < 3; i++)
                Assert.AreEqual(data[i], 70 + i, $"FieldA mismatch after reload {i + 1}");

            Assert.AreEqual(3, reloadCount, "Should have triggered 3 reload events");
        }

        [TestMethod]
        [TestCategory("Events")]
        public async Task BadReload_MalformedJson_DoesNotOverwriteSettings()
        {
            // Arrange
            JsonConfigParams userConfigServiceParams = new(ReloadOnChange: true);

            this.configService!.Register(typeof(UnitTestAConfig), userConfigServiceParams, this.capabilityToken);

            int initial = this.configHandler!.GetValue<int>(typeof(UnitTestAConfig), nameof(UnitTestAConfig.FieldA), this.capabilityToken);

            string defaultPath = Path.Combine(this.msTestHelpers.TempDirectory, $"{nameof(UnitTestAConfig)}.json");

            // Act
            this.msTestHelpers.CreateTempFile("}{ not: json", defaultPath);

            await Task.Delay(800);

            int after = this.configHandler!.GetValue<int>(typeof(UnitTestAConfig), nameof(UnitTestAConfig.FieldA), this.capabilityToken);

            // Assert
            Assert.AreEqual(initial, after, "Settings should remain unchanged when a reload encounters malformed JSON.");
        }

        #endregion

        #region ConfigServiceTests: Security
        //TODO: Remove or Fix
        /*
        [TestCategory("Security")]
        [DataTestMethod]
        [DataRow("SaveConfigInstance")]
        [DataRow("GetBaseConfigFileContents")]
        [DataRow("SaveDefaultConfigFileContents")]
        [DataRow("SetSetting")]
        public async Task SecurityCriticalOperations_InvalidToken_Throw(string operationType)
        {
            // Arrange
            ITokenSet invalidTokenSet = this.tokenService!.CreateTokenSet();

            switch (operationType)
            {
                case "SaveConfigInstance":
                    // Act
                    this.configService!.RegisterConfig(typeof(UnitTestAConfig), this.fileParams, this.capabilityToken);
                    UnitTestAConfig updatedConfig = new() { FieldA = 200, FieldB = true };

                    // Assert
                    await Assert.ThrowsExceptionAsync<UnauthorizedAccessException>(() =>
                        this.configService!.SaveConfigInstanceAsync(typeof(UnitTestAConfig), updatedConfig, invalidTokenSet));
                    break;

                case "GetBaseConfigFileContents":
                    // Act
                    this.configService!.RegisterConfig(typeof(UnitTestAConfig), this.fileParams, this.capabilityToken);
                    await this.configService!.SaveDefaultConfigFileContentsAsync(typeof(UnitTestAConfig), "{}", this.capabilityToken);

                    // Assert
                    Assert.ThrowsException<UnauthorizedAccessException>(() =>
                        this.configService!.GetDefaultConfigFileContents(typeof(UnitTestAConfig), invalidTokenSet));
                    break;

                case "SaveDefaultConfigFileContents":
                    // Act
                    this.configService!.RegisterConfig(typeof(UnitTestBConfig), this.fileParams, this.capabilityToken);

                    // Assert
                    await Assert.ThrowsExceptionAsync<UnauthorizedAccessException>(() =>
                        this.configService!.SaveDefaultConfigFileContentsAsync(typeof(UnitTestBConfig), "{}", invalidTokenSet));
                    break;

                case "SetSetting":
                    // Act
                    this.configService!.RegisterConfig(typeof(UnitTestAConfig), this.fileParams, this.capabilityToken);

                    // Assert
                    Assert.ThrowsException<UnauthorizedAccessException>(() =>
                        this.configService!.SetSetting(typeof(UnitTestAConfig), "ApiKey", "new-value", invalidTokenSet));
                    break;
            }
        }
        */

        #endregion

        #region ConfigServiceTests: Concurrency

        [TestMethod]
        [TestCategory("Concurrency")]
        public async Task ConcurrentReadersWriters_DoNotThrow()
        {
            // Arrange
            const int iterations = 200;
            const int readerTasks = 6;
            const int writerTasks = 4;

            this.configService!.Register(typeof(UnitTestAConfig), this.fileParams, this.capabilityToken);

            // Act 
            IEnumerable<Task> writers = Enumerable.Range(0, writerTasks).Select(_ => Task.Run(() =>
            {
                for (int i = 0; i < iterations; i++)
                    this.configHandler!.SetValue(typeof(UnitTestAConfig), nameof(UnitTestAConfig.FieldA), i, this.capabilityToken);
            }));

            IEnumerable<Task> readers = Enumerable.Range(0, readerTasks).Select(_ => Task.Run(() =>
            {
                for (int i = 0; i < iterations; i++)
                    _ = this.configHandler!.GetValue<int>(typeof(UnitTestAConfig), nameof(UnitTestAConfig.FieldA), this.capabilityToken);
            }));

            // Assert
            await Task.WhenAll(writers.Concat(readers));

            _ = this.configHandler!.GetValue<int>(typeof(UnitTestAConfig), nameof(UnitTestAConfig.FieldA), this.capabilityToken);
        }

        #endregion
    }
}