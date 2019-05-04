using ComicbookArchiveToolbox.CommonTools.Interfaces;
using ComicbookArchiveToolbox.Events;
using Prism.Events;
using Prism.Ioc;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

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
