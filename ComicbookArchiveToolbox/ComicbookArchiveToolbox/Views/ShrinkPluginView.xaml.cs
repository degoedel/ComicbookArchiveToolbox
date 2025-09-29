using System.Windows.Controls;
using System.Windows.Input;

namespace ComicbookArchiveToolbox.Views
{
	/// <summary>
	/// Interaction logic for ShrinkPluginView.xaml
	/// </summary>
	public partial class ShrinkPluginView : UserControl
	{
		public ShrinkPluginView()
		{
			InitializeComponent();
		}

		private void NumericOnly(object sender, TextCompositionEventArgs e)
		{
			e.Handled = (!uint.TryParse(e.Text, out uint result));
		}
	}
}
