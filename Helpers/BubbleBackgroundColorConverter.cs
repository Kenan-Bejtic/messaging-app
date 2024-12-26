using System;
using System.Globalization;
using Microsoft.Maui.Controls;

namespace LoginWithFirebase.Helpers
{
    public class BubbleBackgroundColorConverter : IValueConverter
    {
        public static string CurrentUserId { get; set; } = string.Empty;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var fromUserId = value as string;

            if (fromUserId == CurrentUserId)
            {
                return (Color)Application.Current.Resources["SentMessageColor"];
            }
            else
            {
                return (Color)Application.Current.Resources["ReceivedMessageColor"]; 
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
