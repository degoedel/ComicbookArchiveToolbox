using MahApps.Metro.Controls;
using Prism.Navigation.Regions;

namespace ComicbookArchiveToolbox.Views
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : MetroWindow
	{
		public MainWindow(IRegionManager regionManager)
		{
			this.InitializeComponent(); // Fully qualify to resolve ambiguity
			regionManager.RegisterViewWithRegion("HostRegion", typeof(HostView));
		}
	}
}
