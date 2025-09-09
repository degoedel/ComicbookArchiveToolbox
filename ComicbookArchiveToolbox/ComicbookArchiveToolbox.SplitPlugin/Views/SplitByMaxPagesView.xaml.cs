using System.Windows.Controls;
using System.Windows.Input;

namespace CatPlugin.Split.Views
{
	/// <summary>
	/// Interaction logic for SplitByMaxPagesView.xaml
	/// </summary>
	public partial class SplitByMaxPagesView : UserControl
	{
		public SplitByMaxPagesView()
		{
			InitializeComponent();
		}

		private void NumericOnly(object sender, TextCompositionEventArgs e)
		{
			e.Handled = (!uint.TryParse(e.Text, out uint result));
		}
	}
}
