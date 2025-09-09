using System.Windows.Controls;
using System.Windows.Input;

namespace ComicbookArchiveToolbox.Views
{
	/// <summary>
	/// Interaction logic for CompressPluginView.xaml
	/// </summary>
	public partial class CompressPluginView : UserControl
	{
		public CompressPluginView()
		{
			InitializeComponent();
		}

		private void NumericOnly(object sender, TextCompositionEventArgs e)
		{
			e.Handled = (!uint.TryParse(e.Text, out uint result));
		}
	}
}
