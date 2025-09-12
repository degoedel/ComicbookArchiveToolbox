using System;
using System.IO;

namespace ComicbookArchiveToolbox.Services
{
	public interface IFileConflictService
	{
		string ResolveOutputFileConflict(string outputFile, string inputFile, string suffix = "_processed");
		bool FilesAreEqual(string file1, string file2);
	}

	public class FileConflictService : IFileConflictService
	{
		public string ResolveOutputFileConflict(string outputFile, string inputFile, string suffix = "_processed")
		{
			if (!FilesAreEqual(outputFile, inputFile))
				return outputFile;

			return GenerateAlternativeFileName(outputFile, suffix, inputFile);
		}

		public bool FilesAreEqual(string file1, string file2)
		{
			if (string.IsNullOrWhiteSpace(file1) || string.IsNullOrWhiteSpace(file2))
				return false;

			try
			{
				return Path.GetFullPath(file1).Equals(Path.GetFullPath(file2), StringComparison.OrdinalIgnoreCase);
			}
			catch
			{
				return false;
			}
		}

		private string GenerateAlternativeFileName(string originalPath, string suffix, string conflictFile)
		{
			string directory = Path.GetDirectoryName(originalPath) ?? "";
			string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(originalPath);
			string extension = Path.GetExtension(originalPath);

			string newFileName = $"{fileNameWithoutExtension}{suffix}{extension}";
			string newPath = Path.Combine(directory, newFileName);

			int counter = 1;
			while (File.Exists(newPath) || FilesAreEqual(newPath, conflictFile))
			{
				newFileName = $"{fileNameWithoutExtension}{suffix}_{counter}{extension}";
				newPath = Path.Combine(directory, newFileName);
				counter++;
			}

			return newPath;
		}
	}
}
