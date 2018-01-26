using System;
using System.Globalization;
using System.Windows.Data;

namespace LogCleaner
{
  internal class CursorConverter : IValueConverter
  {
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
      return (bool)value ? "Wait" : "Default";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
      return (bool)value ? "Default" : "Wait";
    }
  }
}
