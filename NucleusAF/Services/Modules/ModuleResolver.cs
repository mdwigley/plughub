using Microsoft.Extensions.Logging;
using NucleusAF.Attributes;
using NucleusAF.Extensions;
using NucleusAF.Interfaces.Services.Modules;
using NucleusAF.Models;
using NucleusAF.Models.Descriptors;
using System.Reflection;

namespace NucleusAF.Services.Modules
{
    public class DescriptorResolutionContext<TDescriptor>(List<TDescriptor> descriptors, HashSet<TDescriptor> duplicates) where TDescriptor : Descriptor
    {
        public Dictionary<Guid, TDescriptor> IdToDescriptor { get; } = descriptors.ToDictionary(d => d.DescriptorId, d => d);
        public Dictionary<TDescriptor, HashSet<TDescriptor>> Graph { get; } = descriptors.ToDictionary(d => d, _ => new HashSet<TDescriptor>());

        public HashSet<TDescriptor> DependencyDisabled { get; } = [];
        public HashSet<TDescriptor> ConflictDisabled { get; } = [];
        public HashSet<TDescriptor> DuplicateIdDisabled { get; } = duplicates;

        public IReadOnlyList<TDescriptor> GetSorted()
            => [.. this.Graph.Keys.TopologicalSort(d => this.Graph[d])];
    }

    public class ModuleResolver : IModuleResolver
    {
        private readonly ILogger<IModuleResolver> logger;

        public ModuleResolver(ILogger<IModuleResolver> logger)
        {
            ArgumentNullException.ThrowIfNull(logger);

            this.logger = logger;
        }

        #region ModuleResolver: Resolution

        public DescriptorResolutionContext<TDescriptor> ResolveContext<TDescriptor>(IEnumerable<TDescriptor> descriptors) where TDescriptor : Descriptor
        {
            ArgumentNullException.ThrowIfNull(descriptors);

            HashSet<TDescriptor> duplicates = FilterDuplicates(descriptors, out List<TDescriptor> descriptorsList);

            foreach (TDescriptor duplicate in duplicates)
                this.logger.LogError("[ModuleResolver] Duplicate DescriptorId {DescriptorId} for module {ModuleId}, version {Version} detected and excluded.", duplicate.DescriptorId, duplicate.ModuleId, duplicate.Version);

            DescriptorResolutionContext<TDescriptor> context = new(descriptorsList, duplicates);

            foreach (TDescriptor descriptor in descriptorsList)
            {
                this.ProcessDependencies(descriptor, context);
                this.ProcessConflicts(descriptor, context);
                this.ProcessLoadBefore(descriptor, context);
                this.ProcessLoadAfter(descriptor, context);
            }

            RemoveInvalidDescriptors(context);

            return context;
        }
        public IEnumerable<TDescriptor> ResolveDescriptors<TDescriptor>(IEnumerable<TDescriptor> descriptors) where TDescriptor : Descriptor
        {
            DescriptorResolutionContext<TDescriptor> context = this.ResolveContext(descriptors);

            return context.GetSorted();
        }
        public IReadOnlyList<TDescriptor> ResolveAndOrder<TProvider, TDescriptor>(IEnumerable<TProvider> modules) where TProvider : class where TDescriptor : Descriptor
        {
            ArgumentNullException.ThrowIfNull(modules);

            Type ifaceType = typeof(TProvider);
            DescriptorProviderAttribute attr =
                ifaceType.GetCustomAttribute<DescriptorProviderAttribute>(inherit: false)
                    ?? throw new InvalidOperationException($"Provider {ifaceType.Name} is missing DescriptorProviderAttribute");

            MethodInfo accessor =
                ifaceType.GetMethod(attr.DescriptorAccessorName)
                    ?? throw new InvalidOperationException($"Descriptor accessor {attr.DescriptorAccessorName} not found on {ifaceType.Name}");

            List<TDescriptor> allDescriptors = [];

            foreach (TProvider module in modules)
                if (accessor.Invoke(module, null) is IEnumerable<TDescriptor> descriptors)
                    allDescriptors.AddRange(descriptors);

            DescriptorResolutionContext<TDescriptor> context = this.ResolveContext(allDescriptors);
            IReadOnlyList<TDescriptor> sorted = context.GetSorted();

            return attr.SortContext switch
            {
                DescriptorSortContext.Forward => sorted,
                DescriptorSortContext.Reverse => [.. sorted.Reverse()],
                _ => sorted
            };
        }
        public IReadOnlyList<TDescriptor> ResolveAndOrder<TDescriptor>(IEnumerable<TDescriptor> descriptors, DescriptorSortContext sortContext) where TDescriptor : Descriptor
        {
            DescriptorResolutionContext<TDescriptor> context = ResolveContext(descriptors);
            IReadOnlyList<TDescriptor> sorted = context.GetSorted();

            return sortContext switch
            {
                DescriptorSortContext.Forward => sorted,
                DescriptorSortContext.Reverse => [.. sorted.Reverse()],
                _ => sorted
            };
        }

        private static HashSet<TDescriptor> FilterDuplicates<TDescriptor>(IEnumerable<TDescriptor> descriptors, out List<TDescriptor> cleanDescriptors) where TDescriptor : Descriptor
        {
            cleanDescriptors = [];

            HashSet<TDescriptor> duplicates = [];
            HashSet<Guid> seenIds = [];

            foreach (TDescriptor descriptor in descriptors)
            {
                if (seenIds.Add(descriptor.DescriptorId))
                    cleanDescriptors.Add(descriptor);
                else
                    duplicates.Add(descriptor);
            }
            return duplicates;
        }
        private void ProcessDependencies<TDescriptor>(TDescriptor descriptor, DescriptorResolutionContext<TDescriptor> context) where TDescriptor : Descriptor
        {
            foreach (DescriptorReference dep in descriptor.DependsOn ?? [])
            {
                bool hasDep = context.IdToDescriptor.TryGetValue(dep.DescriptorId, out TDescriptor? depDesc);

                if (!hasDep)
                {
                    this.logger.LogWarning("[ModuleResolver] Descriptor {DescriptorId} depends on {DependencyID} (version in [{Min}, {Max}]), but it is missing or version mismatch (found: missing).", descriptor.DescriptorId, dep.DescriptorId, dep.MinVersion, dep.MaxVersion);

                    context.DependencyDisabled.Add(descriptor);
                }
                else if (depDesc != null)
                {
                    bool matchesDep = dep.Matches(depDesc.ModuleId, depDesc.DescriptorId, depDesc.Version);

                    if (!matchesDep)
                    {
                        this.logger.LogWarning("[ModuleResolver] Descriptor {DescriptorId} depends on {DependencyID} (version in [{Min}, {Max}]), but it is missing or version mismatch (found: {FoundVersion}).", descriptor.DescriptorId, dep.DescriptorId, dep.MinVersion, dep.MaxVersion, depDesc.Version);

                        context.DependencyDisabled.Add(descriptor);
                    }
                }
                else
                {
                    this.logger.LogCritical("[ModuleResolver] Critical error: TryGetValue returned true but descriptor is null for {DescriptorId}. This indicates a serious backend data integrity issue.", dep.DescriptorId);

                    throw new InvalidOperationException($"Descriptor lookup returned null for descriptor {dep.DescriptorId} despite successful lookup");
                }
            }
        }
        private void ProcessConflicts<TDescriptor>(TDescriptor descriptor, DescriptorResolutionContext<TDescriptor> context) where TDescriptor : Descriptor
        {
            if (descriptor.ConflictsWith == null) return;

            foreach (TDescriptor otherDescriptor in context.Graph.Keys)
            {
                if (otherDescriptor == descriptor)
                    continue;

                foreach (DescriptorReference conflict in descriptor.ConflictsWith)
                {
                    bool matchesConflict = conflict.Matches(otherDescriptor.ModuleId, otherDescriptor.DescriptorId, otherDescriptor.Version);

                    if (matchesConflict)
                    {
                        this.logger.LogWarning("[ModuleResolver] Descriptor {DescriptorId} conflicts with {ConflictID} (version in [{Min}, {Max}]), but both are enabled (found: {FoundVersion}).", descriptor.DescriptorId, conflict.DescriptorId, conflict.MinVersion, conflict.MaxVersion, otherDescriptor.Version);

                        context.ConflictDisabled.Add(descriptor);

                        return;
                    }
                }
            }
        }
        private void ProcessLoadBefore<TDescriptor>(TDescriptor descriptor, DescriptorResolutionContext<TDescriptor> context) where TDescriptor : Descriptor
        {
            foreach (DescriptorReference before in descriptor.LoadBefore ?? [])
            {
                bool hasBefore = context.IdToDescriptor.TryGetValue(before.DescriptorId, out TDescriptor? beforeDesc);

                if (!hasBefore)
                    continue;

                else if (beforeDesc != null)
                {
                    bool matchesBefore = before.Matches(beforeDesc.ModuleId, beforeDesc.DescriptorId, beforeDesc.Version);
                    bool notInvalid = !context.ConflictDisabled.Contains(beforeDesc) && !context.DependencyDisabled.Contains(beforeDesc);

                    if (matchesBefore && notInvalid)
                        context.Graph[beforeDesc].Add(descriptor);
                }
                else
                {
                    this.logger.LogCritical("[ModuleResolver] Critical error: TryGetValue returned true but descriptor is null for {DescriptorId}. This indicates a serious backend data integrity issue.", before.DescriptorId);

                    throw new InvalidOperationException($"Descriptor lookup returned null for descriptor {before.DescriptorId} despite successful lookup");
                }
            }
        }
        private void ProcessLoadAfter<TDescriptor>(TDescriptor descriptor, DescriptorResolutionContext<TDescriptor> context) where TDescriptor : Descriptor
        {
            foreach (DescriptorReference after in descriptor.LoadAfter ?? [])
            {
                bool hasAfter = context.IdToDescriptor.TryGetValue(after.DescriptorId, out TDescriptor? afterDesc);

                if (!hasAfter)
                    continue;

                else if (afterDesc != null)
                {
                    bool matchesAfter = after.Matches(afterDesc.ModuleId, afterDesc.DescriptorId, afterDesc.Version);
                    bool notInvalid = !context.ConflictDisabled.Contains(afterDesc) && !context.DependencyDisabled.Contains(afterDesc);

                    if (matchesAfter && notInvalid)
                        context.Graph[descriptor].Add(afterDesc);
                }
                else
                {
                    this.logger.LogCritical("[ModuleResolver] Critical error: TryGetValue returned true but descriptor is null for {DescriptorId}. This indicates a serious backend data integrity issue.", after.DescriptorId);

                    throw new InvalidOperationException($"Descriptor lookup returned null for descriptor {after.DescriptorId} despite successful lookup");
                }
            }
        }

        private static void RemoveInvalidDescriptors<TDescriptor>(DescriptorResolutionContext<TDescriptor> context) where TDescriptor : Descriptor
        {
            IEnumerable<TDescriptor> allDisabled = context.ConflictDisabled
                    .Concat(context.DependencyDisabled)
                    .Distinct();

            foreach (TDescriptor invalid in allDisabled)
                context.Graph.Remove(invalid);
        }

        #endregion
    }
}