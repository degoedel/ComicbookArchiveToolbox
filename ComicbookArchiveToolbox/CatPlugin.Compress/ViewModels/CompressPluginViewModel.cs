using CatPlugin.Compress.Services;
using ComicbookArchiveToolbox.CommonTools;
using Microsoft.WindowsAPICodePack.Dialogs;
using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity;

namespace CatPlugin.Compress.ViewModels
{
	public class CompressPluginViewModel : BindableBase
	{
		private Logger _logger;
		private IUnityContainer _container;

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

		public DelegateCommand BrowseFileCommand { get; private set; }
		public DelegateCommand BrowseOutputFileCommand { get; private set; }
		public DelegateCommand CompressCommand { get; private set; }

		public CompressPluginViewModel(IUnityContainer container)
		{
			_container = container;
			BrowseFileCommand = new DelegateCommand(BrowseFile, CanExecute);
			BrowseOutputFileCommand = new DelegateCommand(BrowseOutputFile, CanExecute);
			CompressCommand = new DelegateCommand(DoCompress, CanCompress);
			_logger = _container.Resolve<Logger>();
		}

		private void BrowseFile()
		{
			_logger.Log("Browse for file to split");

			using (var dialog = new CommonOpenFileDialog())
			{
				dialog.Filters.Add(new CommonFileDialogFilter("Comics Archive files (*.cb7;*.cba;*cbr;*cbt;*.cbz)", "*.cb7;*.cba;*cbr;*cbt;*.cbz"));
				dialog.Filters.Add(new CommonFileDialogFilter("All files (*.*)", "*.*"));
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
				CommonFileDialogResult result = dialog.ShowDialog();
				if (result == CommonFileDialogResult.Ok)
				{
					FileToCompress = dialog.FileName;
				}
			}
		}

		private void BrowseOutputFile()
		{
			using (var dialog = new CommonOpenFileDialog())
			{
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
				CommonFileDialogResult result = dialog.ShowDialog();
				if (result == CommonFileDialogResult.Ok)
				{
					OutputFile = dialog.FileName;
				}
			}
		}

		private bool CanExecute()
		{
			return true;
		}

		private void DoCompress()
		{
			JpgCompresser compresser = new JpgCompresser(_logger);
			Task.Run(() => compresser.Compress(FileToCompress, OutputFile, ImageQuality));
		}

		private bool CanCompress()
		{
			return (!string.IsNullOrWhiteSpace(FileToCompress) && !string.IsNullOrWhiteSpace(OutputFile));
		}

	}

}
