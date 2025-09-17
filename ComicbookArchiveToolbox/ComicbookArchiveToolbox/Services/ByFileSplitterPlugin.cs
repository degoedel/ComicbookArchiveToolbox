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
		public ByFileSplitterPlugin(Logger logger, IEventAggregator eventAggregator)
			: base(logger, eventAggregator)
		{
		}

		protected override bool ValidateInput(ArchiveTemplate archiveTemplate)
		{
			if (archiveTemplate.NumberOfSplittedFiles < 2)
			{
				_logger.Log($"Cannot split archive in {archiveTemplate.NumberOfSplittedFiles} files");
				return false;
			}
			return true;
		}

		protected override bool ExecuteSplitting(SplitContext context)
		{
			// Additional validation specific to this splitter
			if (context.ArchiveTemplate.NumberOfSplittedFiles > context.TotalPagesCount)
			{
				_logger.Log($"Not enough pages to split into {context.ArchiveTemplate.NumberOfSplittedFiles} files.");
				return false;
			}

			int pagesPerFile = Math.DivRem(context.TotalPagesCount, (int)context.ArchiveTemplate.NumberOfSplittedFiles, out int extraPages);
			_logger.Log($"Pages per resulting file : {pagesPerFile}");

			int sourcePageIndex = 0;
			for (int fileIndex = 0; fileIndex < context.ArchiveTemplate.NumberOfSplittedFiles; ++fileIndex)
			{
				List<FileInfo> pagesToAdd = [];
				int pagesAdded = 0;

				// Collect pages for this file
				for (int currentPageIndex = sourcePageIndex; (pagesAdded < pagesPerFile) && (currentPageIndex < context.Pages.Count); ++currentPageIndex)
				{
					if (SystemTools.IsImageFile(context.Pages[currentPageIndex]))
					{
						pagesToAdd.Add(context.Pages[currentPageIndex]);
						++pagesAdded;
					}
					sourcePageIndex = currentPageIndex + 1;
				}

				// Add remaining pages to the last file
				if (fileIndex == context.ArchiveTemplate.NumberOfSplittedFiles - 1)
				{
					for (int i = sourcePageIndex; i < context.Pages.Count; ++i)
					{
						if (SystemTools.IsImageFile(context.Pages[i]))
						{
							pagesToAdd.Add(context.Pages[i]);
						}
					}
				}

				// Process the batch
				if (!ProcessFileBatch(context, fileIndex, pagesToAdd))
				{
					return false;
				}
			}

			return true;
		}
	}
}