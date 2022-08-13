using System;
using System.Globalization;
using System.Windows.Data;

namespace EbayListings
{
    public class AddConverter : IValueConverter
    {
        public object Convert(object value, Type targetType,
              object parameter, CultureInfo culture)
        {
            return ((double)value - 150).ToString();
        }

        public object ConvertBack(object value, Type targetType,
            object parameter, CultureInfo culture)
        {
            return (double)value + 150;
        }
    }
}
