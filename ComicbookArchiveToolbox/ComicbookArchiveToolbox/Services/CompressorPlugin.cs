using ComicbookArchiveToolbox.CommonTools;
using ComicbookArchiveToolbox.CommonTools.Events;
using Prism.Events;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ComicbookArchiveToolbox.Services
{
	public class CompressorPlugin
	{
		private Logger _logger;
		private IEventAggregator _eventAggregator;

		public CompressorPlugin(Logger logger, IEventAggregator eventAggregator)
		{
			_logger = logger;
			_eventAggregator = eventAggregator;
		}

		public async Task CompressAsync(string inputFile, string outputFile, long imageQuality, bool resizeByPx, long size, long ratio, CancellationToken cancellationToken = default)
		{
			var stopwatch = Stopwatch.StartNew();
			_logger.Log($"=== Starting compression operation ===");
			_logger.Log($"Input file: {inputFile}");
			_logger.Log($"Output file: {outputFile}");
			_logger.Log($"Settings - Quality: {imageQuality}, Resize by pixels: {resizeByPx}, Size: {size}, Ratio: {ratio}%");

			_eventAggregator.GetEvent<BusinessEvent>().Publish(true);

			try
			{
				// Validate input file
				if (!File.Exists(inputFile))
				{
					_logger.Log($"ERROR: Input file does not exist: {inputFile}");
					throw new FileNotFoundException($"Input file not found: {inputFile}");
				}

				var inputFileInfo = new FileInfo(inputFile);
				_logger.Log($"Input file size: {inputFileInfo.Length:N0} bytes ({inputFileInfo.Length / (1024.0 * 1024.0):F2} MB)");

				// Pre-validate and normalize output file path
				outputFile = NormalizeOutputPath(outputFile);
				_logger.Log($"Normalized output file: {outputFile}");

				// Setup buffer directories
				string nameTemplate = "page";
				string bufferPath = Settings.Instance.GetBufferDirectory(inputFile, nameTemplate);
				string outputBuffer = Path.Combine(bufferPath, "outputBuffer");

				_logger.Log($"Buffer directory: {bufferPath}");
				_logger.Log($"Output buffer directory: {outputBuffer}");

				try
				{
					Directory.CreateDirectory(outputBuffer);
					_logger.Log($"Buffer directories created successfully");
				}
				catch (Exception ex)
				{
					_logger.Log($"ERROR: Failed to create buffer directories: {ex.Message}");
					throw;
				}

				// Initialize collections and helpers
				var metadataFiles = new List<FileInfo>();
				var pages = new List<FileInfo>();
				var ch1 = new CompressionHelper(_logger);

				// Extract archive
				_logger.Log($"Starting decompression of {Path.GetFileName(inputFile)}");
				var extractStopwatch = Stopwatch.StartNew();

				try
				{
					ch1.DecompressToDirectory(inputFile, bufferPath);
					extractStopwatch.Stop();
					_logger.Log($"Extraction completed in {extractStopwatch.ElapsedMilliseconds:N0}ms");
				}
				catch (Exception ex)
				{
					_logger.Log($"ERROR: Failed to extract archive: {ex.Message}");
					throw;
				}

				// Parse files (maintains natural order)
				_logger.Log($"Parsing extracted files...");
				try
				{
					SystemTools.ParseArchiveFiles(bufferPath, ref metadataFiles, ref pages);
					_logger.Log($"Found {pages.Count} page files and {metadataFiles.Count} metadata files");

					var imagePages = pages.Where(SystemTools.IsImageFile).ToList();
					_logger.Log($"Image pages to process: {imagePages.Count}");

					if (imagePages.Count == 0)
					{
						_logger.Log($"WARNING: No image files found in archive");
					}
				}
				catch (Exception ex)
				{
					_logger.Log($"ERROR: Failed to parse archive files: {ex.Message}");
					throw;
				}

				// Process metadata files
				var metadataStopwatch = Stopwatch.StartNew();
				await ProcessMetadataFilesAsync(metadataFiles, outputBuffer, cancellationToken);
				metadataStopwatch.Stop();
				_logger.Log($"Metadata processing completed in {metadataStopwatch.ElapsedMilliseconds:N0}ms");

				// Process image pages with parallel optimization while preserving order
				var imageProcessingStopwatch = Stopwatch.StartNew();
				await ProcessImagePagesAsync(pages, outputBuffer, nameTemplate, imageQuality, resizeByPx, size, ratio, cancellationToken);
				imageProcessingStopwatch.Stop();
				_logger.Log($"Image processing completed in {imageProcessingStopwatch.ElapsedMilliseconds:N0}ms");

				// Compress final archive
				var compressionStopwatch = Stopwatch.StartNew();
				var ch2 = new CompressionHelper(_logger);
				_logger.Log($"Starting final compression to {Path.GetFileName(outputFile)}");

				try
				{
					ch2.CompressDirectoryContent(outputBuffer, outputFile);
					compressionStopwatch.Stop();
					_logger.Log($"Final compression completed in {compressionStopwatch.ElapsedMilliseconds:N0}ms");

					// Log output file information
					if (File.Exists(outputFile))
					{
						var outputFileInfo = new FileInfo(outputFile);
						_logger.Log($"Output file size: {outputFileInfo.Length:N0} bytes ({outputFileInfo.Length / (1024.0 * 1024.0):F2} MB)");

						double compressionRatio = (1.0 - (double)outputFileInfo.Length / inputFileInfo.Length) * 100;
						_logger.Log($"Compression ratio: {compressionRatio:F1}% (saved {inputFileInfo.Length - outputFileInfo.Length:N0} bytes)");
					}
				}
				catch (Exception ex)
				{
					_logger.Log($"ERROR: Failed to create final compressed archive: {ex.Message}");
					throw;
				}

				// Cleanup
				_logger.Log($"Cleaning up buffer directory: {bufferPath}");
				try
				{
					SystemTools.CleanDirectory(bufferPath, _logger);
					_logger.Log($"Buffer cleanup completed");
				}
				catch (Exception ex)
				{
					_logger.Log($"WARNING: Failed to clean buffer directory completely: {ex.Message}");
				}

				stopwatch.Stop();
				_logger.Log($"=== Compression operation completed successfully in {stopwatch.ElapsedMilliseconds:N0}ms ({stopwatch.Elapsed:mm\\:ss}) ===");
			}
			catch (OperationCanceledException)
			{
				stopwatch.Stop();
				_logger.Log($"=== Compression operation cancelled after {stopwatch.ElapsedMilliseconds:N0}ms ===");
				throw;
			}
			catch (Exception ex)
			{
				stopwatch.Stop();
				_logger.Log($"=== COMPRESSION FAILED after {stopwatch.ElapsedMilliseconds:N0}ms ===");
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
		public void Compress(string inputFile, string outputFile, long imageQuality, bool resizeByPx, long size, long ratio)
		{
			try
			{
				CompressAsync(inputFile, outputFile, imageQuality, resizeByPx, size, ratio).GetAwaiter().GetResult();
			}
			catch (Exception ex)
			{
				_logger.Log($"ERROR in synchronous compression wrapper: {ex.Message}");
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

				_logger.Log($"Output path validation completed");
				return outputFile;
			}
			catch (Exception ex)
			{
				_logger.Log($"ERROR: Failed to normalize output path '{outputFile}': {ex.Message}");
				throw;
			}
		}

		private async Task ProcessMetadataFilesAsync(List<FileInfo> metadataFiles, string outputBuffer, CancellationToken cancellationToken)
		{
			if (!Settings.Instance.IncludeMetadata)
			{
				_logger.Log($"Metadata inclusion disabled in settings, skipping {metadataFiles.Count} metadata files");
				return;
			}

			if (metadataFiles.Count == 0)
			{
				_logger.Log($"No metadata files found to process");
				return;
			}

			_logger.Log($"Processing {metadataFiles.Count} metadata files...");

			try
			{
				var copyTasks = metadataFiles.Select(async (file, index) =>
				{
					cancellationToken.ThrowIfCancellationRequested();

					try
					{
						string destFile = SystemTools.GetOutputFilePath(outputBuffer, file);
						_logger.Log($"Copying metadata file {index + 1}/{metadataFiles.Count}: {file.Name}");

						// Use async file copy for better performance
						using var sourceStream = new FileStream(file.FullName, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 81920, useAsync: true);
						using var destStream = new FileStream(destFile, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 81920, useAsync: true);
						await sourceStream.CopyToAsync(destStream, cancellationToken);

						_logger.Log($"Successfully copied metadata file: {file.Name}");
					}
					catch (Exception ex)
					{
						_logger.Log($"ERROR: Failed to copy metadata file '{file.Name}': {ex.Message}");
						throw;
					}
				});

				await Task.WhenAll(copyTasks);
				_logger.Log($"All metadata files processed successfully");
			}
			catch (Exception ex)
			{
				_logger.Log($"ERROR: Failed to process metadata files: {ex.Message}");
				throw;
			}
		}

		private async Task ProcessImagePagesAsync(List<FileInfo> pages, string outputBuffer, string nameTemplate,
			long imageQuality, bool resizeByPx, long size, long ratio, CancellationToken cancellationToken)
		{
			var imagePages = pages.Where(SystemTools.IsImageFile).ToList();

			if (imagePages.Count == 0)
			{
				_logger.Log($"No image pages to process");
				return;
			}

			_logger.Log($"Starting processing of {imagePages.Count} image pages with quality={imageQuality}, resizeByPx={resizeByPx}, size={size}, ratio={ratio}%");

			int pagePadSize = imagePages.Count.ToString().Length;
			var jpgConverter = new JpgConverter(_logger, imageQuality);

			// Create ordered processing tasks that preserve page sequence
			var processingTasks = new List<Task>();
			var semaphore = new SemaphoreSlim(Environment.ProcessorCount, Environment.ProcessorCount);

			_logger.Log($"Using {Environment.ProcessorCount} concurrent processing threads");

			try
			{
				int pageIndex = 0;
				foreach (var page in imagePages)
				{
					int currentPageIndex = ++pageIndex; // Capture current index for closure

					var task = ProcessSinglePageAsync(page, outputBuffer, nameTemplate, pagePadSize,
						currentPageIndex, jpgConverter, imageQuality, resizeByPx, size, ratio,
						semaphore, cancellationToken);

					processingTasks.Add(task);
				}

				// Wait for all pages to be processed
				await Task.WhenAll(processingTasks);
				_logger.Log($"All {imagePages.Count} image pages processed successfully");
			}
			catch (Exception ex)
			{
				_logger.Log($"ERROR: Failed to process image pages: {ex.Message}");
				throw;
			}
			finally
			{
				semaphore.Dispose();
			}
		}

		private async Task ProcessSinglePageAsync(FileInfo page, string outputBuffer, string nameTemplate,
			int pagePadSize, int pageNumber, JpgConverter jpgConverter, long imageQuality,
			bool resizeByPx, long size, long ratio, SemaphoreSlim semaphore, CancellationToken cancellationToken)
		{
			await semaphore.WaitAsync(cancellationToken);

			try
			{
				cancellationToken.ThrowIfCancellationRequested();

				string destFile = SystemTools.GetOutputFilePath(outputBuffer, page);
				var destFi = new FileInfo(destFile);
				destFile = Path.Combine(destFi.Directory.FullName,
					$"{nameTemplate}_{pageNumber.ToString().PadLeft(pagePadSize, '0')}{page.Extension}".Replace(' ', '_'));

				var pageStopwatch = Stopwatch.StartNew();

				await Task.Run(() =>
				{
					try
					{
						if (!resizeByPx && ratio == 100)
						{
							// No resizing needed
							if (imageQuality == 100)
							{
								// Simple file move
								_logger.Log($"Moving page {pageNumber}: {page.Name} -> {Path.GetFileName(destFile)} (no processing needed)");
								File.Move(page.FullName, destFile);
							}
							else
							{
								// Re-encode with quality setting
								_logger.Log($"Re-encoding page {pageNumber}: {page.Name} -> {Path.GetFileName(destFile)} (quality={imageQuality})");
								jpgConverter.SaveJpeg(page.FullName, destFile);
								File.Delete(page.FullName);
							}
						}
						else
						{
							// Resizing required
							string resizeInfo = resizeByPx ? $"resize to {size}px height" : $"resize to {ratio}% ratio";
							_logger.Log($"Processing page {pageNumber}: {page.Name} -> {Path.GetFileName(destFile)} ({resizeInfo}, quality={imageQuality})");

							Bitmap reduced = resizeByPx ?
								jpgConverter.ResizeImageByPx(page.FullName, size) :
								jpgConverter.ResizeImageByRatio(page.FullName, ratio);

							jpgConverter.SaveJpeg(reduced, destFile);
							reduced?.Dispose(); // Ensure proper cleanup
							File.Delete(page.FullName);
						}

						pageStopwatch.Stop();
						_logger.Log($"Page {pageNumber} completed in {pageStopwatch.ElapsedMilliseconds}ms");
					}
					catch (Exception ex)
					{
						pageStopwatch.Stop();
						_logger.Log($"ERROR: Failed to process page {pageNumber} '{page.Name}': {ex.Message}");
						throw;
					}
				}, cancellationToken);
			}
			catch (Exception ex)
			{
				_logger.Log($"ERROR: Page {pageNumber} processing failed: {ex.Message}");
				throw;
			}
			finally
			{
				semaphore.Release();
			}
		}
	}
}