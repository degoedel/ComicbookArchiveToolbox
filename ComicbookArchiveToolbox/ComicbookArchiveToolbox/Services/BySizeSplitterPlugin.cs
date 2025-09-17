using ComicbookArchiveToolbox.CommonTools;
using ComicbookArchiveToolbox.CommonTools.Events;
using Prism.Events;
using System;
using System.Collections.Generic;
using System.IO;

namespace ComicbookArchiveToolbox.Module.Split.Services
{
	public class BySizeSplitterPlugin : BaseSplitterPlugin, ISplitter
	{
		public BySizeSplitterPlugin(Logger logger, IEventAggregator eventAggregator)
			: base(logger, eventAggregator)
		{
		}

		protected override bool ValidateInput(ArchiveTemplate archiveTemplate)
		{
			if (archiveTemplate.MaxSizePerSplittedFile < 2)
			{
				_logger.Log($"Cannot split archive with less than 2Mb : {archiveTemplate.MaxSizePerSplittedFile} Mb");
				return false;
			}
			return true;
		}

		protected override int ComputeNumberOfSplittedFiles(SplitContext context)
		{
			long maxSizeAsBytes = context.ArchiveTemplate.MaxSizePerSplittedFile * 1048576;
			long totalSizeInBytes = 0;
			foreach (FileInfo fi in context.Pages)
			{
				totalSizeInBytes += fi.Length;
			}
			_logger.Log($"Total size in Bytes of the source archive is {totalSizeInBytes}");
			_logger.Log($"Max size in Bytes of the splitted archives is {maxSizeAsBytes}");
			return (int)((totalSizeInBytes / maxSizeAsBytes) + 1);
		}

		protected override bool ExecuteSplitting(SplitContext context)
		{
			long maxSizeAsBytes = context.ArchiveTemplate.MaxSizePerSplittedFile * 1048576;
			long sizeAdded = 0;
			int fileIndex = 0;
			List<FileInfo> pagesToAdd = [];

			for (int i = 0; i < context.TotalPagesCount; ++i)
			{
				if (sizeAdded == 0)
				{
					pagesToAdd = [];
				}

				// Collect pages until size limit is reached
				while (sizeAdded < maxSizeAsBytes && i < context.TotalPagesCount)
				{
					if (SystemTools.IsImageFile(context.Pages[i]))
					{
						pagesToAdd.Add(context.Pages[i]);
						sizeAdded += context.Pages[i].Length;
					}
					++i;
				}

				// Process the batch
				if (!ProcessFileBatch(context, fileIndex, pagesToAdd))
				{
					return false;
				}

				sizeAdded = 0;
				++fileIndex;
			}

			return true;
		}
	}
}