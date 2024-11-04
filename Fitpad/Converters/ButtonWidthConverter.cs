using System;
using System.Globalization;
using System.Windows.Data;

namespace Fitpad.Converters
{
    public class ButtonWidthConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (bool)value ? double.NaN : 50; // double.NaN для автоширины
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
