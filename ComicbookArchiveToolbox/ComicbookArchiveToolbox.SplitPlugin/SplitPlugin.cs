using CatPlugin.Split.ViewModels;
using CatPlugin.Split.Views;
using ComicbookArchiveToolbox.CommonTools;
using ComicbookArchiveToolbox.CommonTools.Interfaces;
using Prism.Commands;
using Prism.Ioc;
using Prism.Regions;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Unity;

namespace CatPlugin.Split
{
  public class SplitPlugin : ICatPlugin
  {
    #region Attributes
	  private readonly IUnityContainer _container;
    #endregion Attributes


    #region Properties
    public string Name => "Split";

    public DelegateCommand LoadViewCommand { get; private set; }
    #endregion Properties

    #region Constructors
    public SplitPlugin(IUnityContainer container)
    {
      _container = container;
      _container.RegisterType<ICatPlugin, SplitPlugin>("Split");
      LoadViewCommand = new DelegateCommand(LoadView, CanExecute);
    }

    #endregion Constructors

    #region Command
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
    public void OnInitialized(IContainerProvider containerProvider)
		{
			var regionManager = containerProvider.Resolve<IRegionManager>();
      IRegion region = regionManager.Regions["PluginRegion"];
      region.Add(_container.Resolve<SplitPluginView>(), "SplitView");
      //regionManager.RegisterViewWithRegion("PluginRegion", typeof(SplitPluginView));
    }

    public void RegisterTypes(IContainerRegistry containerRegistry)
		{
      
    }
		#endregion IModule

	}
}
