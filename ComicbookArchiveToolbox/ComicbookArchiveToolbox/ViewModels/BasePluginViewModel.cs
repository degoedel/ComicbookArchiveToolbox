using ComicbookArchiveToolbox.CommonTools;
using ComicbookArchiveToolbox.Services;
using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using System;
using System.IO;
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
		public DelegateCommand BrowseOutputFileCommand { get; protected set; }

		private bool _isBatchMode = false;
		public bool IsBatchMode
		{
			get => _isBatchMode;
			set => SetProperty(ref _isBatchMode, value);
		}

		protected BasePluginViewModel(IUnityContainer container, IEventAggregator eventAggregator)
		{
			_container = container;
			_eventAggregator = eventAggregator;
			_logger = container.Resolve<Logger>();
			_fileDialogService = container.Resolve<IFileDialogService>();
			_fileConflictService = container.Resolve<IPathConflictService>();

			BrowseFileCommand = new DelegateCommand(BrowseInput, CanExecute);
			BrowseOutputFileCommand = new DelegateCommand(BrowseOutput, CanExecute);
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

		protected virtual void BrowseOutput()
		{
			var outputType = IsBatchMode ? "folder" : "file";
			_logger.Log($"Browse for output {outputType}");
			string? result;
			if (IsBatchMode)
			{
				result = _fileDialogService.BrowseForDirectory( GetCurrentInputPath());
			}
			else
			{
				result = _fileDialogService.BrowseForOutputFile(GetCurrentOutputPath(), GetCurrentInputPath());
			}
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
		protected abstract void SetOutputPath(string file);
		protected abstract string GetOutputSuffix();
	}
}
