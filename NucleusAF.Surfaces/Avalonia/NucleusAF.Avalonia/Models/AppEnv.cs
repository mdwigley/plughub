using Avalonia.Controls;
using NucleusAF.Models;

namespace NucleusAF.Avalonia.Models
{
    /// <summary>
    /// Defines environment-specific settings for the NucleusAF application.
    /// These values typically vary by deployment environment (e.g., dev, test, prod)
    /// and can override or complement values in <see cref="AppConfig"/>.
    /// </summary>
    public sealed class AppEnv
    {
        #region AppConfig: Shell & Theme Settings

        /// <summary>
        /// Gets or sets the identifier of the main view to load at startup.
        /// This should match the composite key of a module-provided main view
        /// (e.g., "Namespace.ViewType:Key"). If not set, the default main view is used.
        /// </summary>
        public string MainViewKey { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets a value indicating whether the application should use
        /// the built-in default theme engine (e.g., FluentAvalonia) at startup.
        /// If false, no theme is applied unless explicitly configured elsewhere.
        /// </summary>
        public bool UseDefaultTheme { get; set; } = true;

        /// <summary>
        /// Gets or sets the system theme to request at startup.
        /// Valid values are the same as Avalonia's ThemeVariant: Default, Light, Dark.
        /// </summary>
        public string SystemTheme { get; set; } = "Default";

        /// <summary>
        /// Gets or sets a value indicating whether the application should prefer
        /// the operating system's theme setting when determining the theme.
        /// </summary>
        public bool PreferSystemTheme { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether the application should prefer
        /// the user's accent color from the operating system.
        /// </summary>
        public bool PreferUserAccentColor { get; set; } = true;

        #endregion

        #region AppConfig: Main Window Settings

        /// <summary>
        /// Gets or sets the Avalonia resource URI for the window icon.
        /// Example: "avares://NucleusAF/Assets/avalonia-logo.ico"
        /// </summary>
        public string WindowIconPath { get; set; } = "avares://NucleusAF.Avalonia/Assets/avalonia-logo.ico";

        /// <summary>
        /// Gets or sets the title text for the main window.
        /// </summary>
        public string WindowTitle { get; set; } = "NucleusAF";

        /// <summary>
        /// Gets or sets the minimum width of the main window at startup.
        /// </summary>
        public double WindowMinWidth { get; set; } = 640;

        /// <summary>
        /// Gets or sets the minimum height of the main window at startup.
        /// </summary>
        public double WindowMinHeight { get; set; } = 480;

        /// <summary>
        /// Gets or sets the initial width of the main window.
        /// </summary>
        public double WindowWidth { get; set; } = 1024;

        /// <summary>
        /// Gets or sets the initial height of the main window.
        /// </summary>
        public double WindowHeight { get; set; } = 768;

        /// <summary>
        /// Gets or sets the initial window state (Normal, Minimized, Maximized, FullScreen).
        /// </summary>
        public WindowState WindowStartupState { get; set; } = WindowState.Normal;

        /// <summary>
        /// Gets or sets the startup location of the main window (Manual, CenterScreen, CenterOwner).
        /// </summary>
        public WindowStartupLocation WindowStartupLocation { get; set; } = WindowStartupLocation.CenterScreen;

        /// <summary>
        /// Gets or sets the transparency level hint for the main window.
        /// </summary>
        public WindowTransparencyLevel TransparencyPreference { get; set; } = WindowTransparencyLevel.None;

        /// <summary>
        /// Gets or sets a value indicating whether the main window can be resized.
        /// </summary>
        public bool CanResize { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether the main window should appear in the taskbar.
        /// </summary>
        public bool ShowInTaskbar { get; set; } = true;

        /// <summary>
        /// Gets or sets the system decorations mode for the main window.
        /// </summary>
        public SystemDecorations SystemDecorations { get; set; } = SystemDecorations.BorderOnly;

        /// <summary>
        /// Gets or sets a value indicating whether the client area should extend into the window decorations.
        /// </summary>
        public bool ExtendClientAreaToDecorationsHint { get; set; } = true;

        /// <summary>
        /// Gets or sets the height hint for the extended client area title bar.
        /// </summary>
        public int ExtendClientAreaTitleBarHeightHint { get; set; } = 0;

        #endregion

        #region AppConfig: Application Identity

        /// <summary>
        /// Gets or sets the display name of the application.
        /// Defaults to "NucleusAF" if not specified.
        /// </summary>
        public string AppName { get; set; } = "NucleusAF";

        /// <summary>
        /// Gets or sets the Avalonia resource URI for the application icon.
        /// Example: "avares://NucleusAF/Assets/avalonia-logo.ico"
        /// </summary>
        public string AppIconPath { get; set; } = "avares://NucleusAF.Avalonia/Assets/avalonia-logo.ico";

        /// <summary>
        /// Gets or sets the application link (e.g., website URL) to open when the app icon is clicked.
        /// </summary>
        public string AppLink { get; set; } = "https://enterlucent.com";

        #endregion
    }
}