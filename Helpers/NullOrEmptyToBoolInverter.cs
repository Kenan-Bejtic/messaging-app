using System;
using System.Globalization;
using Microsoft.Maui.Controls;

namespace LoginWithFirebase.Helpers
{
    public class NullOrEmptyToBoolInverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var str = value as string;
            return string.IsNullOrEmpty(str);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
