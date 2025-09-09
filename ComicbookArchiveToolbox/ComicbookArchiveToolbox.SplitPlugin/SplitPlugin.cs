using CatPlugin.Split.Services;
using CatPlugin.Split.Views;
using ComicbookArchiveToolbox.CommonTools.Interfaces;
using Prism.Commands;
using Prism.Ioc;
using Prism.Navigation.Regions;
using System;
using System.Windows;
using System.Windows.Controls;
using Unity;

namespace CatPlugin.Split
{
	public class SplitPlugin : ICatPlugin
	{
		#region Attributes
		private readonly IUnityContainer _container;
		private Canvas _icon;
		#endregion Attributes


		#region Properties
		public string Name => "Split";
		public string ToolDescription => "Split the selected archive in several files. Allow images recompression. Available split methods are : by file number, by maximum size per file, or by maximum pages number in files.";
		public DelegateCommand LoadViewCommand { get; private set; }

		public Canvas Icon => _icon;
		#endregion Properties

		#region Constructors
		public SplitPlugin(IUnityContainer container)
		{
			_container = container;
			_container.RegisterType<ICatPlugin, SplitPlugin>("Split");
			LoadViewCommand = new DelegateCommand(LoadView, CanExecute);
			var myResourceDictionary = new ResourceDictionary
			{
				Source = new Uri("/CatPlugin.Split;component/Resources/Icons.xaml", UriKind.RelativeOrAbsolute)
			};
			_icon = myResourceDictionary["appbar_slice"] as Canvas;
			container.RegisterType<ISplitter, ByFileSplitter>("By File Nb");
			container.RegisterType<ISplitter, ByMaxPageSplitter>("By Max Pages Nb");
			container.RegisterType<ISplitter, BySizeSplitter>("By Size (Mb)");

		}

		#endregion Constructors

		#region Command
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Interoperability", "CA1416:Validate platform compatibility", Justification = "<Pending>")]
		private void LoadView()
    {
      var regionManager = _container.Resolve<IRegionManager>();
      IRegion region = regionManager.Regions["PluginRegion"];
      var view = region.GetView("SplitView");
      region.Activate(view);
    }

    private bool CanExecute()
    {
      return true;
    }
		#endregion Command

		#region IModule
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Interoperability", "CA1416:Validate platform compatibility", Justification = "<Pending>")]
		public void OnInitialized(IContainerProvider containerProvider)
		{
			var regionManager = containerProvider.Resolve<IRegionManager>();
      IRegion region = regionManager.Regions["PluginRegion"];
      region.Add(_container.Resolve<SplitPluginView>(), "SplitView");
			region = regionManager.Regions["SplitArgsRegion"];
			region.Add(_container.Resolve<SplitByFileNbView>(), "SplitByFileNbView");
			region.Add(_container.Resolve<SplitByMaxPagesView>(), "SplitByMaxPagesView");
			region.Add(_container.Resolve<SplitByMaxSizeView>(), "SplitByMaxSizeView");
		}

    public void RegisterTypes(IContainerRegistry containerRegistry)
		{
      
    }
		#endregion IModule

	}
}
