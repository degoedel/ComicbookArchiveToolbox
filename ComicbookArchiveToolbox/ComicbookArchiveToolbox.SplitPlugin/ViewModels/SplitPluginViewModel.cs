using CatPlugin.Split.Services;
using ComicbookArchiveToolbox.CommonTools;
using Microsoft.Win32;
using Prism.Commands;
using Prism.Mvvm;
using System;
using System.IO;
using Unity;

namespace CatPlugin.Split.ViewModels
{
  public class SplitPluginViewModel : BindableBase
  {
    private Logger _logger;
    private Splitter _splitter; 

	  private string _fileToSplit = "";
    public string FileToSplit
	{
	  get { return _fileToSplit; }
	  set
      {
        SetProperty(ref _fileToSplit, value);
        SplitCommand.RaiseCanExecuteChanged();
      }
	}

	private uint _fileNb = 5;
	public uint FileNb
	{
	  get { return _fileNb; }
	  set
      {
        SetProperty(ref _fileNb, value);
        SplitCommand.RaiseCanExecuteChanged();
      }
	}

    public DelegateCommand BrowseFileCommand { get; private set; }
    public DelegateCommand SplitCommand { get; private set; }

    public SplitPluginViewModel(IUnityContainer container)
    {
      BrowseFileCommand = new DelegateCommand(BrowseFile, CanExecute);
      SplitCommand = new DelegateCommand(DoSplit, CanSplit);
      _logger = container.Resolve<Logger>();
      _splitter = new Splitter(_logger);
    }

    private void BrowseFile()
    {
      _logger.Log("Browse for file to split");
      OpenFileDialog openFileDialog = new OpenFileDialog
      {
        Filter = "Comics Archive files (*.cb7;*.cba;*cbr;*cbt;*.cbz)|*.cb7;*.cba;*cbr;*cbt;*.cbz|All files (*.*)|*.*"
      };
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
            _logger.Log("WARNING: cannot reach selected path... Open standard path instead.");
		  }
		}
		catch(Exception)
		{
          _logger.Log("ERROR: selected path is not valid... Open standard path instead.");
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

      _splitter.Split(FileToSplit, FileNb);
    }

    private bool CanExecute()
    {
      return true;
    }

    private bool CanSplit()
    {
      return (!string.IsNullOrWhiteSpace(FileToSplit) && File.Exists(FileToSplit) && (FileNb > 1));
    }


  }
}
