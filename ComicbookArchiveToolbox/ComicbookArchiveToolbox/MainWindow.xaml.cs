using ComicbookArchiveToolbox.CommonTools.Interfaces;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ComicbookArchiveHost
{
  /// <summary>
  /// Interaction logic for MainWindow.xaml
  /// </summary>
  public partial class MainWindow : Window
  {
    [ImportMany(typeof(ICatPlugin))]
    public IEnumerable<ICatPlugin> Plugins { get; set; }

    public MainWindow()
    {
      InitializeComponent();
      try
      {
        var catalog = new AggregateCatalog();
        catalog.Catalogs.Add(new AssemblyCatalog(System.Reflection.Assembly.GetExecutingAssembly()));
        foreach( string s in GetPluginsCatalog())
        {
          catalog.Catalogs.Add(new AssemblyCatalog(Assembly.LoadFrom(s)));
        }
        var container = new CompositionContainer(catalog);
        container.ComposeParts(this);

        HostViewModel vm = new HostViewModel(Plugins);
        DataContext = vm;
      }
      catch (Exception e)
      {
        Debug.WriteLine(e.Message);
      }

    }

    private static List<string> GetPluginsCatalog()
    {
      DirectoryInfo installDir = new DirectoryInfo(AssemblyDirectory);
      var listing = installDir.GetFiles("*CatPlugin*.dll");
      List<string> result = new List<string>();
      foreach (FileInfo fi in listing)
      {
        result.Add(fi.FullName);
      }
      return result;
    }

    private static string AssemblyDirectory
    {
      get
      {
        string codeBase = Assembly.GetExecutingAssembly().CodeBase;
        UriBuilder uri = new UriBuilder(codeBase);
        string path = Uri.UnescapeDataString(uri.Path);
        return System.IO.Path.GetDirectoryName(path);
      }
    }
  }
}
