using System;
using System.Globalization;
using System.Windows.Data;

namespace Fitpad.Converters
{
    public class WidthConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isExpanded = (bool)value;
            return isExpanded ? 220 : 72; // 80 вместо 50 для расширенной узкой панели
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
