using ComicbookArchiveToolbox.CommonTools;
using ComicbookArchiveToolbox.CommonTools.Events;
using Prism.Events;
using System;
using System.Collections.Generic;
using System.IO;

namespace ComicbookArchiveToolbox.Module.Split.Services
{
	public class ByMaxPageSplitterPlugin : BaseSplitterPlugin, ISplitter
	{
		private IEventAggregator _eventAggregator;
		public ByMaxPageSplitterPlugin(Logger logger, IEventAggregator eventAggregator)
			: base(logger)
		{
			_eventAggregator = eventAggregator;
		}

		public void Split(string filePath, ArchiveTemplate archiveTemplate)
		{
			_eventAggregator.GetEvent<BusinessEvent>().Publish(true);
			if (archiveTemplate.MaxPagesPerSplittedFile < 2)
			{
				_logger.Log($"Cannot split archive with {archiveTemplate.MaxPagesPerSplittedFile} page per file");
				_eventAggregator.GetEvent<BusinessEvent>().Publish(false);
				return;
			}
			List<FileInfo> metadataFiles = new List<FileInfo>();
			List<FileInfo> pages = new List<FileInfo>();
			//Extract file in buffer
			archiveTemplate.PathToBuffer = ExtractArchive(filePath, archiveTemplate);
			//Count files in directory except metadata
			SystemTools.ParseArchiveFiles(archiveTemplate.PathToBuffer, ref metadataFiles, ref pages);
			archiveTemplate.Pages = pages;
			archiveTemplate.MetadataFiles = metadataFiles;
			int totalPagesCount = archiveTemplate.Pages.Count - archiveTemplate.MetadataFiles.Count;
			_logger.Log($"Total number of pages is {totalPagesCount}");
			int numberOfSplittedFiles = ComputeNumberOfSplittedFiles(totalPagesCount, archiveTemplate);
			_logger.Log($"Creating {numberOfSplittedFiles} splitted files");
			archiveTemplate.IndexSize = Math.Max(archiveTemplate.NumberOfSplittedFiles.ToString().Length, 2);
			archiveTemplate.CoverPath = "";
			if (Settings.Instance.IncludeCover)
			{
				archiveTemplate.CoverPath = SaveCoverInBuffer(archiveTemplate.PathToBuffer, archiveTemplate.ComicName, archiveTemplate.IndexSize, archiveTemplate.Pages);
			}

			int pagesAdded = 0;
			int fileIndex = 0;
			string subBufferPath = "";
			List<FileInfo> pagesToAdd = new List<FileInfo>();
			for (int i = 0; i < totalPagesCount; ++i)
			{
				if (pagesAdded == 0)
				{
					pagesToAdd = new List<FileInfo>();
					subBufferPath = GetSubBufferPath(archiveTemplate, fileIndex);
					Directory.CreateDirectory(subBufferPath);
					if (Settings.Instance.IncludeMetadata)
					{
						CopyMetaDataToSubBuffer(archiveTemplate.MetadataFiles, subBufferPath);
					}
					if (fileIndex != 0)
					{
						if (Settings.Instance.IncludeCover)
						{
							CopyCoverToSubBuffer(archiveTemplate.CoverPath, subBufferPath, fileIndex + 1, (int)archiveTemplate.NumberOfSplittedFiles);
							++pagesAdded;
						}
					}
				}
				while (pagesAdded < archiveTemplate.MaxPagesPerSplittedFile && i < totalPagesCount)
				{
					if (SystemTools.IsImageFile(archiveTemplate.Pages[i]))
					{
						pagesToAdd.Add(archiveTemplate.Pages[i]);
						++pagesAdded;
					}
					++i;
				}
				bool ok = MovePicturesToSubBuffer(subBufferPath, pagesToAdd, archiveTemplate.ComicName, fileIndex == 0, archiveTemplate.ImageCompression);
				if (!ok)
				{
					SystemTools.CleanDirectory(subBufferPath, _logger);
					break;
				}
				_logger.Log($"Compress {subBufferPath}");
				CompressArchiveContent(subBufferPath, archiveTemplate);
				_logger.Log($"Clean Buffer {subBufferPath}");
				SystemTools.CleanDirectory(subBufferPath, _logger);
				pagesAdded = 0;
				++fileIndex;
			}
			_logger.Log($"Clean Buffer {archiveTemplate.PathToBuffer}");
			SystemTools.CleanDirectory(archiveTemplate.PathToBuffer, _logger);
			_logger.Log("Done.");
			_eventAggregator.GetEvent<BusinessEvent>().Publish(false);
		}

		private int ComputeNumberOfSplittedFiles(int totalPagesCount, ArchiveTemplate template)
		{
			int numberOfSplittedFiles = 0;
			int remainder = 0;
			if (Settings.Instance.IncludeCover)
			{
				numberOfSplittedFiles = Math.DivRem(totalPagesCount - 1, (int)template.MaxPagesPerSplittedFile - 1, out remainder);
			}
			else
			{
				numberOfSplittedFiles = Math.DivRem(totalPagesCount, (int)template.MaxPagesPerSplittedFile, out remainder);
			}
			if (remainder > 0)
			{
				++numberOfSplittedFiles;
			}
			return numberOfSplittedFiles;
		}
	}
}
