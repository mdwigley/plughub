using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using NucleusAF.Attributes;
using NucleusAF.Avalonia.Interfaces.Providers;
using NucleusAF.Avalonia.Models.Descriptors;
using NucleusAF.Avalonia.ViewModels.Modules;
using NucleusAF.Interfaces.Services.Configuration;
using NucleusAF.Interfaces.Services.Modules;
using NucleusAF.Models;
using NucleusAF.Models.Descriptors;
using NucleusAF.Models.Modules;
using NucleusAF.Services.Modules;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;

namespace NucleusAF.Avalonia.ViewModels.Pages
{
    public record DescriptorWarning(
        string Header,
        IReadOnlyList<string> Items,
        bool AllowContinue
    );

    public class DescriptorData
    {
        public string? ModuleName { get; set; }
        public Descriptor? Descriptor { get; set; }
        public int? SortOrder { get; set; }
        public string? State { get; set; }
        public string? Message { get; set; }
    }

    public partial class ModuleSettingsViewModel : BaseViewModel
    {
        public event Func<DescriptorWarning, Task<bool>>? WarningRequested;

        private readonly ILogger<ModuleSettingsViewModel> logger;
        private readonly IConfigService configService;
        private readonly IConfigAccessorFor<ModuleManifest> moduleManifest;
        private readonly IModuleRegistrar moduleRegistrar;
        private readonly IModuleResolver moduleResolver;
        private readonly IModuleCache moduleCache;

        private bool isUpdating = false;
        private bool isModuleCheckboxUpdating = false;
        private readonly string originalManifestJson = string.Empty;

        [ObservableProperty] private ObservableCollection<ModuleViewModel> originalModules = [];
        [ObservableProperty] private ObservableCollection<ModuleViewModel> modules = [];
        [ObservableProperty] private ObservableCollection<ModuleViewModel> filteredModules = [];
        [ObservableProperty] private ObservableCollection<string> availableCategories = [];
        [ObservableProperty] private ObservableCollection<string> selectedCategories = [];
        [ObservableProperty] private string moduleSearchText = string.Empty;
        [ObservableProperty] private bool hasSearchOrCategorySelected = false;
        [ObservableProperty] private bool isPanelVisible = false;
        [ObservableProperty] private string selectedProvider = string.Empty;
        [ObservableProperty] private string descriptorSearchText = string.Empty;
        [ObservableProperty] private ObservableCollection<DescriptorData> descriptorData = [];
        [ObservableProperty] private ObservableCollection<DescriptorData> filteredDescriptorData = [];
        [ObservableProperty] private bool showRestartBanner = false;

        [ObservableProperty]
        private DescriptorData? selectedDescriptor = new()
        {
            Descriptor = new DescriptorDependencyInjection(Guid.Empty, Guid.Empty, "", typeof(object))
        };

        public Task<bool> RequestWarningAsync(DescriptorWarning w)
        {
            return WarningRequested?.Invoke(w) ?? Task.FromResult(false);
        }

        public ModuleSettingsViewModel(ILogger<ModuleSettingsViewModel> logger, IConfigService configService, IModuleCache moduleCache, IModuleRegistrar moduleRegistrar, IModuleResolver moduleResolver)
        {
            this.logger = logger;
            this.configService = configService;
            this.moduleManifest = configService.GetConfigAccessor<ModuleManifest>();
            this.moduleRegistrar = moduleRegistrar;
            this.moduleResolver = moduleResolver;
            this.moduleCache = moduleCache;

            this.moduleSearchText = string.Empty;

            ModuleManifest manifest = this.moduleManifest.Get();

            this.originalManifestJson = JsonSerializer.Serialize(manifest);

            List<DescriptorLoadState> descriptorStates = manifest.DescriptorStates;

            this.GatherModules(moduleCache.Modules, descriptorStates);
            this.GatherSnapshot();
            this.GatherCategories();
            this.ApplyModuleFilters();

            foreach (ModuleViewModel module in this.Modules)
                module.IsEnabledChanged += this.OnModuleEnabledChanged;
        }

        private void GatherModules(IEnumerable<ModuleReference> moduleMetadatas, List<DescriptorLoadState> descriptorLoadStates)
        {
            this.Modules = [.. moduleMetadatas
                .Select(module =>
                {
                    // Filter descriptor load states to only those relevant to this module's assembly & class
                    List<DescriptorLoadState> relevantStates = [.. descriptorLoadStates
                        .Where(s =>
                            // Match by assembly name, ignoring case
                            string.Equals(s.AssemblyName, module.AssemblyName, StringComparison.OrdinalIgnoreCase) &&
                            // Match by class/type name, ignoring case
                            string.Equals(s.ClassName, module.TypeName, StringComparison.OrdinalIgnoreCase))];

                    // Determine if the module is enabled based on any relevant descriptor state being enabled
                    bool moduleEnabled = relevantStates.Any(s => s.Enabled);

                    // Create a ModuleViewModel instance for this module type and initial enabled state
                    ModuleViewModel moduleVM = ModuleViewModel.CreateFromModuleType(module.Type, moduleEnabled);

                    // Build the list of provided descriptors for this module
                    moduleVM.ProvidedDescriptors = [.. relevantStates
                        .Select(ls =>
                        {
                            // Find the matching provider in the module's providers list
                            ProviderInterface? provider = module.Providers.FirstOrDefault(pi =>
                                string.Equals(pi.InterfaceName, ls.ProviderName, StringComparison.OrdinalIgnoreCase));

                            // Use the provider's interface name if found, otherwise fallback to the name in the load state
                            // Only keep the last part after the '.' to simplify display
                            string providerName = (provider?.InterfaceName ?? ls.ProviderName).Split('.').Last();

                            // Create the descriptor view model
                            DescriptorViewModel descriptorVM = new(providerName, provider?.InterfaceType, ls.Enabled, ls.System);

                            // Hook up a change event to handle toggling the enabled state
                            descriptorVM.IsEnabledChanged += () => this.OnProviderEnabledChanged(moduleVM, descriptorVM);

                            return descriptorVM;
                        })
                        .OrderBy(pivm => pivm.Name)];

                    // If there are no provided descriptors, disable the module entirely
                    if (!moduleVM.ProvidedDescriptors.Any())
                    {
                        moduleVM.IsEnabled = false;
                    }
                    else
                    {
                        // Determine the module's enabled state based on its descriptors:
                        // - true if all are enabled
                        // - null (indeterminate) if some are enabled
                        // - false if none are enabled
                        bool anyEnabled = moduleVM.ProvidedDescriptors.Any(i => i.IsEnabled);
                        bool allEnabled = moduleVM.ProvidedDescriptors.All(i => i.IsEnabled);

                        moduleVM.IsEnabled = allEnabled ? true : anyEnabled ? null : false;
                    }

                    return moduleVM;
                })
                .OrderBy(vm => vm.AssemblyName)
                .ThenBy(vm => vm.Name)
                .ToList()];
        }
        private void GatherSnapshot()
        {
            this.OriginalModules = [.. this.Modules
                .Select(p => new ModuleViewModel
                {
                    // Copy the module name
                    Name = p.Name,
                    // Copy the enabled state of the module
                    IsEnabled = p.IsEnabled,
                    // Deep copy the list of descriptors for this module
                    ProvidedDescriptors = new ObservableCollection<DescriptorViewModel>(
                        // For each descriptor in the original module, create a new DescriptorViewModel
                        p.ProvidedDescriptors?.Select(i => new DescriptorViewModel(
                            i.Name,       // Copy the descriptor name
                            i.Type,       // Copy the interface/type info
                            i.IsEnabled,  // Copy the enabled state
                            i.IsSystem    // Copy whether it's a system descriptor
                        )) ?? []
                    )
                }
            )];
        }
        private void GatherCategories()
        {
            this.AvailableCategories = [.. this.Modules
                // Flatten all module categories into a single sequence
                .SelectMany(p => p.Categories ?? [])
                // Remove duplicates so each category appears only once
                .Distinct()
                // Sort categories alphabetically
                .OrderBy(c => c)];
        }

        partial void OnModuleSearchTextChanged(string value)
        {
            this.UpdateHasSearchOrCategorySelected();
            this.ApplyModuleFilters();
        }
        partial void OnSelectedCategoriesChanged(ObservableCollection<string> value)
        {
            UpdateHasSearchOrCategorySelected();
            ApplyModuleFilters();
        }
        private void UpdateHasSearchOrCategorySelected() =>
            this.HasSearchOrCategorySelected = !string.IsNullOrEmpty(this.ModuleSearchText) || (this.SelectedCategories?.Any() ?? false);

        public void ApplyModuleFilters()
        {
            string searchLower = (this.ModuleSearchText ?? "").Trim().ToLowerInvariant();
            ObservableCollection<string> categories = this.SelectedCategories ?? [];

            IEnumerable<ModuleViewModel> filtered =
                this.Modules.Where(p =>
                    // Check if search text is empty OR any of the module's fields contain the search text
                    (string.IsNullOrEmpty(searchLower) ||
                        // Match against module name
                        (p.Name?.Contains(searchLower, StringComparison.InvariantCultureIgnoreCase) ?? false) ||
                        // Match against module description
                        (p.Description?.Contains(searchLower, StringComparison.InvariantCultureIgnoreCase) ?? false) ||
                        // Match against module author
                        (p.Author?.Contains(searchLower, StringComparison.InvariantCultureIgnoreCase) ?? false))
                    // AND filter by categories
                    && (
                        // If no categories are selected, skip this filter (always true)
                        !categories.Any() ||
                        // Otherwise, the module must have at least one category in the selected list
                        p.Categories.Any(c => categories.Contains(c))
                    )
                );

            this.FilteredModules.Clear();

            foreach (ModuleViewModel item in filtered)
                this.FilteredModules.Add(item);
        }
        public void ClearAllModuleFilters()
        {
            this.ModuleSearchText = string.Empty;
            this.SelectedCategories.Clear();
            this.ApplyModuleFilters();
        }

        partial void OnDescriptorSearchTextChanged(string value)
        {
            ApplyDescriptorFilters();
        }
        public void ApplyDescriptorFilters()
        {
            string searchLower = (this.DescriptorSearchText ?? "").Trim().ToLowerInvariant();

            IEnumerable<DescriptorData> filtered = this.DescriptorData
                .Where(d =>
                    // If the search text is empty, accept all items (no filtering).
                    string.IsNullOrEmpty(searchLower) ||
                    // Otherwise, check if ModuleName contains the search text (case‑insensitive, null‑safe).
                    (d.ModuleName?.Contains(searchLower, StringComparison.InvariantCultureIgnoreCase) ?? false) ||
                    // Or if State contains the search text (case‑insensitive, null‑safe).
                    (d.State?.Contains(searchLower, StringComparison.InvariantCultureIgnoreCase) ?? false) ||
                    // Or if Message contains the search text (case‑insensitive, null‑safe).
                    (d.Message?.Contains(searchLower, StringComparison.InvariantCultureIgnoreCase) ?? false)
                );

            this.FilteredDescriptorData.Clear();

            foreach (DescriptorData item in filtered)
                this.FilteredDescriptorData.Add(item);
        }

        private async void OnModuleEnabledChanged(ModuleViewModel module)
        {
            if (this.isUpdating) return;
            if (this.isModuleCheckboxUpdating) return;
            if (!module.ProvidedDescriptors.Any()) return;

            this.isUpdating = true;

            bool intended = module.DesiredEnabledState;

            IReadOnlyList<DescriptorViewModel> affected =
                intended
                    ? this.GetConflictsForEnable(module)
                    : this.GetDependentsForDisable(module);

            bool proceed = true;

            if (affected.Count > 0)
            {
                proceed = await this.RequestWarningAsync(
                    new DescriptorWarning(
                        intended ? "Conflicts Detected" : "Dependents Will Be Disabled",
                        [.. affected.Select(d => d.Name)],
                        AllowContinue: true));
            }

            if (proceed)
            {
                this.isModuleCheckboxUpdating = true;

                this.ApplyModuleState(module, intended);
                module.DesiredEnabledState = !intended;

                this.UpdateShowRestartBanner();
                this.isModuleCheckboxUpdating = false;
            }
            else
            {
                this.isModuleCheckboxUpdating = true;
                module.IsEnabled = module.LastEnableState;
                this.isModuleCheckboxUpdating = false;
            }

            this.isUpdating = false;
        }

        private void ApplyModuleState(ModuleViewModel module, bool targetEnabled)
        {
            var systemStates = module.ProvidedDescriptors
                .Where(d => d.IsSystem)
                .Select(d => new { d.Name, WasEnabled = d.IsEnabled })
                .ToList();

            foreach (DescriptorViewModel d in module.ProvidedDescriptors)
                if (!d.IsSystem)
                    d.IsEnabled = targetEnabled;

            this.moduleRegistrar.SetAllEnabled(module.ModuleId, targetEnabled);

            foreach (var system in systemStates)
            {
                Type iface = module.ModuleType.GetInterface(system.Name)
                    ?? throw new InvalidOperationException($"Provider {system.Name} not found on type");

                this.moduleRegistrar.SetEnabled(module.ModuleId, iface, enabled: system.WasEnabled);

                DescriptorViewModel vm = module.ProvidedDescriptors.First(d => d.IsSystem && d.Name == system.Name);

                vm.IsEnabled = system.WasEnabled;
            }

            bool allEnabled = module.ProvidedDescriptors.All(d => d.IsEnabled);
            bool noneEnabled = module.ProvidedDescriptors.All(d => !d.IsEnabled);

            if (allEnabled)
                module.IsEnabled = true;
            else if (noneEnabled)
                module.IsEnabled = false;
            else
                module.IsEnabled = null;
        }

        private void OnProviderEnabledChanged(ModuleViewModel module, DescriptorViewModel descVM)
        {
            if (this.isUpdating) return;
            this.isUpdating = true;

            Type? interfaceType = module.ModuleType
                .GetInterfaces()
                .FirstOrDefault(t => t.Name == descVM.Name);

            if (interfaceType != null)
            {
                if (descVM.IsEnabled)
                    this.moduleRegistrar.SetEnabled(module.ModuleId, interfaceType, enabled: true);
                else
                    this.moduleRegistrar.SetEnabled(module.ModuleId, interfaceType, enabled: false);
            }

            bool anyEnabled = module.ProvidedDescriptors.Any(i => i.IsEnabled);
            bool allEnabled = module.ProvidedDescriptors.All(i => i.IsEnabled);

            module.IsEnabled = allEnabled ? true : anyEnabled ? null : false;

            this.UpdateShowRestartBanner();
            this.isUpdating = false;
        }
        private void UpdateShowRestartBanner()
        {
            this.ShowRestartBanner =
                // Check if the number of modules has changed
                this.OriginalModules.Count != this.Modules.Count ||

                // Compare each module in current vs original by pairing them
                this.Modules.Zip(this.OriginalModules, (current, original) =>
                    // Check if the module name changed OR its enabled state changed
                    !string.Equals(current.Name, original.Name, StringComparison.OrdinalIgnoreCase) ||
                    current.IsEnabled != original.IsEnabled ||

                    // Compare the module's provided descriptors
                    !(current.ProvidedDescriptors ?? [])
                        // Sort descriptors by name for consistent comparison
                        .OrderBy(d => d.Name)
                        .Zip(original.ProvidedDescriptors ?? [], (currDesc, origDesc) =>
                            // For each descriptor pair, check if name and enabled state match
                            string.Equals(currDesc.Name, origDesc.Name, StringComparison.OrdinalIgnoreCase) &&
                            currDesc.IsEnabled == origDesc.IsEnabled
                        )
                        // If all descriptor comparisons are true, the sequences are equal
                        .All(equal => equal)
                )
                // If any module pair is different, we need to show the restart banner
                .Any(diff => diff);
        }

        [RelayCommand]
        private void OpenDescriptorInfo(DescriptorViewModel descVM)
        {
            if (descVM.Name == this.SelectedProvider)
            {
                this.CloseDescriptorInfo();
                return;
            }
            if (descVM.Type == null)
            {
                this.logger.LogWarning("DescriptorViewModel.Type was null for interface '{InterfaceName}'", descVM.Name);
                this.CloseDescriptorInfo();
                return;
            }

            this.IsPanelVisible = true;
            this.SelectedProvider = descVM.Name;

            this.PopulateDescriptorData(descVM);
        }
        [RelayCommand]
        private void CloseDescriptorInfo()
        {
            this.IsPanelVisible = false;
            this.SelectedProvider = string.Empty;
        }
        [RelayCommand]
        private void RevertChanges()
        {
            if (string.IsNullOrEmpty(this.originalManifestJson))
                return;
            ModuleManifest? restoredManifest = JsonSerializer.Deserialize<ModuleManifest>(this.originalManifestJson);
            if (restoredManifest == null || restoredManifest.DescriptorStates == null)
                return;

            this.moduleRegistrar.SaveManifest(restoredManifest);
            this.UpdateViewModelsFromManifest(restoredManifest);

            this.isUpdating = true;

            foreach (ModuleViewModel moduleVM in this.Modules)
            {
                if (moduleVM.ProvidedDescriptors != null && moduleVM.ProvidedDescriptors.Any())
                {
                    bool allEnabled = moduleVM.ProvidedDescriptors.All(d => d.IsEnabled);
                    bool noneEnabled = moduleVM.ProvidedDescriptors.All(d => !d.IsEnabled);
                    bool? triState = allEnabled ? true : (noneEnabled ? false : null);

                    if (moduleVM.IsEnabled != triState)
                        moduleVM.IsEnabled = triState;
                }
                else
                {
                    if (moduleVM.IsEnabled != false)
                        moduleVM.IsEnabled = false;
                }
            }

            this.isUpdating = false;

            this.GatherSnapshot();
            this.GatherCategories();
            this.ApplyModuleFilters();
            this.ShowRestartBanner = false;
        }

        private void PopulateDescriptorData(DescriptorViewModel descVM)
        {
            ArgumentNullException.ThrowIfNull(descVM);
            ArgumentNullException.ThrowIfNull(descVM.Type);

            List<DescriptorLoadState> manifestStates = this.moduleManifest.Get().DescriptorStates;

            DescriptorProviderAttribute? providesDescAttr =
                descVM.Type.GetCustomAttribute<DescriptorProviderAttribute>(inherit: false);

            DescriptorSortContext sortContext = providesDescAttr?.SortContext ?? DescriptorSortContext.None;

            List<Descriptor> descriptors = moduleRegistrar.GetDescriptorsForProvider(descVM.Type);
            DescriptorResolutionContext<Descriptor> context = moduleResolver.ResolveContext(descriptors);
            IReadOnlyList<Descriptor> sortedDescriptors = moduleResolver.ResolveAndOrder(descriptors, sortContext);

            Dictionary<Descriptor, int> sortedIndexLookup = sortedDescriptors
                .Select((desc, index) => new { desc, index })
                .ToDictionary(x => x.desc, x => x.index);

            List<DescriptorData> rows = [];

            foreach (Descriptor descriptor in descriptors)
            {
                string moduleName = this.moduleCache.Modules
                    .FirstOrDefault(p => p.Metadata.ModuleId == descriptor.ModuleId)?
                    .Metadata.Name ?? "Unknown Module";

                int sortOrder = -1;

                string state;
                string message;

                DescriptorLoadState? manifestState = manifestStates.FirstOrDefault(s =>
                    s.ModuleId == descriptor.ModuleId &&
                    string.Equals(
                        s.ProviderName.Split('.').Last(),
                        this.SelectedProvider,
                        StringComparison.OrdinalIgnoreCase));

                if (manifestState != null && !manifestState.Enabled)
                {
                    rows.Add(new DescriptorData
                    {
                        ModuleName = moduleName,
                        Descriptor = descriptor,
                        SortOrder = -100,
                        State = "❌",
                        Message = "Interface disabled"
                    });
                    continue;
                }

                if (context.DependencyDisabled.Contains(descriptor))
                {
                    state = "🚫";
                    sortOrder = -200;
                    message = "Missing dependency: \nPlease check logs for details!";
                }
                else if (context.ConflictDisabled.Contains(descriptor))
                {
                    state = "⚡";
                    sortOrder = -300;
                    IEnumerable<DescriptorReference> conflicts = descriptor.ConflictsWith ?? [];
                    IEnumerable<string> conflictingNames = conflicts.Select(conf =>
                    {
                        ModuleReference? module = this.moduleCache.Modules.FirstOrDefault(p => p.Metadata.ModuleId == conf.ModuleId);
                        return module?.Metadata?.Name ?? conf.ModuleId.ToString();
                    });
                    message = "Conflicts with: \n" +
                        (conflictingNames.Any()
                            ? string.Join(", ", conflictingNames)
                            : "Unknown module(s).");
                }
                else if (context.DuplicateIdDisabled.Contains(descriptor))
                {
                    state = "🌀";
                    sortOrder = -400;
                    message = "Ignored due to duplicate DescriptorId.";
                }
                else if (sortedIndexLookup.TryGetValue(descriptor, out int index))
                {
                    sortOrder = index;

                    switch (sortContext)
                    {
                        case DescriptorSortContext.Forward:
                            state = index == 0 ? "⬇️⭐" : $"⬇️";
                            message = index == 0
                                ? "Primary descriptor (forward order applied)"
                                : $"Forward order position: {index}";
                            break;

                        case DescriptorSortContext.Reverse:
                            state = index == 0 ? "⬆️⭐" : $"⬆️";
                            message = index == 0
                                ? "Primary descriptor (reverse order applied)"
                                : $"Reverse order position: {index}";
                            break;

                        case DescriptorSortContext.None:
                        default:
                            state = index == 0 ? "➖⭐" : $"➖";
                            message = index == 0
                                ? "Primary descriptor (unordered context)"
                                : $"Unordered (index {index})";
                            break;
                    }
                }
                else
                {
                    state = "❓";
                    message = "Unresolved state: this module was neither sorted nor explicitly rejected.";
                }

                rows.Add(new DescriptorData
                {
                    ModuleName = moduleName,
                    Descriptor = descriptor,
                    SortOrder = sortOrder,
                    State = state,
                    Message = message
                });
            }

            this.DescriptorData = [.. rows];
            this.ApplyDescriptorFilters();
        }
        private void UpdateViewModelsFromManifest(ModuleManifest restoredManifest)
        {
            List<DescriptorLoadState> interfaceStates = restoredManifest.DescriptorStates;

            foreach (ModuleViewModel moduleVM in this.Modules)
            {
                List<DescriptorLoadState> statesForModule =
                    [.. interfaceStates.Where(s =>
                        // Match only states that belong to the same assembly as this module
                        string.Equals(s.AssemblyName, moduleVM.AssemblyName, StringComparison.OrdinalIgnoreCase) &&
                        // AND match only states that correspond to the same class/type as this module
                        string.Equals(s.ClassName, moduleVM.TypeName, StringComparison.OrdinalIgnoreCase))];

                bool isEnabled = statesForModule.Any(s => s.Enabled);
                this.isUpdating = true;

                moduleVM.IsEnabled = isEnabled;
                moduleVM.DesiredEnabledState = !isEnabled;

                foreach (DescriptorViewModel descriptorVm in moduleVM.ProvidedDescriptors)
                {
                    DescriptorLoadState? state = statesForModule.FirstOrDefault(s =>
                        s.ProviderName.Split('.').Last().Equals(descriptorVm.Name, StringComparison.OrdinalIgnoreCase));

                    descriptorVm.IsEnabled = state?.Enabled ?? false;
                }

                this.isUpdating = false;
            }
        }

        public IReadOnlyList<DescriptorViewModel> GetDependentsForDisable(ModuleViewModel module)
        {
            List<DescriptorViewModel> result = new List<DescriptorViewModel>();

            foreach (ModuleViewModel otherModule in this.Modules)
            {
                foreach (DescriptorViewModel desc in otherModule.ProvidedDescriptors)
                {
                    if (otherModule == module || !desc.IsEnabled)
                        continue;

                    // TODO: Must first add the depend/conflict data to the descriptor VM
                    // if desc depends on ANY descriptor in the module being disabled
                    //if (desc.DependsOn.Any(d => module.ProvidedDescriptors.Contains(d)))
                    //    result.Add(desc);
                }
            }

            return result;
        }
        public IReadOnlyList<DescriptorViewModel> GetConflictsForEnable(ModuleViewModel module)
        {
            List<DescriptorViewModel> result = new List<DescriptorViewModel>();

            foreach (ModuleViewModel otherModule in this.Modules)
            {
                foreach (DescriptorViewModel desc in otherModule.ProvidedDescriptors)
                {
                    if (otherModule == module || !desc.IsEnabled)
                        continue;

                    // TODO: Must first add the depend/conflict data to the descriptor VM
                    // if desc conflicts with ANY descriptor in the module being enabled
                    //if (desc.ConflictsWith.Any(d => module.ProvidedDescriptors.Contains(d)))
                    //    result.Add(desc);
                }
            }

            return result;
        }
    }
}