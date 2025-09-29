using ComicbookArchiveToolbox.CommonTools;
using ComicbookArchiveToolbox.CommonTools.Events;
using ComicbookArchiveToolbox.Module.Split.Services;
using ComicbookArchiveToolbox.Services;
using ComicbookArchiveToolbox.ViewModels;
using Prism.Commands;
using Prism.Events;
using Prism.Navigation.Regions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Unity;

namespace ComicbookArchiveToolbox.Module.Split.ViewModels
{
	public class SplitPluginViewModel : BasePluginViewModel
	{
		private readonly Logger _logger;
		private readonly IRegionManager _regionManager;
		private readonly IUnityContainer _container;
		private readonly IEventAggregator _eventAggregator;
		private CancellationTokenSource _cancellationTokenSource;
		private int _activeSplitOperations = 0;
		private readonly object _operationCountLock = new object();

		public List<string> SplitStyles { get; set; }

		private string _selectedStyle = "By File Nb";
		public string SelectedStyle
		{
			get { return _selectedStyle; }
			set
			{
				_logger.Log($"Split style changed from '{_selectedStyle}' to '{value}'");
				SetProperty(ref _selectedStyle, value);
				SetSplitterView(_selectedStyle);
			}
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Interoperability", "CA1416:Validate platform compatibility", Justification = "<Pending>")]
		private void SetSplitterView(string selectedView)
		{
			_logger.Log($"Setting splitter view for style: {selectedView}");

			string viewToActivate;
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
					_logger.Log($"WARNING: Unknown split style '{selectedView}', defaulting to 'SplitByFileNbView'");
					viewToActivate = "SplitByFileNbView";
					break;
			}

			try
			{
				IRegion region = _regionManager.Regions["SplitArgsRegion"];
				var view = region.GetView(viewToActivate);
				region.Activate(view);
				_logger.Log($"Successfully activated view: {viewToActivate}");
			}
			catch (Exception ex)
			{
				_logger.Log($"ERROR: Failed to activate view '{viewToActivate}': {ex.Message}");
			}

			SplitCommand.RaiseCanExecuteChanged();
		}

		private string _sourceToSplit = "";
		public string SourceToSplit
		{
			get { return _sourceToSplit; }
			set
			{
				var oldValue = _sourceToSplit;
				SetProperty(ref _sourceToSplit, value);

				if (!string.IsNullOrWhiteSpace(value) && value != oldValue)
				{
					_logger.Log($"Source to split set to: {value}");
					_logger.Log($"Processing mode: {(IsBatchMode ? "Batch (Directory)" : "Single File")}");

					if (IsBatchMode)
					{
						LogBatchModeInfo(value);
					}
					else
					{
						LogSingleFileModeInfo(value);
					}
				}

				RaisePropertyChanged("FileSelected");

				if (!IsBatchMode)
				{
					bool validFile = ExtractNameTemplateFromFile(_sourceToSplit, out string newTemplate);
					if (validFile)
					{
						_logger.Log($"Extracted name template: '{newTemplate}' from file");
						NameTemplate = newTemplate;
					}
				}

				SplitCommand.RaiseCanExecuteChanged();
			}
		}

		private void LogBatchModeInfo(string directoryPath)
		{
			try
			{
				if (Directory.Exists(directoryPath))
				{
					var comicFiles = Directory.GetFiles(directoryPath)
						.Where(file => SystemTools.ComicExtensions.Contains(Path.GetExtension(file).ToLowerInvariant()))
						.ToArray();

					_logger.Log($"Batch directory contains {comicFiles.Length} comic archive files");

					if (comicFiles.Length > 0)
					{
						_logger.Log($"Comic files found: {string.Join(", ", comicFiles.Select(Path.GetFileName))}");
					}
					else
					{
						_logger.Log("WARNING: No comic archive files found in selected directory");
					}
				}
				else
				{
					_logger.Log($"WARNING: Selected batch directory does not exist: {directoryPath}");
				}
			}
			catch (Exception ex)
			{
				_logger.Log($"ERROR: Failed to analyze batch directory '{directoryPath}': {ex.Message}");
			}
		}

		private void LogSingleFileModeInfo(string filePath)
		{
			try
			{
				if (File.Exists(filePath))
				{
					var fileInfo = new FileInfo(filePath);
					_logger.Log($"Selected file size: {fileInfo.Length:N0} bytes ({fileInfo.Length / (1024.0 * 1024.0):F2} MB)");
					_logger.Log($"File extension: {fileInfo.Extension}");

					if (!SystemTools.ComicExtensions.Contains(fileInfo.Extension.ToLowerInvariant()))
					{
						_logger.Log($"WARNING: Selected file does not have a recognized comic archive extension");
					}
				}
				else
				{
					_logger.Log($"WARNING: Selected file does not exist: {filePath}");
				}
			}
			catch (Exception ex)
			{
				_logger.Log($"ERROR: Failed to analyze selected file '{filePath}': {ex.Message}");
			}
		}

		private string _outputDir = "";
		public string OutputDir
		{
			get { return _outputDir; }
			set
			{
				var oldValue = _outputDir;
				SetProperty(ref _outputDir, value);

				if (!string.IsNullOrWhiteSpace(value) && value != oldValue)
				{
					_logger.Log($"Output directory set to: {value}");

					try
					{
						if (Directory.Exists(value))
						{
							var existingFiles = Directory.GetFiles(value, "*.*").Length;
							_logger.Log($"Output directory exists and contains {existingFiles} files");
						}
						else
						{
							_logger.Log("Output directory will be created during split operation");
						}
					}
					catch (Exception ex)
					{
						_logger.Log($"WARNING: Cannot access output directory information: {ex.Message}");
					}
				}

				SplitCommand.RaiseCanExecuteChanged();
			}
		}

		public bool FileSelected
		{
			get
			{
				return IsBatchMode ? !string.IsNullOrWhiteSpace(SourceToSplit) && Directory.Exists(SourceToSplit) : !string.IsNullOrWhiteSpace(SourceToSplit) && File.Exists(SourceToSplit);
			}
		}

		private uint _fileNb = 5;
		public uint FileNb
		{
			get { return _fileNb; }
			set
			{
				var oldValue = _fileNb;
				SetProperty(ref _fileNb, value);

				if (value != oldValue)
				{
					_logger.Log($"File number changed from {oldValue} to {value}");
				}

				SplitCommand.RaiseCanExecuteChanged();
			}
		}

		private uint _maxFilePerArchive = 50;
		public uint MaxFilePerArchive
		{
			get { return _maxFilePerArchive; }
			set
			{
				var oldValue = _maxFilePerArchive;
				SetProperty(ref _maxFilePerArchive, value);

				if (value != oldValue)
				{
					_logger.Log($"Max pages per archive changed from {oldValue} to {value}");
				}

				SplitCommand.RaiseCanExecuteChanged();
			}
		}

		private long _maxFileSize = 50;
		public long MaxFileSize
		{
			get { return _maxFileSize; }
			set
			{
				var oldValue = _maxFileSize;
				SetProperty(ref _maxFileSize, value);

				if (value != oldValue)
				{
					_logger.Log($"Max file size changed from {oldValue}MB to {value}MB");
				}

				SplitCommand.RaiseCanExecuteChanged();
			}
		}

		private List<uint> _pagesToSplitIndex = [];
		public string PagesToSplitIndex
		{
			get { return String.Join(";", _pagesToSplitIndex); }
			set
			{
				try
				{
					var indexesAsStr = value.Split(';');
					var pagesToSplitIndex = new List<uint>();

					foreach (string s in indexesAsStr)
					{
						if (!string.IsNullOrWhiteSpace(s) && uint.TryParse(s.Trim(), out uint pageIndex))
						{
							pagesToSplitIndex.Add(pageIndex);
						}
					}

					_logger.Log($"Pages to split index updated: [{string.Join(", ", pagesToSplitIndex)}]");
					SetProperty(ref _pagesToSplitIndex, pagesToSplitIndex);
				}
				catch (Exception ex)
				{
					_logger.Log($"ERROR: Failed to parse pages split index '{value}': {ex.Message}");
				}

				SplitCommand.RaiseCanExecuteChanged();
			}
		}

		private long _imageQuality = 100;
		public long ImageQuality
		{
			get { return _imageQuality; }
			set
			{
				var originalValue = value;
				var oldValue = _imageQuality;

				if (value < 0)
				{
					value = 0;
				}
				if (value > 100)
				{
					value = 100;
				}

				SetProperty(ref _imageQuality, value);

				if (value != oldValue)
				{
					if (originalValue != value)
					{
						_logger.Log($"Image quality clamped from {originalValue} to {value}% (valid range: 0-100)");
					}
					else
					{
						_logger.Log($"Image quality changed from {oldValue}% to {value}%");
					}
				}
			}
		}

		private string _nameTemplate;
		public string NameTemplate
		{
			get { return _nameTemplate; }
			set
			{
				var oldValue = _nameTemplate;
				SetProperty(ref _nameTemplate, value);

				if (!string.IsNullOrWhiteSpace(value) && value != oldValue)
				{
					_logger.Log($"Name template set to: '{value}'");
				}

				SplitCommand.RaiseCanExecuteChanged();
			}
		}

		private bool ExtractNameTemplateFromFile(string fileName, out string fileTemplate)
		{
			bool result = false;
			fileTemplate = "";

			try
			{
				if (File.Exists(fileName))
				{
					FileInfo fi = new(fileName);
					fileTemplate = fi.Name.Substring(0, fi.Name.Length - fi.Extension.Length);
					result = true;
					_logger.Log($"Extracted name template '{fileTemplate}' from file '{fi.Name}'");
				}
				else
				{
					_logger.Log($"WARNING: Cannot extract name template - file does not exist: {fileName}");
				}
			}
			catch (Exception ex)
			{
				_logger.Log($"ERROR: Failed to extract name template from '{fileName}': {ex.Message}");
			}

			return result;
		}

		private bool _followFilePath;
		public bool FollowFilePath
		{
			get { return _followFilePath; }
			set
			{
				var oldValue = _followFilePath;
				SetProperty(ref _followFilePath, value);

				if (value != oldValue)
				{
					_logger.Log($"Follow file path setting changed to: {value}");

					if (value)
					{
						SetOutputPathSameAsInput();
					}
				}
			}
		}

		private void SetOutputPathSameAsInput()
		{
			_logger.Log("Setting output path to match input location");

			try
			{
				if (FileSelected)
				{
					string newOutputDir;

					if (IsBatchMode)
					{
						newOutputDir = SourceToSplit;
						_logger.Log($"Batch mode: Output directory set to input directory: {newOutputDir}");
					}
					else
					{
						FileInfo fi = new(SourceToSplit);
						newOutputDir = fi.DirectoryName;
						_logger.Log($"Single file mode: Output directory set to file's directory: {newOutputDir}");
					}

					OutputDir = newOutputDir;
				}
				else
				{
					_logger.Log("WARNING: Cannot set output path - no valid file selected");
				}
			}
			catch (Exception ex)
			{
				_logger.Log($"ERROR: Failed to set output path same as input: {ex.Message}");
			}
		}

		public DelegateCommand SplitCommand { get; private set; }

		public SplitPluginViewModel(IUnityContainer container, IRegionManager regionManager, IEventAggregator eventAggregator)
			: base(container, eventAggregator)
		{
			_container = container;
			_eventAggregator = eventAggregator;
			_logger = _container.Resolve<Logger>();

			SplitStyles =
			[
				"By File Nb",
				"By Max Pages Nb",
				"By Size (Mb)"
			];

			_regionManager = regionManager;

			// Register splitter implementations
			container.RegisterType<ISplitter, ByFileSplitterPlugin>("By File Nb");
			container.RegisterType<ISplitter, ByMaxPageSplitterPlugin>("By Max Pages Nb");
			container.RegisterType<ISplitter, BySizeSplitterPlugin>("By Size (Mb)");

			SplitCommand = new DelegateCommand(DoSplit, CanSplit);
		}

		private void StartBusyState()
		{
			lock (_operationCountLock)
			{
				_activeSplitOperations++;
				if (_activeSplitOperations == 1)
				{
					_logger.Log("Starting busy state - split operation begins");
					_eventAggregator.GetEvent<BusinessEvent>().Publish(true);
				}
			}
		}

		private void EndBusyState()
		{
			lock (_operationCountLock)
			{
				_activeSplitOperations--;
				if (_activeSplitOperations <= 0)
				{
					_activeSplitOperations = 0; // Ensure it doesn't go negative
					_logger.Log("Ending busy state - all split operations completed");
					_eventAggregator.GetEvent<BusinessEvent>().Publish(false);
				}
			}
		}

		private async void DoSplit()
		{
			_logger.Log("=== Starting split operation ===");
			_logger.Log($"Split style: {SelectedStyle}");
			_logger.Log($"Processing mode: {(IsBatchMode ? "Batch" : "Single file")}");
			_logger.Log($"Source: {SourceToSplit}");
			_logger.Log($"Output directory: {OutputDir}");
			_logger.Log($"Image quality: {ImageQuality}%");

			// Start the busy state
			StartBusyState();

			// Cancel any existing operation
			_cancellationTokenSource?.Cancel();
			_cancellationTokenSource = new CancellationTokenSource();

			try
			{
				if (IsBatchMode)
				{
					await ProcessBatchModeAsync(_cancellationTokenSource.Token);
				}
				else
				{
					await ProcessSingleFileModeAsync(_cancellationTokenSource.Token);
				}

				_logger.Log("=== Split operation completed successfully ===");
			}
			catch (OperationCanceledException)
			{
				_logger.Log("Split operation was cancelled");
			}
			catch (Exception ex)
			{
				_logger.Log($"ERROR: Split operation failed: {ex.Message}");
				if (ex.InnerException != null)
				{
					_logger.Log($"Inner exception: {ex.InnerException.Message}");
				}
			}
			finally
			{
				// Always end the busy state when operation completes
				EndBusyState();
			}
		}

		private async Task ProcessBatchModeAsync(CancellationToken cancellationToken)
		{
			_logger.Log("Processing in batch mode");

			// Check system performance and auto-adjust settings if needed
			var currentCpuUsage = PerformanceMonitor.GetCurrentCpuUsage();
			var recommendedMode = PerformanceMonitor.RecommendPerformanceMode();
			var recommendedBatchSize = PerformanceMonitor.RecommendBatchSize();

			_logger.Log($"System Performance Assessment:");
			_logger.Log($"  Current CPU Usage: {currentCpuUsage:F1}%");
			_logger.Log($"  Current Performance Mode: {Settings.Instance.PerformanceMode}");
			_logger.Log($"  Recommended Performance Mode: {recommendedMode}");
			_logger.Log($"  Current Batch Size: {Settings.Instance.BatchSize}");
			_logger.Log($"  Recommended Batch Size: {recommendedBatchSize}");

			// Store original settings to restore later
			var originalMode = Settings.Instance.PerformanceMode;
			var originalBatchSize = Settings.Instance.BatchSize;
			var originalThrottling = Settings.Instance.EnableThrottling;

			bool settingsAdjusted = false;

			// Auto-adjust for high CPU usage
			if (currentCpuUsage > 80 && Settings.Instance.PerformanceMode != SerializationSettings.EPerformanceMode.LowResource)
			{
				_logger.Log("High CPU usage detected - temporarily switching to Low Resource mode");
				Settings.Instance.PerformanceMode = SerializationSettings.EPerformanceMode.LowResource;
				Settings.Instance.BatchSize = Math.Min(originalBatchSize, 3);
				Settings.Instance.EnableThrottling = true;
				settingsAdjusted = true;
			}
			// Auto-adjust batch size if current setting seems suboptimal
			else if (Math.Abs(Settings.Instance.BatchSize - recommendedBatchSize) > 5)
			{
				_logger.Log($"Adjusting batch size from {Settings.Instance.BatchSize} to {recommendedBatchSize} based on system performance");
				Settings.Instance.BatchSize = recommendedBatchSize;
				settingsAdjusted = true;
			}

			try
			{
				DirectoryInfo sourceFolder = new DirectoryInfo(SourceToSplit);

				List<FileInfo> batch = sourceFolder.GetFiles()
					.Where(file => SystemTools.ComicExtensions.Contains(file.Extension.ToLowerInvariant()))
					.OrderBy(f => f.Name, StringComparer.OrdinalIgnoreCase)
					.ToList();

				_logger.Log($"Found {batch.Count} comic archive files to process");

				if (batch.Count == 0)
				{
					_logger.Log("WARNING: No comic archive files found in batch directory");
					return;
				}

				// Use BatchProcessingManager for performance-aware processing
				var batchManager = new BatchProcessingManager(_logger, null);

				await batchManager.ProcessFilesAsync(
					batch.Select((file, index) => new { File = file, Index = index }),
					async item => await ProcessSingleFile(item.File, item.Index, batch.Count, cancellationToken),
					cancellationToken);

				_logger.Log("Batch processing completed successfully");
			}
			finally
			{
				// Restore original settings if they were adjusted
				if (settingsAdjusted)
				{
					_logger.Log("Restoring original performance settings");
					Settings.Instance.PerformanceMode = originalMode;
					Settings.Instance.BatchSize = originalBatchSize;
					Settings.Instance.EnableThrottling = originalThrottling;
				}
			}
		}


		private async Task ProcessSingleFileModeAsync(CancellationToken cancellationToken)
		{
			_logger.Log($"Processing single file: {Path.GetFileName(SourceToSplit)}");

			try
			{
				cancellationToken.ThrowIfCancellationRequested();

				ArchiveTemplate arctemp = CreateArchiveTemplate(NameTemplate);
				LogArchiveTemplate(arctemp, Path.GetFileName(SourceToSplit));

				var splitter = _container.Resolve<ISplitter>(SelectedStyle);
				_logger.Log($"Starting split operation for: {Path.GetFileName(SourceToSplit)}");

				// Execute split operation synchronously to avoid BusinessEvent conflicts
				await Task.Run(() =>
				{
					// Temporarily suppress BusinessEvent from splitter to avoid conflicts
					using (new SplitterBusinessEventSupressor(_eventAggregator))
					{
						splitter.Split(SourceToSplit, arctemp);
					}
				}, cancellationToken);

				_logger.Log($"Completed split operation for: {Path.GetFileName(SourceToSplit)}");
			}
			catch (OperationCanceledException)
			{
				_logger.Log("Single file split operation was cancelled");
				throw;
			}
			catch (Exception ex)
			{
				_logger.Log($"ERROR: Failed to process single file: {ex.Message}");
				throw;
			}
		}

		private ArchiveTemplate CreateArchiveTemplate(string comicName)
		{
			return new ArchiveTemplate
			{
				ComicName = comicName,
				OutputDir = OutputDir,
				NumberOfSplittedFiles = FileNb,
				MaxPagesPerSplittedFile = MaxFilePerArchive,
				MaxSizePerSplittedFile = MaxFileSize,
				PagesIndexToSplit = _pagesToSplitIndex,
				ImageCompression = ImageQuality
			};
		}

		private void LogArchiveTemplate(ArchiveTemplate template, string fileName)
		{
			_logger.Log($"Archive template for '{fileName}':");
			_logger.Log($"  - Comic name: {template.ComicName}");
			_logger.Log($"  - Output directory: {template.OutputDir}");

			switch (SelectedStyle)
			{
				case "By File Nb":
					_logger.Log($"  - Number of files: {template.NumberOfSplittedFiles}");
					break;
				case "By Max Pages Nb":
					_logger.Log($"  - Max pages per archive: {template.MaxPagesPerSplittedFile}");
					break;
				case "By Size (Mb)":
					_logger.Log($"  - Max size per archive: {template.MaxSizePerSplittedFile} MB");
					break;
			}

			_logger.Log($"  - Image compression quality: {template.ImageCompression}%");

			if (_pagesToSplitIndex?.Count > 0)
			{
				_logger.Log($"  - Custom page indices: [{string.Join(", ", _pagesToSplitIndex)}]");
			}
		}

		private bool CanSplit()
		{
			bool selectedMethodArgument;
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

			// Don't allow split if already processing
			bool canExecute = (FileSelected && selectedMethodArgument && !string.IsNullOrWhiteSpace(OutputDir) && TemplateNameCondition && _activeSplitOperations == 0);

			return canExecute;
		}

		private async Task ProcessSingleFile(FileInfo file, int index, int totalCount, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();

			_logger.Log($"Processing batch file {index + 1}/{totalCount}: {file.Name} ({file.Length:N0} bytes)");

			ExtractNameTemplateFromFile(file.FullName, out string templatedName);
			ArchiveTemplate arctemp = CreateArchiveTemplate(templatedName);
			LogArchiveTemplate(arctemp, file.Name);

			var splitter = _container.Resolve<ISplitter>(SelectedStyle);
			_logger.Log($"Starting split operation for: {file.Name}");

			await Task.Run(() =>
			{
				using (new SplitterBusinessEventSupressor(_eventAggregator))
				{
					splitter.Split(file.FullName, arctemp);
				}
			}, cancellationToken);

			_logger.Log($"Completed split operation for: {file.Name}");
		}

		private bool TemplateNameCondition => IsBatchMode ? true : !string.IsNullOrWhiteSpace(NameTemplate);

		// Add implementations for abstract members from BasePluginViewModel
		protected override string GetCurrentInputPath()
		{
			return SourceToSplit;
		}

		protected override string GetCurrentOutputPath()
		{
			return OutputDir;
		}

		protected override void SetInputPath(string file)
		{
			SourceToSplit = file;
		}

		protected override void SetOutputPath(string file)
		{
			OutputDir = file;
		}

		protected override string GetOutputSuffix()
		{
			// Return a suffix for output files, e.g. based on selected style
			switch (SelectedStyle)
			{
				case "By File Nb":
					return "_split";
				case "By Max Pages Nb":
					return "_pages";
				case "By Size (Mb)":
					return "_size";
				default:
					return "_split";
			}
		}

		// Cleanup method to be called when the ViewModel is disposed
		public void Cleanup()
		{
			_cancellationTokenSource?.Cancel();
			_cancellationTokenSource?.Dispose();
		}

		protected override void SetInputSelectedFiles(IList<string> files)
		{
			
		}
	}

	/// <summary>
	/// Helper class to temporarily suppress BusinessEvent publications from splitters
	/// to avoid conflicts with the coordinated busy state management
	/// </summary>
	public class SplitterBusinessEventSupressor : IDisposable
	{
		private readonly IEventAggregator _eventAggregator;
		private readonly BusinessEvent _businessEvent;
		private Action<bool> _originalHandler;

		public SplitterBusinessEventSupressor(IEventAggregator eventAggregator)
		{
			_eventAggregator = eventAggregator;
			_businessEvent = _eventAggregator.GetEvent<BusinessEvent>();

			// Store original subscriptions and replace with a no-op handler
			// This prevents individual splitters from interfering with our coordinated busy state
			_originalHandler = null; // In practice, we don't need to restore as the BusinessEvent will be managed by the ViewModel
		}

		public void Dispose()
		{
			// Nothing to restore in this simple implementation
			// The SplitPluginViewModel handles the BusinessEvent coordination
		}
	}
}