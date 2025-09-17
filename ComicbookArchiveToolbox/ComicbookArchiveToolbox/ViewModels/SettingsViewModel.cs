using ComicbookArchiveToolbox.CommonTools;
using ComicbookArchiveToolbox.Events;
using Microsoft.Win32;
using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity;

namespace ComicbookArchiveToolbox.ViewModels
{
	public class SettingsViewModel : BindableBase
	{
		private readonly Logger _logger;
		private readonly IEventAggregator _eventAggregator;
		public DelegateCommand BrowseDirectoryCommand { get; private set; }
		public DelegateCommand SaveSettingsCommand { get; private set; }

		#region Tooltip Properties
		public string AlwaysIncludeCoverTooltip => "When enabled, cover images will always be included\nin the generated comic book archive files,\neven if they weren't originally present.";

		public string AlwaysIncludeMetadataTooltip => "When enabled, metadata files (like ComicInfo.xml\nor Calibre metadata html files) will be preserved\nand included in the output archive if they exist\nin the source file.\n\nWarning: it is recommended to deactivate this option\nif you intend to flatten the content of the archive.";

		public string AddFileIndexToCoversTooltip => "When enabled, adds a numerical index to cover\nfile names to maintain proper sorting order\nin the archive.";

		public string UseFileDirAsBufferTooltip => "When enabled, uses the same directory as the input\nfile for temporary processing.\n\nWhen disabled, uses the specified buffer directory below.";

		public string BufferPathTooltip => "Specifies the directory used for temporary file\nprocessing during compression operations.\n\nOnly used when 'Use input file folder as buffer'\nis disabled.";

		public string FlattenStructureTooltip => "When enabled, removes subdirectory structure\nfrom the archive, placing all files in the root\nlevel of the output archive.\n\nWarning: it is recommended to deactivate this option\nif you intend to keep metadata files\n(especially Calibre metadata).";

		public string SelectedFormatTooltip => "Choose the preferred output format for compressed\ncomic book archives.\n\nCommon formats include CBZ (ZIP) and CBR (RAR).";

		public string DefaultImageHeightTooltip => "Sets the default height in pixels for image\nresizing operations.\n\nImages will be scaled proportionally to match\nthis height while maintaining aspect ratio.";

		public string SaveSettingsTooltip => "Saves all current settings to disk so they will\nbe remembered when the application is restarted.";
		#endregion

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
				Settings.ArchiveFormat compression = (Settings.ArchiveFormat)Enum.Parse(typeof(Settings.ArchiveFormat), _selectedFormat);
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

		private bool _flattenStruture;
		public bool FlattenStruture
		{
			get { return _flattenStruture; }
			set
			{
				SetProperty(ref _flattenStruture, value);
				Settings.Instance.FlattenStructure = _flattenStruture;
			}
		}

		private bool CanExecute()
		{
			return true;
		}

		private void BrowseDirectory()
		{
			var dialog = new OpenFolderDialog();
			if (!string.IsNullOrWhiteSpace(BufferPath))
			{
				dialog.InitialDirectory = BufferPath;
			}
			if (dialog.ShowDialog() == true)
			{
				BufferPath = dialog.FolderName;
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
			catch (Exception e)
			{
				_logger.Log($"Failed to save settings: {e.Message}");
			}
		}

	}
}