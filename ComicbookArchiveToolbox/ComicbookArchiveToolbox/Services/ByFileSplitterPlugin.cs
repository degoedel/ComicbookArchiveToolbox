using ComicbookArchiveToolbox.CommonTools;
using ComicbookArchiveToolbox.CommonTools.Events;
using Prism.Events;
using System;
using System.Collections.Generic;
using System.IO;

namespace ComicbookArchiveToolbox.Module.Split.Services
{
	public class ByFileSplitterPlugin : BaseSplitterPlugin, ISplitter
	{
		private IEventAggregator _eventAggregator;
		public ByFileSplitterPlugin(Logger logger, IEventAggregator eventAggregator)
				: base(logger)
		{
			_eventAggregator = eventAggregator;
		}

		public void Split(string filePath, ArchiveTemplate archiveTemplate)
		{
			_eventAggregator.GetEvent<BusinessEvent>().Publish(true);
			if (archiveTemplate.NumberOfSplittedFiles < 2)
			{
				_logger.Log($"Cannot split archive in {archiveTemplate.NumberOfSplittedFiles} files");
				_eventAggregator.GetEvent<BusinessEvent>().Publish(false);
				return;
			}
			//Extract file in buffer
			List<FileInfo> metadataFiles = new List<FileInfo>();
			List<FileInfo> pages = new List<FileInfo>();
			string pathToBuffer = ExtractArchive(filePath, archiveTemplate);
			//Count files in directory except metadata
			SystemTools.ParseArchiveFiles(pathToBuffer, ref metadataFiles, ref pages);
			int totalPagesCount = pages.Count - metadataFiles.Count;
			_logger.Log($"Total number of pages is {totalPagesCount}");
			// Check that the resulting split files number is consistent with the number of pages
			if (archiveTemplate.NumberOfSplittedFiles > totalPagesCount)
			{
				_logger.Log($"Not enough pages to split into {archiveTemplate.NumberOfSplittedFiles} files.");
				SystemTools.CleanDirectory(pathToBuffer, _logger);
				_eventAggregator.GetEvent<BusinessEvent>().Publish(false);
				return;
			}
			//Create one folder per resulting file and copy pictures in it
			int pagesPerFile = Math.DivRem(totalPagesCount, (int)archiveTemplate.NumberOfSplittedFiles, out int extraPages);
			_logger.Log($"Pages per resulting file : {pagesPerFile}");

			int indexSize = Math.Max(archiveTemplate.NumberOfSplittedFiles.ToString().Length, 2);
			string coverPath = "";
			if (Settings.Instance.IncludeCover)
			{
				coverPath = SaveCoverInBuffer(pathToBuffer, archiveTemplate.ComicName, indexSize, pages);
			}
			int sourcePageIndex = 0;
			for (int fileIndex = 0; fileIndex < archiveTemplate.NumberOfSplittedFiles; ++fileIndex)
			{
				archiveTemplate.PathToBuffer = pathToBuffer;
				archiveTemplate.IndexSize = indexSize;
				archiveTemplate.PagesPerFile = pagesPerFile;
				archiveTemplate.Pages = pages;
				archiveTemplate.MetadataFiles = metadataFiles;
				archiveTemplate.CoverPath = coverPath;

				string splittedContentPath = BuildSplittedArchive(archiveTemplate, fileIndex, ref sourcePageIndex);
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
			_eventAggregator.GetEvent<BusinessEvent>().Publish(false);
		}

		private string BuildSplittedArchive(ArchiveTemplate template, int fileIndex, ref int sourcePageIndex)
		{
			// Create the subBuffer
			string subBufferPath = Path.Combine(template.PathToBuffer, $"{template.ComicName}_{(fileIndex + 1).ToString().PadLeft(template.IndexSize, '0')}");

			_logger.Log($"Create the subFolder {subBufferPath}");
			int pagesAdded = 0;
			List<FileInfo> pagesToAdd = new List<FileInfo>();

			for (int currentPageIndex = sourcePageIndex; (pagesAdded < template.PagesPerFile) && (currentPageIndex < template.Pages.Count); ++currentPageIndex)
			{
				if (SystemTools.IsImageFile(template.Pages[currentPageIndex]))
				{
					pagesToAdd.Add(template.Pages[currentPageIndex]);
					++pagesAdded;
				}
				sourcePageIndex = currentPageIndex + 1;
			}
			if (fileIndex == template.NumberOfSplittedFiles - 1)
			{
				for (int i = sourcePageIndex; i < template.Pages.Count; ++i)
				{
					if (SystemTools.IsImageFile(template.Pages[i]))
					{
						pagesToAdd.Add(template.Pages[i]);
					}
				}
			}
			bool ok = MovePicturesToSubBuffer(subBufferPath, pagesToAdd, template.ComicName, fileIndex == 0, template.ImageCompression);
			if (!ok)
			{
				SystemTools.CleanDirectory(subBufferPath, _logger);
				return "";
			}
			if (!string.IsNullOrWhiteSpace(template.CoverPath))
			{
				if (fileIndex != 0)
				{
					CopyCoverToSubBuffer(template.CoverPath, subBufferPath, fileIndex + 1, (int)template.NumberOfSplittedFiles);
				}
			}
			if (Settings.Instance.IncludeMetadata)
			{
				CopyMetaDataToSubBuffer(template.MetadataFiles, subBufferPath);
			}
			return subBufferPath;
		}
	}
}
