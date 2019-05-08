using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ComicbookArchiveToolbox.CommonTools
{
  public sealed class Settings
  {
    private string _settingsPath = @"C:\ProgramData\ComicbookArchiveToolbox\Settings\Settings.xml";
    public string BufferDirectory => @"C:\ProgramData\ComicbookArchiveToolbox\Buffer";
    public bool UseFileDirAsBuffer => false;

    private static readonly Lazy<Settings> lazy =
    new Lazy<Settings>(() => new Settings());

    public static Settings Instance { get { return lazy.Value; } }

    private Settings()
    {
    }

    private void InitBuffer()
    {
      if (!Directory.Exists(BufferDirectory))
      {
        Directory.CreateDirectory(BufferDirectory);
      }
    }

    public string GetBufferDirectory(string filePath)
    {
      FileInfo fi = new FileInfo(filePath);
      string archiveName = fi.Name.Substring(0, fi.Name.Length - 4);
      string result = "";
      if (UseFileDirAsBuffer)
      {
        result = Path.Combine(fi.DirectoryName, archiveName);
      }
      else
      {
        InitBuffer();
        result = Path.Combine(BufferDirectory, archiveName);
      }
      return result;
    }
  }
}
