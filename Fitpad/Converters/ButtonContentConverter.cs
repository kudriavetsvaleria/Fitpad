using System;
using System.Globalization;
using System.Windows.Data;

namespace Fitpad.Converters
{
    public class ButtonContentConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Если это кнопка для сворачивания/разворачивания панели
            if (parameter?.ToString() == "Toggle")
            {
                return (bool)value ? ">" : "<";
            }

            // Для остальных кнопок навигации
            string content = parameter as string;
            return (bool)value ? content : content?.Substring(0, 1);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
