using ComicbookArchiveToolbox.Module.Merge.Service;
using ComicbookArchiveToolbox.CommonTools;
using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Unity;

namespace ComicbookArchiveToolbox.Module.Merge.ViewModels
{
	public class MergePluginViewModel : BindableBase
	{
		private IUnityContainer _container;
		private IEventAggregator _eventAggregator;
		private Logger _logger;
		public DelegateCommand BrowseFilesCommand { get; private set; }
		public DelegateCommand ClearFilesCommand { get; private set; }
		public DelegateCommand BrowseOutputFileCommand { get; private set; }
		public DelegateCommand MergeCommand { get; private set; }

		private string _outputFile = "";
		public string OutputFile
		{
			get { return _outputFile; }
			set
			{
				SetProperty(ref _outputFile, value);
				MergeCommand.RaiseCanExecuteChanged();
			}
		}

		private ObservableCollection<string> _selectedFiles = new ObservableCollection<string>();
		public ObservableCollection<string> SelectedFiles
		{
			get { return _selectedFiles; }
			set
			{
				SetProperty(ref _selectedFiles, value);
				MergeCommand.RaiseCanExecuteChanged();
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

		public MergePluginViewModel(IUnityContainer container, IEventAggregator eventAggregator)
		{
			_container = container;
			_eventAggregator = eventAggregator;
			_logger = _container.Resolve<Logger>();
			BrowseFilesCommand = new DelegateCommand(BrowseFiles, CanExecute);
			ClearFilesCommand = new DelegateCommand(ClearFiles, CanExecute);
			MergeCommand = new DelegateCommand(DoMerge, CanMerge);
			BrowseOutputFileCommand = new DelegateCommand(BrowseOutputFile, CanExecute);
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Interoperability", "CA1416:Validate platform compatibility", Justification = "<Pending>")]
		private void BrowseFiles()
		{
			var dialog = new Microsoft.Win32.OpenFileDialog();
			dialog.Filter = "Comics Archive files (*.cb7;*.cba;*cbr;*cbt;*.cbz)|*.cb7;*.cba;*cbr;*cbt;*.cbz";
			string defaultPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
			if (SelectedFiles != null && SelectedFiles.Count > 0)
			{
				try
				{
					FileInfo fi = new FileInfo(SelectedFiles[0]);
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
			dialog.Multiselect = true;
			bool? result = dialog.ShowDialog();
			if (result.HasValue && result.Value == true)
			{
				List<string> dialogSelection = dialog.FileNames.ToList();
				dialogSelection.Sort();
				ClearFiles();
				SelectedFiles.AddRange(dialogSelection);
			}
		}

		private void ClearFiles()
		{
			SelectedFiles.Clear();
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
			else
			{
				if (SelectedFiles != null && SelectedFiles.Count > 0)
				{
					outputPath = (new FileInfo(SelectedFiles[0])).Directory.FullName;
				}
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

		private void DoMerge()
		{
			Merger merger = new Merger(_logger, _eventAggregator);
			Task.Run(() => merger.Merge(OutputFile, SelectedFiles, ImageQuality));
		}

		private bool CanMerge()
		{
			return (SelectedFiles != null && SelectedFiles.Count > 1 && !string.IsNullOrWhiteSpace(OutputFile));
		}


	}

}
