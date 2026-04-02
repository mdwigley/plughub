using CommunityToolkit.Mvvm.ComponentModel;

namespace NucleusAF.Avalonia.ViewModels
{
    /// <summary>
    /// Base class for all view models in NucleusAF, providing property change notification.
    /// </summary>
    public abstract partial class BaseViewModel : ObservableValidator
    {
    }
}
