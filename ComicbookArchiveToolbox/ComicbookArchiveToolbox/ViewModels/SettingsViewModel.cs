using ComicbookArchiveToolbox.CommonTools;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ComicbookArchiveToolbox.ViewModels
{
	public class SettingsViewModel : BindableBase
	{

    public SettingsViewModel()
    {
      Formats = Enum.GetNames(typeof(Settings.ArchiveFormat)).ToList();
      UseFileDirAsBuffer = Settings.Instance.UseFileDirAsBuffer;
      BufferPath = Settings.Instance.BufferDirectory;
      AlwaysIncludeCover = Settings.Instance.IncludeCover;
      AlwaysIncludeMetadata = Settings.Instance.IncludeMetadata;
    }

    bool _useFileDirAsBuffer;
    public bool UseFileDirAsBuffer
    {
      get { return _useFileDirAsBuffer; }
      set
      {
        SetProperty(ref _useFileDirAsBuffer, value);
        Settings.Instance.UseFileDirAsBuffer = _useFileDirAsBuffer;
      }
    }

    string _bufferPath;
    public string BufferPath
    {
      get { return _bufferPath; }
      set
      {
        SetProperty(ref _bufferPath, value);
        Settings.Instance.BufferDirectory = _bufferPath;
      }
    }

    bool _alwaysIncludeCover;
    public bool AlwaysIncludeCover
    {
      get { return _alwaysIncludeCover; }
      set
      {
        SetProperty(ref _alwaysIncludeCover, value);
        Settings.Instance.IncludeCover = _alwaysIncludeCover;
      }
    }

    bool _alwaysIncludeMetadata;
    public bool AlwaysIncludeMetadata
    {
      get { return _alwaysIncludeMetadata; }
      set
      {
        SetProperty(ref _alwaysIncludeMetadata, value);
        Settings.Instance.IncludeMetadata = _alwaysIncludeMetadata;
      }
    }

     public List<string> Formats { get; set; }

  }
}
