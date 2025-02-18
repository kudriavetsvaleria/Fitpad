using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace Fitpad.Converters
{
    public class FavoriteIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isFavorite)
            {
                return new BitmapImage(new Uri(isFavorite
                    ? "pack://application:,,,/Images/star_yellow.png"
                    : "pack://application:,,,/Images/star_grey.png"));
            }
            return new BitmapImage(new Uri("pack://application:,,,/Images/star_grey.png"));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
