using System.Text.Json;

namespace NucleusAF.Models
{
    /// <summary>
    /// Defines the interval at which log files should be rolled over to new files.
    /// </summary>
    public enum LoggingRollingInterval
    {
        /// <summary>
        /// Log files never roll over - all logging goes to a single file.
        /// </summary>
        Infinite,

        /// <summary>
        /// Log files roll over annually.
        /// </summary>
        Year,

        /// <summary>
        /// Log files roll over monthly.
        /// </summary>
        Month,

        /// <summary>
        /// Log files roll over daily.
        /// </summary>
        Day,

        /// <summary>
        /// Log files roll over hourly.
        /// </summary>
        Hour,

        /// <summary>
        /// Log files roll over every minute.
        /// </summary>
        Minute
    }

    /// <summary>
    /// Central configuration class for NucleusAF application settings, including logging, 
    /// configuration management, storage paths, and module directories. All properties 
    /// have sensible defaults suitable for most deployment scenarios.
    /// </summary>
    public sealed class AppConfig
    {
        /// <summary>
        /// Gets or sets the root directory for all NucleusAF application data.
        /// Defaults to %APPDATA%\NucleusAF on Windows or equivalent on other platforms.
        /// </summary>
        public string? BaseDirectory { get; set; } =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "NucleusAF");

        #region AppConfig: Logging Settings

        /// <summary>
        /// Gets or sets the directory where log files will be written.
        /// Defaults to a "Logging" subdirectory within the base directory.
        /// </summary>
        public string? LoggingDirectory { get; set; } =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "NucleusAF", "Logging");

        /// <summary>
        /// Gets or sets how frequently log files should roll over to new files.
        /// Daily rollover provides a good balance between file management and granularity.
        /// </summary>
        public LoggingRollingInterval? LoggingRolloverInterval { get; set; } = LoggingRollingInterval.Day;

        /// <summary>
        /// Gets or sets the filename pattern for log files.
        /// The dash in "application-.log" will be replaced with timestamp information
        /// based on the rollover interval (e.g., "application-20250123.log" for daily).
        /// </summary>
        public string? LoggingFileName { get; set; } = "application-.log";

        #endregion

        #region AppConfig: Config Settings

        /// <summary>
        /// Gets or sets the directory where configuration files will be stored.
        /// Defaults to a "Config" subdirectory within the base directory.
        /// </summary>
        public string? ConfigDirectory { get; set; } =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "NucleusAF", "Config");

        /// <summary>
        /// Gets or sets the JSON serialization options used for configuration file operations.
        /// Customize this to control formatting, naming policies, and other serialization behavior.
        /// </summary>
        public JsonSerializerOptions? ConfigJsonOptions { get; set; } = new JsonSerializerOptions();

        #endregion

        #region AppConfig: Local Storage Settings

        /// <summary>
        /// Gets or sets the directory path for application data storage.
        /// Used by modules and the core application for persistent data storage.
        /// Defaults to a "Storage" subdirectory within the base directory.
        /// </summary>
        public string? StorageDirectory { get; set; }
            = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "NucleusAF", "Storage");

        #endregion

        #region AppConfig: Local Module Settings

        /// <summary>
        /// Gets or sets the directory path where module assemblies are located.
        /// Defaults to a "Modules" subdirectory within the application's base directory,
        /// making it suitable for portable deployments and easy module management.
        /// </summary>
        public string? ModuleDirectory { get; set; }
            = Path.Combine(AppContext.BaseDirectory, "Modules");

        #endregion
    }
}