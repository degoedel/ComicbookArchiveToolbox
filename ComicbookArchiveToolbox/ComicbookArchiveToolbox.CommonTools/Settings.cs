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

	// ace and rar being proprietary format 7zip cannot create them.
	public enum ArchiveFormat
	{
		Cb7,
		Cbt,
		Cbz
	};

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

    public string GetBufferDirectory(string filePath, string outputNameTemplate)
    {
      FileInfo fi = new FileInfo(filePath);
      string result = "";
      if (UseFileDirAsBuffer)
      {
        result = Path.Combine(fi.DirectoryName, outputNameTemplate);
      }
      else
      {
        InitBuffer();
        result = Path.Combine(BufferDirectory, outputNameTemplate);
      }
      return result;
    }

    public bool IncludeCover => true;

    public bool IncludeMetadata => true;

	public ArchiveFormat OutputFormat => ArchiveFormat.Cbz;
  }
}
