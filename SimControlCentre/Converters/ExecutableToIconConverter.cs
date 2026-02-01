using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using SimControlCentre.Helpers;

namespace SimControlCentre.Converters
{
    /// <summary>
    /// Converts an executable path to its icon
    /// </summary>
    public class ExecutableToIconConverter : IValueConverter
    {
        public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string executablePath)
            {
                return IconExtractor.GetIconFromExecutable(executablePath);
            }
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
