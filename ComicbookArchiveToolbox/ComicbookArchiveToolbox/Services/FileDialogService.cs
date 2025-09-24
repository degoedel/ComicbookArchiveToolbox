using Microsoft.Win32;
using System;
using System.IO;

namespace ComicbookArchiveToolbox.Services
{
	public interface IFileDialogService
	{
		string? BrowseForInputFile(string currentFile = "", string filter = "");
		string? BrowseForOutputFile(string currentFile = "", string inputFile = "", string filter = "");
		string? BrowseForDirectory(string currentDirectory = "");
	}

	public class FileDialogService : IFileDialogService
	{
		private const string ComicsFilter = "Comics Archive files (*.cb7;*.cba;*cbr;*cbt;*.cbz)|*.cb7;*.cba;*cbr;*cbt;*.cbz";
		private const string AllFilesFilter = "All files (*.*)|*.*";

		public string? BrowseForInputFile(string currentFile = "", string filter = "")
		{
			var dialog = new OpenFileDialog
			{
				Filter = string.IsNullOrEmpty(filter) ? ComicsFilter : filter,
				InitialDirectory = GetInitialDirectory(currentFile)
			};

			return dialog.ShowDialog() == true ? dialog.FileName : null;
		}

		public string? BrowseForOutputFile(string currentFile = "", string inputFile = "", string filter = "")
		{
			var dialog = new SaveFileDialog
			{
				Filter = string.IsNullOrEmpty(filter) ? AllFilesFilter : filter,
				InitialDirectory = GetOutputInitialDirectory(currentFile, inputFile)
			};

			return dialog.ShowDialog() == true ? dialog.FileName : null;
		}

		public string? BrowseForDirectory(string currentDirectory = "")
		{
			var folderDialog = new OpenFolderDialog
			{
				Title = "Select Folder",
				InitialDirectory = currentDirectory
			};

			if (folderDialog.ShowDialog() == true)
			{
				currentDirectory = folderDialog.FolderName;
			}
			return currentDirectory;
		}

		private static string GetInitialDirectory(string currentFile)
		{
			if (!string.IsNullOrEmpty(currentFile))
			{
				try
				{
					var fileInfo = new FileInfo(currentFile);
					if (Directory.Exists(fileInfo.DirectoryName))
						return fileInfo.DirectoryName;
				}
				catch
				{
					// Fall through to default
				}
			}
			return Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
		}

		private static string GetOutputInitialDirectory(string currentOutput, string inputFile)
		{
			if (!string.IsNullOrWhiteSpace(currentOutput))
				return new FileInfo(currentOutput).Directory?.FullName ?? "";

			if (!string.IsNullOrWhiteSpace(inputFile))
				return new FileInfo(inputFile).Directory?.FullName ?? "";

			return Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
		}
	}
}
