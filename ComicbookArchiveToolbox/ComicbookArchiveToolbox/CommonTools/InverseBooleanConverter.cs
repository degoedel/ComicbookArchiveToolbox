using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ComicbookArchiveToolbox.CommonTools
{
	public class InverseBoolToVisibilityConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value is bool boolValue)
				return boolValue ? Visibility.Collapsed : Visibility.Visible;
			return Visibility.Visible;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value is Visibility visibility)
				return visibility != Visibility.Visible;
			return true;
		}
	}
	public class InverseBooleanConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value is bool b)
				return !b;
			return value;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value is bool b)
				return !b;
			return value;
		}
	}
}
