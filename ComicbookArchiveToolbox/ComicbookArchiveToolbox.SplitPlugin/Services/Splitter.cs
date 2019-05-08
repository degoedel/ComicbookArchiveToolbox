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
      int sourcePageIndex = 0;
      for (int fileIndex = 0; fileIndex < fileNb; ++fileIndex)
      {
        // Create the subBuffer
        string subBufferPath = Path.Combine(pathToBuffer, comicName + (fileIndex + 1).ToString().PadLeft(indexSize, '0'));
        _logger.Log($"Create the subFolder {subBufferPath}");
        int pagesAdded = 0;
        List<FileInfo> pagesToAdd = new List<FileInfo>();
        if (Settings.Instance.IncludeCover)
        {
          bool coverFound = GetCoverIfFound(pages, out FileInfo cover);
          if (coverFound)
          {
            pagesToAdd.Add(cover);
          }
          else
          {
            _logger.Log("Cannot find cover page.");
          }
        }
        if (Settings.Instance.IncludeMetadata)
        {
          if (metadataFiles.Count > 0)
          {
            pagesToAdd.Add(metadataFiles[0]);
          }
          else
          {
            //TODO create metadata file
          }
        }
        for (int currentPageIndex = sourcePageIndex; (pagesAdded < pagesPerFile) && (currentPageIndex < pages.Count); ++currentPageIndex)
        {
          if (pages[currentPageIndex].Extension != ".xml")
          {
            pagesToAdd.Add(pages[currentPageIndex]);
            ++pagesAdded;
          }
          sourcePageIndex = currentPageIndex;
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
        CreateArchive(subBufferPath, pagesToAdd, comicName);
      }


      // rename the files in the directories
      // compress the resulting file
      // clean the temp directories
      _logger.Log("Done.");
    }

    private void CreateArchive(string destFolder, List<FileInfo> files, string archiveName)
    {
      _logger.Log($"Copy the selected files in {destFolder}");
      Directory.CreateDirectory(destFolder);
      int padSize = Math.Max(2, files.Count.ToString().Length);
      for (int i = 0; i < files.Count; ++i)
      {
        if(files[i].Extension == ".xml")
        {
          File.Copy(files[i].FullName, Path.Combine(destFolder, files[i].Name),true);
        }
        else
        {
          File.Copy(files[i].FullName, Path.Combine(destFolder, $"{archiveName}_{i.ToString().PadLeft(padSize, '0')}{files[i].Extension}"), true);
        }
      }
    }

    private string ExtractComicName(string filePath)
    {
      FileInfo sourceFile = new FileInfo(filePath);
      return sourceFile.Name.Substring(0, sourceFile.Name.Length - 4);
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
