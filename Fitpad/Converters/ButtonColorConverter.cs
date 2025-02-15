using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace Fitpad.Converters  // Важно, чтобы этот namespace совпадал с XAML
{
    public class ButtonColorConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            bool allFieldsFilled = true;
            foreach (var value in values)
            {
                if (value == null || string.IsNullOrWhiteSpace(value.ToString()))
                {
                    allFieldsFilled = false;
                    break;
                }
            }

            return allFieldsFilled ? new SolidColorBrush(Colors.Green) : new SolidColorBrush(Colors.Gray);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
