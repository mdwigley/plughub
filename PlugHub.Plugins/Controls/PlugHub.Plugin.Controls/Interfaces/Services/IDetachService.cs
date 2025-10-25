using Avalonia.Controls;

namespace PlugHub.Plugin.Controls.Interfaces.Services
{
    /// <summary>
    /// Provides methods to forcibly detach a control from its parent container.
    /// Intended for scenarios where normal removal is blocked or unreliable.
    /// </summary>
    public interface IDetachService
    {
        /// <summary>
        /// Synchronously detaches the specified control from its parent container.
        /// </summary>
        /// <param name="control">The control to detach.</param>
        void HardDetach(Control control);

        /// <summary>
        /// Asynchronously detaches the specified control from its parent container,
        /// ensuring the operation is executed on the UI thread.
        /// </summary>
        /// <param name="control">The control to detach.</param>
        /// <param name="cancellationToken">Optional token to cancel the operation.</param>
        Task HardDetachAsync(Control control, CancellationToken cancellationToken = default);
    }
}