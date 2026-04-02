using System;

namespace NucleusAF.Avalonia.Compatibility
{
    /// <summary>
    /// Provides a compatibility placeholder for WPF's <c>DefaultStyleKeyProperty.OverrideMetadata</c> in Avalonia.
    /// </summary>
    /// <remarks>
    /// In Avalonia, the default style key is automatically set to the derived control's type,
    /// so overriding metadata is typically unnecessary unless you want to use a different style key.
    /// This class exists to minimize code conflicts when porting or sharing code with WPF projects.
    /// </remarks>
    internal class DefaultStyleKeyProperty
    {
        /// <summary>
        /// Placeholder method to match WPF's API for overriding the default style key metadata.
        /// No action is performed in Avalonia by default.
        /// </summary>
        /// <param name="control">The control type for which to override the style key.</param>
        /// <param name="metadata">The style metadata (unused in Avalonia).</param>
        public static void OverrideMetadata(Type control, FrameworkPropertyMetadata metadata)
        {
            // just a placeholder so that the line doesn't have to be removed to minimize conflicts
            // note: in Avalonia by default StyleOverrideKey is set to the type of the derived control (unlike WPF)
            // so you need to add StyleOverrideKey only if you want to set the type to a different class
            _ = control;
            _ = metadata;
        }
    }

    /// <summary>
    /// Provides a compatibility placeholder for WPF's <c>FrameworkPropertyMetadata</c> in Avalonia.
    /// </summary>
    /// <remarks>
    /// Exists only to reduce code conflicts when sharing or porting code from WPF.
    /// </remarks>
    /// <remarks>
    /// Initializes a new instance of the <see cref="FrameworkPropertyMetadata"/> class.
    /// </remarks>
    internal class FrameworkPropertyMetadata
    {
        /// <param name="type">The type associated with the property metadata.</param>
        public FrameworkPropertyMetadata(Type type)
        {
            _ = type;
        }
    }
}