using CatPlugin.Split.Services;
using ComicbookArchiveToolbox.CommonTools;
using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using Prism.Navigation.Regions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using Unity;

namespace CatPlugin.Split.ViewModels
{
  public class SplitPluginViewModel : BindableBase
  {
		private Logger _logger;
		private IRegionManager _regionManager;
		private IUnityContainer _container;
		private IEventAggregator _eventAggregator;

		public List<string> SplitStyles { get; set; }

		private string _selectedStyle = "By File Nb";
		public string SelectedStyle
		{
			get { return _selectedStyle; }
			set
			{
				SetProperty(ref _selectedStyle, value);
				SetSplitterView(_selectedStyle);
			}
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Interoperability", "CA1416:Validate platform compatibility", Justification = "<Pending>")]
		private void SetSplitterView(string selectedView)
		{
			string viewToActivate = "";
			switch (selectedView)
			{
				case "By File Nb":
					viewToActivate = "SplitByFileNbView";
					break;
				case "By Max Pages Nb":
					viewToActivate = "SplitByMaxPagesView";
					break;
				case "By Size (Mb)":
					viewToActivate = "SplitByMaxSizeView";
					break;
				default:
					viewToActivate = "SplitByFileNbView";
					break;

			}
			IRegion region = _regionManager.Regions["SplitArgsRegion"];
			var view = region.GetView(viewToActivate);
			region.Activate(view);
			SplitCommand.RaiseCanExecuteChanged();
		}

		private string _fileToSplit = "";
    public string FileToSplit
	{
	  get { return _fileToSplit; }
	  set
      {
        SetProperty(ref _fileToSplit, value);
		RaisePropertyChanged("FileSelected");
		bool validFile = ExtractNameTemplateFromFile(_fileToSplit, out string newTemplate);
		if (validFile)
		{
		  NameTemplate = newTemplate;
		}
        SplitCommand.RaiseCanExecuteChanged();
      }
	}

		private string _outputDir = "";
		public string OutputDir
		{
			get { return _outputDir; }
			set
			{
				SetProperty(ref _outputDir, value);
				SplitCommand.RaiseCanExecuteChanged();
			}
		}

		public bool FileSelected
	{
		get { return File.Exists(FileToSplit); }

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

		private uint _maxFilePerArchive = 50;
		public uint MaxFilePerArchive
		{
			get { return _maxFilePerArchive; }
			set
			{
				SetProperty(ref _maxFilePerArchive, value);
				SplitCommand.RaiseCanExecuteChanged();
			}
		}

		private long _maxFileSize = 50;
		public long MaxFileSize
		{
			get { return _maxFileSize; }
			set
			{
				SetProperty(ref _maxFileSize, value);
				SplitCommand.RaiseCanExecuteChanged();
			}
		}

		private List<uint> _pagesToSplitIndex = new List<uint>();
		public string PagesToSplitIndex
		{
			get { return String.Join(";", _pagesToSplitIndex); }
			set
			{
				var indexesAsStr = value.Split(';');
				var pagesToSplitIndex = new List<uint>();
				foreach (string s in indexesAsStr)
				{
					pagesToSplitIndex.Add(uint.Parse(s.Trim()));
				}
				SetProperty(ref _pagesToSplitIndex, pagesToSplitIndex);
				SplitCommand.RaiseCanExecuteChanged();
			}
		}

		private long _imageQuality = 100;
		public long ImageQuality
		{
			get { return _imageQuality; }
			set
			{
				if (value < 0)
				{
					value = 0;
				}
				if (value > 100)
				{
					value = 100;
				}
				SetProperty(ref _imageQuality, value);
			}
		}

	private string _nameTemplate;
	public string NameTemplate
	{
	  get { return _nameTemplate; }
	  set
	  {
	  	SetProperty(ref _nameTemplate, value);
		SplitCommand.RaiseCanExecuteChanged();
	  }
	}

	private bool ExtractNameTemplateFromFile(string fileName, out string fileTemplate)
	{
		bool result = false;
		fileTemplate = "";
		if (File.Exists(fileName))
		{
		  FileInfo fi = new FileInfo(fileName);
		  fileTemplate = fi.Name.Substring(0, fi.Name.Length - fi.Extension.Length);
		  result = true;
		}
		return result;
	}

	private bool _followFilePath;
	public bool FollowFilePath
	{
	  get { return _followFilePath; }
	  set
	  {
	  	SetProperty(ref _followFilePath, value);
		if (_followFilePath)
		{
		  SetOutputPathSameAsInput();
		}
	  }
	}

	private void SetOutputPathSameAsInput()
	{
	  if (File.Exists(FileToSplit))
	  {
	  	FileInfo fi = new FileInfo(FileToSplit);
	  	OutputDir = fi.DirectoryName;
	  }
	}

	  public DelegateCommand BrowseFileCommand { get; private set; }
    public DelegateCommand BrowseOutputDirCommand { get; private set; }
    public DelegateCommand SplitCommand { get; private set; }

    public SplitPluginViewModel(IUnityContainer container, IRegionManager regionManager, IEventAggregator eventAggregator)
    {
			_container = container;
			_eventAggregator = eventAggregator;
			SplitStyles = new List<string>()
			{
				"By File Nb",
				"By Max Pages Nb",
				"By Size (Mb)"
			};
			_regionManager = regionManager;
			BrowseFileCommand = new DelegateCommand(BrowseFile, CanExecute);
      SplitCommand = new DelegateCommand(DoSplit, CanSplit);
      BrowseOutputDirCommand = new DelegateCommand(BrowseDirectory, CanExecute);
      _logger = _container.Resolve<Logger>();
    }

    private void BrowseFile()
    {
      _logger.Log("Browse for file to split");

	  var dialog = new Microsoft.Win32.OpenFileDialog();
        dialog.Filter = "Comics Archive files (*.cb7;*.cba;*cbr;*cbt;*.cbz)|*.cb7;*.cba;*cbr;*cbt;*.cbz";
        string defaultPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        if (!string.IsNullOrEmpty(_fileToSplit))
        {
          try
          {
            FileInfo fi = new FileInfo(_fileToSplit);
            string selectedDir = fi.DirectoryName;
            if (Directory.Exists(selectedDir))
            {
              defaultPath = selectedDir;
            }
            else
            {
              _logger.Log("WARNING: cannot reach selected path... Open standard path instead.");
            }
          }
          catch (Exception)
          {
            _logger.Log("ERROR: selected path is not valid... Open standard path instead.");
          }

        }
        dialog.InitialDirectory = defaultPath;
        bool? result = dialog.ShowDialog();
        if (result.HasValue && result.Value == true)
        {
          FileToSplit = dialog.FileName;
        }
	}

    private void DoSplit()
    {
		ArchiveTemplate arctemp = new ArchiveTemplate()
		{
			ComicName = NameTemplate,
			OutputDir = OutputDir,
			NumberOfSplittedFiles = FileNb,
			MaxPagesPerSplittedFile = MaxFilePerArchive,
			MaxSizePerSplittedFile = MaxFileSize,
			PagesIndexToSplit = _pagesToSplitIndex,
			ImageCompression = ImageQuality
		};
		var splitter = _container.Resolve<ISplitter>(SelectedStyle);
		Task.Run(() => splitter.Split(FileToSplit, arctemp));
    }

    private bool CanExecute()
    {
      return true;
    }

    private bool CanSplit()
    {
			bool selectedMethodArgument = false;
			switch (SelectedStyle)
			{
				case "By File Nb":
					selectedMethodArgument = FileNb > 1;
					break;
				case "By Max Pages Nb":
					selectedMethodArgument = MaxFilePerArchive > 1;
					break;
				case "By Size (Mb)":
					selectedMethodArgument = MaxFileSize > 1;
					break;
				case "By Pages Index":
					selectedMethodArgument = _pagesToSplitIndex.Count > 0;
					break;
				default:
					selectedMethodArgument = false;
					break;

			}
			return (!string.IsNullOrWhiteSpace(FileToSplit) && File.Exists(FileToSplit) && selectedMethodArgument && !string.IsNullOrWhiteSpace(OutputDir) && !string.IsNullOrWhiteSpace(NameTemplate));
    }

    private void BrowseDirectory()
    {
		var dialog = new FolderBrowserDialog();
        if (!string.IsNullOrWhiteSpace(FileToSplit))
        {
          dialog.InitialDirectory = (new FileInfo(FileToSplit)).Directory.FullName;
        }
        DialogResult result = dialog.ShowDialog();
        if (result == DialogResult.OK)
        {
          OutputDir = dialog.SelectedPath;
        }
    }


  }
}
