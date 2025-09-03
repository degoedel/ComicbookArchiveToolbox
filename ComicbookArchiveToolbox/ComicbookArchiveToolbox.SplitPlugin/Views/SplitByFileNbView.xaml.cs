using System.Windows.Controls;
using System.Windows.Input;

namespace CatPlugin.Split.Views
{
	/// <summary>
	/// Interaction logic for SplitByFileNbView.xaml
	/// </summary>
	public partial class SplitByFileNbView : UserControl
	{
		public SplitByFileNbView()
		{
			InitializeComponent();
		}

		private void NumericOnly(object sender, TextCompositionEventArgs e)
		{
			e.Handled = (!uint.TryParse(e.Text, out uint result));
		}
	}
}
