using System;
using System.Drawing;
using System.IO;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace SimControlCentre.Helpers
{
    /// <summary>
    /// Helper class to extract and convert icons from executables
    /// </summary>
    public static class IconExtractor
    {
        /// <summary>
        /// Extract icon from executable and convert to WPF ImageSource
        /// </summary>
        public static ImageSource? GetIconFromExecutable(string executablePath)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(executablePath) || !File.Exists(executablePath))
                {
                    return null;
                }

                // Extract the icon
                using var icon = Icon.ExtractAssociatedIcon(executablePath);
                if (icon == null)
                {
                    return null;
                }

                // Convert to WPF ImageSource
                return Imaging.CreateBitmapSourceFromHIcon(
                    icon.Handle,
                    Int32Rect.Empty,
                    BitmapSizeOptions.FromEmptyOptions());
            }
            catch
            {
                return null;
            }
        }
    }
}
