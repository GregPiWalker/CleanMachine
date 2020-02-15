using System;
using System.Globalization;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace CleanMachineDemo
{
    public class SelectionToColorConverter : IValueConverter
    {
        public SolidColorBrush Selected { get; set; }
        public SolidColorBrush Deselected { get; set; }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool)
            {
                var val = (bool)value ? Selected : Deselected;
                return val;
            }
            else
            {
                return new ValidationResult(false, "Selection conversion error");
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();

            //int result;
            //if (int.TryParse(value as string, out result))
            //{
            //    return result;
            //}
            //else
            //{
            //    return new ValidationResult(false, "Int32 parse error");
            //}
        }
    }
}
