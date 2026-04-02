using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NucleusAF.Utility;
using Serilog;

namespace NucleusAF.UnitTests
{
    internal sealed class MSTestHelpers : IDisposable
    {
        public Guid Guid { get; init; }
        public string TempDirectory { get; init; }
        public string ModuleDirectory { get; init; }


        public MSTestHelpers()
        {
            this.Guid = Guid.NewGuid();
            this.TempDirectory = Path.Combine(Path.GetTempPath(), "NucleusAFTest", this.Guid.ToString());
            this.ModuleDirectory = Path.Combine(GetDesktopProjectOutputPath(), "Modules");
        }

        public static string GetDesktopProjectOutputPath()
        {
            string testDir = AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar);
            DirectoryInfo dir = new(testDir);

            string rid = System.Runtime.InteropServices.RuntimeInformation.RuntimeIdentifier;
            string tfm = dir.Name;
            string config = dir.Parent!.Name;
            string bin = dir.Parent!.Parent!.Name;
            string solutionRoot = dir.Parent!.Parent!.Parent!.Parent!.FullName;

            string result = Path.Combine(
                solutionRoot,
                "NucleusAF.Surfaces",
                "Avalonia",
                "NucleusAF.Desktop",
                "bin",
                config,
                tfm,
                rid
            );

            return result;
        }


        public void CreateTempFile(string text, string filename)
            => Atomic.Write(Path.Combine(this.TempDirectory, filename), text);

        public ServiceCollection CreateTempServiceCollection()
        {
            ServiceCollection services = [];

            services.AddLogging(builder =>
            {
                Directory.CreateDirectory(this.TempDirectory);

                Log.Logger = new LoggerConfiguration()
                    .MinimumLevel.Debug()
                    .WriteTo.File(Path.Combine(this.TempDirectory, "UnitTest.logs"),
                        rollingInterval: RollingInterval.Infinite)
                    .CreateLogger();

                builder.ClearProviders();
                builder.AddSerilog(Log.Logger, dispose: true);
            });

            return services;
        }

        public void Dispose()
        {
            Log.CloseAndFlush();

            if (!string.IsNullOrWhiteSpace(this.TempDirectory) && Directory.Exists(this.TempDirectory))
                Directory.Delete(this.TempDirectory, recursive: true);
        }
    }
}