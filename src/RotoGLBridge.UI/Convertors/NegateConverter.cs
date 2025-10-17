using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace RotoGLBridge.UI.Convertors
{
    [ValueConversion(typeof(double), typeof(double))]
    public class NegateConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is null)
            {
                return null;
            }

            if (double.TryParse(value.ToString(), out double number))
            {
                return number == 0 ? 0 : -number;
            }

            return DependencyProperty.UnsetValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is null)
            {
                return null;
            }

            if (double.TryParse(value.ToString(), out double number))
            {
                return number == 0 ? 0 : -number;
            }

            return DependencyProperty.UnsetValue;
        }
    }
}
