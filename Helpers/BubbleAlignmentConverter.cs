using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LoginWithFirebase.Helpers
{
    public class BubbleAlignmentConverter : IValueConverter
    {
       
        public static string CurrentUserId { get; set; } = string.Empty;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var fromUserId = value as string;
          
            if (fromUserId == CurrentUserId)
            {
                return LayoutOptions.EndAndExpand;  
            }
            else
            {
                return LayoutOptions.StartAndExpand; 
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
