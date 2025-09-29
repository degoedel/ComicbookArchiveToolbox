using ComicbookArchiveToolbox.CommonTools;
using ComicbookArchiveToolbox.CommonTools.Events;
using ComicbookArchiveToolbox.Services;
using Prism.Events;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ComicbookArchiveToolbox.Module.Merge.Service
{
	public class MergerPlugin
	{
		private readonly Logger _logger;
		private readonly IEventAggregator _eventAggregator;
		private readonly BatchProcessingManager _batchProcessingManager;

		public MergerPlugin(Logger logger, IEventAggregator eventAggregator, BatchProcessingManager batchProcessingManager)
		{
			_logger = logger ?? throw new ArgumentNullException(nameof(logger));
			_eventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));
			_batchProcessingManager = batchProcessingManager ?? throw new ArgumentNullException(nameof(batchProcessingManager));
		}

		public async Task MergeAsync(string outputFile, IList<string> files, long imageQuality, CancellationToken cancellationToken = default)
		{
			var stopwatch = Stopwatch.StartNew();
			_logger.Log($"=== Starting merge operation ===");
			_logger.Log($"Output file: {outputFile}");
			_logger.Log($"Input files count: {files.Count}");
			_logger.Log($"Image quality: {imageQuality}");

			// Log performance information
			_logger.Log($"System CPU Usage: {PerformanceMonitor.GetCurrentCpuUsage():F1}%");
			_logger.Log($"Performance Mode: {Settings.Instance.PerformanceMode}");
			_logger.Log($"Batch Size: {Settings.Instance.BatchSize}");

			_eventAggregator.GetEvent<BusinessEvent>().Publish(true);

			try
			{
				// Validate and normalize output file
				outputFile = NormalizeOutputPath(outputFile);
				_logger.Log($"Normalized output file: {outputFile}");

				// Setup buffer directories
				var outputFileInfo = new FileInfo(outputFile);
				string nameTemplate = outputFileInfo.Name.Substring(0, outputFileInfo.Name.Length - outputFileInfo.Extension.Length);
				string bufferPath = Settings.Instance.GetBufferDirectory(files[0], nameTemplate);
				string outputBuffer = Path.Combine(bufferPath, "outputBuffer");

				_logger.Log($"Buffer directory: {bufferPath}");
				_logger.Log($"Output buffer directory: {outputBuffer}");

				Directory.CreateDirectory(outputBuffer);

				var allMetadataFiles = new List<FileInfo>();
				var allPages = new List<FileInfo>();

				// Process archives in performance-aware batches
				_logger.Log($"Starting extraction of {files.Count} archives...");
				var extractionStopwatch = Stopwatch.StartNew();

				await _batchProcessingManager.ProcessFilesAsync(
					files.Select((file, index) => new { File = file, Index = index }),
					async item => await ExtractSingleArchiveAsync(item.File, item.Index, bufferPath, files.Count,
						allMetadataFiles, allPages, cancellationToken),
					cancellationToken);

				extractionStopwatch.Stop();
				_logger.Log($"All extractions completed in {extractionStopwatch.ElapsedMilliseconds:N0}ms");
				_logger.Log($"Total pages found: {allPages.Count}, Total metadata files: {allMetadataFiles.Count}");

				// Process pages in performance-aware batches
				var imagePages = allPages.Where(SystemTools.IsImageFile).ToList();
				_logger.Log($"Processing {imagePages.Count} image pages...");

				var processingStopwatch = Stopwatch.StartNew();
				await ProcessPagesAsync(imagePages, outputBuffer, nameTemplate, imageQuality, cancellationToken);
				processingStopwatch.Stop();
				_logger.Log($"Page processing completed in {processingStopwatch.ElapsedMilliseconds:N0}ms");

				// Final compression
				var compressionStopwatch = Stopwatch.StartNew();
				var compressionHelper = new CompressionHelper(_logger);
				_logger.Log($"Starting final compression to {Path.GetFileName(outputFile)}");

				await Task.Run(() => compressionHelper.CompressDirectoryContent(outputBuffer, outputFile), cancellationToken);

				compressionStopwatch.Stop();
				_logger.Log($"Compression completed in {compressionStopwatch.ElapsedMilliseconds:N0}ms");

				// Cleanup
				_logger.Log($"Cleaning buffer directory: {bufferPath}");
				try
				{
					SystemTools.CleanDirectory(bufferPath, _logger);
					_logger.Log($"Buffer cleanup completed");
				}
				catch (Exception ex)
				{
					_logger.Log($"WARNING: Failed to clean buffer directory: {ex.Message}");
				}

				stopwatch.Stop();
				_logger.Log($"=== Merge operation completed successfully in {stopwatch.ElapsedMilliseconds:N0}ms ({stopwatch.Elapsed:mm\\:ss}) ===");
			}
			catch (OperationCanceledException)
			{
				stopwatch.Stop();
				_logger.Log($"=== Merge operation cancelled after {stopwatch.ElapsedMilliseconds:N0}ms ===");
				throw;
			}
			catch (Exception ex)
			{
				stopwatch.Stop();
				_logger.Log($"=== MERGE FAILED after {stopwatch.ElapsedMilliseconds:N0}ms ===");
				_logger.Log($"ERROR: {ex.GetType().Name}: {ex.Message}");
				if (ex.InnerException != null)
				{
					_logger.Log($"Inner exception: {ex.InnerException.Message}");
				}
				throw;
			}
			finally
			{
				_eventAggregator.GetEvent<BusinessEvent>().Publish(false);
			}
		}

		// Synchronous wrapper for backward compatibility
		public void Merge(string outputFile, IList<string> files, long imageQuality)
		{
			try
			{
				MergeAsync(outputFile, files, imageQuality).GetAwaiter().GetResult();
			}
			catch (Exception ex)
			{
				_logger.Log($"ERROR in synchronous merge wrapper: {ex.Message}");
				throw;
			}
		}

		private string NormalizeOutputPath(string outputFile)
		{
			_logger.Log($"Validating and normalizing output path");

			try
			{
				var fi = new FileInfo(outputFile);
				string settingsExtension = $".{Settings.Instance.OutputFormat.ToString().ToLower()}";

				if (string.IsNullOrEmpty(fi.Extension))
				{
					_logger.Log($"Adding extension to filename: {settingsExtension}");
					return outputFile + settingsExtension;
				}

				if (fi.Extension != settingsExtension)
				{
					_logger.Log($"Replacing incorrect extension {fi.Extension} with {settingsExtension}");
					return outputFile[..^fi.Extension.Length] + settingsExtension;
				}

				return outputFile;
			}
			catch (Exception ex)
			{
				_logger.Log($"ERROR: Failed to normalize output path '{outputFile}': {ex.Message}");
				throw;
			}
		}

		private async Task ExtractSingleArchiveAsync(string archiveFile, int index, string bufferPath, int totalCount,
			List<FileInfo> allMetadataFiles, List<FileInfo> allPages, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();

			var stopwatch = Stopwatch.StartNew();
			_logger.Log($"Extracting archive {index + 1}/{totalCount}: {Path.GetFileName(archiveFile)}");

			try
			{
				int bufferPadSize = totalCount.ToString().Length;
				string decompressionBuffer = Path.Combine(bufferPath, $"archive_{index.ToString().PadLeft(bufferPadSize, '0')}");

				await Task.Run(() =>
				{
					var compressionHelper = new CompressionHelper(_logger);
					compressionHelper.DecompressToDirectory(archiveFile, decompressionBuffer);
				}, cancellationToken);

				// Parse files in the extracted directory
				var metadataFiles = new List<FileInfo>();
				var pages = new List<FileInfo>();
				SystemTools.ParseArchiveFiles(decompressionBuffer, ref metadataFiles, ref pages);

				// Thread-safe addition to shared collections
				lock (allMetadataFiles)
				{
					allMetadataFiles.AddRange(metadataFiles);
				}
				lock (allPages)
				{
					allPages.AddRange(pages);
				}

				stopwatch.Stop();
				_logger.Log($"Archive {index + 1} extracted in {stopwatch.ElapsedMilliseconds}ms - Found {pages.Count} pages, {metadataFiles.Count} metadata files");
			}
			catch (Exception ex)
			{
				stopwatch.Stop();
				_logger.Log($"ERROR: Failed to extract archive {index + 1} '{Path.GetFileName(archiveFile)}': {ex.Message}");
				throw;
			}
		}

		private async Task ProcessPagesAsync(List<FileInfo> imagePages, string outputBuffer, string nameTemplate,
			long imageQuality, CancellationToken cancellationToken)
		{
			if (imagePages.Count == 0)
			{
				_logger.Log("No image pages to process");
				return;
			}

			int pagePadSize = imagePages.Count.ToString().Length;
			var jpgConverter = new JpgConverter(_logger, imageQuality);

			await _batchProcessingManager.ProcessFilesAsync(
				imagePages.Select((page, index) => new { Page = page, Index = index + 1 }),
				async item => await ProcessSinglePageAsync(item.Page, item.Index, outputBuffer, nameTemplate,
					pagePadSize, jpgConverter, imageQuality, cancellationToken),
				cancellationToken);
		}

		private async Task ProcessSinglePageAsync(FileInfo page, int pageNumber, string outputBuffer, string nameTemplate,
			int pagePadSize, JpgConverter jpgConverter, long imageQuality, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();

			try
			{
				string destFile = Path.Combine(outputBuffer,
					$"{nameTemplate}_{pageNumber.ToString().PadLeft(pagePadSize, '0')}{page.Extension}".Replace(' ', '_'));

				await Task.Run(() =>
				{
					if (imageQuality == 100)
					{
						_logger.Log($"Moving page {pageNumber}: {page.Name} -> {Path.GetFileName(destFile)}");
						File.Move(page.FullName, destFile);
					}
					else
					{
						_logger.Log($"Re-encoding page {pageNumber}: {page.Name} -> {Path.GetFileName(destFile)} (quality={imageQuality})");
						jpgConverter.SaveJpeg(page.FullName, destFile);
						File.Delete(page.FullName);
					}
				}, cancellationToken);
			}
			catch (Exception ex)
			{
				_logger.Log($"ERROR: Failed to process page {pageNumber} '{page.Name}': {ex.Message}");
				throw;
			}
		}
	}
}