using ComicbookArchiveToolbox.CommonTools.Interfaces;
using Prism.Regions;
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

namespace ComicbookArchiveToolbox.Views
{
  /// <summary>
  /// Interaction logic for MainWindow.xaml
  /// </summary>
  public partial class MainWindow : Window
  {
    public IEnumerable<ICatPlugin> Plugins { get; set; }

    public MainWindow(IRegionManager regionManager)
    {
      InitializeComponent();
	    regionManager.RegisterViewWithRegion("HostRegion", typeof(HostView));
	  }
  }
}
