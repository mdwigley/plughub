using Microsoft.Extensions.Logging;
using NucleusAF.Extensions;
using NucleusAF.Interfaces.Models;
using NucleusAF.Interfaces.Services.Capabilities;
using NucleusAF.Interfaces.Services.Configuration;
using NucleusAF.Interfaces.Services.Configuration.Accessors;
using NucleusAF.Interfaces.Services.Encryption;
using NucleusAF.Models.Encryption;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace NucleusAF.Services.Configuration.Accessors
{
    public class SecureJsonConfigAccessor(ILogger<IConfigAccessor> logger, ICapabilityService capabilityService, IEncryptionService encryptionService)
        : JsonConfigAccessor(logger, capabilityService), ISecureJsonConfigAccessor
    {
        protected IEncryptionService EncryptionService = encryptionService;
        protected IEncryptionContext? EncryptionContext;

        public override Type Key => typeof(ISecureJsonConfigAccessor);

        #region SecureJsonConfigAccessor: Fluent Configuration API

        public virtual ISecureJsonConfigAccessor SetEncryptionService(IEncryptionService encryptionService)
        {
            this.EncryptionService = encryptionService;
            this.Logger.LogDebug("[SecureJsonConfigAccessor] EncryptionService set");
            return this;
        }
        public virtual ISecureJsonConfigAccessor SetEncryptionContext(IEncryptionContext encryptionContext)
        {
            this.EncryptionContext = encryptionContext;
            this.Logger.LogDebug("[SecureJsonConfigAccessor] EncryptionContext set");
            return this;
        }

        public override ISecureJsonConfigAccessor SetConfigService(IConfigService? service = null)
            => (ISecureJsonConfigAccessor)base.SetConfigService(service);
        public override ISecureJsonConfigAccessor SetConfigHandler(IConfigHandler? handler = null)
            => (ISecureJsonConfigAccessor)base.SetConfigHandler(handler);
        public override ISecureJsonConfigAccessor SetAccess(ICapabilityToken? token = null)
            => (ISecureJsonConfigAccessor)base.SetAccess(token);

        #endregion

        #region SecureJsonConfigAccessor: Factory Methods

        public override ISecureJsonConfigAccessorFor<TConfig> For<TConfig>() where TConfig : class
        {
            if (this.ConfigService == null)
            {
                this.Logger.LogError("[SecureJsonConfigAccessor] ConfigService must be set before creating typed accessors");
                throw new InvalidOperationException("ConfigService must be set before creating typed accessors");
            }

            if (this.ConfigHandler == null)
            {
                this.Logger.LogError("[SecureJsonConfigAccessor] ConfigHandler must be set before creating typed accessors");
                throw new InvalidOperationException("ConfigHandler must be set before creating typed accessors");
            }

            if (this.EncryptionContext == null)
            {
                this.Logger.LogError("[SecureJsonConfigAccessor] EncryptionContext must be set before accessing secure configuration. Call SetEncryptionContext() with a valid IEncryptionContext.");
                throw new InvalidOperationException("EncryptionContext must be set before accessing secure configuration. Call SetEncryptionContext() with a valid IEncryptionContext.");
            }

            this.Logger.LogDebug("[SecureJsonConfigAccessor] Delegating creation of secure accessor for configuration type {ConfigType}", typeof(TConfig).Name);

            return this.For<TConfig>(this.ConfigService, this.ConfigHandler, this.EncryptionContext, this.Token);
        }
        public virtual ISecureJsonConfigAccessorFor<TConfig> For<TConfig>(IConfigService configService, IConfigHandler configHandler, IEncryptionContext encryptionContext, ICapabilityToken? token = null) where TConfig : class
        {
            IEncryptionContext context = encryptionContext
                ?? this.EncryptionContext
                ?? this.EncryptionService.GetEncryptionContext(typeof(TConfig), typeof(TConfig).ToDeterministicGuid());

            this.Logger.LogDebug("[SecureJsonConfigAccessor] Creating secure JSON accessor for configuration type {ConfigType}", typeof(TConfig).Name);

            return new SecureJsonConfigAccessorFor<TConfig>(this.Logger, configService, configHandler, context, this.CapabilityService, token);
        }

        #endregion

        public class SecureJsonConfigAccessorFor<TConfig>(ILogger<IConfigAccessor> logger, IConfigService configService, IConfigHandler configHandler, IEncryptionContext encryptionContext, ICapabilityService capabilityService, ICapabilityToken? token = null)
            : JsonConfigAccessorFor<TConfig>(logger, configService, configHandler, capabilityService, token), ISecureJsonConfigAccessorFor<TConfig> where TConfig : class
        {
            protected IEncryptionContext EncryptionContext = encryptionContext;

            protected static readonly ConcurrentDictionary<Type, PropertyInfo[]> PropertyCache = new();

            #region SecureJsonConfigAccessorFor: Property Access

            [return: MaybeNull]
            public override T Get<T>(string key)
            {
                ValidateEncryptionContext(this.EncryptionContext);

                this.Logger.LogDebug("[SecureJsonConfigAccessorFor] Get<{ValueType}> called for key {Key} in configuration type {ConfigType}", typeof(T).Name, key, typeof(TConfig).Name);

                SecureValue? secureValue = this.ConfigHandler.GetValue<SecureValue>(typeof(TConfig), key, this.CapabilityToken);

                if (secureValue != null)
                {
                    this.Logger.LogDebug("[SecureJsonConfigAccessorFor] Retrieved secure value for key {Key} in configuration type {ConfigType}", key, typeof(TConfig).Name);
                    return secureValue.As<T>(this.EncryptionContext);
                }

                this.Logger.LogDebug("[SecureJsonConfigAccessorFor] No secure value found for key {Key}, delegating to base accessor", key);
                return base.Get<T>(key);
            }

            public override void Set<T>(string key, T value)
            {
                ValidateEncryptionContext(this.EncryptionContext);

                this.Logger.LogDebug("[SecureJsonConfigAccessorFor] Set<{ValueType}> called for key {Key} in configuration type {ConfigType}", typeof(T).Name, key, typeof(TConfig).Name);

                PropertyInfo[] properties = GetCachedProperties();
                PropertyInfo? property = Array.Find(properties, p => p.Name == key);

                bool isSecureProperty = IsSecureProperty(property);

                if (!isSecureProperty)
                {
                    this.Logger.LogDebug("[SecureJsonConfigAccessorFor] Key {Key} is not a secure property, delegating directly", key);
                    base.Set(key, value);
                    return;
                }

                bool shouldUpdateValue = this.ShouldUpdateSecureValue(key, value);

                if (!shouldUpdateValue)
                {
                    this.Logger.LogDebug("[SecureJsonConfigAccessorFor] Secure property {Key} did not require update", key);
                    return;
                }

                this.Logger.LogDebug("[SecureJsonConfigAccessorFor] Secure property {Key} requires update, wrapping value", key);

                SecureValue wrappedValue = SecureValue.From(value!, this.EncryptionContext);

                base.Set(key, wrappedValue);
            }

            #endregion

            #region SecureJsonConfigAccessorFor: Instance Operations

            public override TConfig Get()
            {
                ValidateEncryptionContext(this.EncryptionContext);

                this.Logger.LogDebug("[SecureJsonConfigAccessorFor] Get called for configuration type {ConfigType}", typeof(TConfig).Name);

                TConfig instance = base.Get();

                PropertyInfo[] secureProperties = SecureJsonConfigAccessorFor<TConfig>.GetSecureProperties();

                foreach (PropertyInfo property in secureProperties)
                    this.SetSecurePropertyValue(instance, property);

                return instance;
            }
            public override void Save(TConfig instance)
            {
                ValidateEncryptionContext(this.EncryptionContext);

                ArgumentNullException.ThrowIfNull(instance);

                this.Logger.LogDebug("[SecureJsonConfigAccessorFor] Save called for configuration type {ConfigType}", typeof(TConfig).Name);

                this.SaveSecurePropertiesToConfig(instance);

                this.ConfigHandler.SaveValues(typeof(TConfig), this.CapabilityToken);
                this.ConfigService.OnSaveOperationComplete(this, instance.GetType());
            }
            public override async Task SaveAsync(TConfig instance, CancellationToken cancellationToken = default)
            {
                ValidateEncryptionContext(this.EncryptionContext);

                ArgumentNullException.ThrowIfNull(instance);

                if (cancellationToken.IsCancellationRequested)
                {
                    this.Logger.LogInformation("[SecureJsonConfigAccessorFor] SaveAsync cancelled before secure property processing for configuration type {ConfigType}", typeof(TConfig).Name);
                    cancellationToken.ThrowIfCancellationRequested();
                }

                this.SaveSecurePropertiesToConfig(instance);

                if (cancellationToken.IsCancellationRequested)
                {
                    this.Logger.LogInformation("[SecureJsonConfigAccessorFor] SaveAsync cancelled before persisting values for configuration type {ConfigType}", typeof(TConfig).Name);
                    cancellationToken.ThrowIfCancellationRequested();
                }

                this.Logger.LogDebug("[SecureJsonConfigAccessorFor] SaveAsync called for configuration type {ConfigType}", typeof(TConfig).Name);

                await this.ConfigHandler.SaveValuesAsync(typeof(TConfig), this.CapabilityToken, cancellationToken);
            }

            #endregion

            #region SecureJsonConfigAccessorFor: Core Helper Methods

            private bool ShouldUpdateSecureValue<T>(string key, T value)
            {
                SecureValue? currentValue = this.ConfigHandler.GetValue<SecureValue>(typeof(TConfig), key, this.CapabilityToken);

                if (currentValue == null)
                    return true;

                T decryptedCurrent = currentValue.As<T>(this.EncryptionContext);

                return !EqualityComparer<T>.Default.Equals(decryptedCurrent, value);
            }

            private static PropertyInfo[] GetSecureProperties()
            {
                PropertyInfo[] allProperties = GetCachedProperties();

                return [.. allProperties.Where(prop => prop.CanWrite && IsSecureProperty(prop))];
            }

            private void SetSecurePropertyValue(TConfig instance, PropertyInfo property)
            {
                MethodInfo genericGet = typeof(SecureJsonConfigAccessorFor<TConfig>)
                    .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                    .Single(m =>
                        m.Name == nameof(Get) &&
                        m.IsGenericMethodDefinition &&
                        m.GetParameters().Length == 1 &&
                        m.GetParameters()[0].ParameterType == typeof(string));

                MethodInfo typedGet = genericGet.MakeGenericMethod(property.PropertyType);

                object? decryptedValue = typedGet.Invoke(this, [property.Name]);

                if (decryptedValue == null)
                    return;

                bool isCorrectType = property.PropertyType.IsInstanceOfType(decryptedValue);
                bool isConvertible = decryptedValue is IConvertible;

                if (isCorrectType)
                {
                    property.SetValue(instance, decryptedValue);
                }
                else if (isConvertible)
                {
                    object convertedValue = Convert.ChangeType(decryptedValue, property.PropertyType);

                    property.SetValue(instance, convertedValue);
                }
            }
            private void SaveSecurePropertiesToConfig(TConfig instance)
            {
                PropertyInfo[] allProperties = GetCachedProperties();

                foreach (PropertyInfo property in allProperties)
                {
                    object? rawValue = property.GetValue(instance);

                    MethodInfo setMethod = typeof(SecureJsonConfigAccessorFor<TConfig>).GetMethod(nameof(Set))!;
                    MethodInfo setGeneric = setMethod.MakeGenericMethod(property.PropertyType);

                    setGeneric.Invoke(this, [property.Name, rawValue]);
                }
            }

            #endregion

            #region SecureJsonConfigAccessorFor: Static Helper Methods

            protected static void ValidateEncryptionContext(IEncryptionContext? encryptionContext)
            {
                if (encryptionContext == null)
                    throw new InvalidOperationException("EncryptionContext is null. Call SetEncryptionContext() with a valid IEncryptionContext before accessing secure configuration.");
            }
            protected static PropertyInfo[] GetCachedProperties()
                => PropertyCache.GetOrAdd(typeof(TConfig), t => t.GetProperties(BindingFlags.Public | BindingFlags.Instance));
            protected static bool IsSecureProperty(PropertyInfo? property)
            {
                if (property == null)
                    return false;

                bool isSecureValueType = property.PropertyType == typeof(SecureValue);
                bool hasSecureAttribute = Attribute.IsDefined(property, typeof(SecureAttribute));

                return isSecureValueType || hasSecureAttribute;
            }

            #endregion
        }
    }
}