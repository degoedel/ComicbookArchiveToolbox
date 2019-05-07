using ComicbookArchiveToolbox.CommonTools;
using Microsoft.Win32;
using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CatPlugin.Split.ViewModels
{
  public class SplitPluginViewModel : BindableBase
  {
	private string _fileToSplit = "";
    public string FileToSplit
	{
	  get { return _fileToSplit; }
	  set { SetProperty(ref _fileToSplit, value); }
	}

	private uint _fileNb = 5;
	public uint FileNb
	{
	  get { return _fileNb; }
	  set { SetProperty(ref _fileNb, value); }
	}

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
	  OpenFileDialog openFileDialog = new OpenFileDialog();
	  openFileDialog.Filter = "Comics Archive files (*.cb7;*.cba;*cbr;*cbt;*.cbz)|*.cb7;*.cba;*cbr;*cbt;*.cbz|All files (*.*)|*.*";
	  string defaultPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
	  if (!string.IsNullOrEmpty(_fileToSplit))
	  {
		try
		{
		  FileInfo fi = new FileInfo(_fileToSplit);
		  string selectedDir = fi.DirectoryName;
		  if(Directory.Exists(selectedDir))
		  {
			defaultPath = selectedDir;
		  }
		  else
		  {
			AddLogLine("WARNING: cannot reach selected path... Open standard path instead.");
		  }
		}
		catch(Exception e)
		{
		  AddLogLine("ERROR: selected path is not valid... Open standard path instead.");
		}
		
	  }
	  openFileDialog.InitialDirectory = defaultPath;
	  //openFileDialog.Multiselect = true;
	  if (openFileDialog.ShowDialog() == true)
	  {
		//foreach (string filename in openFileDialog.FileNames)
		//lbFiles.Items.Add(Path.GetFileName(filename));
		FileToSplit = openFileDialog.FileName;
	  }

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
