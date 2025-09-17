using ComicbookArchiveToolbox.Module.Edit.Views;
using ComicbookArchiveToolbox.Module.Merge.Views;
using ComicbookArchiveToolbox.Module.Split.Views;
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
		readonly IContainerExtension _container;
		readonly IRegionManager _regionManager;


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
			region.Add(_container.Resolve<SettingsView>(), "SettingsView");
			var about = _container.Resolve<AboutView>();
			region.Add(about, "AboutView");
			region.Add(_container.Resolve<MergePluginView>(), "MergeView");
			region.Add(_container.Resolve<CompressPluginView>(), "CompressView");
			region.Add(_container.Resolve<SplitPluginView>(), "SplitView");
			region.Add(_container.Resolve<EditPluginView>(), "EditView");
			region.Activate(about);
			region = _regionManager.Regions["SplitArgsRegion"];
			region.Add(_container.Resolve<SplitByFileNbView>(), "SplitByFileNbView");
			region.Add(_container.Resolve<SplitByMaxPagesView>(), "SplitByMaxPagesView");
			region.Add(_container.Resolve<SplitByMaxSizeView>(), "SplitByMaxSizeView");
		}

		private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
		{
			var tb = sender as TextBox;
			tb.ScrollToEnd();
		}
	}
}
