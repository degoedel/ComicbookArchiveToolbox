using System;
using System.IO;

namespace ComicbookArchiveToolbox.Services
{
	public interface IPathConflictService
	{
		string ResolveOutputPathConflict(string outputFile, string inputFile, string suffix = "_processed");
		bool PathsAreEqual(string file1, string file2);
	}

	public class PathConflictService : IPathConflictService
	{
		public string ResolveOutputPathConflict(string outputFile, string inputFile, string suffix = "_processed")
		{
			if (!PathsAreEqual(outputFile, inputFile))
				return outputFile;

			return GenerateAlternativePathName(outputFile, suffix, inputFile);
		}

		public bool PathsAreEqual(string file1, string file2)
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

		private string GenerateAlternativePathName(string originalPath, string suffix, string conflictFile)
		{
			// Determine if this is a directory or file path
			bool isDirectory = IsDirectoryPath(originalPath);

			if (isDirectory)
			{
				return GenerateAlternativeDirectoryName(originalPath, suffix, conflictFile);
			}
			else
			{
				return GenerateAlternativeFileName(originalPath, suffix, conflictFile);
			}
		}

		private bool IsDirectoryPath(string path)
		{
			try
			{
				// First check if the path actually exists
				if (Directory.Exists(path))
					return true;
				if (File.Exists(path))
					return false;

				// If path doesn't exist, make educated guess based on characteristics
				// Directories typically don't have extensions or end with path separators
				string fileName = Path.GetFileName(path);

				// If path ends with separator, it's likely a directory
				if (path.EndsWith(Path.DirectorySeparatorChar) || path.EndsWith(Path.AltDirectorySeparatorChar))
					return true;

				// If no extension and no dot in filename, likely a directory
				if (string.IsNullOrEmpty(Path.GetExtension(fileName)) && !fileName.Contains('.'))
					return true;

				// Otherwise assume it's a file
				return false;
			}
			catch
			{
				// Default to file if we can't determine
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
			while (File.Exists(newPath) || PathsAreEqual(newPath, conflictFile))
			{
				newFileName = $"{fileNameWithoutExtension}{suffix}_{counter}{extension}";
				newPath = Path.Combine(directory, newFileName);
				counter++;
			}

			return newPath;
		}

		private string GenerateAlternativeDirectoryName(string originalPath, string suffix, string conflictFile)
		{
			string parentDirectory = Path.GetDirectoryName(originalPath) ?? "";
			string directoryName = Path.GetFileName(originalPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));

			string newDirectoryName = $"{directoryName}{suffix}";
			string newPath = Path.Combine(parentDirectory, newDirectoryName);

			int counter = 1;
			while (Directory.Exists(newPath) || PathsAreEqual(newPath, conflictFile))
			{
				newDirectoryName = $"{directoryName}{suffix}_{counter}";
				newPath = Path.Combine(parentDirectory, newDirectoryName);
				counter++;
			}

			return newPath;
		}
	}
}
