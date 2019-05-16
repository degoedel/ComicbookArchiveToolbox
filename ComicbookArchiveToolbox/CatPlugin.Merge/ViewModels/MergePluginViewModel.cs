using CatPlugin.Merge.Service;
using ComicbookArchiveToolbox.CommonTools;
using Microsoft.WindowsAPICodePack.Dialogs;
using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity;

namespace CatPlugin.Merge.ViewModels
{
  public class MergePluginViewModel : BindableBase
  {
		private IUnityContainer _container;
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

		private List<string> _selectedFiles = new List<string>();
		public List<string> SelectedFiles
		{
			get { return _selectedFiles; }
			set
			{
				SetProperty(ref _selectedFiles, value);
				MergeCommand.RaiseCanExecuteChanged();
			}
		}

		public MergePluginViewModel(IUnityContainer container)
		{
			_container = container;
			_logger = _container.Resolve<Logger>();
			BrowseFilesCommand = new DelegateCommand(BrowseFiles, CanExecute);
			ClearFilesCommand = new DelegateCommand(ClearFiles, CanExecute);
			MergeCommand = new DelegateCommand(DoMerge, CanMerge);
			BrowseOutputFileCommand = new DelegateCommand(BrowseOutputFile, CanExecute);
		}

		private void BrowseFiles()
		{
			using (var dialog = new CommonOpenFileDialog())
			{
				dialog.Filters.Add(new CommonFileDialogFilter("Comics Archive files (*.cb7;*.cba;*cbr;*cbt;*.cbz)", "*.cb7;*.cba;*cbr;*cbt;*.cbz"));
				dialog.Filters.Add(new CommonFileDialogFilter("All files (*.*)", "*.*"));
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
				CommonFileDialogResult result = dialog.ShowDialog();
				if (result == CommonFileDialogResult.Ok)
				{
					List<string> newValue = new List<string>(SelectedFiles);
					List<string> dialogSelection = dialog.FileNames.ToList();
					dialogSelection.Sort();
					newValue.AddRange(dialogSelection);
					SelectedFiles = newValue;
				}
			}
		}

		private void ClearFiles()
		{
			SelectedFiles = new List<string>();
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
				else
				{
					if (SelectedFiles != null && SelectedFiles.Count > 0)
					{
						outputPath = (new FileInfo(SelectedFiles[0])).Directory.FullName;
					}
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

		private void DoMerge()
		{
      Merger merger = new Merger(_logger);
      Task.Run(() => merger.Merge(OutputFile, SelectedFiles));
		}

		private bool CanMerge()
		{
			return (SelectedFiles != null && SelectedFiles.Count > 1 && !string.IsNullOrWhiteSpace(OutputFile));
		}


	}

}
