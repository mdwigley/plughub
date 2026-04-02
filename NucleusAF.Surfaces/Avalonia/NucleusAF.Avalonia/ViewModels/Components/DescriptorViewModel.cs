using CommunityToolkit.Mvvm.ComponentModel;
using System;

namespace NucleusAF.Avalonia.ViewModels.Modules
{
    public partial class DescriptorViewModel : ObservableObject
    {
        [ObservableProperty]
        private string name;

        [ObservableProperty]
        private Type? type = null;

        [ObservableProperty]
        private bool isEnabled;

        [ObservableProperty]
        private bool isSystem;

        public event Action? IsEnabledChanged;

        public DescriptorViewModel(string name, Type? type, bool isEnabled = false, bool isSystem = false)
        {
            this.Name = name;
            this.Type = type;
            this.IsEnabled = isEnabled;
            this.IsSystem = isSystem;
        }

        partial void OnIsEnabledChanged(bool value)
        {
            IsEnabledChanged?.Invoke();
        }
    }
}
