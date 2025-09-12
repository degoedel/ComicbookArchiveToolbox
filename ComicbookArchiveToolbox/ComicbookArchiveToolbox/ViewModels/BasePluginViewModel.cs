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
		protected readonly IFileConflictService _fileConflictService;

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

		protected BasePluginViewModel(IUnityContainer container, IEventAggregator eventAggregator)
		{
			_container = container;
			_eventAggregator = eventAggregator;
			_logger = container.Resolve<Logger>();
			_fileDialogService = container.Resolve<IFileDialogService>();
			_fileConflictService = container.Resolve<IFileConflictService>();

			BrowseFileCommand = new DelegateCommand(BrowseFile, CanExecute);
			BrowseOutputFileCommand = new DelegateCommand(BrowseOutputFile, CanExecute);
		}

		protected virtual void BrowseFile()
		{
			_logger.Log("Browse for input file");
			var result = _fileDialogService.BrowseForInputFile(GetCurrentInputFile());
			if (result != null)
			{
				SetInputFile(result);
			}
		}

		protected virtual void BrowseOutputFile()
		{
			var result = _fileDialogService.BrowseForOutputFile(GetCurrentOutputFile(), GetCurrentInputFile());
			if (result != null)
			{
				var resolvedFile = _fileConflictService.ResolveOutputFileConflict(result, GetCurrentInputFile(), GetOutputSuffix());
				if (resolvedFile != result)
				{
					_logger.Log($"Output file conflict resolved. Changed to: {Path.GetFileName(resolvedFile)}");
				}
				SetOutputFile(resolvedFile);
			}
		}

		protected virtual bool CanExecute() => true;

		// Abstract methods to be implemented by derived classes
		protected abstract string GetCurrentInputFile();
		protected abstract string GetCurrentOutputFile();
		protected abstract void SetInputFile(string file);
		protected abstract void SetOutputFile(string file);
		protected abstract string GetOutputSuffix();
	}
}
