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
		IRegion _region;
    IEventAggregator _eventAggregator;


    public HostView(IContainerExtension container, IRegionManager regionManager, IEventAggregator eventAggregator)
		{
			InitializeComponent();
			_container = container;
			_regionManager = regionManager;
      _eventAggregator = eventAggregator;
			this.Loaded += HostControl_Loaded;
		}

		private void HostControl_Loaded(object sender, RoutedEventArgs e)
		{
			//_viewA = _container.Resolve<ViewA>();
			//_viewB = _container.Resolve<ViewB>();

			_region = _regionManager.Regions["PluginRegion"];

      _eventAggregator.GetEvent<InterfaceLoadedEvent>().Publish();
			//_region.Add(_viewA);
			//_region.Add(_viewB);
		}

		//private void Button_Click(object sender, RoutedEventArgs e)
		//{
		//	var view = _container.Resolve<ViewA>();
		//	IRegion region = _regionManager.Regions["ContentRegion"];
		//	region.Add(view);
		//}

		//private void Button_Click(object sender, RoutedEventArgs e)
		//{
		//	//activate view a
		//	_region.Activate(_viewA);
		//}

		//private void Button_Click_1(object sender, RoutedEventArgs e)
		//{
		//	//deactivate view a
		//	_region.Deactivate(_viewA);
		//}
	}
}
