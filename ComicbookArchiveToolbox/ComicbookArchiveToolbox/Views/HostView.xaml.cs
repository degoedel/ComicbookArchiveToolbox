using ComicbookArchiveToolbox.Events;
using Prism.Events;
using Prism.Ioc;
using Prism.Regions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

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
