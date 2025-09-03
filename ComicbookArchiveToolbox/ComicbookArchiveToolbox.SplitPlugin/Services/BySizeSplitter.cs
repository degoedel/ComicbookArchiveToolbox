using ComicbookArchiveToolbox.CommonTools;
using ComicbookArchiveToolbox.CommonTools.Events;
using Prism.Events;
using System;
using System.Collections.Generic;
using System.IO;

namespace CatPlugin.Split.Services
{
	public class BySizeSplitter : BaseSplitter, ISplitter
	{
		private IEventAggregator _eventAggregator;
		public BySizeSplitter(Logger logger, IEventAggregator eventAggregator)
	: base(logger)
		{
			_eventAggregator = eventAggregator;
		}

		public void Split(string filePath, ArchiveTemplate archiveTemplate)
		{
			_eventAggregator.GetEvent<BusinessEvent>().Publish(true);
			if (archiveTemplate.MaxSizePerSplittedFile < 2)
			{
				_logger.Log($"Cannot split archive with less than 2Mb : {archiveTemplate.MaxSizePerSplittedFile} Mb");
				_eventAggregator.GetEvent<BusinessEvent>().Publish(false);
				return;
			}
			//Extract file in buffer
			archiveTemplate.PathToBuffer = ExtractArchive(filePath, archiveTemplate);
			//Count files in directory except metadata
			ParseArchiveFiles(archiveTemplate.PathToBuffer, out List<FileInfo> metadataFiles, out List<FileInfo> pages);
			archiveTemplate.Pages = pages;
			archiveTemplate.MetadataFiles = metadataFiles;
			int totalPagesCount = archiveTemplate.Pages.Count - archiveTemplate.MetadataFiles.Count;
			_logger.Log($"Total number of pages is {totalPagesCount}");
			long maxSizeAsBytes = archiveTemplate.MaxSizePerSplittedFile * 1048576;
			long numberOfSplittedFiles = ComputeApproximateNumberOfSplittedFiles(pages, maxSizeAsBytes);
			_logger.Log($"Creating {numberOfSplittedFiles} splitted files");
			archiveTemplate.IndexSize = Math.Max(archiveTemplate.NumberOfSplittedFiles.ToString().Length, 2);
			archiveTemplate.CoverPath = "";
			if (Settings.Instance.IncludeCover)
			{
				archiveTemplate.CoverPath = SaveCoverInBuffer(archiveTemplate.PathToBuffer, archiveTemplate.ComicName, archiveTemplate.IndexSize, archiveTemplate.Pages);
			}
			long sizeAdded = 0;
			int fileIndex = 0;
			string subBufferPath = "";
			List<FileInfo> pagesToAdd = new List<FileInfo>();
			for (int i = 0; i < totalPagesCount; ++i)
			{
				if (sizeAdded == 0)
				{
					pagesToAdd = new List<FileInfo>();
					subBufferPath = GetSubBufferPath(archiveTemplate, fileIndex);
					Directory.CreateDirectory(subBufferPath);
					if (Settings.Instance.IncludeMetadata)
					{
						sizeAdded += CopyMetaDataToSubBuffer(archiveTemplate.MetadataFiles, subBufferPath);
					}
					if (fileIndex != 0)
					{
						if (Settings.Instance.IncludeCover)
						{
							sizeAdded += CopyCoverToSubBuffer(archiveTemplate.CoverPath, subBufferPath, fileIndex, (int)numberOfSplittedFiles);
						}
					}
				}
				while (sizeAdded < maxSizeAsBytes && i < totalPagesCount)
				{
					if (archiveTemplate.Pages[i].Extension != ".xml")
					{
						pagesToAdd.Add(archiveTemplate.Pages[i]);
						sizeAdded += archiveTemplate.Pages[i].Length;
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
				sizeAdded = 0;
				++fileIndex;
			}
			_logger.Log($"Clean Buffer {archiveTemplate.PathToBuffer}");
			SystemTools.CleanDirectory(archiveTemplate.PathToBuffer, _logger);
			_logger.Log("Done.");
			_eventAggregator.GetEvent<BusinessEvent>().Publish(false);
		}

		private long ComputeApproximateNumberOfSplittedFiles(List<FileInfo> pages, long maxSizeAsBytes)
		{
			long result = 0;
			long totalSizeInBytes = 0;
			foreach(FileInfo fi in pages)
			{
				totalSizeInBytes += fi.Length;
			}
			_logger.Log($"Total size in Bytes of the source archive is {totalSizeInBytes}");
			_logger.Log($"Max size in Bytes of the splitted archives is {maxSizeAsBytes}");
			result = (totalSizeInBytes / maxSizeAsBytes) + 1;
			return result;
		}

	}
}
