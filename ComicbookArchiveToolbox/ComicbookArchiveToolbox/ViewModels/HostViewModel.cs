using ComicbookArchiveToolbox.CommonTools;
using ComicbookArchiveToolbox.CommonTools.Interfaces;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace ComicbookArchiveHost.ViewModels
{
  public class HostViewModel : BindableBase
  {

    #region Attributes
    public IEnumerable<ICatPlugin> Plugins { get; private set; }

    private List<string> _pluginsNames;
    private List<string> _pluginCategories;
    #endregion Attributes

    public CatViewModel DisplayedView { get; set; }

    #region Constructors
    public HostViewModel(IEnumerable<ICatPlugin> plugins)
    {
      Plugins = plugins;
      _pluginsNames = new List<string>();
      _pluginCategories = new List<string>();
      InitPlugins();
    }

    private void InitPlugins()
    {
      var plugins = Plugins.OrderBy(p => p.Name);
      foreach (var app in plugins)
      {
        // Take the View from the Plugin and Merge it with,
        // our Applications Resource Dictionary.
        //Application.Current.Resources.MergedDictionaries.Add(app.View);
        _pluginsNames.Add(app.Name);
        if (!_pluginCategories.Contains(app.Category))
        {
          _pluginCategories.Add(app.Category);
        }

        DisplayedView = plugins.First().ViewModel;
        // Then add the ViewModel of our plugin to our collection of ViewModels.
        //var vm = app.Value.ViewModel;
        //Workspaces.Add(vm);
      }
    }
    #endregion Constructors

  }
}
