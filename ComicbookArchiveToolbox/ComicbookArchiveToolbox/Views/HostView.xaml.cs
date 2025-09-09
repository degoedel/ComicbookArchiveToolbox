using Prism.Ioc;
using Prism.Navigation.Regions;
using System.Windows;
using System.Windows.Controls;

namespace ComicbookArchiveToolbox.Views
{
	/// <summary>
	/// Interaction logic for HostView.xaml
	/// </summary>
	public partial class HostView : UserControl
	{
		IContainerExtension _container;
		IRegionManager _regionManager;


    public HostView(IContainerExtension container, IRegionManager regionManager)
	{
	  InitializeComponent();
	  _container = container;
	  _regionManager = regionManager;
      this.Loaded += HostControl_Loaded;
	}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Interoperability", "CA1416:Validate platform compatibility", Justification = "<Pending>")]
		private void HostControl_Loaded(object sender, RoutedEventArgs e)
    {
      IRegion region = _regionManager.Regions["PluginRegion"];
	  var tools = _container.Resolve<ToolsView>();
	  region.Add(tools, "ToolsView");
	  region.Add(_container.Resolve<SettingsView>(), "SettingsView");
	  region.Add(_container.Resolve<AboutView>(), "AboutView");
	  region.Activate(tools);
	}

    private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
      var tb = sender as TextBox;
      tb.ScrollToEnd();
    }
  }
}
