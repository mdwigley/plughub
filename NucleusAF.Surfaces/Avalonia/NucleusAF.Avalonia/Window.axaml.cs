using Avalonia.Controls;
using Microsoft.Extensions.DependencyInjection;
using NucleusAF.Avalonia.Interfaces.Providers;
using NucleusAF.Avalonia.Models;
using NucleusAF.Avalonia.Models.Descriptors;
using NucleusAF.Avalonia.ViewModels;
using NucleusAF.Avalonia.Views;
using NucleusAF.Interfaces.Services.Configuration;
using NucleusAF.Interfaces.Services.Modules;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NucleusAF.Avalonia
{
    public partial class Window : global::Avalonia.Controls.Window
    {
        public Window() => this.InitializeComponent();

        public Window(IServiceProvider serviceProvider, IConfigService configService, IModuleResolver moduleResolver, IEnumerable<IProviderMainView> viewProviders)
            : this()
        {
            IReadOnlyList<DescriptorMainView> orderedDescriptors = moduleResolver.ResolveAndOrder<IProviderMainView, DescriptorMainView>(viewProviders);
            IConfigAccessorFor<AppEnv> appEnv = configService.GetConfigAccessor<AppEnv>();

            string? mainViewKey = string.IsNullOrWhiteSpace(appEnv.Get().MainViewKey) ? appEnv.Get().MainViewKey : null;

            DescriptorMainView? found = orderedDescriptors
                .FirstOrDefault(d => string.Equals($"{d.ViewType.FullName}:{d.Key}", mainViewKey, StringComparison.OrdinalIgnoreCase));

            UserControl view;
            BaseViewModel viewModel;

            if (found != null)
            {
                view = found.ViewFactory != null
                    ? found.ViewFactory(serviceProvider)
                    : serviceProvider.GetService(found.ViewType) as UserControl
                      ?? (UserControl)ActivatorUtilities.CreateInstance(serviceProvider, found.ViewType);

                viewModel = found.ViewModelFactory != null
                    ? found.ViewModelFactory(serviceProvider)
                    : serviceProvider.GetService(found.ViewModelType) as BaseViewModel
                      ?? (BaseViewModel)ActivatorUtilities.CreateInstance(serviceProvider, found.ViewModelType);
            }
            else
            {
                view = serviceProvider.GetRequiredService<MainView>();
                viewModel = serviceProvider.GetRequiredService<MainViewModel>();
            }

            this.Content = view;
            this.DataContext = viewModel;
        }
    }
}