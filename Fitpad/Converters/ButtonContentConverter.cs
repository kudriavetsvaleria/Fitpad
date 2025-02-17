using System;
using System.Globalization;
using System.Windows.Data;

namespace Fitpad.Converters
{
    public class ButtonContentConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isExpanded && parameter is string fullName)
            {
                return isExpanded ? fullName : fullName.Substring(0, 1).ToUpper(); // Первая буква, если свернуто
            }
            return parameter;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
