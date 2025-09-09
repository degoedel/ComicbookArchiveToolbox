using ComicbookArchiveToolbox.CommonTools.Interfaces;
using Prism.Ioc;
using Prism.Mvvm;
using System.Collections.ObjectModel;

namespace ComicbookArchiveToolbox.ViewModels
{
  public class ToolsViewModel : BindableBase
  {
    private IContainerExtension _container;


    public ObservableCollection<ICatPlugin> Plugins { get; set; }


    public ToolsViewModel(IContainerExtension container, ICatPlugin[] catPlugins)
    {
      _container = container;
      Plugins = new ObservableCollection<ICatPlugin>();
      foreach (ICatPlugin plugin in catPlugins)
      {
        Plugins.Add(plugin);
      }
    }

  }
}
