using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

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

		public static void ParseArchiveFiles(string pathToBuffer, ref List<FileInfo> metadataFiles, ref List<FileInfo> pages)
		{
			DirectoryInfo di = new DirectoryInfo(pathToBuffer);
			pages.AddRange(di.GetFiles().OrderBy(f => f.Name));
			metadataFiles.AddRange(di.GetFiles().Where(x => !SystemTools.IsImageFile(x)).OrderBy(f => f.Name));
			var subdirs = di.GetDirectories();
			if (subdirs.Count() > 0)
			{
				foreach (DirectoryInfo subdir in subdirs)
				{
					pages.AddRange(subdir.GetFiles().OrderBy(f => f.Name));
					metadataFiles.AddRange(subdir.GetFiles().Where(x => !SystemTools.IsImageFile(x)).OrderBy(f => f.Name));
				}
			}
		}

	}
}
