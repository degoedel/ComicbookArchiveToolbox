using ComicbookArchiveToolbox.Services;
using ComicbookArchiveToolbox.CommonTools;
using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using System;
using System.IO;
using System.Threading.Tasks;
using Unity;

namespace ComicbookArchiveToolbox.ViewModels
{
	public class CompressPluginViewModel : BindableBase
	{
		private Logger _logger;
		private IUnityContainer _container;
		private IEventAggregator _eventAggregator;

		private string _fileToCompress = "";
		public string FileToCompress
		{
			get { return _fileToCompress; }
			set
			{
				SetProperty(ref _fileToCompress, value);
				CompressCommand.RaiseCanExecuteChanged();
			}
		}

		private string _outputFile = "";
		public string OutputFile
		{
			get { return _outputFile; }
			set
			{
				SetProperty(ref _outputFile, value);
				CompressCommand.RaiseCanExecuteChanged();
			}
		}

		private long _imageQuality = 80;
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

		private bool _strictRatio = true;
		public bool StrictRatio
		{
			get { return _strictRatio; }
			set
			{
				SetProperty(ref _strictRatio, value);
			}
		}

		private long _imageRatio = 100;
		public long ImageRatio
		{
			get { return _imageRatio; }
			set
			{
				SetProperty(ref _imageRatio, value);
			}
		}

		private long _imageHeight = Settings.Instance.DefaultImageHeight;
		public long ImageHeight
		{
			get { return _imageHeight; }
			set
			{
				SetProperty(ref _imageHeight, value);
			}
		}

		public DelegateCommand BrowseFileCommand { get; private set; }
		public DelegateCommand BrowseOutputFileCommand { get; private set; }
		public DelegateCommand CompressCommand { get; private set; }

		public CompressPluginViewModel(IUnityContainer container, IEventAggregator eventAggregator)
		{
			_container = container;
			_eventAggregator = eventAggregator;
			BrowseFileCommand = new DelegateCommand(BrowseFile, CanExecute);
			BrowseOutputFileCommand = new DelegateCommand(BrowseOutputFile, CanExecute);
			CompressCommand = new DelegateCommand(DoCompress, CanCompress);
			_logger = _container.Resolve<Logger>();
			ImageHeight = Settings.Instance.DefaultImageHeight;
		}

		private void BrowseFile()
		{
			_logger.Log("Browse for file to compress");

			var dialog = new Microsoft.Win32.OpenFileDialog();
			dialog.Filter = "Comics Archive files (*.cb7;*.cba;*cbr;*cbt;*.cbz)|*.cb7;*.cba;*cbr;*cbt;*.cbz";
			string defaultPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
			if (!string.IsNullOrEmpty(_fileToCompress))
			{
				try
				{
					FileInfo fi = new FileInfo(_fileToCompress);
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
				FileToCompress = dialog.FileName;
			}
		}

		private void BrowseOutputFile()
		{
			var dialog = new Microsoft.Win32.SaveFileDialog();
			string outputPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
			if (!string.IsNullOrWhiteSpace(OutputFile))
			{
				outputPath = (new FileInfo(OutputFile)).Directory.FullName;
				dialog.InitialDirectory = outputPath;
			}
			else if (!string.IsNullOrWhiteSpace(FileToCompress))
			{
				outputPath = (new FileInfo(FileToCompress)).Directory.FullName;
			}
			dialog.InitialDirectory = outputPath;
			bool? result = dialog.ShowDialog();
			if (result.HasValue && result.Value == true)
			{
				OutputFile = dialog.FileName;
			}
		}

		private bool CanExecute()
		{
			return true;
		}

		private void DoCompress()
		{
			JpgCompresser compresser = new JpgCompresser(_logger, _eventAggregator);
			Task.Run(() => compresser.Compress(FileToCompress, OutputFile, ImageQuality, StrictRatio, ImageHeight, ImageRatio));
		}

		private bool CanCompress()
		{
			return (!string.IsNullOrWhiteSpace(FileToCompress) && !string.IsNullOrWhiteSpace(OutputFile));
		}

	}

}
