using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace CreativeWrites.Converters
{
    /// <summary>
    /// Returns Visible when two bound values are equal (string-compared), Collapsed otherwise.
    /// Used to gate UI on ownership: e.g. compare Comment.AuthorId to ActiveUser.UserId.
    /// </summary>
    public class EqualityToVisibilityMultiConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values.Length < 2) return Visibility.Collapsed;
            var a = values[0]?.ToString();
            var b = values[1]?.ToString();
            if (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b)) return Visibility.Collapsed;
            return string.Equals(a, b, StringComparison.Ordinal)
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
