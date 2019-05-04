using ComicbookArchiveToolbox.CommonTools;
using ComicbookArchiveToolbox.CommonTools.Interfaces;
using ComicbookArchiveToolbox.Events;
using Prism.Events;
using Prism.Ioc;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace ComicbookArchiveToolbox.ViewModels
{
  public class HostViewModel : BindableBase
  {

	  #region Attributes
	  private List<ICatPlugin> _plugins;
	  private IContainerExtension _container;
    private IEventAggregator _eventAggregator;

    #endregion Attributes

    public CatViewModel DisplayedView { get; set; }

    #region Constructors
    public HostViewModel(IContainerExtension container, IEventAggregator eventAggregator)
    {
		  _container = container;
      _eventAggregator = eventAggregator;
      _eventAggregator.GetEvent<InterfaceLoadedEvent>().Subscribe(InitPluginsList);
    }
	  #endregion Constructors

	  public string HostTextContent => "This is the host from vm";

	  private void InitPluginsList()
	  {
		  try
		  {
			  _plugins = new List<ICatPlugin>();
			  var type = typeof(ICatPlugin);
			  string codeBase = Assembly.GetExecutingAssembly().CodeBase;
			  UriBuilder uri = new UriBuilder(codeBase);
			  string assemblyPath = Uri.UnescapeDataString(uri.Path);
			  DirectoryInfo installDir = new DirectoryInfo(System.IO.Path.GetDirectoryName(assemblyPath));
			  var listing = installDir.GetFiles("*CatPlugin*.dll").ToList();
			  List<string> pluginsPath = new List<string>();
			  foreach (FileInfo fi in listing)
			  {
				  pluginsPath.Add(fi.Name.Split('.')[1]);
			  }
        List<ICatPlugin> plugins = new List<ICatPlugin>();
        foreach (string s in pluginsPath)
        {
          plugins.Add(_container.Resolve<ICatPlugin>(s));
        }
		  }
		  catch (Exception e)
		  {
			  Debug.WriteLine(e.Message);
		  }
	  }
  }
}
