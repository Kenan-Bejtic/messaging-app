using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using LoginWithFirebase.Model;

namespace LoginWithFirebase.Helpers
{
    public class MergeUsernameContentConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            
            if (value is MessageModel message)
            {
                if (string.IsNullOrWhiteSpace(message.Content))
                    return string.Empty;

                return $"{message.SenderUsername}: {message.Content}";
            }
            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
