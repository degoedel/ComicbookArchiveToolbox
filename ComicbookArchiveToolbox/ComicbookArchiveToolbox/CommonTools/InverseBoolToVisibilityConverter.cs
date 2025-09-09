using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ComicbookArchiveToolbox.CommonTools
{
	[ValueConversion(typeof(bool?), typeof(Visibility))]
	public class InverseBoolToVisibilityConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			bool flag = false;
			if (value is bool b)
				flag = b;
			else if (value is bool?)
				flag = ((bool?)value).GetValueOrDefault();
			return flag ? Visibility.Collapsed : Visibility.Visible;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value is Visibility v)
				return v != Visibility.Visible;
			return true;
		}
	}
}
