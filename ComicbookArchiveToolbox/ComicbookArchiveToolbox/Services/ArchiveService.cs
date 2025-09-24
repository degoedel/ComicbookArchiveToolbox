using System;
using System.IO;
using System.Threading.Tasks;
using ComicbookArchiveToolbox.CommonTools;

namespace ComicbookArchiveToolbox.Services
{
    /// <summary>
    /// Implementation of archive service for handling comic archive operations.
    /// </summary>
    public class ArchiveService : IArchiveService
    {
        private readonly Logger _logger;
        private readonly CompressionHelper _compressionHelper;

        // Supported direct update extensions
        private static readonly string[] DirectUpdateExtensions = { ".cbz", ".cb7", ".cbt" };

        public ArchiveService(Logger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _compressionHelper = new CompressionHelper(_logger);
        }

        public async Task ExtractMetadataFilesAsync(string archivePath, string bufferPath)
        {
            if (!File.Exists(archivePath))
                throw new FileNotFoundException($"Archive file not found: {archivePath}");

            if (!Directory.Exists(bufferPath))
                Directory.CreateDirectory(bufferPath);

            _logger.Log($"Extracting metadata files from {Path.GetFileName(archivePath)}");

            // Run extraction on background thread to avoid blocking
            await Task.Run(() =>
            {
                _compressionHelper.ExtractFileType(archivePath, bufferPath, "*.xml");
                _compressionHelper.ExtractFileType(archivePath, bufferPath, "*.html");
            });
        }

        public async Task UpdateArchiveAsync(string archivePath, string metadataFilePath, string calibreFilePath)
        {
            if (!File.Exists(archivePath))
                throw new FileNotFoundException($"Archive file not found: {archivePath}");

            _logger.Log($"Updating archive {Path.GetFileName(archivePath)} with metadata files");

            await Task.Run(() =>
            {
                if (File.Exists(metadataFilePath))
                {
                    _compressionHelper.UpdateFileForced(archivePath, metadataFilePath, true);
                }

                if (!string.IsNullOrEmpty(calibreFilePath) && File.Exists(calibreFilePath))
                {
                    _compressionHelper.UpdateFileForced(archivePath, calibreFilePath, true);
                }
            });
        }

        public async Task RecompressArchiveAsync(string archivePath, string bufferPath, string outputPath)
        {
            if (!File.Exists(archivePath))
                throw new FileNotFoundException($"Archive file not found: {archivePath}");

            if (!Directory.Exists(bufferPath))
                throw new DirectoryNotFoundException($"Buffer directory not found: {bufferPath}");

            _logger.Log("Archive format does not support update with 7zip. Decompression and recompression is required");

            await Task.Run(() =>
            {
                // Extract entire archive to buffer
                _compressionHelper.DecompressToDirectory(archivePath, bufferPath);

                // Compress buffer contents to new archive
                _compressionHelper.CompressDirectoryContent(bufferPath, outputPath);
            });

            _logger.Log("Recompression completed.");
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
    }
}