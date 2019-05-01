using ComicbookArchiveToolbox.CommonTools;
using ComicbookArchiveToolbox.CommonTools.Interfaces;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace ComicbookArchiveToolbox.SplitPlugin
{
  [Export(typeof(ICatPlugin))]
  public class Plugin : ICatPlugin
  {
    #region Attributes
    private SplitPluginViewModel _viewModel;
    private ResourceDictionary _viewDictionary = new ResourceDictionary();
    #endregion Attributes


    #region Properties
    public string Name => "Split";

    public string Category => "Splitters";

    public CatViewModel ViewModel => _viewModel;

    public System.Windows.ResourceDictionary View => _viewDictionary;
    #endregion Properties

    #region Constructors
    [ImportingConstructor]
    public Plugin()
    {
      _viewModel = new SplitPluginViewModel();
      _viewDictionary.Source =
            new Uri("/CatPlugin.SplitPlugin;component/View.xaml",
            UriKind.RelativeOrAbsolute);
    }
    #endregion Constructors
  }
}
