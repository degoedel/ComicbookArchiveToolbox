using ComicbookArchiveToolbox.CommonTools;
using ComicbookArchiveToolbox.Events;
using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Unity;

namespace ComicbookArchiveToolbox.ViewModels
{
	public class SettingsViewModel : BindableBase
	{
    private Logger _logger;
		private IEventAggregator _eventAggregator;
    public DelegateCommand BrowseDirectoryCommand { get; private set; }
    public DelegateCommand SaveSettingsCommand { get; private set; }


    public SettingsViewModel(IUnityContainer container, IEventAggregator eventAggregator)
    {
      _logger = container.Resolve<Logger>();
			_eventAggregator = eventAggregator;
      Formats = Enum.GetNames(typeof(Settings.ArchiveFormat)).ToList();
      UseFileDirAsBuffer = Settings.Instance.UseFileDirAsBuffer;
      BufferPath = Settings.Instance.BufferDirectory;
      AlwaysIncludeCover = Settings.Instance.IncludeCover;
			AddFileIndexToCovers = Settings.Instance.AddFileIndexToCovers;
      AlwaysIncludeMetadata = Settings.Instance.IncludeMetadata;
      BrowseDirectoryCommand = new DelegateCommand(BrowseDirectory, CanExecute);
      SaveSettingsCommand = new DelegateCommand(SaveSettings, CanExecute);
      SelectedFormat = Settings.Instance.OutputFormat.ToString();
			HideLog = Settings.Instance.HideLog;
			DefaultImageHeight = Settings.Instance.DefaultImageHeight;
    }

    bool _useFileDirAsBuffer;
    public bool UseFileDirAsBuffer
    {
      get { return _useFileDirAsBuffer; }
      set
      {
        SetProperty(ref _useFileDirAsBuffer, value);
        Settings.Instance.UseFileDirAsBuffer = _useFileDirAsBuffer;
      }
    }

    string _bufferPath;
    public string BufferPath
    {
      get { return _bufferPath; }
      set
      {
        SetProperty(ref _bufferPath, value);
        Settings.Instance.BufferDirectory = _bufferPath;
      }
    }

    bool _alwaysIncludeCover;
    public bool AlwaysIncludeCover
    {
      get { return _alwaysIncludeCover; }
      set
      {
        SetProperty(ref _alwaysIncludeCover, value);
        Settings.Instance.IncludeCover = _alwaysIncludeCover;
      }
    }

		private bool _addFileIndexToCovers;
		public bool AddFileIndexToCovers
		{
			get { return _addFileIndexToCovers; }
			set
			{
				SetProperty(ref _addFileIndexToCovers, value);
				Settings.Instance.AddFileIndexToCovers = value;
			}
		}

		bool _hideLog;
		public bool HideLog
		{
			get { return _hideLog; }
			set
			{
				SetProperty(ref _hideLog, value);
				Settings.Instance.HideLog = value;
				_eventAggregator.GetEvent<HideLogEvent>().Publish(value);
			}
		}

		bool _alwaysIncludeMetadata;
    public bool AlwaysIncludeMetadata
    {
      get { return _alwaysIncludeMetadata; }
      set
      {
        SetProperty(ref _alwaysIncludeMetadata, value);
        Settings.Instance.IncludeMetadata = _alwaysIncludeMetadata;
      }
    }

     public List<string> Formats { get; set; }

    string _selectedFormat;
    public string SelectedFormat
    {
      get { return _selectedFormat; }
      set
      {
        SetProperty(ref _selectedFormat, value);
        Settings.ArchiveFormat compression = (Settings.ArchiveFormat) Enum.Parse(typeof(Settings.ArchiveFormat), _selectedFormat);
      }
    }

		private long _defaultImageHeight;
		public long DefaultImageHeight
		{
			get { return _defaultImageHeight; }
			set
			{
				SetProperty(ref _defaultImageHeight, value);
				Settings.Instance.DefaultImageHeight = _defaultImageHeight;
			}
		}

    private bool CanExecute()
    {
      return true;
    }

    private void BrowseDirectory()
    {
      var dialog = new FolderBrowserDialog();
        if (!string.IsNullOrWhiteSpace(BufferPath))
        {
          dialog.InitialDirectory = BufferPath;
        }
        DialogResult result = dialog.ShowDialog();
        if (result == DialogResult.OK)
        {
          BufferPath = dialog.SelectedPath;
        }
    }

    private void SaveSettings()
    {
      _logger.Log("Save settings");
      try
      {
        Settings.Instance.SerializeSettings();
        _logger.Log("Save done");
      }
      catch(Exception e)
      {
        _logger.Log($"Failed to save settings: {e.Message}");
      }
    }

  }
}
