using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LoginWithFirebase.Helpers
{
    public class MessageTypeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return string.Empty;

            var message = value.ToString();
            if (IsImageMessage(message))
            {
                return "Slika";
            }
            return message;
        }

        private bool IsImageMessage(string message)
        {
          
            if (Uri.TryCreate(message, UriKind.Absolute, out var uri))
            {
                var extensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp" };
                return extensions.Any(ext => uri.AbsolutePath.EndsWith(ext, StringComparison.OrdinalIgnoreCase));
            }
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
