using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NucleusAF.Extensions;
using NucleusAF.Interfaces.Abstractions.CompositeRegistry;
using NucleusAF.Interfaces.Models;
using NucleusAF.Interfaces.Models.Configuration;
using NucleusAF.Interfaces.Services.Capabilities;
using NucleusAF.Interfaces.Services.Configuration;
using NucleusAF.Interfaces.Services.Configuration.Accessors;
using NucleusAF.Interfaces.Services.Configuration.Handlers;
using NucleusAF.Interfaces.Services.Encryption;
using NucleusAF.Models.Capabilities;
using NucleusAF.Models.Configuration.Parameters;
using NucleusAF.Models.Encryption;
using NucleusAF.Utility;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NucleusAF.Services.Configuration.Handlers
{
    public sealed class SecureValueJsonConverter : JsonConverter<SecureValue>
    {
        public override SecureValue? Read(ref Utf8JsonReader r, Type t, JsonSerializerOptions o)
        {
            if (r.TokenType == JsonTokenType.String)
            {
                return new SecureValue(r.GetString()!);
            }

            if (r.TokenType == JsonTokenType.StartObject)
            {
                using JsonDocument doc = JsonDocument.ParseValue(ref r);
                string base64 = doc.RootElement.GetProperty("EncryptedBase64").GetString()!;

                return new SecureValue(base64);
            }

            throw new JsonException("[SecureValueJsonConverter] Invalid SecureValue payload.");
        }
        public override void Write(Utf8JsonWriter w, SecureValue v, JsonSerializerOptions o) =>
            w.WriteStringValue(v.EncryptedBase64);
    }

    public class SecureJsonConfigHandler : JsonConfigHandler,
        ISecureJsonConfigHandler,
        IConfigHandler<SecureJsonConfigParams>,
        ICompositeRegistryHandlerFor<ISecureJsonConfigAccessor>
    {
        public override Type Key => typeof(SecureJsonConfigParams);

        protected IEncryptionService EncryptionService;

        public SecureJsonConfigHandler(ILogger<IConfigHandler> logger, ICapabilityService capabilityService, IEncryptionService encryptionService)
            : base(logger, capabilityService)
        {
            ArgumentNullException.ThrowIfNull(encryptionService);

            this.EncryptionService = encryptionService;

            this.Logger.LogDebug("[SecureJsonConfigHandler] Initialized.");
        }

        #region SecureJsonConfigHandler: Registration

        public override ICapabilityToken Register(Type configType, IConfigParams configParams, IConfigService configService, ICapabilityToken? token = null)
        {
            ArgumentNullException.ThrowIfNull(configType);
            ArgumentNullException.ThrowIfNull(configParams);
            ArgumentNullException.ThrowIfNull(configService);

            if (configParams is not SecureJsonConfigParams p)
            {
                this.Logger.LogError("[SecureJsonConfigHandler] Invalid config params type for {ConfigType}. Expected SecureJsonConfigParams, got {ParamType}", configType.Name, configParams.GetType().Name);

                return CapabilityToken.None;
            }

            if (!HasSecureFields(configType))
                this.Logger.LogInformation("[SecureJsonConfigHandler] Type '{ConfigType}' contains no SecureValue properties. Using the secure provider is valid, but consider RegisterConfig for faster startup if encryption is not required.", configType.Name);

            JsonSerializerOptions jsonOptions = p.JsonSerializerOptions ?? this.JsonOptions;
            jsonOptions.Converters.Add(new SecureValueJsonConverter());

            string defaultConfigFilePath = this.ResolveLocalFilePath(
                p.ConfigUriOverride,
                configService.ConfigDataDirectory,
                configType,
                "json");

            try
            {
                IEncryptionContext encryptionContext = this.GetOrCreateEncryptionContext(p, configType);

                this.EnsureEncryptedFileExists(defaultConfigFilePath, configType, encryptionContext, jsonOptions);

                ICapabilityToken registeredToken = base.Register(configType, p, configService, token);

                this.Logger.LogDebug("[SecureJsonConfigHandler] Registered secure configuration: {ConfigType}", configType.Name);

                return registeredToken;
            }
            catch (Exception ex)
            {
                this.Logger.LogError(ex, "[SecureJsonConfigHandler] Failed to register secure configuration for {ConfigType}", configType.Name);

                return CapabilityToken.None;
            }
        }

        #endregion

        #region SecureJsonConfigHandler: Secure Value Handling

        [return: MaybeNull]
        protected override T CastStoredValue<T>(object? raw)
        {
            if (raw is T correct)
                return correct;

            bool isSecureValueRequest = typeof(T) == typeof(SecureValue);
            bool rawIsSecureValue = raw is SecureValue;

            if (isSecureValueRequest && rawIsSecureValue)
                return (T)raw!;

            if (rawIsSecureValue)
                this.Logger.LogError("[SecureJsonConfigHandler] SecureValue requested as {TargetType}", typeof(T).Name);

            try
            {
                if (raw is IConvertible)
                    return (T)Convert.ChangeType(raw, typeof(T))!;
            }
            catch (Exception ex)
            {
                this.Logger.LogError(ex, "[SecureJsonConfigHandler] Failed to convert stored value to {TargetType}", typeof(T).Name);
            }

            this.Logger.LogError("[SecureJsonConfigHandler] Unable to cast stored value to {TargetType}", typeof(T).Name);

            return default!;
        }

        protected override object? GetBuildSettingsValue(IConfiguration config, PropertyInfo prop)
        {
            ArgumentNullException.ThrowIfNull(config);
            ArgumentNullException.ThrowIfNull(prop);

            bool isSecureProperty = IsSecureProperty(prop);

            return isSecureProperty ? this.ExtractSecureValue(config, prop) : base.GetBuildSettingsValue(config, prop);
        }

        #endregion

        #region SecureJsonConfigHandler: Configuration Validation

        private static bool HasSecureFields(Type configType)
        {
            PropertyInfo[] properties = configType.GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (PropertyInfo property in properties)
            {
                bool isSecureProperty = IsSecureProperty(property);

                if (isSecureProperty)
                    return true;
            }

            return false;
        }

        private static bool IsSecureProperty(PropertyInfo prop)
        {
            bool isSecureValueType = prop.PropertyType == typeof(SecureValue);
            bool hasSecureAttribute = prop.GetCustomAttribute<SecureAttribute>() != null;

            return isSecureValueType || hasSecureAttribute;
        }

        #endregion

        #region SecureJsonConfigHandler: Configuration Setup

        private IEncryptionContext GetOrCreateEncryptionContext(SecureJsonConfigParams p, Type configType)
        {
            if (p.EncryptionContext == null)
            {
                Guid deterministicId = configType.ToDeterministicGuid();

                return this.EncryptionService.GetEncryptionContext(configType, deterministicId);
            }
            else
            {
                return p.EncryptionContext;
            }
        }

        #endregion

        #region SecureJsonConfigHandler: File Operations

        protected virtual void EnsureEncryptedFileExists(string filePath, Type configType, IEncryptionContext context, JsonSerializerOptions jsonOptions)
        {
            ArgumentNullException.ThrowIfNull(filePath);
            ArgumentNullException.ThrowIfNull(configType);
            ArgumentNullException.ThrowIfNull(context);
            ArgumentNullException.ThrowIfNull(jsonOptions);

            bool fileExists = File.Exists(filePath);

            if (fileExists)
                return;

            this.EnsureDirectoryExists(filePath);

            Dictionary<string, object?> settings = this.BuildDefaultSecureSettings(configType, context);
            string jsonContent = this.SerializeSecureSettings(settings, jsonOptions);

            this.WriteSecureConfigFile(filePath, jsonContent);

            this.Logger.LogDebug("[SecureJsonConfigHandler] Created encrypted configuration file: {FilePath}", filePath);
        }
        private Dictionary<string, object?> BuildDefaultSecureSettings(Type configType, IEncryptionContext context)
        {
            Dictionary<string, object?> settings = [];
            PropertyInfo[] properties = configType.GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (PropertyInfo property in properties)
            {
                object? defaultValue = this.GetPropertyDefaultValue(property, configType);
                object? settingValue = PrepareSecureSettingValue(property, defaultValue, context);

                settings[property.Name] = settingValue;
            }

            return settings;
        }
        private object? GetPropertyDefaultValue(PropertyInfo property, Type configType)
        {
            object? rawValue = property.PropertyType.IsValueType
                ? Activator.CreateInstance(property.PropertyType)
                : null;

            bool canReadProperty = property.CanRead;

            if (canReadProperty)
            {
                try
                {
                    object? instance = Activator.CreateInstance(configType);

                    rawValue = property.GetValue(instance);
                }
                catch (Exception ex)
                {
                    this.Logger.LogWarning(ex, "[SecureJsonConfigHandler] Failed to read default value for property {PropertyName} on type {ConfigType}", property.Name, configType.Name);
                }
            }

            return rawValue;
        }
        private static object? PrepareSecureSettingValue(PropertyInfo property, object? rawValue, IEncryptionContext context)
        {
            bool isSecureProperty = IsSecureProperty(property);

            if (isSecureProperty)
            {
                SecureValue secureValue = SecureValue.From(rawValue, context);

                return secureValue.EncryptedBase64;
            }
            else return rawValue;
        }
        private string SerializeSecureSettings(Dictionary<string, object?> settings, JsonSerializerOptions jsonOptions)
        {
            try
            {
                return JsonSerializer.Serialize(settings, jsonOptions);
            }
            catch (Exception ex)
            {
                this.Logger.LogError(ex, "[SecureJsonConfigHandler] Failed to serialize secure configuration settings");
                throw new InvalidOperationException("Failed to serialize secure configuration settings", ex);
            }
        }
        protected virtual void WriteSecureConfigFile(string filePath, string jsonContent)
        {
            try
            {
                string? directoryPath = Path.GetDirectoryName(filePath);

                if (!string.IsNullOrEmpty(directoryPath))
                    Directory.CreateDirectory(directoryPath);

                Atomic.Write(filePath, jsonContent);

                this.Logger.LogDebug("[SecureJsonConfigHandler] Successfully wrote secure configuration file: {FilePath}", filePath);
            }
            catch (Exception ex)
            {
                this.Logger.LogError(ex, "[SecureJsonConfigHandler] Failed to write secure configuration file: {FilePath}", filePath);
                throw new IOException($"Failed to write secure configuration file: {filePath}", ex);
            }
        }

        #endregion

        #region SecureJsonConfigHandler: Secure Value Extraction

        private SecureValue? ExtractSecureValue(IConfiguration config, PropertyInfo prop)
        {
            IConfigurationSection section = config.GetSection(prop.Name);

            bool sectionExists = section.Exists();

            if (!sectionExists)
            {
                this.Logger.LogDebug("[SecureJsonConfigHandler] Configuration section '{Section}' does not exist, skipping extraction.", prop.Name);
                return null;
            }

            string? base64Value = GetSecureValueFromSection(section);

            return base64Value is null
                ? null
                : new SecureValue(base64Value);
        }
        private static string? GetSecureValueFromSection(IConfigurationSection section)
        {
            string? directValue = section.Value;

            if (!string.IsNullOrEmpty(directValue))
                return directValue;

            string? base64Property = section["EncryptedBase64"];

            return base64Property;
        }

        #endregion
    }
}