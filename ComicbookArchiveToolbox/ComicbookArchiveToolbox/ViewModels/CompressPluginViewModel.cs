using ComicbookArchiveToolbox.Services;
using ComicbookArchiveToolbox.CommonTools;
using Prism.Commands;
using Prism.Events;
using System.Threading.Tasks;
using Unity;
using System.IO;

namespace ComicbookArchiveToolbox.ViewModels
{
	public class CompressPluginViewModel : BasePluginViewModel
	{
		private string _fileToCompress = "";
		public string FileToCompress
		{
			get => _fileToCompress;
			set
			{
				SetProperty(ref _fileToCompress, value);
				CompressCommand.RaiseCanExecuteChanged();
				EnsureOutputFileNameIsDifferent();
			}
		}

		private string _outputFile = "";
		public string OutputFile
		{
			get => _outputFile;
			set
			{
				SetProperty(ref _outputFile, value);
				CompressCommand.RaiseCanExecuteChanged();
			}
		}

		private bool _strictRatio = true;
		public bool StrictRatio
		{
			get => _strictRatio;
			set => SetProperty(ref _strictRatio, value);
		}

		private long _imageRatio = 100;
		public long ImageRatio
		{
			get => _imageRatio;
			set => SetProperty(ref _imageRatio, value);
		}

		private long _imageHeight = Settings.Instance.DefaultImageHeight;
		public long ImageHeight
		{
			get => _imageHeight;
			set => SetProperty(ref _imageHeight, value);
		}

		public DelegateCommand CompressCommand { get; private set; }

		public CompressPluginViewModel(IUnityContainer container, IEventAggregator eventAggregator)
			: base(container, eventAggregator)
		{
			CompressCommand = new DelegateCommand(DoCompress, CanCompress);
			ImageHeight = Settings.Instance.DefaultImageHeight;
		}

		private void EnsureOutputFileNameIsDifferent()
		{
			if (!string.IsNullOrWhiteSpace(FileToCompress) && !string.IsNullOrWhiteSpace(OutputFile))
			{
				var resolvedFile = _fileConflictService.ResolveOutputFileConflict(OutputFile, FileToCompress, "_compressed");
				if (resolvedFile != OutputFile)
				{
					_logger.Log($"Input file conflicts with output file. Changed output to: {Path.GetFileName(resolvedFile)}");
					OutputFile = resolvedFile;
				}
			}
		}

		private async void DoCompress()
		{
			var compresser = new CompressorPlugin(_logger, _eventAggregator);
			Task.Run(() => compresser.Compress(FileToCompress, OutputFile, ImageQuality, StrictRatio, ImageHeight, ImageRatio));
		}

		private bool CanCompress() =>
			!string.IsNullOrWhiteSpace(FileToCompress) && !string.IsNullOrWhiteSpace(OutputFile);

		// Base class implementations
		protected override string GetCurrentInputFile() => FileToCompress;
		protected override string GetCurrentOutputFile() => OutputFile;
		protected override void SetInputFile(string file) => FileToCompress = file;
		protected override void SetOutputFile(string file) => OutputFile = file;
		protected override string GetOutputSuffix() => "_compressed";
	}
}