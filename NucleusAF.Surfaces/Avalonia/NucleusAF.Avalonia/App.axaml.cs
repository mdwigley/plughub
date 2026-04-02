using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Styling;
using FluentAvalonia.Styling;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NucleusAF.Avalonia.Interfaces.Providers;
using NucleusAF.Avalonia.Models;
using NucleusAF.Avalonia.Models.Descriptors;
using NucleusAF.Avalonia.ViewModels;
using NucleusAF.Avalonia.ViewModels.Pages;
using NucleusAF.Avalonia.Views;
using NucleusAF.Avalonia.Views.Pages;
using NucleusAF.Interfaces.Services.Configuration;
using NucleusAF.Interfaces.Services.Modules;
using NucleusAF.Models;
using NucleusAF.Models.Capabilities;
using NucleusAF.Models.Configuration.Parameters;
using NucleusAF.Services.Capabilities;
using NucleusAF.Utility;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;

namespace NucleusAF.Avalonia
{
    public partial class App : Application
    {
        private static IServiceProvider? serviceProvider;

        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);

            ServiceCollection serviceCollection = new();

            CollectViewModels(serviceCollection);

            serviceProvider = Nucleus.Materialize(serviceCollection);

            IConfigService configService = serviceProvider.GetRequiredService<IConfigService>();

            CapabilityToken token = new(Guid.NewGuid());

            ConfigureAppEnv(serviceProvider, configService, token);
            ConfigureTheme(configService, token, this.Styles, this.Resources);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (serviceProvider == null)
            {
                return;
            }

            if (this.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                // Avoid duplicate validations from both Avalonia and the CommunityToolkit. 
                // More info: https://docs.avaloniaui.net/docs/guides/development-guides/data-validation#manage-validationplugins
                DisableAvaloniaDataAnnotationValidation();

                desktop.ShutdownRequested += OnShutdownRequested;
                desktop.MainWindow = serviceProvider.GetRequiredService<Window>();
            }
            else if (this.ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform)
            {
                //TODO: This needs to be integrated with the new MainView selection code which means it will need ot be extracted into something reusable
                singleViewPlatform.MainView = serviceProvider.GetRequiredService<MainView>();
            }

            base.OnFrameworkInitializationCompleted();
        }

        private static void OnShutdownRequested(object? sender, ShutdownRequestedEventArgs e)
        {
            (serviceProvider as IDisposable)?.Dispose();

            Log.CloseAndFlush();
        }
        private static void DisableAvaloniaDataAnnotationValidation()
        {
            DataAnnotationsValidationPlugin[] dataValidationPluginsToRemove =
                [.. BindingPlugins.DataValidators.OfType<DataAnnotationsValidationPlugin>()];

            foreach (DataAnnotationsValidationPlugin? plugin in dataValidationPluginsToRemove)
                BindingPlugins.DataValidators.Remove(plugin);
        }


        #region App: Core Services & ViewModels

        private static void CollectViewModels(IServiceCollection services)
        {
            ArgumentNullException.ThrowIfNull(services);

            services.AddSingleton<MainView>();
            services.AddSingleton<MainViewModel>();
            services.AddSingleton<SettingsView>();
            services.AddSingleton<SettingsViewModel>();
            services.AddSingleton<ModuleSettingsView>();
            services.AddSingleton<ModuleSettingsViewModel>();

            services.AddSingleton<Window>();
        }

        #endregion

        #region App: Post-Init Configuration

        private static void ConfigureTheme(IConfigService configService, CapabilityToken token, Styles styles, IResourceDictionary resources)
        {
            ArgumentNullException.ThrowIfNull(serviceProvider);
            ArgumentNullException.ThrowIfNull(configService);

            ILogger<App> logger = serviceProvider.GetRequiredService<ILogger<App>>();
            IConfigAccessorFor<AppEnv> envAccessor = configService.GetConfigAccessor<AppEnv>(token);

            AppEnv appEnv = envAccessor.Get();

            bool useDefaultTheme = appEnv.UseDefaultTheme;
            bool preferSystemTheme = appEnv.PreferSystemTheme;
            bool preferUserAccentColor = appEnv.PreferUserAccentColor;

            string systemThemeStr = appEnv.SystemTheme;

            ThemeVariant requested = systemThemeStr?.Trim().ToLowerInvariant() switch
            {
                "light" => ThemeVariant.Light,
                "dark" => ThemeVariant.Dark,
                _ => ThemeVariant.Default
            };

            Current?.RequestedThemeVariant = requested;

            if (useDefaultTheme)
            {
                styles.Add(new FluentAvaloniaTheme
                {
                    PreferSystemTheme = preferSystemTheme,
                    PreferUserAccentColor = preferUserAccentColor
                });
            }

            resources.MergedDictionaries.Add(new ResourceInclude(new Uri("avares://NucleusAF.Avalonia/"))
            {
                Source = new Uri("avares://NucleusAF.Avalonia/Themes/FluentAvalonia/Theme.axaml")
            });

            AddModuleResources(resources, logger);

            styles.Add(new StyleInclude(new Uri("avares://NucleusAF.Avalonia/"))
            {
                Source = new Uri("avares://NucleusAF.Avalonia/Themes/FluentAvalonia/Style.axaml")
            });

            AddModuleStyles(styles, logger);
        }

        private static void AddModuleResources(IResourceDictionary resources, ILogger<App> logger)
        {
            IEnumerable<IProviderResourceInclusion> providers = serviceProvider?.GetServices<IProviderResourceInclusion>() ?? [];
            IModuleResolver? resolver = serviceProvider?.GetRequiredService<IModuleResolver>();

            IReadOnlyList<DescriptorResourceInclude>? descriptors = resolver?.ResolveAndOrder<IProviderResourceInclusion, DescriptorResourceInclude>(providers);

            HashSet<string> loadedUris = [];
            HashSet<Type> loadedFactories = [];

            foreach (DescriptorResourceInclude descriptor in descriptors ?? [])
            {
                try
                {
                    if (descriptor.Factory != null)
                    {
                        IResourceDictionary? dict = descriptor.Factory();

                        if (dict != null && loadedFactories.Add(dict.GetType()))
                        {
                            resources.MergedDictionaries.Add(dict);
                        }
                        else
                        {
                            logger.LogDebug("[App] Skipped duplicate resource factory of type {Type}", dict?.GetType().FullName);
                        }
                    }
                    else if (!string.IsNullOrEmpty(descriptor.ResourceUri) && loadedUris.Add(descriptor.ResourceUri))
                    {
                        Uri baseUri = string.IsNullOrEmpty(descriptor.BaseUri)
                            ? new Uri("avares://NucleusAF/")
                            : new Uri(descriptor.BaseUri);

                        ResourceInclude include = new(baseUri)
                        {
                            Source = new Uri(descriptor.ResourceUri)
                        };

                        resources.MergedDictionaries.Add(include);
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "[App] Failed to load resource from {Source}", descriptor.ResourceUri ?? descriptor.Factory?.Method?.Name ?? "unknown");
                }
            }

            logger.LogInformation("[App] Added {UriCount} URI-based resource dictionaries and {FactoryCount} factory dictionaries", loadedUris.Count, loadedFactories.Count);
        }
        private static void AddModuleStyles(Styles styles, ILogger<App> logger)
        {
            if (Current?.Styles is null)
                return;

            IEnumerable<IProviderStyleInclusion> providers = serviceProvider?.GetServices<IProviderStyleInclusion>() ?? [];
            IModuleResolver? resolver = serviceProvider?.GetRequiredService<IModuleResolver>();

            IReadOnlyList<DescriptorStyleInclude>? descriptors = resolver?.ResolveAndOrder<IProviderStyleInclusion, DescriptorStyleInclude>(providers);

            HashSet<Type> loadedFactories = [];
            HashSet<string> loadedIncludes = [];

            foreach (DescriptorStyleInclude descriptor in descriptors ?? [])
            {
                try
                {
                    if (descriptor.Factory != null)
                    {
                        IStyle style = descriptor.Factory();

                        if (loadedFactories.Add(style.GetType()))
                        {
                            styles.Add(style);
                        }
                        else
                        {
                            logger.LogDebug("[App] Skipped duplicate factory style of type {StyleType}", style.GetType().FullName);
                        }
                    }
                    else if (!string.IsNullOrEmpty(descriptor.ResourceUri) && loadedIncludes.Add(descriptor.ResourceUri))
                    {
                        Uri baseUri = string.IsNullOrEmpty(descriptor.BaseUri)
                            ? new Uri("avares://NucleusAF/")
                            : new Uri(descriptor.BaseUri);

                        StyleInclude include = new(baseUri)
                        {
                            Source = new Uri(descriptor.ResourceUri)
                        };

                        styles.Add(include);
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "[App] Failed to load style from {Source}", descriptor.ResourceUri ?? descriptor.Factory?.Method?.Name ?? "unknown");
                }
            }

            logger.LogInformation("[App] Added {FactoryCount} factory styles and {IncludeCount} style includes", loadedFactories.Count, loadedIncludes.Count);
        }

        #endregion

        #region App: Configuration Service Initialization

        private static void ConfigureAppEnv(IServiceProvider provider, IConfigService configService, CapabilityToken token)
        {
            AppConfig liveConfig = configService.GetConfigAccessor<AppConfig>(token).Get();

            AppEnv appEnv = new();
            AppEnv baseEnv = GetAppEnv(configService, appEnv, token);

            string envPath = Path.Combine(liveConfig.ConfigDirectory ?? AppContext.BaseDirectory, "AppEnv.json");

            configService.Register(
                new JsonConfigParams(
                    envPath,
                    Read: CapabilityValue.Public,
                    Write: CapabilityValue.Public),
                token,
                out IConfigAccessorFor<AppEnv>? envAccessor);

            AppEnv userAppEnv = UpdateAppEnv(provider, baseEnv);

            SaveAppEnv(envAccessor, userAppEnv);
        }
        private static AppEnv GetAppEnv(IConfigService configService, AppEnv env, CapabilityToken token)
        {
            string configFilePath = Path.Combine(AppContext.BaseDirectory, "AppEnv.json");

            if (PlatformPath.Exists(configFilePath))
            {
                configService.Register(
                    new JsonConfigParams(configFilePath),
                    token,
                    out IConfigAccessorFor<AppEnv>? accessor);

                AppEnv? loadedEnv = accessor?.Get() ?? new AppEnv();

                foreach (PropertyInfo prop in typeof(AppEnv).GetProperties(BindingFlags.Public | BindingFlags.Instance))
                {
                    if (prop.CanRead && prop.CanWrite)
                    {
                        object? loadedValue = prop.GetValue(loadedEnv);

                        if (loadedValue != null)
                            prop.SetValue(env, loadedValue);
                    }
                }

                configService.Unregister(typeof(AppEnv), token);
            }

            return env;
        }
        private static AppEnv UpdateAppEnv(IServiceProvider provider, AppEnv appEnv)
        {
            ArgumentNullException.ThrowIfNull(provider);
            ArgumentNullException.ThrowIfNull(appEnv);

            IModuleResolver moduleResolver = provider.GetRequiredService<IModuleResolver>();
            IEnumerable<IProviderAppEnv> appEnvModules = provider.GetServices<IProviderAppEnv>();

            IReadOnlyList<DescriptorAppEnv> orderedDescriptors =
                moduleResolver.ResolveAndOrder<IProviderAppEnv, DescriptorAppEnv>(appEnvModules);

            foreach (DescriptorAppEnv descriptor in orderedDescriptors)
            {
                try
                {
                    descriptor.AppEnv?.Invoke(appEnv);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "[App] Failed to apply environment mutation for module {ModuleId}", descriptor.ModuleId);
                }
            }

            Log.Information("[App] ModulesAppEnv completed: applied {ModuleCount} environment mutation descriptors.", orderedDescriptors.Count);

            return appEnv;
        }
        private static void SaveAppEnv(IConfigAccessorFor<AppEnv>? accessor, AppEnv appEnv)
        {
            if (accessor is null) return;

            JsonSerializerOptions options = new() { WriteIndented = false };

            string currentJson = JsonSerializer.Serialize(appEnv, options);
            string persistedJson = JsonSerializer.Serialize(accessor.Get(), options);

            if (currentJson == persistedJson)
                return;

            try
            {
                Task.Run(() => accessor.SaveAsync(appEnv)).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "[App] Failed to persist AppEnv");
            }
        }

        #endregion
    }
}