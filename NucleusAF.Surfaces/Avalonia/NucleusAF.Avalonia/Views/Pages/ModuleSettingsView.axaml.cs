using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using FluentAvalonia.UI.Controls;
using Microsoft.Extensions.Logging;
using NucleusAF.Avalonia.ViewModels.Pages;
using System.Linq;
using System.Threading.Tasks;

namespace NucleusAF.Avalonia.Views.Pages
{
    public partial class ModuleSettingsView : UserControl
    {
        protected ILogger<ModuleSettingsView>? Logger;

        public ModuleSettingsView() => this.InitializeComponent();
        public ModuleSettingsView(ILogger<ModuleSettingsView> logger, ModuleSettingsViewModel moduleSettingsViewModel)
            : this()
        {
            this.Logger = logger;
            this.DataContext = moduleSettingsViewModel;

            moduleSettingsViewModel.WarningRequested += this.OnWarningRequestedAsync;
        }

        private async Task<bool> OnWarningRequestedAsync(DescriptorWarning w)
        {
            // Get the template
            IDataTemplate? template = (IDataTemplate?)this.Resources["ConfirmationModalTemplate"];

            // Instantiate it
            Border? block = (Border?)template?.Build(this);

            TextBlock? header = block?.GetLogicalDescendants()
                .OfType<TextBlock>()
                .FirstOrDefault(x => x.Name == "Header");

            TextBlock? body = block?.GetLogicalDescendants()
                .OfType<TextBlock>()
                .FirstOrDefault(x => x.Name == "Body");

            header?.Text = w.Header;
            body?.Text = string.Join("\n", w.Items);

            ContentDialog dialog = new ContentDialog
            {
                Title = "Warning",
                Content = block,
                PrimaryButtonText = w.AllowContinue ? "Continue" : null,
                CloseButtonText = "Cancel"
            };

            ContentDialogResult result = await dialog.ShowAsync();

            return result == ContentDialogResult.Primary;
        }

        private void OnModuleClearButtonClicked(object sender, RoutedEventArgs e)
        {
            if (this.DataContext is ModuleSettingsViewModel vm)
            {
                vm.ClearAllModuleFilters();
                this.ModuleSettingsCategoryListBox.UnselectAll();
            }
        }
        private void OnModuleCategorySelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (this.DataContext is ModuleSettingsViewModel vm && sender is ListBox listBox)
            {
                vm.SelectedCategories = [.. listBox.SelectedItems!.Cast<string>()];
                vm.ApplyModuleFilters();
            }
        }

        private void UserControl_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                if (this.DataContext is ModuleSettingsViewModel vm)
                {
                    bool handled = false;

                    if (vm.SelectedDescriptor != null)
                    {
                        vm.SelectedDescriptor = null;

                        handled = true;
                    }
                    else if (vm.IsPanelVisible)
                    {
                        vm.SelectedProvider = string.Empty;
                        vm.IsPanelVisible = false;

                        handled = true;
                    }

                    if (handled) e.Handled = true;
                }
            }
        }
    }
}