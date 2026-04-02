using NucleusAF.Extensions;
using System.Reflection;

namespace NucleusAF.Models.Modules
{
    public record ModuleMetadata(Guid ModuleId, string IconSource, string Name, string Description, string Version, string Author, List<string> Categories, List<string>? Tags = null, string? DocsLink = null, string? SupportLink = null, string? SupportContact = null, string? License = null, string? ChangeLog = null)
    {
        #region ModuleMetadata: Key Fields

        public Guid ModuleId { get; set; } = ModuleId;
        public string IconSource { get; set; } = IconSource;
        public string Name { get; set; } = Name;
        public string Description { get; set; } = Description;
        public string Version { get; set; } = Version;
        public string Author { get; set; } = Author;
        public List<string> Categories { get; set; } = Categories;

        #endregion

        #region ModuleMetadata: Metadata
        public List<string> Tags { get; set; } = Tags ?? [];
        public string DocsLink { get; set; } = DocsLink ?? string.Empty;
        public string SupportLink { get; set; } = SupportLink ?? string.Empty;
        public string SupportContact { get; set; } = SupportContact ?? string.Empty;
        public string License { get; set; } = License ?? string.Empty;
        public string ChangeLog { get; set; } = ChangeLog ?? string.Empty;
        #endregion

        public static ModuleMetadata FromModule(Type moduleType)
        {
            ArgumentNullException.ThrowIfNull(moduleType);

            Dictionary<string, object?> staticProps = moduleType.GetStaticProperties();
            Type metadataType = typeof(ModuleMetadata);

            ConstructorInfo ctor =
                metadataType
                    .GetConstructors()
                    .SingleOrDefault(c => c.GetParameters().Length > 0)
                        ?? throw new InvalidOperationException("ModuleMetadata must have a primary constructor.");

            ParameterInfo[] ctorParams = ctor.GetParameters();
            object?[] args = new object?[ctorParams.Length];

            for (int i = 0; i < ctorParams.Length; i++)
            {
                ParameterInfo param = ctorParams[i];
                if (staticProps.TryGetValue(param.Name!, out object? value))
                {
                    if (value is null && param.ParameterType.IsValueType)
                        throw new InvalidOperationException($"Cannot assign null to value type parameter '{param.Name}'.");

                    if (value != null && !param.ParameterType.IsAssignableFrom(value.GetType()))
                        throw new InvalidOperationException($"Type mismatch for parameter '{param.Name}'. Expected: {param.ParameterType.Name}, Got: {value.GetType().Name}");

                    args[i] = value;
                }
                else
                {
                    throw new InvalidOperationException($"Required parameter '{param.Name}' was not provided and cannot be resolved automatically.");
                }
            }

            return (ModuleMetadata)ctor.Invoke(args);
        }
    }
}