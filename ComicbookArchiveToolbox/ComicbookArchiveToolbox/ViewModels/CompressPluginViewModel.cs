using ComicbookArchiveToolbox.CommonTools;
using ComicbookArchiveToolbox.Services;
using Prism.Commands;
using Prism.Events;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Documents;
using Unity;

namespace ComicbookArchiveToolbox.ViewModels
{
	public class CompressPluginViewModel : BasePluginViewModel
	{
		private static readonly string[] ComicExtensions = { ".cb7", ".cba", ".cbr", ".cbt", ".cbz" };

		private string _inputPathToCompress = "";
		public string InputPathToCompress
		{
			get => _inputPathToCompress;
			set
			{
				SetProperty(ref _inputPathToCompress, value);
				CompressCommand.RaiseCanExecuteChanged();
				EnsureOutputFileNameIsDifferent();
			}
		}

		private string _outputPath = "";
		public string OutputPath
		{
			get => _outputPath;
			set
			{
				SetProperty(ref _outputPath, value);
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
			if (!string.IsNullOrWhiteSpace(InputPathToCompress) && !string.IsNullOrWhiteSpace(OutputPath))
			{
				var resolvedFile = _fileConflictService.ResolveOutputPathConflict(OutputPath, InputPathToCompress, "_compressed");
				if (resolvedFile != OutputPath)
				{
					_logger.Log($"Input file conflicts with output file. Changed output to: {Path.GetFileName(resolvedFile)}");
					OutputPath = resolvedFile;
				}
			}
		}

		private async void DoCompress()
		{
			var compresser = new CompressorPlugin(_logger, _eventAggregator);
			if (IsBatchMode)
			{
				DirectoryInfo batchSource = new DirectoryInfo(InputPathToCompress);

				// Get all files with comic book archive extensions
				List<FileInfo> batch = batchSource.GetFiles()
					.Where(file => ComicExtensions.Contains(file.Extension.ToLowerInvariant()))
					.ToList();

				_logger.Log($"Found {batch.Count} comic archive files in directory: {InputPathToCompress}");

				// Process each file in the batch
				foreach (var file in batch)
				{
					string outputFile = Path.Combine(OutputPath, Path.GetFileNameWithoutExtension(file.Name) + "_compressed" + file.Extension);
					_logger.Log($"Processing: {file.Name} -> {Path.GetFileName(outputFile)}");

					await Task.Run(() => compresser.Compress(file.FullName, outputFile, ImageQuality, StrictRatio, ImageHeight, ImageRatio));
				}
			}
			else
			{
				await Task.Run(() => compresser.Compress(InputPathToCompress, OutputPath, ImageQuality, StrictRatio, ImageHeight, ImageRatio));
			}
		}

		private bool CanCompress() =>
			!string.IsNullOrWhiteSpace(InputPathToCompress) && !string.IsNullOrWhiteSpace(OutputPath);

		// Base class implementations
		protected override string GetCurrentInputPath() => InputPathToCompress;
		protected override string GetCurrentOutputPath() => OutputPath;
		protected override void SetInputPath(string file) => InputPathToCompress = file;
		protected override void SetOutputPath(string file) => OutputPath = file;
		protected override string GetOutputSuffix() => "_compressed";
	}
}