using Prism.Events;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ComicbookArchiveToolbox.CommonTools
{
  public class CompressionHelper
  {
    private string _pathTo7z = "";
    private Logger _logger;
    public CompressionHelper(Logger logger)
    {
      _logger = logger;

      string codeBase = Assembly.GetExecutingAssembly().CodeBase;
      UriBuilder uri = new UriBuilder(codeBase);
      string path = Uri.UnescapeDataString(uri.Path);
      DirectoryInfo installDir = new DirectoryInfo(System.IO.Path.GetDirectoryName(path));

      _pathTo7z = Path.Combine(installDir.FullName, "7z.exe");
    }

    public void DecompressToDirectory(string archivePath, string decompressionFolder)
    {
      if (!Directory.Exists(decompressionFolder))
        Directory.CreateDirectory(decompressionFolder);

      try
      {
        ProcessStartInfo pro = new ProcessStartInfo();
        pro.WindowStyle = ProcessWindowStyle.Hidden;
        pro.FileName = _pathTo7z;
        pro.Arguments = $"x -o\"{decompressionFolder}\" \"{archivePath}\"";
        _logger.Log($"Launch external command {_pathTo7z} {pro.Arguments}");
        Process x = Process.Start(pro);
        x.WaitForExit();
      }
      catch (Exception e)
      {
        _logger.Log($"Failure during decompression : {e.Message}");
      }
    }
  }
}
