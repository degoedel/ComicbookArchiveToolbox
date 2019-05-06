using Prism.Commands;
using Prism.Modularity;
using System.Windows.Controls;

namespace ComicbookArchiveToolbox.CommonTools.Interfaces
{
  public interface ICatPlugin : IModule
  {
    string Name { get; }
    DelegateCommand LoadViewCommand { get; }

	Canvas Icon { get; }
  }
}
