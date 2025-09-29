using ComicbookArchiveToolbox.CommonTools;
using ComicbookArchiveToolbox.Services;
using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Input;
using Unity;

namespace ComicbookArchiveToolbox.ViewModels
{
	public abstract class BasePluginViewModel : BindableBase
	{
		protected readonly Logger _logger;
		protected readonly IUnityContainer _container;
		protected readonly IEventAggregator _eventAggregator;
		protected readonly IFileDialogService _fileDialogService;
		protected readonly IPathConflictService _fileConflictService;

		private long _imageQuality = 80;
		public long ImageQuality
		{
			get => _imageQuality;
			set
			{
				value = Math.Clamp(value, 0, 100);
				SetProperty(ref _imageQuality, value);
			}
		}

		public DelegateCommand BrowseFileCommand { get; protected set; }

		public DelegateCommand BrowseFilesCommand {  get; protected set; }
		public DelegateCommand BrowseOutputFileCommand { get; protected set; }

		public DelegateCommand BrowseOutputDirectoryCommand { get; set; }

		private bool _isBatchMode = false;
		public bool IsBatchMode
		{
			get => _isBatchMode;
			set
			{
				SetProperty(ref _isBatchMode, value);
				SetInputPath("");
				SetOutputPath("");
			}
		}

		protected BasePluginViewModel(IUnityContainer container, IEventAggregator eventAggregator)
		{
			_container = container;
			_eventAggregator = eventAggregator;
			_logger = container.Resolve<Logger>();
			_fileDialogService = container.Resolve<IFileDialogService>();
			_fileConflictService = container.Resolve<IPathConflictService>();

			BrowseFileCommand = new DelegateCommand(BrowseInput, CanExecute);
			BrowseFilesCommand = new DelegateCommand(BrowseInputs, CanExecute);
			BrowseOutputFileCommand = new DelegateCommand(BrowseOutput, CanExecute);
			BrowseOutputDirectoryCommand = new DelegateCommand(BrowseOutputDirectory, CanExecute);
		}

		protected virtual void BrowseInput()
		{
			var inputType = IsBatchMode ? "folder" : "file";
			_logger.Log($"Browse for input {inputType}");
			string? result;
			if (IsBatchMode)
			{
				result = _fileDialogService.BrowseForDirectory(GetCurrentInputPath());
			}
			else
			{
				result = _fileDialogService.BrowseForInputFile(GetCurrentInputPath());
			}
			if (result != null)
			{
				SetInputPath(result);
			}
		}

		protected virtual void BrowseInputs()
		{
			_logger.Log($"Browse for input files");
			IList<string> result;
			result = _fileDialogService.BrowseForInputMultiFiles(GetCurrentInputPath());
			result = result.OrderBy(x => x).ToList();
			SetInputSelectedFiles(result);
		}

		protected virtual void BrowseOutput()
		{
			if (IsBatchMode)
			{
				BrowseOutputDirectory();
			}
			else
			{
				BrowseOutputFile();
			}
		}

		protected virtual void BrowseOutputFile()
		{
			_logger.Log($"Browse for output file");
			string? result;
			result = _fileDialogService.BrowseForOutputFile(GetCurrentOutputPath(), GetCurrentInputPath());
			SetOutput(result);
		}

		protected virtual void BrowseOutputDirectory()
		{
			_logger.Log($"Browse for output folder");
			string? result;
			result = _fileDialogService.BrowseForDirectory(GetCurrentInputPath());
			SetOutput(result);
		}

		private void SetOutput(string result)
		{
			if (result != null)
			{
				var resolvedFile = _fileConflictService.ResolveOutputPathConflict(result, GetCurrentInputPath(), GetOutputSuffix());
				if (resolvedFile != result)
				{
					_logger.Log($"Output file conflict resolved. Changed to: {Path.GetFileName(resolvedFile)}");
				}
				SetOutputPath(resolvedFile);
			}
		}

		protected virtual bool CanExecute() => true;

		// Abstract methods to be implemented by derived classes
		protected abstract string GetCurrentInputPath();
		protected abstract string GetCurrentOutputPath();
		protected abstract void SetInputPath(string file);
		protected abstract void SetInputSelectedFiles(IList<string> files);
		protected abstract void SetOutputPath(string file);
		protected abstract string GetOutputSuffix();
	}
}
