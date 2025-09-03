using System;
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

  }
}
