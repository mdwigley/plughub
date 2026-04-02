using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Styling;


namespace NucleusAF.Avalonia.Extensions
{
    public static class ApplicationExtensions
    {
        /// <summary>
        /// Retrieves the application window.
        /// </summary>
        /// <param name="app">
        /// The <see cref="Avalonia.Application"/> instance from which to retrieve the window.
        /// </param>
        /// <returns>The application <see cref="Window"/>, if available; otherwise, <c>null</c>.</returns>
        /// <remarks>
        /// Handles different application lifetime scenarios:
        /// <list type="bullet">
        /// <item><description>For <see cref="IClassicDesktopStyleApplicationLifetime"/>, returns <see cref="IClassicDesktopStyleApplicationLifetime.MainWindow"/> cast to <see cref="Window"/>.</description></item>
        /// <item><description>For <see cref="ISingleViewApplicationLifetime"/>, returns <see cref="ISingleViewApplicationLifetime.MainView"/> cast to <see cref="Window"/>.</description></item>
        /// <item><description>Otherwise, returns <c>null</c>.</description></item>
        /// </list>
        /// </remarks>
        public static Window? GetWindow(this global::Avalonia.Application app)
        {
            IApplicationLifetime? lifetime = app.ApplicationLifetime;

            if (lifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                return desktop.MainWindow as Window;
            }
            else if (lifetime is ISingleViewApplicationLifetime singleView)
            {
                return singleView.MainView as Window;
            }

            return null;
        }

        /// <summary>
        /// Attempts to find a style resource with the specified key in the current application's styles.
        /// </summary>
        /// <param name="key">The resource key to search for.</param>
        /// <param name="resource">The found resource, if any; otherwise, <c>null</c>.</param>
        /// <returns>
        /// <c>true</c> if the resource was found; otherwise, <c>false</c>.
        /// </returns>
        public static bool TryFindStyleResource(this global::Avalonia.Application app, object key, out object? resource)
        {
            resource = null;
            if (app?.Styles == null)
                return false;

            foreach (IStyle style in app.Styles)
            {
                if (style is IResourceProvider rp && rp.TryGetResource(key, null, out resource))
                    return true;
            }
            return false;
        }
    }
}
