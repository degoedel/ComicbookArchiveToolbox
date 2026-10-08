using ComicbookArchiveToolbox.CommonTools;
using ComicbookArchiveToolbox.Module.Split.Services;
using Prism.Events;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ComicbookArchiveToolbox.Services
{
	public class ByPageIdSplitterPlugin : BaseSplitterPlugin, ISplitter
	{
		public ByPageIdSplitterPlugin(Logger logger, IEventAggregator eventAggregator)
			: base(logger, eventAggregator)
		{
		}

		protected override bool ValidateInput(ArchiveTemplate archiveTemplate)
		{
			ArgumentNullException.ThrowIfNull(archiveTemplate);

			var splitPageIds = GetConfiguredSplitPageIds(archiveTemplate);
			if (splitPageIds.Count == 0)
			{
				_logger.Log("No page IDs specified for splitting.");
				return false;
			}

			return true;
		}

		protected override int ComputeNumberOfSplittedFiles(SplitContext context)
		{
			ArgumentNullException.ThrowIfNull(context);

			return NormalizeSplitPageIds(context.ArchiveTemplate, context.TotalPagesCount).Count + 1;
		}

		protected override bool ExecuteSplitting(SplitContext context)
		{
			ArgumentNullException.ThrowIfNull(context);

			var splitPageIds = NormalizeSplitPageIds(context.ArchiveTemplate, context.TotalPagesCount);
			if (splitPageIds.Count == 0)
			{
				_logger.Log("No valid page IDs specified for splitting.");
				return false;
			}

			context.NumberOfSplittedFiles = splitPageIds.Count + 1;
			context.ArchiveTemplate.IndexSize = Math.Max(context.NumberOfSplittedFiles.ToString().Length, 2);

			int fileIndex = 0;
			int currentPageId = 0;
			int splitPageIndex = 0;
			int nextSplitPageId = splitPageIds[splitPageIndex];
			List<FileInfo> pagesToAdd = [];

			foreach (FileInfo page in context.Pages)
			{
				if (!SystemTools.IsImageFile(page))
				{
					continue;
				}

				++currentPageId;
				if (currentPageId == nextSplitPageId)
				{
					if (!ProcessFileBatch(context, fileIndex, pagesToAdd))
					{
						return false;
					}

					++fileIndex;
					pagesToAdd = [];
					++splitPageIndex;
					nextSplitPageId = splitPageIndex < splitPageIds.Count ? splitPageIds[splitPageIndex] : int.MaxValue;
				}

				pagesToAdd.Add(page);
			}

			if (pagesToAdd.Count == 0)
			{
				_logger.Log("ERROR: No pages available for the last split archive.");
				return false;
			}

			return ProcessFileBatch(context, fileIndex, pagesToAdd);
		}

		private static List<uint> GetConfiguredSplitPageIds(ArchiveTemplate archiveTemplate)
		{
			if (archiveTemplate.PageIdsToSplit is { Count: > 0 })
			{
				return archiveTemplate.PageIdsToSplit;
			}

			return archiveTemplate.PagesIndexToSplit ?? [];
		}

		private List<int> NormalizeSplitPageIds(ArchiveTemplate archiveTemplate, int totalPagesCount)
		{
			List<uint> configuredSplitPageIds = GetConfiguredSplitPageIds(archiveTemplate);
			List<uint> invalidSplitPageIds = configuredSplitPageIds
				.Where(pageId => pageId <= 1 || pageId > totalPagesCount)
				.Distinct()
				.OrderBy(pageId => pageId)
				.ToList();

			if (invalidSplitPageIds.Count > 0)
			{
				_logger.Log($"Ignoring invalid split page IDs: [{string.Join(", ", invalidSplitPageIds)}]");
			}

			return configuredSplitPageIds
				.Where(pageId => pageId > 1 && pageId <= totalPagesCount)
				.Distinct()
				.OrderBy(pageId => pageId)
				.Select(pageId => (int)pageId)
				.ToList();
		}
	}
}
