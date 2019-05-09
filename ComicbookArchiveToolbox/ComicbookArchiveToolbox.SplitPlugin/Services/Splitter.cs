using ComicbookArchiveToolbox.CommonTools;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CatPlugin.Split.Services
{
  internal class Splitter
  {
    Logger _logger;
    internal Splitter(Logger logger)
    {
      _logger = logger;
    }

    internal void Split(string filePath, uint fileNb)
    {
      if (fileNb < 2)
      {
        _logger.Log($"Cannot split archive in {fileNb} files");
        return;
      }
      //Extract file in buffer
      CompressionHelper ch = new CompressionHelper(_logger);
      string pathToBuffer = Settings.Instance.GetBufferDirectory(filePath);
      _logger.Log($"Start extraction of {filePath} into {pathToBuffer} ...");
      ch.DecompressToDirectory(filePath, pathToBuffer);
      _logger.Log($"Extraction done.");
      //Count files in directory except metadata
      List<FileInfo> metadataFiles = new List<FileInfo>();
      DirectoryInfo di = new DirectoryInfo(pathToBuffer);
      List<FileInfo> pages = new List<FileInfo>();
      var subdirs = di.GetDirectories();
      if (subdirs.Count() > 0)
      {
        foreach (DirectoryInfo subdir in subdirs)
        {
          pages.AddRange(subdir.GetFiles().OrderBy(f => f.Name));
          metadataFiles.AddRange(subdir.GetFiles("*.xml").OrderBy(f => f.Name));
        }
      }
      else
      {
        pages.AddRange(di.GetFiles().OrderBy(f => f.Name));
        metadataFiles.AddRange(di.GetFiles("*.xml").OrderBy(f => f.Name));
      }
      int totalPagesCount = pages.Count - metadataFiles.Count;
      _logger.Log($"Total number of pages is {totalPagesCount}");
      // Check that the resulting split files number is consistent with the number of pages
      if (fileNb > totalPagesCount)
      {
        _logger.Log($"Not enough pages to split into {fileNb} files.");
        SystemTools.CleanDirectory(pathToBuffer, _logger);
        return;
      }
      //Create one folder per resulting file and copy pictures in it
      int pagesPerFile = Math.DivRem(totalPagesCount, (int)fileNb, out int extraPages);
      _logger.Log($"Pages per resulting file : {pagesPerFile}");

      string comicName = ExtractComicName(filePath);
      int indexSize = Math.Max(fileNb.ToString().Length, 2);
	  string coverPath = "";
	  if (Settings.Instance.IncludeCover)
	  {
		coverPath = SaveCoverInBuffer(pathToBuffer, comicName, indexSize, pages);
	  }
	  int sourcePageIndex = 0;
      for (int fileIndex = 0; fileIndex < fileNb; ++fileIndex)
      {
        // Create the subBuffer
        string subBufferPath = Path.Combine(pathToBuffer, $"{comicName}_{(fileIndex + 1).ToString().PadLeft(indexSize, '0')}");

		_logger.Log($"Create the subFolder {subBufferPath}");
        int pagesAdded = 0;
        List<FileInfo> pagesToAdd = new List<FileInfo>();

        for (int currentPageIndex = sourcePageIndex; (pagesAdded < pagesPerFile) && (currentPageIndex < pages.Count); ++currentPageIndex)
        {
          if (pages[currentPageIndex].Extension != ".xml")
          {
            pagesToAdd.Add(pages[currentPageIndex]);
            ++pagesAdded;
          }
          sourcePageIndex = currentPageIndex + 1;
        }
        if (fileIndex == fileNb - 1)
        {
          for (int i = sourcePageIndex; i < pages.Count; ++i)
          {
            if (pages[i].Extension != ".xml")
            {
              pagesToAdd.Add(pages[i]);
            }
          }
        }
        MovePicturesToSubBuffer(subBufferPath, pagesToAdd, comicName);
		if (Settings.Instance.IncludeCover)
		{
			if (fileIndex != 0)
			{
				CopyCoverToSubBuffer(coverPath, subBufferPath);
			}
		}
		if (Settings.Instance.IncludeMetadata)
		{
			CopyMetaDataToSubBuffer(metadataFiles, subBufferPath);
		}
      }


      // rename the files in the directories
      // compress the resulting file
      // clean the temp directories
      _logger.Log("Done.");
    }

    private void MovePicturesToSubBuffer(string destFolder, List<FileInfo> files, string archiveName)
    {
      _logger.Log($"Copy the selected files in {destFolder}");
      Directory.CreateDirectory(destFolder);
      int padSize = Math.Max(2, files.Count.ToString().Length);
      for (int i = 0; i < files.Count; ++i)
      {
		File.Move(files[i].FullName, Path.Combine(destFolder, $"{archiveName}_{(i + 1).ToString().PadLeft(padSize, '0')}{files[i].Extension}"));
      }
    }

	private void CopyCoverToSubBuffer(string coverFile, string subBuffer)
	{
		if (string.IsNullOrWhiteSpace(coverFile))
		{
			return;
		}
		FileInfo coverInfo = new FileInfo(coverFile);
		File.Copy(coverFile, Path.Combine(subBuffer, coverInfo.Name));
	}

	private void CopyMetaDataToSubBuffer(List<FileInfo> metaDataFiles, string subBuffer)
	{
		if (metaDataFiles.Count > 0)
		{
			File.Copy(metaDataFiles[0].FullName, Path.Combine(subBuffer, metaDataFiles[0].Name));
		}
	}

	private string ExtractComicName(string filePath)
    {
      FileInfo sourceFile = new FileInfo(filePath);
      return sourceFile.Name.Substring(0, sourceFile.Name.Length - 4);
    }

	private string SaveCoverInBuffer(string pathToBuffer, string archiveName, int indexSize, List<FileInfo> files)
	{
			string savedCoverPath = "";
			bool coverFound = GetCoverIfFound(files, out FileInfo coverFile);
			int coverIndex = 0;
			if (coverFound)
			{
				savedCoverPath = Path.Combine(pathToBuffer, $"{archiveName}_{coverIndex.ToString().PadLeft(indexSize, '0')}{coverFile.Extension}");
				File.Copy(coverFile.FullName, savedCoverPath);
			}
			return savedCoverPath;
	}

    private bool GetCoverIfFound(List<FileInfo> files, out FileInfo cover)
    {
      int i = 0;
      cover = null;
      while (i < files.Count && cover == null)
      {
        if (files[i].Extension != ".xml")
        {
          cover = files[i];
          break;
        }
        ++i;
      }
      return cover!=null;
    }
  }
}
