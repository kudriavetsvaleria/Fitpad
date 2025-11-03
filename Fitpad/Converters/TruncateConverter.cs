using System;
using System.Globalization;
using System.Net;            // WebUtility.HtmlDecode
using System.Windows;        // FormattedText
using System.Windows.Data;
using System.Windows.Media;  // Typeface, Brushes

namespace Fitpad.Converters
{
    public class TruncateConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var full = (value ?? string.Empty).ToString();
            full = WebUtility.HtmlDecode(full);

            // Параметр: "lines x width x fontSize x lineHeight [x Bold|Normal]"
            // пример: "3x147x14x18xBold"
            int lines = 3, width = 147;
            double fontSize = 14, lineHeight = 18;
            bool isBold = true;

            var p = (parameter ?? string.Empty).ToString();
            if (!string.IsNullOrWhiteSpace(p))
            {
                var parts = p.Split('x', 'X', '*', '|');
                if (parts.Length >= 1 && int.TryParse(parts[0], out var l) && l > 0) lines = l;
                if (parts.Length >= 2 && int.TryParse(parts[1], out var w) && w > 0) width = w;
                if (parts.Length >= 3 && double.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out var fs) && fs > 0) fontSize = fs;
                if (parts.Length >= 4 && double.TryParse(parts[3], NumberStyles.Float, CultureInfo.InvariantCulture, out var lh) && lh > 0) lineHeight = lh;
                if (parts.Length >= 5) isBold = parts[4].Equals("bold", StringComparison.OrdinalIgnoreCase);
            }

            if (string.IsNullOrEmpty(full)) return full;

            // если помещается — отдаем как есть
            if (FitsByHeight(full, lines, width, fontSize, lineHeight, isBold))
                return full;

            // иначе подрезаем бинарным поиском и добавляем «…»
            const string suffix = "…";
            int lo = 0, hi = full.Length, best = 0;
            while (lo <= hi)
            {
                int mid = (lo + hi) / 2;
                var candidate = full.Substring(0, Math.Max(0, mid)).TrimEnd() + suffix;
                if (FitsByHeight(candidate, lines, width, fontSize, lineHeight, isBold))
                {
                    best = mid;
                    lo = mid + 1;
                }
                else hi = mid - 1;
            }
            return full.Substring(0, Math.Max(0, best)).TrimEnd() + suffix;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();

        private static bool FitsByHeight(string text, int maxLines, double maxWidth, double fontSize, double lineHeight, bool isBold)
        {
            var ft = new FormattedText(
                text,
                CultureInfo.CurrentUICulture,
                FlowDirection.LeftToRight,
                new Typeface(new FontFamily("Segoe UI"), FontStyles.Normal, isBold ? FontWeights.Bold : FontWeights.Normal, FontStretches.Normal),
                fontSize,
                Brushes.Black,
                1.0);

            ft.MaxTextWidth = Math.Max(0, maxWidth);
            // сравниваем реальную высоту с лимитом высоты строк
            double maxHeight = lineHeight * maxLines;
            return ft.Height <= maxHeight + 0.5; // небольшой допуск
        }
    }
}
