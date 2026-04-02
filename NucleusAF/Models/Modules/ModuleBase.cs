namespace NucleusAF.Models.Modules
{
    /// <summary>
    /// Provides a base class for all module in a NucleusAF-based application.
    /// <para>
    /// <b>Required:</b> All modules must override the static properties in the "Key Fields" region
    /// (Id, Name, etc.) using <c>static</c> and <c>new</c>.
    /// This allows the host to gather module metadata via reflection <b>without instantiating the module class</b>.
    /// </para>
    /// <para>
    /// <b>Optional:</b> All other metadata properties (such as DocsLink, SupportLink, Tags, License, etc.) and
    /// dependency management properties are optional, but encouraged for richer module discovery and management.
    /// </para>
    /// <para>
    /// <b>Example:</b>
    /// <code>
    /// public class MyModule : ModuleBase
    /// {
    ///     public new static Guid Id { get; } = new Guid("...");
    ///     public new static string Name { get; } = "My Module";
    ///     // ...etc
    /// }
    /// </code>
    /// </para>
    /// </summary>
    public abstract class ModuleBase
    {
        #region ModuleBase: Key Fields

        /// <summary>
        /// Unique identifier for the module. This id should be unique across all modules. (e.g., GUID)
        /// This id is used in <see cref="Dependencies"/> and <see cref="Conflicts"/> which require a <see cref="DescriptorReference"/> 
        /// including both the id and version range, as well as in <see cref="LoadBefore"/> and <see cref="LoadAfter"/> which only use the id.
        /// </summary>
        public static Guid ModuleId { get; } = Guid.Empty;

        /// <summary>
        /// The module icon is specified using a URI. This can be an Avalonia resource URI starting with "avares://", such as "avares://YourApp/Assets/module-icon.png", 
        /// or a standard remote URI beginning with "http://" or "https://". 
        /// 
        /// If the URI is null or empty, a default icon will be used.
        /// </summary>
        public static string IconSource { get; } = string.Empty;

        /// <summary>
        /// Name of the module.
        /// </summary>
        public static string Name { get; } = string.Empty;

        /// <summary>
        /// Description of the module.
        /// </summary>
        public static string Description { get; } = string.Empty;

        /// <summary>
        /// Version of the module.
        /// </summary>
        public static string Version { get; } = "0.0.0";

        /// <summary>
        /// Author of the module.
        /// </summary>
        public static string Author { get; } = string.Empty;

        /// <summary>
        /// List of category names for this module. Used for grouping and filtering modules in the UI.
        /// Modules can belong to multiple categories. Default: ["Uncategorized"].
        /// </summary>
        public static List<string> Categories { get; } = [];

        #endregion

        #region ModuleBase: Metadata

        /// <summary>
        /// Tags associated with the module. Tags can be used for additional filtering or categorization.
        /// </summary>
        public static List<string> Tags { get; } = [];

        /// <summary>
        /// URL to the documentation for the module.
        /// </summary>
        public static string DocsLink { get; } = string.Empty;

        /// <summary>
        /// URL to the support page for the module.
        /// </summary>
        public static string SupportLink { get; } = string.Empty;

        /// <summary>
        /// Contact information for support related to the module.
        /// </summary>
        public static string SupportContact { get; } = string.Empty;

        /// <summary>
        /// License information for the module.
        /// </summary>
        public static string License { get; } = string.Empty;

        /// <summary>
        /// URL to the change log for the module.
        /// </summary>
        public static string ChangeLog { get; } = string.Empty;

        #endregion
    }
}