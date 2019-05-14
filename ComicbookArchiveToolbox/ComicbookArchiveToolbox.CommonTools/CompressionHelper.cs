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
        pro.Arguments = $"x -aoa -o\"{decompressionFolder}\" \"{archivePath}\"";
        _logger.Log($"Launch external command {_pathTo7z} {pro.Arguments}");
        Process x = Process.Start(pro);
        x.WaitForExit();
      }
      catch (Exception e)
      {
        _logger.Log($"Failure during decompression of {archivePath} in {decompressionFolder}: {e.Message}");
      }
    }

	public void CompressDirectoryContent(string inputDir, string outputFile)
	{
			string compressionArg = "";
			switch (Settings.Instance.OutputFormat)
			{
				case SerializationSettings.ArchiveFormat.Cb7:
					compressionArg = "7z";
					break;
				case SerializationSettings.ArchiveFormat.Cbt:
					compressionArg = "tar";
					break;
				case SerializationSettings.ArchiveFormat.Cbz:
					compressionArg = "zip";
					break;
				default:
					compressionArg = "zip";
					break;
			}
			try
		{
			ProcessStartInfo pro = new ProcessStartInfo();
			pro.WindowStyle = ProcessWindowStyle.Hidden;
			pro.FileName = _pathTo7z;
			pro.Arguments = $"a -aoa -t{compressionArg} \"{outputFile}\" \"{inputDir}\\*\" ";
			_logger.Log($"Launch external command {_pathTo7z} {pro.Arguments}");
			Process x = Process.Start(pro);
			x.WaitForExit();
		}
			catch (Exception e)
		{
			_logger.Log($"Failure during compression of {inputDir} in {outputFile} : {e.Message}");
		}
	}
  }
}
