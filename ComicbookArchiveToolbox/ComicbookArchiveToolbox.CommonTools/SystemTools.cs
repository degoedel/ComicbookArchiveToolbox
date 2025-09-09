using System;
using System.IO;
using System.Linq;
using System.Security.RightsManagement;

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

	}
}
