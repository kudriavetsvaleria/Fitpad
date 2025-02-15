using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace Fitpad.Converters
{
    public class ButtonColorConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            foreach (var value in values)
            {
                if (value == null || string.IsNullOrWhiteSpace(value.ToString()))
                {
                    return new SolidColorBrush(Color.FromRgb(189, 189, 189)); // Серый (неактивное состояние)
                }
            }

            return new SolidColorBrush(Color.FromRgb(76, 175, 80)); // Светлый зеленый (активное состояние)
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
