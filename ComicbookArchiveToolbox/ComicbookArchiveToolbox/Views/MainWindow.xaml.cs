using ComicbookArchiveToolbox.CommonTools.Interfaces;
using MahApps.Metro.Controls;
using Prism.Navigation.Regions;
using System.Collections.Generic;

namespace ComicbookArchiveToolbox.Views
{
  /// <summary>
  /// Interaction logic for MainWindow.xaml
  /// </summary>
  public partial class MainWindow : MetroWindow
  {
    public IEnumerable<ICatPlugin> Plugins { get; set; }

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Interoperability", "CA1416:Validate platform compatibility", Justification = "<Pending>")]
		public MainWindow(IRegionManager regionManager)
    {
      InitializeComponent();
	    regionManager.RegisterViewWithRegion("HostRegion", typeof(HostView));
	  }
  }
}
