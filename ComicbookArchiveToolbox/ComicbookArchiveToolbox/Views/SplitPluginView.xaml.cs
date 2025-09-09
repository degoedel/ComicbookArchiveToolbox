using System.Windows.Controls;
using System.Windows.Input;

namespace ComicbookArchiveToolbox.Module.Split.Views
{
  /// <summary>
  /// Interaction logic for SplitPluginView.xaml
  /// </summary>
  public partial class SplitPluginView : UserControl
  {
    public SplitPluginView()
    {
      InitializeComponent();
    }

		private void NumericOnly(object sender, TextCompositionEventArgs e)
		{
			e.Handled = (!uint.TryParse(e.Text, out uint result));
		}

	}
}
