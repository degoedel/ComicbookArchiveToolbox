using ComicbookArchiveToolbox.CommonTools;
using ComicbookArchiveToolbox.CommonTools.Interfaces;
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

namespace ComicbookArchiveHost.ViewModels
{
  public class HostViewModel : BindableBase
  {

	#region Attributes
	private List<ICatPlugin> _plugins;
	private IContainerExtension _container;

	#endregion Attributes

	public CatViewModel DisplayedView { get; set; }

    #region Constructors
    public HostViewModel(IContainerExtension container)
    {
		_container = container;
		//InitPluginsList();
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
				pluginsPath.Add(fi.FullName);
			}
			pluginsPath.Add(assemblyPath);

			List<Assembly> assemblies = new List<Assembly>();
			foreach (string s in pluginsPath)
			{
				assemblies.Add(Assembly.LoadFile(s));
			}

			var types = assemblies.SelectMany(x => x.GetTypes())
					.Where(x => x.IsClass && type.IsAssignableFrom(x));

			foreach (Type t in types)
			{
				ICatPlugin obj = _container.Resolve(t) as ICatPlugin;
				_plugins.Add(obj);
			}

		}
		catch (Exception e)
		{
			Debug.WriteLine(e.Message);
		}
	}

  }
}
