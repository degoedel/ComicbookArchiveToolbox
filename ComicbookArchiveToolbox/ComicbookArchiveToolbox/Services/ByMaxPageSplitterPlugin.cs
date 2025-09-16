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
		public ByMaxPageSplitterPlugin(Logger logger, IEventAggregator eventAggregator)
			: base(logger, eventAggregator)
		{
		}

		protected override bool ValidateInput(ArchiveTemplate archiveTemplate)
		{
			if (archiveTemplate.MaxPagesPerSplittedFile < 2)
			{
				_logger.Log($"Cannot split archive with {archiveTemplate.MaxPagesPerSplittedFile} page per file");
				return false;
			}
			return true;
		}

		protected override int ComputeNumberOfSplittedFiles(SplitContext context)
		{
			int numberOfSplittedFiles = 0;
			int remainder = 0;
			if (Settings.Instance.IncludeCover)
			{
				numberOfSplittedFiles = Math.DivRem(context.TotalPagesCount - 1, (int)context.ArchiveTemplate.MaxPagesPerSplittedFile - 1, out remainder);
			}
			else
			{
				numberOfSplittedFiles = Math.DivRem(context.TotalPagesCount, (int)context.ArchiveTemplate.MaxPagesPerSplittedFile, out remainder);
			}
			if (remainder > 0)
			{
				++numberOfSplittedFiles;
			}
			return numberOfSplittedFiles;
		}

		protected override bool ExecuteSplitting(SplitContext context)
		{
			int pagesAdded = 0;
			int fileIndex = 0;
			List<FileInfo> pagesToAdd = new List<FileInfo>();

			for (int i = 0; i < context.TotalPagesCount; ++i)
			{
				if (pagesAdded == 0)
				{
					pagesToAdd = new List<FileInfo>();
				}

				// Collect pages until max pages limit is reached
				while (pagesAdded < context.ArchiveTemplate.MaxPagesPerSplittedFile && i < context.TotalPagesCount)
				{
					if (SystemTools.IsImageFile(context.Pages[i]))
					{
						pagesToAdd.Add(context.Pages[i]);
						++pagesAdded;
					}
					++i;
				}

				// Process the batch
				if (!ProcessFileBatch(context, fileIndex, pagesToAdd))
				{
					return false;
				}

				pagesAdded = 0;
				++fileIndex;
			}

			return true;
		}
	}
}