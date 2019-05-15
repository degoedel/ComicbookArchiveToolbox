using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace CatPlugin.Split.Views
{
	/// <summary>
	/// Interaction logic for SplitByMaxSizeView.xaml
	/// </summary>
	public partial class SplitByMaxSizeView : UserControl
	{
		public SplitByMaxSizeView()
		{
			InitializeComponent();
		}

		private void NumericOnly(object sender, TextCompositionEventArgs e)
		{
			e.Handled = (!long.TryParse(e.Text, out long result));
		}
	}
}
