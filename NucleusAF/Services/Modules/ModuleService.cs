using Microsoft.Extensions.Logging;
using NucleusAF.Attributes;
using NucleusAF.Interfaces.Providers;
using NucleusAF.Interfaces.Services.Modules;
using NucleusAF.Models.Modules;
using System.Reflection;

namespace NucleusAF.Services.Modules
{
    public class ModuleService : IModuleService
    {
        private readonly ILogger<IModuleService> logger;
        private readonly SemaphoreSlim iolock = new(1, 1);

        public ModuleService(ILogger<IModuleService> logger)
        {
            ArgumentNullException.ThrowIfNull(logger);

            this.logger = logger;
        }

        #region ModuleService: Module Instances

        public TModule? GetLoadedModule<TModule>(ProviderInterface provider) where TModule : ModuleBase
        {
            ArgumentNullException.ThrowIfNull(provider);

            this.iolock.Wait();

            try
            {
                if (!File.Exists(provider.AssemblyLocation))
                {
                    this.logger.LogWarning("[ModuleService] Module assembly does not exist: {AssemblyPath}", provider.AssemblyLocation);

                    return null;
                }

                Assembly assembly = Assembly.LoadFrom(provider.AssemblyLocation);
                Type? type = assembly.GetType(provider.ImplementationName);

                if (type == null)
                {
                    this.logger.LogWarning("[ModuleService] Type {TypeName} not found in assembly {AssemblyName}.", provider.ImplementationName, provider.AssemblyName);

                    return null;
                }

                bool isSubclass = type.IsSubclassOf(typeof(ModuleBase));
                bool isNotAbstract = !type.IsAbstract;

                if (isSubclass && isNotAbstract)
                {
                    object? instance = Activator.CreateInstance(type);

                    bool instanceCreated = instance != null;
                    bool instanceIsCorrectType = instance is TModule;

                    if (instanceCreated && instanceIsCorrectType)
                    {
                        this.logger.LogInformation("[ModuleService] Loaded module: {ModuleName} ({TypeName})", provider.AssemblyName, provider.ImplementationName);

                        return (TModule)instance!;
                    }
                    else
                    {
                        this.logger.LogError("[ModuleService] Failed to create instance of type {TypeName} from assembly {AssemblyName}.", provider.ImplementationName, provider.AssemblyName);

                        return null;
                    }
                }
                else return null;
            }
            catch (Exception ex)
            {
                this.logger.LogError("[ModuleService] Failed to load module {TypeName} from {AssemblyPath}. Exception: {ExceptionMessage}", provider.ImplementationName, provider.AssemblyLocation, ex.Message);

                return null;
            }
            finally
            {
                this.iolock.Release();
            }
        }
        public TProvider? GetLoadedProviders<TProvider>(ProviderInterface provider) where TProvider : class
        {
            ArgumentNullException.ThrowIfNull(provider);

            ModuleBase? instance = this.GetLoadedModule<ModuleBase>(provider);

            return instance as TProvider;
        }

        #endregion

        #region ModuleService: Provider Data

        public DescriptorProviderAttribute? GetDescriptorProviderAttribute(string providerFullName)
        {
            if (string.IsNullOrWhiteSpace(providerFullName))
                throw new ArgumentNullException(nameof(providerFullName));

            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type? providerType = null;

                try
                {
                    providerType = assembly.GetType(providerFullName, throwOnError: false, ignoreCase: false);
                }
                catch { /* nothing to see here */ }

                if (providerType != null && providerType.IsInterface)
                {
                    DescriptorProviderAttribute? attr =
                        providerType.GetCustomAttribute<DescriptorProviderAttribute>(inherit: false);

                    if (attr != null) return attr;
                }
            }

            return null;
        }

        #endregion

        #region ModuleService: Discovery

        public IEnumerable<ModuleReference> Discover(string moduleDirectory)
        {
            ArgumentNullException.ThrowIfNull(moduleDirectory);

            List<Assembly> assemblies = this.LoadAssembliesFromDirectory(moduleDirectory);

            Dictionary<Assembly, Type[]> assemblyTypes = this.ExtractTypesFromAssemblies(assemblies);

            List<Type> filteredModuleTypes = FilterModuleTypes(assemblyTypes);
            List<Type> moduleTypes = FilterInterfaceTypes(filteredModuleTypes);

            IEnumerable<IGrouping<ModuleMetadata, Type>> moduleGroups = moduleTypes.GroupBy(t => ModuleMetadata.FromModule(t));

            HashSet<Guid> seenModuleIds = [];

            List<IGrouping<ModuleMetadata, Type>> uniqueModuleGroups = [];

            foreach (IGrouping<ModuleMetadata, Type> group in moduleGroups)
            {
                Guid moduleId = group.Key.ModuleId;

                if (seenModuleIds.Contains(moduleId))
                {
                    this.logger.LogError("[ModuleService] Duplicate moduleId detected and ignored: {ModuleId}", moduleId);

                    continue;
                }

                seenModuleIds.Add(moduleId);

                uniqueModuleGroups.Add(group);
            }

            return this.BuildModulesFromGroups(uniqueModuleGroups);
        }

        private List<Assembly> LoadAssembliesFromDirectory(string moduleDirectory)
        {
            List<Assembly> assemblies = [];

            this.iolock.Wait();

            try
            {
                string[] assemblyPaths = Directory.GetFiles(moduleDirectory, "*.dll", SearchOption.AllDirectories);

                foreach (string assemblyPath in assemblyPaths)
                {
                    try
                    {
                        Assembly assembly = Assembly.LoadFrom(assemblyPath);
                        assemblies.Add(assembly);
                    }
                    catch (Exception ex)
                    {
                        this.logger.LogError("[ModuleService] Failed to load assembly {AssemblyPath}: {ExceptionMessage}", assemblyPath, ex.Message);
                    }
                }
            }
            catch (Exception ex)
            {
                this.logger.LogError("[ModuleService] Failed to load modules: {ExceptionMessage}", ex.Message);
            }
            finally
            {
                this.iolock.Release();
            }

            return assemblies;
        }
        private Dictionary<Assembly, Type[]> ExtractTypesFromAssemblies(List<Assembly> assemblies)
        {
            Dictionary<Assembly, Type[]> assemblyTypes = [];

            foreach (Assembly assembly in assemblies)
            {
                try
                {
                    assemblyTypes[assembly] = assembly.GetTypes();
                }
                catch (ReflectionTypeLoadException rtle)
                {
                    foreach (Exception? loaderEx in rtle.LoaderExceptions)
                        if (loaderEx != null)
                            this.logger.LogError("[ModuleService] Loader exception in assembly {AssemblyName}: {ExceptionMessage}", assembly.FullName, loaderEx.Message);

                    Type[] validTypes = rtle.Types.Where(t => t != null).ToArray()!;

                    assemblyTypes[assembly] = validTypes;
                }
                catch (Exception ex)
                {
                    this.logger.LogError("[ModuleService] Failed to get types from assembly {AssemblyName}: {ExceptionMessage}", assembly.FullName, ex.Message);

                    assemblyTypes[assembly] = [];
                }
            }

            return assemblyTypes;
        }
        private static List<Type> FilterModuleTypes(Dictionary<Assembly, Type[]> assemblyTypes)
        {
            List<Type> moduleTypes = [];

            foreach (Type? type in assemblyTypes.SelectMany(kvp => kvp.Value))
            {
                if (type != null)
                {
                    bool derivesFromModuleBase = type.IsSubclassOf(typeof(ModuleBase));
                    bool isNotAbstract = !type.IsAbstract;

                    if (!(derivesFromModuleBase && isNotAbstract))
                        continue;

                    moduleTypes.Add(type);
                }
            }
            return moduleTypes;
        }
        private static List<Type> FilterInterfaceTypes(List<Type> moduleInterfaceTypes)
        {
            List<Type> validInterfaces = [];

            foreach (Type interfaceType in moduleInterfaceTypes)
            {
                DescriptorProviderAttribute? attr = null;

                Type[] allInterfaces = [interfaceType, .. interfaceType.GetInterfaces()];

                foreach (Type it in allInterfaces)
                {
                    attr = it.GetCustomAttribute<DescriptorProviderAttribute>(inherit: false);

                    if (attr != null) break;
                }

                if (attr == null) continue;

                string methodName = attr.DescriptorAccessorName;

                MethodInfo? methodInfo = interfaceType.GetMethod(methodName, BindingFlags.Public | BindingFlags.Instance);

                if (methodInfo == null) continue;

                validInterfaces.Add(interfaceType);
            }

            return validInterfaces;
        }
        private static List<ProviderInterface> ExtractProviders(Type concreteType)
        {
            List<ProviderInterface> implementations = [];
            Assembly moduleAssembly = concreteType.Assembly;

            Type[] allInterfaces = concreteType.GetInterfaces();

            foreach (Type interfaceType in allInterfaces)
            {
                bool implementsModule = typeof(IProvider).IsAssignableFrom(interfaceType);
                bool isNotBaseModule = interfaceType != typeof(IProvider);

                if (implementsModule && isNotBaseModule)
                {
                    ProviderInterface provider = new(moduleAssembly, interfaceType, concreteType);

                    implementations.Add(provider);
                }
            }

            return implementations;
        }
        private List<ModuleReference> BuildModulesFromGroups(IEnumerable<IGrouping<ModuleMetadata, Type>> moduleGroups)
        {
            List<ModuleReference> modules = [];

            foreach (IGrouping<ModuleMetadata, Type> group in moduleGroups)
            {
                ModuleMetadata moduleMetadata = group.Key;
                List<ProviderInterface> implementations = [];

                foreach (Type concreteType in group)
                {
                    List<ProviderInterface> providers = ExtractProviders(concreteType);

                    implementations.AddRange(providers);
                }

                Type? primaryType = group.FirstOrDefault();

                if (primaryType != null)
                {
                    Assembly primaryAssembly = primaryType.Assembly;
                    ModuleReference module = new(primaryAssembly, primaryType, moduleMetadata, implementations);

                    modules.Add(module);
                }
                else
                {
                    this.logger.LogError("[ModuleService] Module discovery failed: No module types found for metadata {ModuleName} (id={ModuleId}).", moduleMetadata.Name, moduleMetadata.ModuleId);

                    throw new InvalidOperationException($"No module types found in group for metadata: {moduleMetadata.Name} ({moduleMetadata.ModuleId})");
                }
            }

            return modules;
        }

        #endregion
    }
}