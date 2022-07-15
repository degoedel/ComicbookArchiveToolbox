using CatPlugin.Merge.ViewModels;
using CatPlugin.Merge.Views;
using ComicbookArchiveToolbox.CommonTools;
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
using System.Windows.Media;
using Unity;

namespace CatPlugin.Merge
{
  public class MergePlugin : ICatPlugin
  {
    #region Attributes
    private readonly IUnityContainer _container;
		public Canvas _icon;
	#endregion Attributes


		#region Properties
		public string Name => "Merge";
		public string ToolDescription => "Merge the selected archives in one file. Allow image recompression.";


		public DelegateCommand LoadViewCommand { get; private set; }

		public Canvas Icon => _icon;

		#endregion Properties

		#region Constructors
		public MergePlugin(IUnityContainer container)
    {
      _container = container;
      _container.RegisterType<ICatPlugin, MergePlugin>("Merge");
			var myResourceDictionary = new ResourceDictionary();
			myResourceDictionary.Source = new Uri("/CatPlugin.Merge;component/Resources/Icons.xaml", UriKind.RelativeOrAbsolute);
			_icon = myResourceDictionary["appbar_paperclip"] as Canvas;

			LoadViewCommand = new DelegateCommand(LoadView, CanExecute);
    }

		#endregion Constructors

		#region Command
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Interoperability", "CA1416:Validate platform compatibility", Justification = "<Pending>")]
		private void LoadView()
    {
      var regionManager = _container.Resolve<IRegionManager>();
      IRegion region = regionManager.Regions["PluginRegion"];
      var view = region.GetView("MergeView");
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
      region.Add(_container.Resolve<MergePluginView>(), "MergeView");
      //regionManager.RegisterViewWithRegion("PluginRegion", typeof(MergePluginView));
    }

    public void RegisterTypes(IContainerRegistry containerRegistry)
    {

    }
    #endregion IModule
  }
}
