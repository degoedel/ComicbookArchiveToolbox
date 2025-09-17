using Prism.Navigation.Regions.Behaviors;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace ComicbookArchiveToolbox.CommonTools
{
	public class SystemTools
	{
		public static void CleanDirectory(string dirPath, Logger logger)
		{
			try
			{
				DirectoryInfo di = new DirectoryInfo(dirPath);
				var subDirs = di.GetDirectories();
				for (int i = 0; i < subDirs.Count(); ++i)
				{
					if (subDirs[i].Exists)
					{
						SystemTools.CleanDirectory(subDirs[i].FullName, logger);
					}
				}
				var files = di.GetFiles();
				for (int i = 0; i < files.Count(); ++i)
				{
					if (files[i].Exists)
					{
						files[i].Delete();
					}
				}
				di.Delete();
			}
			catch (Exception e)
			{
				if (logger != null)
				{
					logger.Log($"WARNING: unable to clean buffer directory {dirPath}: {e.Message}");
				}
			}
		}

		/// <summary>
		/// Determines if a file is an image file based on its extension.
		/// </summary>
		/// <param name="fileName">The file name or path to check.</param>
		/// <returns>True if the file has an image extension, false otherwise.</returns>
		public static bool IsImageFile(FileInfo fileName)
		{
			string extension = fileName.Extension.ToLowerInvariant();

			return extension switch
			{
				".jpg" or ".jpeg" or ".png" or ".gif" or ".bmp" or ".webp" or ".tiff" or ".tif" => true,
				_ => false
			};
		}

		/// <summary>
		/// Custom comparer for natural/numeric sorting of filenames
		/// </summary>
		private class NaturalFileInfoComparer : IComparer<FileInfo>
		{
			public int Compare(FileInfo x, FileInfo y)
			{
				if (x == null && y == null) return 0;
				if (x == null) return -1;
				if (y == null) return 1;

				return NaturalStringCompare(x.Name, y.Name);
			}

			private static int NaturalStringCompare(string x, string y)
			{
				if (string.IsNullOrEmpty(x) && string.IsNullOrEmpty(y)) return 0;
				if (string.IsNullOrEmpty(x)) return -1;
				if (string.IsNullOrEmpty(y)) return 1;

				// Split strings into parts (text and numbers)
				var xParts = SplitIntoNaturalParts(x);
				var yParts = SplitIntoNaturalParts(y);

				int minLength = Math.Min(xParts.Count, yParts.Count);

				for (int i = 0; i < minLength; i++)
				{
					var xPart = xParts[i];
					var yPart = yParts[i];

					// If both parts are numbers, compare numerically
					if (long.TryParse(xPart, out long xNum) && long.TryParse(yPart, out long yNum))
					{
						int numCompare = xNum.CompareTo(yNum);
						if (numCompare != 0) return numCompare;
					}
					else
					{
						// Compare as strings (case-insensitive)
						int stringCompare = string.Compare(xPart, yPart, StringComparison.OrdinalIgnoreCase);
						if (stringCompare != 0) return stringCompare;
					}
				}

				// If all compared parts are equal, shorter string comes first
				return xParts.Count.CompareTo(yParts.Count);
			}

			private static List<string> SplitIntoNaturalParts(string input)
			{
				var parts = new List<string>();
				var regex = new Regex(@"(\d+|\D+)");
				var matches = regex.Matches(input);

				foreach (Match match in matches)
				{
					parts.Add(match.Value);
				}

				return parts;
			}
		}

		public static void ParseArchiveFiles(string pathToBuffer, ref List<FileInfo> metadataFiles, ref List<FileInfo> pages)
		{
			DirectoryInfo di = new DirectoryInfo(pathToBuffer);
			ParseArchiveFiles(di, ref metadataFiles, ref pages);
		}

		private static void ParseArchiveFiles(DirectoryInfo di, ref List<FileInfo> metadataFiles, ref List<FileInfo> pages)
		{
			var comparer = new NaturalFileInfoComparer();

			// Add and sort files with natural sorting
			pages.AddRange(di.GetFiles().OrderBy(f => f, comparer));
			metadataFiles.AddRange(di.GetFiles().Where(x => !SystemTools.IsImageFile(x)).OrderBy(f => f, comparer));

			var subdirs = di.GetDirectories();
			if (subdirs.Count() > 0)
			{
				foreach (DirectoryInfo subdir in subdirs)
				{
					ParseArchiveFiles(subdir, ref metadataFiles, ref pages);
				}
			}
		}

		public static List<string> GetSubDirs(string pathToBuffer, FileInfo fileInfo)
		{
			try
			{
				// Use Path.GetRelativePath for better performance and reliability
				string relativePath = Path.GetRelativePath(pathToBuffer, fileInfo.Directory?.FullName ?? string.Empty);

				// If the relative path goes up (..) or is empty/current directory, return empty list
				if (relativePath == "." || relativePath.StartsWith(".."))
				{
					return new List<string>();
				}

				// Split the relative path into directory components
				return relativePath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries).ToList();
			}
			catch (Exception)
			{
				return new List<string>();
			}
		}

		public static string GetOutputFilePath(string pathToBuffer, FileInfo fileInfo)
		{
			if (Settings.Instance.FlattenStructure)
			{
				return Path.Combine(pathToBuffer, fileInfo.Name);
			}
			var bufferDI = new DirectoryInfo(pathToBuffer);
			var pathToUncompressedFiles = bufferDI.Parent.FullName;
			// Use Path.GetRelativePath for better performance
			try
			{
				string relativePath = Path.GetRelativePath(pathToUncompressedFiles, fileInfo.FullName);

				// If the relative path goes up (..) or equals the filename, file is not within buffer path
				if (relativePath == "." || relativePath.StartsWith("..") || relativePath == fileInfo.Name)
				{
					return Path.Combine(pathToBuffer, fileInfo.Name);
				}

				string outputPath = Path.Combine(pathToBuffer, relativePath);

				// Ensure directory exists before returning the path
				string outputDir = Path.GetDirectoryName(outputPath);
				if (!string.IsNullOrEmpty(outputDir))
				{
					Directory.CreateDirectory(outputDir);
				}

				return outputPath;
			}
			catch (Exception)
			{
				// Fallback to flattened structure on any error
				return Path.Combine(pathToBuffer, fileInfo.Name);
			}
		}
	}
}