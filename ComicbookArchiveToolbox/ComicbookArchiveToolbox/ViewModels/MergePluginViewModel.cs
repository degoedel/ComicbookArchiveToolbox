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
using ComicbookArchiveToolbox.ViewModels;

namespace ComicbookArchiveToolbox.Module.Merge.ViewModels
{
	public class MergePluginViewModel : BasePluginViewModel
	{
		private readonly IUnityContainer _container;
		private readonly IEventAggregator _eventAggregator;
		private readonly Logger _logger;
		public DelegateCommand ClearFilesCommand { get; private set; }
		public DelegateCommand MergeCommand { get; private set; }

		public string InputPath { get; private set; }

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

		private ObservableCollection<string> _selectedFiles = [];
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
			: base(container, eventAggregator)
		{
			_container = container;
			_eventAggregator = eventAggregator;
			_logger = _container.Resolve<Logger>();
			ClearFilesCommand = new DelegateCommand(ClearFiles, CanExecute);
			MergeCommand = new DelegateCommand(DoMerge, CanMerge);
		}

		private void ClearFiles()
		{
			SelectedFiles.Clear();
		}


		private bool CanExecute()
		{
			return true;
		}

		private void DoMerge()
		{
			MergerPlugin merger = new(_logger, _eventAggregator);
			Task.Run(() => merger.Merge(OutputFile, SelectedFiles, ImageQuality));
		}

		private bool CanMerge()
		{
			return (SelectedFiles != null && SelectedFiles.Count > 1 && !string.IsNullOrWhiteSpace(OutputFile));
		}

		protected override string GetCurrentInputPath() => InputPath;
		protected override string GetCurrentOutputPath() => OutputFile;
		protected override void SetInputPath(string file) { }
		protected override void SetOutputPath(string file) => OutputFile = file;
		protected override string GetOutputSuffix() => "_merged";

		protected override void SetInputSelectedFiles(IList<string> files)
		{
			if (files != null && files.Count > 0)
			{
				ClearFiles();
				SelectedFiles.AddRange(files);
				FileInfo fi = new(SelectedFiles[0]);
				string selectedDir = fi.DirectoryName;
				if (Directory.Exists(selectedDir))
				{
					InputPath = selectedDir;
				}
			}
		}
	}

}
