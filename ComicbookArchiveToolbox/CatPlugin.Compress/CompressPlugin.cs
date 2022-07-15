using CatPlugin.Compress.Views;
using ComicbookArchiveToolbox.CommonTools.Interfaces;
using Prism.Commands;
using Prism.Ioc;
using Prism.Regions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Unity;

namespace CatPlugin.Compress
{
    public class CompressPlugin : ICatPlugin
	{
		#region Attributes
		private readonly IUnityContainer _container;
		public Canvas _icon;
		#endregion Attributes

		public Canvas Icon => _icon;
		public string Name => "Compress";

		public string ToolDescription => "Create a new archive with recompressed (degraded) pictures to allow faster page loading in reader.";

		public DelegateCommand LoadViewCommand { get; private set; }

		public CompressPlugin(IUnityContainer container)
		{
			_container = container;
			_container.RegisterType<ICatPlugin, CompressPlugin>("Compress");
			var myResourceDictionary = new ResourceDictionary();
			myResourceDictionary.Source = new Uri("/CatPlugin.Compress;component/Resources/Icons.xaml", UriKind.RelativeOrAbsolute);
			_icon = myResourceDictionary["appbar_archive"] as Canvas;

			LoadViewCommand = new DelegateCommand(LoadView, CanExecute);
		}

		#region Command
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Interoperability", "CA1416:Validate platform compatibility", Justification = "<Pending>")]
		private void LoadView()
		{
			var regionManager = _container.Resolve<IRegionManager>();
			IRegion region = regionManager.Regions["PluginRegion"];
			var view = region.GetView("CompressView");
			region.Activate(view);
		}

		private bool CanExecute()
		{
			return true;
		}
		#endregion Command


		[System.Diagnostics.CodeAnalysis.SuppressMessage("Interoperability", "CA1416:Validate platform compatibility", Justification = "<Pending>")]
		public void OnInitialized(IContainerProvider containerProvider)
		{
			var regionManager = containerProvider.Resolve<IRegionManager>();
			IRegion region = regionManager.Regions["PluginRegion"];
			region.Add(_container.Resolve<CompressPluginView>(), "CompressView");
		}

		public void RegisterTypes(IContainerRegistry containerRegistry)
		{
		}
	}
}
