using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ComicbookArchiveToolbox.CommonTools;

namespace ComicbookArchiveToolbox.Services
{
	/// <summary>
	/// Manages batch processing operations with performance controls.
	/// </summary>
	public class BatchProcessingManager
	{
		private readonly Logger _logger;
		private readonly IArchiveService _archiveService;

		public BatchProcessingManager(Logger logger, IArchiveService archiveService)
		{
			_logger = logger ?? throw new ArgumentNullException(nameof(logger));
			_archiveService = archiveService ?? throw new ArgumentNullException(nameof(archiveService));
		}

		public async Task ProcessFilesAsync<T>(IEnumerable<T> items, Func<T, Task> processor,
			CancellationToken cancellationToken = default)
		{
			var settings = Settings.Instance;
			var itemList = items.ToList();

			if (!itemList.Any())
				return;

			_logger.Log($"Processing {itemList.Count} items in performance mode: {settings.PerformanceMode}");

			if (settings.UseProgressiveBatching)
			{
				await ProcessInBatches(itemList, processor, settings.BatchSize, cancellationToken);
			}
			else
			{
				await ProcessConcurrently(itemList, processor, settings.MaxConcurrentOperations, cancellationToken);
			}
		}

		private async Task ProcessInBatches<T>(List<T> items, Func<T, Task> processor,
			int batchSize, CancellationToken cancellationToken)
		{
			var settings = Settings.Instance;

			for (int i = 0; i < items.Count; i += batchSize)
			{
				cancellationToken.ThrowIfCancellationRequested();

				var batch = items.Skip(i).Take(batchSize);
				_logger.Log($"Processing batch {(i / batchSize) + 1} of {(items.Count + batchSize - 1) / batchSize}");

				if (settings.PerformanceMode == SerializationSettings.EPerformanceMode.LowResource)
				{
					// Process items sequentially in low resource mode
					foreach (var item in batch)
					{
						cancellationToken.ThrowIfCancellationRequested();
						await processor(item);

						if (settings.EnableThrottling)
							await Task.Delay(settings.ThrottleDelayMs, cancellationToken);
					}
				}
				else
				{
					// Process batch items in parallel
					var batchTasks = batch.Select(processor);
					await Task.WhenAll(batchTasks);
				}

				// Add delay between batches if throttling is enabled
				if (settings.EnableThrottling && i + batchSize < items.Count)
				{
					await Task.Delay(settings.ThrottleDelayMs * 2, cancellationToken);
				}
			}
		}

		private async Task ProcessConcurrently<T>(List<T> items, Func<T, Task> processor,
			int maxConcurrency, CancellationToken cancellationToken)
		{
			using var semaphore = new SemaphoreSlim(maxConcurrency, maxConcurrency);

			var tasks = items.Select(async item =>
			{
				await semaphore.WaitAsync(cancellationToken);
				try
				{
					await processor(item);
				}
				finally
				{
					semaphore.Release();
				}
			});

			await Task.WhenAll(tasks);
		}
	}
}
