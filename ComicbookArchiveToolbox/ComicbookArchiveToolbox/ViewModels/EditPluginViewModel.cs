using ComicbookArchiveToolbox.CommonTools;
using ComicbookArchiveToolbox.CommonTools.Events;
using ComicbookArchiveToolbox.Services;
using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Unity;

namespace ComicbookArchiveToolbox.Module.Edit.ViewModels
{
	public class EditPluginViewModel : BindableBase
	{
		#region Fields
		private readonly Logger _logger;
		private readonly IUnityContainer _container;
		private readonly IEventAggregator _eventAggregator;
		private readonly IFileDialogService _fileDialogService;
		private readonly IMetadataService _metadataService;
		private readonly IArchiveService _archiveService;
		private readonly IBufferManager _bufferManager;

		private string _fileToEdit = "";
		private ObservableCollection<ComicMetadata> _metadataCollection;
		private string _metadataFile = "";
		private string _calibreMetaDataFile = "";
		#endregion

		#region Properties
		public string FileToEdit
		{
			get { return _fileToEdit; }
			set
			{
				SetProperty(ref _fileToEdit, value);
				if (File.Exists(_fileToEdit))
				{
					_ = LoadMetadataAsync();
				}
				RefreshCommandStates();
			}
		}

		public ObservableCollection<ComicMetadata> MetadataCollection
		{
			get { return _metadataCollection; }
			set { SetProperty(ref _metadataCollection, value); }
		}

		public DelegateCommand BrowseFileCommand { get; private set; }
		public DelegateCommand SaveCommand { get; private set; }
		public DelegateCommand ImportCalibreCommand { get; private set; }
		public DelegateCommand ExportCalibreCommand { get; private set; }
		#endregion

		#region Constructor
		public EditPluginViewModel(IUnityContainer container, IEventAggregator eventAggregator)
		{
			_container = container;
			_eventAggregator = eventAggregator;
			_fileDialogService = container.Resolve<IFileDialogService>();
			_logger = container.Resolve<Logger>();
			_metadataService = container.Resolve<IMetadataService>();
			_archiveService = container.Resolve<IArchiveService>();
			_bufferManager = container.Resolve<IBufferManager>();

			InitializeCommands();
		}

		private void InitializeCommands()
		{
			BrowseFileCommand = new DelegateCommand(BrowseFile, CanExecute);
			SaveCommand = new DelegateCommand(async () => await LaunchSaveAsync(), CanSave);
			ImportCalibreCommand = new DelegateCommand(async () => await ImportCalibreMetadataAsync(), CanImportCalibre);
			ExportCalibreCommand = new DelegateCommand(async () => await ExportCalibreMetadataAsync(), CanExportCalibre);
		}

		private void RefreshCommandStates()
		{
			SaveCommand.RaiseCanExecuteChanged();
			ImportCalibreCommand.RaiseCanExecuteChanged();
			ExportCalibreCommand.RaiseCanExecuteChanged();
		}
		#endregion

		#region Command Handlers
		private void BrowseFile()
		{
			FileToEdit = _fileDialogService.BrowseForInputFile();
		}

		private bool CanExecute() => true;

		private bool CanSave() => !string.IsNullOrEmpty(FileToEdit) && File.Exists(FileToEdit);

		private bool CanImportCalibre() => CanSave();

		private bool CanExportCalibre() => MetadataCollection != null && MetadataCollection.Count > 0;

		private async Task LaunchSaveAsync()
		{
			await ExecuteWithBusyStateAsync(DoSaveAsync);
		}
		#endregion

		#region Core Operations
		private async Task DoSaveAsync()
		{
			var fileInfo = new FileInfo(_fileToEdit);
			var bufferContext = _bufferManager.CreateContext(_fileToEdit, _metadataFile, _calibreMetaDataFile);

			try
			{
				// Save metadata files
				await _metadataService.SaveComicInfoAsync(MetadataCollection, bufferContext.MetadataFilePath);
				
				// Export Calibre metadata if we have existing Calibre file or metadata
				await ExportCalibreMetadataToBuffer(bufferContext);

				// Update archive based on format support
				if (_archiveService.SupportsDirectUpdate(fileInfo.Extension))
				{
					await _archiveService.UpdateArchiveAsync(FileToEdit, bufferContext.MetadataFilePath, bufferContext.CalibreMetaDataFilePath);
				}
				else
				{
					var outputFile = DetermineOutputFile(fileInfo);
					await _archiveService.RecompressArchiveAsync(FileToEdit, bufferContext.BufferPath, outputFile);
				}

				_logger.Log("Save operation completed successfully");
			}
			finally
			{
				_bufferManager.Cleanup(bufferContext.BufferPath);
			}
		}

		private async Task LoadMetadataAsync()
		{
			await ExecuteWithBusyStateAsync(async () =>
			{
				var bufferContext = _bufferManager.CreateContext(_fileToEdit);

				try
				{
					await _archiveService.ExtractMetadataFilesAsync(_fileToEdit, bufferContext.BufferPath);

					var (xmlFiles, htmlFiles) = _archiveService.FindMetadataFiles(bufferContext.BufferDirectory);

					if (xmlFiles.Length > 0)
					{
						await LoadComicInfoMetadataAsync(xmlFiles[0]);
					}
					else if (TryFindCalibreMetadata(htmlFiles, out var calibreFile))
					{
						await LoadCalibreMetadataAsync(calibreFile.FullName);
					}
					else
					{
						_logger.Log("No metadata found. Initializing default metadata.");
						MetadataCollection = _metadataService.InitializeDefaultMetadata();
					}
				}
				finally
				{
					_bufferManager.Cleanup(bufferContext.BufferPath);
				}
			});
		}
		#endregion

		#region Helper Methods
		private async Task ExecuteWithBusyStateAsync(Func<Task> operation)
		{
			_eventAggregator.GetEvent<BusinessEvent>().Publish(true);
			try
			{
				await operation();
			}
			catch (Exception ex)
			{
				_logger.Log($"ERROR: Operation failed: {ex.Message}");
			}
			finally
			{
				_eventAggregator.GetEvent<BusinessEvent>().Publish(false);
			}
		}

		private bool TryFindCalibreMetadata(FileInfo[] htmlFiles, out FileInfo calibreFile)
		{
			calibreFile = null;
			
			foreach (var file in htmlFiles)
			{
				if (_metadataService.ContainsCalibreMetadata(file.FullName))
				{
					calibreFile = file;
					_logger.Log($"Found Calibre metadata in {calibreFile.Name}");
					return true;
				}
			}

			_logger.Log("No Calibre metadata found in HTML files.");
			return false;
		}

		private string DetermineOutputFile(FileInfo fileInfo)
		{
			var settingsExtension = $".{Settings.Instance.OutputFormat.ToString().ToLower()}";
			var outputFile = FileToEdit;

			if (fileInfo.Extension != settingsExtension)
			{
				_logger.Log($"Incorrect extension found in filename {fileInfo.Extension} replaced with {settingsExtension}");
				outputFile = Path.ChangeExtension(outputFile, settingsExtension);
			}

			return outputFile;
		}

		private async Task ExportCalibreMetadataToBuffer(BufferContext bufferContext)
		{
			if (!string.IsNullOrEmpty(_calibreMetaDataFile))
			{
				await _metadataService.SaveCalibreAsync(MetadataCollection, bufferContext.CalibreMetaDataFilePath);
			}
		}
		#endregion

		#region Metadata Loading
		private async Task LoadComicInfoMetadataAsync(FileInfo xmlFile)
		{
			_metadataFile = xmlFile.FullName;
			MetadataCollection = await _metadataService.LoadComicInfoAsync(_metadataFile);
		}

		private async Task LoadCalibreMetadataAsync(string htmlFile)
		{
			_calibreMetaDataFile = htmlFile;
			MetadataCollection = await _metadataService.LoadCalibreAsync(htmlFile);
		}
		#endregion

		#region Calibre Import/Export
		private async Task ImportCalibreMetadataAsync()
		{
			_logger.Log("Starting Calibre metadata import");

			var bufferContext = _bufferManager.CreateContext(_fileToEdit);

			try
			{
				await _archiveService.ExtractMetadataFilesAsync(_fileToEdit, bufferContext.BufferPath);
				var (_, htmlFiles) = _archiveService.FindMetadataFiles(bufferContext.BufferDirectory);

				if (TryFindCalibreMetadata(htmlFiles, out var calibreFile))
				{
					await LoadCalibreMetadataAsync(calibreFile.FullName);
				}
				else
				{
					_logger.Log("No Calibre metadata found in HTML files.");
				}
			}
			finally
			{
				_bufferManager.Cleanup(bufferContext.BufferPath);
			}
		}

		private async Task ExportCalibreMetadataAsync()
		{
			_logger.Log("Starting Calibre metadata export");

			try
			{
				var bufferContext = _bufferManager.CreateContext(_fileToEdit);
				
				try
				{
					await _archiveService.ExtractMetadataFilesAsync(_fileToEdit, bufferContext.BufferPath);
					var (_, htmlFiles) = _archiveService.FindMetadataFiles(bufferContext.BufferDirectory);
					
					var calibreFile = htmlFiles.FirstOrDefault(f => _metadataService.ContainsCalibreMetadata(f.FullName));

					if (calibreFile != null)
					{
						await _metadataService.SaveCalibreAsync(MetadataCollection, calibreFile.FullName);
						_logger.Log("Calibre metadata export completed successfully");
					}
					else
					{
						_logger.Log("No existing Calibre HTML file found for export");
					}
				}
				finally
				{
					_bufferManager.Cleanup(bufferContext.BufferPath);
				}
			}
			catch (Exception ex)
			{
				_logger.Log($"ERROR: Failed to export Calibre metadata: {ex.Message}");
			}
		}
		#endregion
	}
}