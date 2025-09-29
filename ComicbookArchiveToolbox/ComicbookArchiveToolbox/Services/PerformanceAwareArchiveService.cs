using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ComicbookArchiveToolbox.CommonTools;

namespace ComicbookArchiveToolbox.Services
{
	/// <summary>
	/// Performance-aware implementation of archive service with CPU usage controls.
	/// </summary>
	public class PerformanceAwareArchiveService : IArchiveService
	{
		private readonly Logger _logger;
		private readonly CompressionHelper _compressionHelper;
		private readonly SemaphoreSlim _concurrencyLimiter;
		private static readonly string[] DirectUpdateExtensions = { ".cbz", ".cb7", ".cbt" };

		public PerformanceAwareArchiveService(Logger logger)
		{
			_logger = logger ?? throw new ArgumentNullException(nameof(logger));
			_compressionHelper = new CompressionHelper(_logger);

			// Initialize concurrency limiter based on settings
			var maxConcurrency = GetMaxConcurrency();
			_concurrencyLimiter = new SemaphoreSlim(maxConcurrency, maxConcurrency);

			_logger.Log($"Archive service initialized with max concurrency: {maxConcurrency}");
		}

		private int GetMaxConcurrency()
		{
			var settings = Settings.Instance;
			return settings.PerformanceMode switch
			{
				SerializationSettings.EPerformanceMode.LowResource => 1,
				SerializationSettings.EPerformanceMode.Balanced => Math.Max(1, settings.MaxConcurrentOperations),
				SerializationSettings.EPerformanceMode.HighPerformance => Environment.ProcessorCount,
				_ => Math.Max(1, Environment.ProcessorCount / 2)
			};
		}

		public async Task ExtractMetadataFilesAsync(string archivePath, string bufferPath)
		{
			if (!File.Exists(archivePath))
				throw new FileNotFoundException($"Archive file not found: {archivePath}");

			if (!Directory.Exists(bufferPath))
				Directory.CreateDirectory(bufferPath);

			await _concurrencyLimiter.WaitAsync();
			try
			{
				_logger.Log($"Extracting metadata files from {Path.GetFileName(archivePath)}");

				if (Settings.Instance.PerformanceMode == SerializationSettings.EPerformanceMode.LowResource)
				{
					// Sequential extraction for low resource mode
					await Task.Run(() =>
					{
						_compressionHelper.ExtractFileType(archivePath, bufferPath, "*.xml");
						ApplyThrottling();
						_compressionHelper.ExtractFileType(archivePath, bufferPath, "*.html");
					});
				}
				else
				{
					// Parallel extraction for balanced and high performance modes
					await Task.Run(() =>
					{
						var xmlTask = Task.Run(() => _compressionHelper.ExtractFileType(archivePath, bufferPath, "*.xml"));
						var htmlTask = Task.Run(() => _compressionHelper.ExtractFileType(archivePath, bufferPath, "*.html"));

						Task.WaitAll(xmlTask, htmlTask);
					});
				}
			}
			finally
			{
				_concurrencyLimiter.Release();
			}
		}

		public async Task UpdateArchiveAsync(string archivePath, string metadataFilePath, string calibreFilePath)
		{
			if (!File.Exists(archivePath))
				throw new FileNotFoundException($"Archive file not found: {archivePath}");

			await _concurrencyLimiter.WaitAsync();
			try
			{
				_logger.Log($"Updating archive {Path.GetFileName(archivePath)} with metadata files");

				await Task.Run(() =>
				{
					if (File.Exists(metadataFilePath))
					{
						_compressionHelper.UpdateFileForced(archivePath, metadataFilePath, true);
						ApplyThrottling();
					}

					if (!string.IsNullOrEmpty(calibreFilePath) && File.Exists(calibreFilePath))
					{
						_compressionHelper.UpdateFileForced(archivePath, calibreFilePath, true);
						ApplyThrottling();
					}
				});
			}
			finally
			{
				_concurrencyLimiter.Release();
			}
		}

		public async Task RecompressArchiveAsync(string archivePath, string bufferPath, string outputPath)
		{
			if (!File.Exists(archivePath))
				throw new FileNotFoundException($"Archive file not found: {archivePath}");

			if (!Directory.Exists(bufferPath))
				throw new DirectoryNotFoundException($"Buffer directory not found: {bufferPath}");

			await _concurrencyLimiter.WaitAsync();
			try
			{
				_logger.Log("Archive format does not support update with 7zip. Decompression and recompression is required");

				await Task.Run(() =>
				{
					// Extract entire archive to buffer
					_compressionHelper.DecompressToDirectory(archivePath, bufferPath);
					ApplyThrottling();

					// Compress buffer contents to new archive
					_compressionHelper.CompressDirectoryContent(bufferPath, outputPath);
				});

				_logger.Log("Recompression completed.");
			}
			finally
			{
				_concurrencyLimiter.Release();
			}
		}

		private void ApplyThrottling()
		{
			if (Settings.Instance.EnableThrottling && Settings.Instance.ThrottleDelayMs > 0)
			{
				Thread.Sleep(Settings.Instance.ThrottleDelayMs);
			}
		}

		public bool SupportsDirectUpdate(string fileExtension)
		{
			if (string.IsNullOrEmpty(fileExtension))
				return false;

			foreach (var supportedExtension in DirectUpdateExtensions)
			{
				if (string.Equals(fileExtension, supportedExtension, StringComparison.OrdinalIgnoreCase))
					return true;
			}

			return false;
		}

		public (FileInfo[] xmlFiles, FileInfo[] htmlFiles) FindMetadataFiles(DirectoryInfo directory)
		{
			if (directory == null || !directory.Exists)
				return (Array.Empty<FileInfo>(), Array.Empty<FileInfo>());

			try
			{
				var xmlFiles = directory.GetFiles("*.xml");
				var htmlFiles = directory.GetFiles("*.html");

				_logger.Log($"Found {xmlFiles.Length} XML files and {htmlFiles.Length} HTML files in buffer directory");

				return (xmlFiles, htmlFiles);
			}
			catch (Exception ex)
			{
				_logger.Log($"ERROR: Failed to find metadata files: {ex.Message}");
				return (Array.Empty<FileInfo>(), Array.Empty<FileInfo>());
			}
		}

		public void Dispose()
		{
			_concurrencyLimiter?.Dispose();
		}
	}
}