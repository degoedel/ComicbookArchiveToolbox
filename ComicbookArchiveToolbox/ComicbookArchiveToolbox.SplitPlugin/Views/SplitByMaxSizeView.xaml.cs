using System.Windows.Controls;
using System.Windows.Input;

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
