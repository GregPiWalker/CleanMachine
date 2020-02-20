using System;
using System.Windows;
using System.Windows.Data;
using System.Globalization;

namespace CleanMachineDemo
{
    public class InvertibleBooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type typeTarget, object parameter, CultureInfo culture)
        {
            Visibility visibility = Visibility.Visible;

            if (value is bool)
            {
                bool boolean = (bool)value;

                if (boolean)
                {
                    visibility = Visibility.Visible;
                }
                else
                {
                    visibility = Visibility.Collapsed;
                }
            }
            else
            {
                if (value == null)
                {
                    visibility = Visibility.Collapsed;
                }
                else
                {
                    visibility = Visibility.Visible;
                }
            }

            // If "True" is passed in, then switch the visibility.
            if ("true".Equals((string)parameter, StringComparison.OrdinalIgnoreCase)
                || "invert".Equals((string)parameter, StringComparison.OrdinalIgnoreCase))
            {
                if (visibility == Visibility.Collapsed)
                {
                    visibility = Visibility.Visible;
                }
                else
                {
                    visibility = Visibility.Collapsed;
                }
            }

            return visibility;
        }

        public object ConvertBack(object value, Type typeTarget, object param, CultureInfo culture)
        {
            return null;
        }
    }
}

