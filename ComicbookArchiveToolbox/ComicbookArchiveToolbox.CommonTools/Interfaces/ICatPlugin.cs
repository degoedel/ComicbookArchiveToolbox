using Prism.Modularity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace ComicbookArchiveToolbox.CommonTools.Interfaces
{
  public interface ICatPlugin : IModule
  {
    string Name { get; }
    string Category { get; }
    CatViewModel ViewModel { get; }
  }
}
