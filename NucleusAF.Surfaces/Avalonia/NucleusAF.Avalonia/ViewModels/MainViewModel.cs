using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using NucleusAF.Avalonia.Interfaces.Events;
using NucleusAF.Avalonia.Interfaces.Providers;
using NucleusAF.Avalonia.Models;
using NucleusAF.Avalonia.Models.Descriptors;
using NucleusAF.Avalonia.ViewModels.Components;
using NucleusAF.Avalonia.ViewModels.Pages;
using NucleusAF.Avalonia.Views.Pages;
using NucleusAF.Interfaces.Services.Configuration;
using NucleusAF.Interfaces.Services.Modules;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace NucleusAF.Avalonia.ViewModels
{
    public partial class MainViewModel : BaseViewModel, IPageNavigationEvents
    {
        public event EventHandler<PageNavigationChangedEventArgs>? PageNavigationChanged;

        private readonly ILogger<MainViewModel> logger;
        private readonly IConfigService configService;
        private readonly IServiceProvider serviceProvider;

        private readonly ContentItemViewModel? mainPage;
        private readonly ContentItemViewModel? settingsPage;

        [ObservableProperty] private Control? selectedMainPageContent;

        [ObservableProperty] private bool isMaximizeVisible = true;
        [ObservableProperty] private bool isRestoreVisible = false;
        [ObservableProperty] private bool isSettingsVisible = true;
        [ObservableProperty] private bool isHomeVisible = false;

        [ObservableProperty] private WindowIcon? windowIcon;
        [ObservableProperty] private string? windowTitle;
        [ObservableProperty] private double? windowMinWidth;
        [ObservableProperty] private double? windowMinHeight;
        [ObservableProperty] private double? windowWidth;
        [ObservableProperty] private double? windowHeight;
        [ObservableProperty] private WindowState? windowStartupState;
        [ObservableProperty] private WindowStartupLocation? windowStartupLocation;
        [ObservableProperty] private WindowTransparencyLevel? transparencyPreference;
        [ObservableProperty] private bool? canResize;
        [ObservableProperty] private bool? showInTaskbar;
        [ObservableProperty] private SystemDecorations? systemDecorations;
        [ObservableProperty] private bool? extendClientAreaToDecorationsHint;
        [ObservableProperty] private int? extendClientAreaTitleBarHeightHint;

        [ObservableProperty] private string? appName;
        [ObservableProperty] private IImage? appIcon;
        [ObservableProperty] private string? appLink;

        public MainViewModel(ILogger<MainViewModel> logger, IConfigService configService, IServiceProvider serviceProvider, SettingsView settingsView, SettingsViewModel settingsViewModel, IModuleResolver resolver, IEnumerable<IProviderPages> pages)
        {
            this.logger = logger;
            this.configService = configService;
            this.serviceProvider = serviceProvider;

            IConfigAccessorFor<AppEnv> appEnv = configService.GetConfigAccessor<AppEnv>();

            this.ApplyConfigValues(appEnv.Get());

            this.settingsPage = new(typeof(SettingsView), typeof(SettingsViewModel), "", null!)
            {
                Control = settingsView,
                ViewModel = settingsViewModel
            };

            IReadOnlyList<DescriptorPage> descriptors =
                resolver.ResolveAndOrder<IProviderPages, DescriptorPage>(pages);

            if (descriptors.Count > 0)
            {
                this.mainPage = DescriptorPage.GetItemViewModel(serviceProvider, descriptors[0]);

                this.selectedMainPageContent = this.mainPage?.Control;
            }

            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop && desktop.MainWindow is Window w)
                this.OnWindowStateChanged(w.WindowState);
        }
        private void ApplyConfigValues(AppEnv appEnv)
        {
            string iconPath = appEnv.WindowIconPath;

            if (!string.IsNullOrWhiteSpace(iconPath))
                this.WindowIcon = new WindowIcon(AssetLoader.Open(new Uri(iconPath)));

            this.WindowTitle = !string.IsNullOrWhiteSpace(appEnv.WindowTitle) ? appEnv.WindowTitle : null;
            this.WindowMinWidth = appEnv.WindowMinWidth;
            this.WindowMinHeight = appEnv.WindowMinHeight;
            this.WindowWidth = appEnv.WindowWidth;
            this.WindowHeight = appEnv.WindowHeight;

            this.WindowStartupState = appEnv.WindowStartupState;
            this.WindowStartupLocation = appEnv.WindowStartupLocation;
            this.TransparencyPreference = appEnv.TransparencyPreference;
            this.SystemDecorations = appEnv.SystemDecorations;

            this.CanResize = appEnv.CanResize;
            this.ShowInTaskbar = appEnv.ShowInTaskbar;
            this.ExtendClientAreaToDecorationsHint = appEnv.ExtendClientAreaToDecorationsHint;
            this.ExtendClientAreaTitleBarHeightHint = appEnv.ExtendClientAreaTitleBarHeightHint;

            string appIconPath = appEnv.AppIconPath;

            if (!string.IsNullOrWhiteSpace(appIconPath))
                this.AppIcon = new Bitmap(AssetLoader.Open(new Uri(appIconPath)));

            this.AppName = appEnv.AppName;
            this.AppLink = appEnv.AppLink;
        }

        private void OnWindowStateChanged(WindowState state)
        {
            this.IsMaximizeVisible = state != WindowState.Maximized;
            this.IsRestoreVisible = state == WindowState.Maximized;
        }

        [RelayCommand]
        private void AppIconClicked()
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = this.AppLink,
                UseShellExecute = true
            });
        }
        [RelayCommand]
        private void Minimize()
        {
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop && desktop.MainWindow is Window w)
            {
                w.WindowState = WindowState.Minimized;

                this.OnWindowStateChanged(w.WindowState);
            }
        }
        [RelayCommand]
        private void MaximizeRestore()
        {
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop && desktop.MainWindow is Window w)
            {
                w.WindowState = w.WindowState == WindowState.Maximized
                    ? WindowState.Normal
                    : WindowState.Maximized;

                this.OnWindowStateChanged(w.WindowState);
            }
        }
        [RelayCommand]
        private static void Close()
        {
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                if (desktop.MainWindow is Window w)
                    w.Close();
        }

        [RelayCommand]
        private void GoHome()
        {
            ContentItemViewModel? previous = this.IsHomeVisible ? this.mainPage : this.settingsPage;
            ContentItemViewModel? next = this.mainPage;

            this.IsSettingsVisible = true;
            this.IsHomeVisible = false;

            this.SelectedMainPageContent = next?.Control;
            this.PageNavigationChanged?.Invoke(this, new PageNavigationChangedEventArgs(previous, next));
        }
        [RelayCommand]
        private void OpenSettings()
        {
            ContentItemViewModel? previous = this.IsHomeVisible ? this.mainPage : this.settingsPage;
            ContentItemViewModel? next = this.settingsPage;

            this.IsSettingsVisible = false;
            this.IsHomeVisible = true;

            this.SelectedMainPageContent = next?.Control;
            this.PageNavigationChanged?.Invoke(this, new PageNavigationChangedEventArgs(previous, next));
        }
    }
}