using MahApps.Metro.Controls;
using Prism.Navigation.Regions;

namespace ComicbookArchiveToolbox.Views
{
  /// <summary>
  /// Interaction logic for MainWindow.xaml
  /// </summary>
  public partial class MainWindow : MetroWindow
  {
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Interoperability", "CA1416:Validate platform compatibility", Justification = "<Pending>")]
		public MainWindow(IRegionManager regionManager)
    {
      InitializeComponent();
	    regionManager.RegisterViewWithRegion("HostRegion", typeof(HostView));
	  }
  }
}
