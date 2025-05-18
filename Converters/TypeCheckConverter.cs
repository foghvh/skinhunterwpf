using System;
using System.Globalization;
using System.Windows.Data;

namespace SkinHunterWPF.Converters
{
    public class TypeCheckConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || !(parameter is Type typeToCheck))
            {
                return false;
            }
            return typeToCheck.IsInstanceOfType(value);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}