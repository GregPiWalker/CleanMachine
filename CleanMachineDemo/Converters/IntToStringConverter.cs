using System;
using System.Globalization;
using System.Windows.Controls;
using System.Windows.Data;

namespace CleanMachineDemo.Converters
{
    public class IntToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int)
            {
                return value;
            }
            else
            {
                return new ValidationResult(false, "Int32 cast error");
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            int result;
            if (int.TryParse(value as string, out result))
            {
                return result;
            }
            else
            {
                return new ValidationResult(false, "Int32 parse error");
            }
        }
    }
}
