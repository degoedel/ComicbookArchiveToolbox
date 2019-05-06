using ComicbookArchiveToolbox.CommonTools;
using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CatPlugin.Split.ViewModels
{
  public class SplitPluginViewModel : BindableBase
  {
    public string TextContent => "MySplitPluginText";

    public string FileToSplit { get; set; }

    public uint FileNb { get; set;}

    private string _splitLog = "";
    public string SplitLog
    {
      get { return _splitLog; }
      set { SetProperty(ref _splitLog, value); }
    }

    public DelegateCommand BrowseFileCommand { get; private set; }
    public DelegateCommand SplitCommand { get; private set; }

    public SplitPluginViewModel()
    {
      BrowseFileCommand = new DelegateCommand(BrowseFile, CanExecute);
      SplitCommand = new DelegateCommand(DoSplit, CanExecute);
      SplitLog = "";
    }

    private void BrowseFile()
    {
      AddLogLine("Browse for file to split");
    }

    private void DoSplit()
    {

      AddLogLine("Split the file...");
    }

    private bool CanExecute()
    {
      return true;
    }

    private void AddLogLine(string line)
    {
      SplitLog += line + "\n";
    }
  }
}
