using Avalonia.Data.Converters;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;

namespace NucleusAF.Avalonia.Converters
{
    public class UriToBitmapConverter : IValueConverter
    {
        public static readonly UriToBitmapConverter Instance = new();

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is string uriString && !string.IsNullOrWhiteSpace(uriString))
            {
                try
                {
                    if (uriString.StartsWith("avares://", StringComparison.OrdinalIgnoreCase))
                    {
                        Uri uri = new(uriString);
                        Stream stream = AssetLoader.Open(uri);
                        return new Bitmap(stream);
                    }

                    if (uriString.StartsWith("resm://", StringComparison.OrdinalIgnoreCase))
                    {
                        string withoutScheme = uriString.Substring(7);

                        int firstSlash = withoutScheme.IndexOf('/');

                        if (firstSlash > 0)
                        {
                            string assemblyName = withoutScheme[..firstSlash];
                            string resourcePath = withoutScheme[(firstSlash + 1)..].Replace('/', '.');

                            Assembly? assembly = AppDomain.CurrentDomain
                                .GetAssemblies()
                                .FirstOrDefault(a => string.Equals(a.GetName().Name, assemblyName, StringComparison.Ordinal));

                            if (assembly != null)
                            {
                                Stream? stream =
                                    assembly.GetManifestResourceStream(resourcePath) ??
                                    assembly.GetManifestResourceStream($"{assembly.GetName().Name}.{resourcePath}");

                                if (stream != null)
                                {
                                    MemoryStream memoryStream = new();
                                    stream.CopyTo(memoryStream);
                                    memoryStream.Position = 0;

                                    return new Bitmap(memoryStream);
                                }
                            }
                        }
                    }
                }
                catch { }
            }

            return null;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}