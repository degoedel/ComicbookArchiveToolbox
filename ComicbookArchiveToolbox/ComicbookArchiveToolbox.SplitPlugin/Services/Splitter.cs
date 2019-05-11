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

    internal void Split(string filePath, uint fileNb, ArchiveTemplate archiveTemplate)
    {
      if (fileNb < 2)
      {
        _logger.Log($"Cannot split archive in {fileNb} files");
        return;
      }
	  //Extract file in buffer
	  string pathToBuffer = ExtractArchive(filePath, archiveTemplate);
	  //Count files in directory except metadata
	  ParseArchiveFiles(pathToBuffer, out List<FileInfo> metadataFiles, out List<FileInfo> pages);
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

      int indexSize = Math.Max(fileNb.ToString().Length, 2);
	    string coverPath = "";
	    if (Settings.Instance.IncludeCover)
	    {
		    coverPath = SaveCoverInBuffer(pathToBuffer, archiveTemplate.ComicName, indexSize, pages);
	    }
	    int sourcePageIndex = 0;
      for (int fileIndex = 0; fileIndex < fileNb; ++fileIndex)
      {
		    archiveTemplate.PathToBuffer = pathToBuffer;
		    archiveTemplate.IndexSize = indexSize;
		    archiveTemplate.PagesPerFile = pagesPerFile;
		    archiveTemplate.Pages = pages;
		    archiveTemplate.MetadataFiles = metadataFiles;
		    archiveTemplate.CoverPath = coverPath;

		    string splittedContentPath = BuildSplittedArchive(archiveTemplate, fileIndex, fileNb, ref sourcePageIndex);
        if (string.IsNullOrWhiteSpace(splittedContentPath))
        {
          _logger.Log("ERROR: Failure to split the file");
          break;
        }
		    _logger.Log($"Compress {splittedContentPath}");
		    CompressArchiveContent(splittedContentPath, archiveTemplate);
		    _logger.Log($"Clean Buffer {splittedContentPath}");
		    SystemTools.CleanDirectory(splittedContentPath, _logger);
	    }
	    _logger.Log($"Clean Buffer {pathToBuffer}");
	    SystemTools.CleanDirectory(pathToBuffer, _logger);
      // compress the resulting file
      // clean the temp directories
      _logger.Log("Done.");
    }

	private string ExtractArchive(string filePath, ArchiveTemplate archiveTemplate)
	{
		string pathToBuffer = Settings.Instance.GetBufferDirectory(filePath, archiveTemplate.ComicName);
		CompressionHelper ch = new CompressionHelper(_logger);
		_logger.Log($"Start extraction of {filePath} into {pathToBuffer} ...");
		ch.DecompressToDirectory(filePath, pathToBuffer);
		_logger.Log($"Extraction done.");
		return pathToBuffer;
	}

	private void ParseArchiveFiles(string pathToBuffer, out List<FileInfo> metadataFiles, out List<FileInfo> pages)
	{
		metadataFiles = new List<FileInfo>();
		DirectoryInfo di = new DirectoryInfo(pathToBuffer);
		pages = new List<FileInfo>();
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

	private bool MovePicturesToSubBuffer(string destFolder, List<FileInfo> files, string archiveName)
   {
      bool result = true;
     _logger.Log($"Copy the selected files in {destFolder}");
      try
      {
        Directory.CreateDirectory(destFolder);
        int padSize = Math.Max(2, files.Count.ToString().Length);
        for (int i = 0; i < files.Count; ++i)
        {
          // rename the files in the directories
          File.Move(files[i].FullName, Path.Combine(destFolder, $"{archiveName}_{(i + 1).ToString().PadLeft(padSize, '0')}{files[i].Extension}"));
        }
      }
      catch (Exception e)
      {
        result = false;
        _logger.Log($"ERROR: Cannot split archive {e.Message}");
      }
      return result;
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

	private string BuildSplittedArchive(ArchiveTemplate template, int fileIndex, uint fileNb, ref int sourcePageIndex)
	{
	  // Create the subBuffer
	  string subBufferPath = Path.Combine(template.PathToBuffer, $"{template.ComicName}_{(fileIndex + 1).ToString().PadLeft(template.IndexSize, '0')}");
	  
	  _logger.Log($"Create the subFolder {subBufferPath}");
	  int pagesAdded = 0;
	  List<FileInfo> pagesToAdd = new List<FileInfo>();
	  
	  for (int currentPageIndex = sourcePageIndex; (pagesAdded < template.PagesPerFile) && (currentPageIndex < template.Pages.Count); ++currentPageIndex)
	  {
	  	if (template.Pages[currentPageIndex].Extension != ".xml")
	  	{
	  		pagesToAdd.Add(template.Pages[currentPageIndex]);
	  		++pagesAdded;
	  	}
	  	sourcePageIndex = currentPageIndex + 1;
	  }
	  if (fileIndex == fileNb - 1)
	  {
	  	for (int i = sourcePageIndex; i < template.Pages.Count; ++i)
	  	{
	  		if (template.Pages[i].Extension != ".xml")
	  		{
	  			pagesToAdd.Add(template.Pages[i]);
	  		}
	  	}
	  }
	  bool ok = MovePicturesToSubBuffer(subBufferPath, pagesToAdd, template.ComicName);
    if (!ok)
    {
        SystemTools.CleanDirectory(subBufferPath, _logger);
        return "";
    }
	  if (!string.IsNullOrWhiteSpace(template.CoverPath))
	  {
	  	if (fileIndex != 0)
	  	{
	  		CopyCoverToSubBuffer(template.CoverPath, subBufferPath);
	  	}
	  }
	  if (Settings.Instance.IncludeMetadata)
	  {
	  	CopyMetaDataToSubBuffer(template.MetadataFiles, subBufferPath);
	  }
	  return subBufferPath;
	}

	private void CompressArchiveContent(string directory, ArchiveTemplate archiveTemplate)
	{
		DirectoryInfo di = new DirectoryInfo(directory);
		string outputFile = Path.Combine(archiveTemplate.OutputDir, $"{di.Name}.cbz");
		CompressionHelper ch = new CompressionHelper(_logger);
		_logger.Log($"Start compression of {directory} into {outputFile} ...");
		ch.CompressDirectoryContent(directory, outputFile);
		_logger.Log($"Compression done.");
	}
  }
}
