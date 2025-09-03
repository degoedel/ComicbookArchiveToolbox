using ComicbookArchiveToolbox.CommonTools;
using ComicbookArchiveToolbox.CommonTools.Events;
using Prism.Events;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CatPlugin.Merge.Service
{
  public class Merger
  {
    private Logger _logger;
		private IEventAggregator _eventAggregator;
    public Merger(Logger logger, IEventAggregator eventAggregator)
    {
      _logger = logger;
			_eventAggregator = eventAggregator;
    }

    public void Merge(string outputFile, IList<string> files, long imageQuality)
    {
			_eventAggregator.GetEvent<BusinessEvent>().Publish(true);
			_logger.Log($"Check input validity vs settings");
      FileInfo fi = new FileInfo(outputFile);
      string fileName = fi.Name;
      string settingsExtension = $".{Settings.Instance.OutputFormat.ToString().ToLower()}";
      if (string.IsNullOrEmpty(fi.Extension))
      {
        _logger.Log($"Add Extension to filename {settingsExtension}");
        outputFile += settingsExtension;
      }
      else
      {
        if (fi.Extension != settingsExtension)
        {
          _logger.Log($"Incorrect extension found in filename {fi.Extension} replaced with {settingsExtension}");
          outputFile = outputFile.Substring(0, outputFile.Length - fi.Extension.Length) + settingsExtension;
        }
      }
      _logger.Log($"Start merging input archives in {outputFile}");
      string nameTemplate = fi.Name.Substring(0, fi.Name.Length - fi.Extension.Length);
      string bufferPath = Settings.Instance.GetBufferDirectory(files[0], nameTemplate);
      string outputBuffer = Path.Combine(bufferPath, "outputBuffer");
      Directory.CreateDirectory(outputBuffer);
      List<FileInfo> metadataFiles = new List<FileInfo>();
      List<FileInfo> pages = new List<FileInfo>();
      int bufferPadSize = files.Count.ToString().Length;
      for (int i = 0; i < files.Count;++i)
      {
        CompressionHelper ch1 = new CompressionHelper(_logger);
        string decompressionBuffer = Path.Combine(bufferPath, $"archive_{i.ToString().PadLeft(bufferPadSize, '0')}");
        _logger.Log($"Uncompress {files[i]} in {decompressionBuffer}");
        ch1.DecompressToDirectory(files[i], decompressionBuffer);
        _logger.Log($"Extraction done.");
        ParseArchiveFiles(decompressionBuffer, ref metadataFiles, ref pages);
      }
      if (Settings.Instance.IncludeMetadata)
      {
        if (metadataFiles.Count > 0)
        {
          string destFile = Path.Combine(outputBuffer, metadataFiles[0].Name);
          _logger.Log($"copy metadata {metadataFiles[0].FullName}  in {destFile}");
          File.Copy(metadataFiles[0].FullName, destFile, true);
        }
      }
      int pagePadSize = pages.Count.ToString().Length;
      int pageAdded = 1;
			JpgConverter jpgConverter = new JpgConverter(_logger, imageQuality);
			for (int i = 0; i < pages.Count; ++i)
      {
        if (pages[i].Extension != ".xml")
        {
          string destFile = Path.Combine(outputBuffer, $"{nameTemplate}_{pageAdded.ToString().PadLeft(pagePadSize, '0')}{pages[i].Extension}".Replace(' ', '_'));
					if (imageQuality == 100)
					{
						// rename the files in the directories
						File.Move(pages[i].FullName, destFile);
					}
					else
					{
						jpgConverter.SaveJpeg(pages[i].FullName, destFile);
						File.Delete(pages[i].FullName);
					}
          ++pageAdded;
        }
      }
      CompressionHelper ch2 = new CompressionHelper(_logger);
      _logger.Log($"Start compression of {outputBuffer} into {outputFile} ...");
      ch2.CompressDirectoryContent(outputBuffer, outputFile);
      _logger.Log($"Compression done.");
      _logger.Log($"Clean Buffer {bufferPath}");
      SystemTools.CleanDirectory(bufferPath, _logger);
      _logger.Log("Done.");
			_eventAggregator.GetEvent<BusinessEvent>().Publish(false);
		}

		private void ParseArchiveFiles(string pathToBuffer, ref List<FileInfo> metadataFiles, ref List<FileInfo> pages)
    {
      DirectoryInfo di = new DirectoryInfo(pathToBuffer);
      pages.AddRange(di.GetFiles().OrderBy(f => f.Name));
      metadataFiles.AddRange(di.GetFiles("*.xml").OrderBy(f => f.Name));
      var subdirs = di.GetDirectories();
      if (subdirs.Count() > 0)
      {
        foreach (DirectoryInfo subdir in subdirs)
        {
          pages.AddRange(subdir.GetFiles().OrderBy(f => f.Name));
          metadataFiles.AddRange(subdir.GetFiles("*.xml").OrderBy(f => f.Name));
        }
      }
    }
  }
}
