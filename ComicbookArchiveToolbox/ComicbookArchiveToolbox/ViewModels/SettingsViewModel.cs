using ComicbookArchiveToolbox.CommonTools;
using Microsoft.Win32;
using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Unity;

namespace ComicbookArchiveToolbox.ViewModels
{
	public class SettingsViewModel : BindableBase
	{
		private readonly Logger _logger;
		private readonly IEventAggregator _eventAggregator;
		private readonly Timer _cpuMonitorTimer;

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

		public string PerformanceModeTooltip => "Controls how the application uses system resources:\n\n• Low Resource: Minimal CPU usage, processes files one at a time\n• Balanced: Moderate resource usage with controlled concurrency\n• High Performance: Maximum speed using all available CPU cores";

		public string BatchSizeTooltip => "Number of files to process in each batch.\nSmaller values reduce memory usage but may be slower.\nLarger values are faster but use more memory.";

		public string EnableThrottlingTooltip => "When enabled, adds small delays between operations\nto reduce CPU load and prevent system overheating.\nRecommended for older or lower-powered systems.";
		#endregion

		public SettingsViewModel(IUnityContainer container, IEventAggregator eventAggregator)
		{
			_logger = container.Resolve<Logger>();
			_eventAggregator = eventAggregator;

			// Initialize collections
			Formats = Enum.GetNames(typeof(SerializationSettings.ArchiveFormat)).ToList();
			PerformanceModes = Enum.GetNames(typeof(SerializationSettings.EPerformanceMode)).ToList();

			// Initialize existing properties
			UseFileDirAsBuffer = Settings.Instance.UseFileDirAsBuffer;
			BufferPath = Settings.Instance.BufferDirectory;
			AlwaysIncludeCover = Settings.Instance.IncludeCover;
			AddFileIndexToCovers = Settings.Instance.AddFileIndexToCovers;
			AlwaysIncludeMetadata = Settings.Instance.IncludeMetadata;
			SelectedFormat = Settings.Instance.OutputFormat.ToString();
			DefaultImageHeight = Settings.Instance.DefaultImageHeight;
			FlattenStructure = Settings.Instance.FlattenStructure;

			// Initialize performance properties
			PerformanceMode = Settings.Instance.PerformanceMode.ToString();
			BatchSize = Settings.Instance.BatchSize;
			EnableThrottling = Settings.Instance.EnableThrottling;

			// Initialize commands
			BrowseDirectoryCommand = new DelegateCommand(BrowseDirectory, CanExecute);
			SaveSettingsCommand = new DelegateCommand(SaveSettings, CanExecute);

			// Start CPU monitoring
			_cpuMonitorTimer = new Timer(UpdateCpuUsage, null, TimeSpan.Zero, TimeSpan.FromSeconds(2));
		}

		#region Existing Properties

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
				if (Enum.TryParse<SerializationSettings.ArchiveFormat>(_selectedFormat, out var format))
				{
					Settings.Instance.OutputFormat = format;
				}
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

		// Fixed typo: was FlattenStruture, now FlattenStructure
		private bool _flattenStructure;
		public bool FlattenStructure
		{
			get { return _flattenStructure; }
			set
			{
				SetProperty(ref _flattenStructure, value);
				Settings.Instance.FlattenStructure = _flattenStructure;
			}
		}

		#endregion

		#region Performance Properties

		public List<string> PerformanceModes { get; set; }

		private string _performanceMode;
		public string PerformanceMode
		{
			get { return _performanceMode; }
			set
			{
				SetProperty(ref _performanceMode, value);
				if (Enum.TryParse<SerializationSettings.EPerformanceMode>(_performanceMode, out var mode))
				{
					Settings.Instance.PerformanceMode = mode;
					_logger?.Log($"Performance mode changed to: {mode}");

					// Auto-adjust batch size based on performance mode
					AdjustBatchSizeForPerformanceMode(mode);
				}
			}
		}

		private int _batchSize;
		public int BatchSize
		{
			get { return _batchSize; }
			set
			{
				SetProperty(ref _batchSize, value);
				Settings.Instance.BatchSize = _batchSize;
			}
		}

		private bool _enableThrottling;
		public bool EnableThrottling
		{
			get { return _enableThrottling; }
			set
			{
				SetProperty(ref _enableThrottling, value);
				Settings.Instance.EnableThrottling = _enableThrottling;
			}
		}

		private float _currentCpuUsage;
		public float CurrentCpuUsage
		{
			get { return _currentCpuUsage; }
			private set { SetProperty(ref _currentCpuUsage, value); }
		}

		#endregion

		#region Private Methods

		private void UpdateCpuUsage(object state)
		{
			try
			{
				CurrentCpuUsage = PerformanceMonitor.GetCurrentCpuUsage();
			}
			catch (Exception ex)
			{
				_logger?.Log($"Error updating CPU usage: {ex.Message}");
			}
		}

		private void AdjustBatchSizeForPerformanceMode(SerializationSettings.EPerformanceMode mode)
		{
			var recommendedSize = mode switch
			{
				SerializationSettings.EPerformanceMode.LowResource => 5,
				SerializationSettings.EPerformanceMode.Balanced => 10,
				SerializationSettings.EPerformanceMode.HighPerformance => 25,
				_ => 10
			};

			// Only auto-adjust if the current batch size is at a default value
			if (_batchSize == 5 || _batchSize == 10 || _batchSize == 25)
			{
				BatchSize = recommendedSize;
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
				_logger.Log("Settings saved successfully");
			}
			catch (Exception e)
			{
				_logger.Log($"Failed to save settings: {e.Message}");
			}
		}

		#endregion

		#region IDisposable Support

		private bool _disposed = false;

		protected virtual void Dispose(bool disposing)
		{
			if (!_disposed)
			{
				if (disposing)
				{
					_cpuMonitorTimer?.Dispose();
				}
				_disposed = true;
			}
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		~SettingsViewModel()
		{
			Dispose(false);
		}

		#endregion
	}
}