using CatPlugin.Merge.ViewModels;
using ComicbookArchiveToolbox.CommonTools;
using ComicbookArchiveToolbox.CommonTools.Interfaces;
using Prism.Ioc;
using Prism.Regions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity;

namespace CatPlugin.Merge
{
  public class MergePlugin : ICatPlugin
  {
    #region Attributes
    private MergePluginViewModel _viewModel;
    private readonly IUnityContainer _container;
    private readonly IRegionManager _manager;
    #endregion Attributes


    #region Properties
    public string Name => "Merge";

    public string Category => "Mergers";

    public CatViewModel ViewModel => _viewModel;

    #endregion Properties

    #region Constructors
    public MergePlugin(IUnityContainer container)
    {
      _container = container;
      _container.RegisterType<ICatPlugin, MergePlugin>("Merge");
      _viewModel = new MergePluginViewModel();
    }

    #endregion Constructors

    #region IModule
    public void OnInitialized(IContainerProvider containerProvider)
    {
      var regionManager = containerProvider.Resolve<IRegionManager>();
      regionManager.RegisterViewWithRegion("PluginRegion", typeof(Views.MergePluginView));
    }

    public void RegisterTypes(IContainerRegistry containerRegistry)
    {

    }
    #endregion IModule
  }
}
