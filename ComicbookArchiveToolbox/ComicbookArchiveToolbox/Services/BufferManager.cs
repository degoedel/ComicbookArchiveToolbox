using System;
using System.IO;
using ComicbookArchiveToolbox.CommonTools;

namespace ComicbookArchiveToolbox.Services
{
    /// <summary>
    /// Implementation of buffer manager for handling temporary file operations.
    /// </summary>
    public class BufferManager : IBufferManager
    {
        private readonly Logger _logger;

        // Constants for file names
        private const string ComicInfoFileName = "ComicInfo.xml";

        public BufferManager(Logger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public BufferContext CreateContext(string filePath, string existingMetadataFile = "", string existingCalibreFile = "")
        {
            if (string.IsNullOrEmpty(filePath))
                throw new ArgumentException("File path cannot be null or empty", nameof(filePath));

            var fileInfo = new FileInfo(filePath);
            var nameTemplate = Path.GetFileNameWithoutExtension(fileInfo.Name);
            var bufferPath = Settings.Instance.GetBufferDirectory(filePath, nameTemplate);

            // Ensure buffer directory exists
            if (!Directory.Exists(bufferPath))
                Directory.CreateDirectory(bufferPath);

            var metadataFilePath = string.IsNullOrEmpty(existingMetadataFile)
                ? Path.Combine(bufferPath, ComicInfoFileName)
                : existingMetadataFile;

            var calibreMetaDataFilePath = string.IsNullOrEmpty(existingCalibreFile)
                ? string.Empty 
                : existingCalibreFile;

            _logger.Log($"Created buffer context: {bufferPath}");

            return new BufferContext
            {
                BufferPath = bufferPath,
                BufferDirectory = new DirectoryInfo(bufferPath),
                MetadataFilePath = metadataFilePath,
                CalibreMetaDataFilePath = calibreMetaDataFilePath
            };
        }

        public void Cleanup(string bufferPath)
        {
            if (string.IsNullOrEmpty(bufferPath) || !Directory.Exists(bufferPath))
                return;

            try
            {
                _logger.Log($"Cleaning buffer directory: {bufferPath}");
                SystemTools.CleanDirectory(bufferPath, _logger);
                _logger.Log("Buffer cleanup completed.");
            }
            catch (Exception ex)
            {
                _logger.Log($"ERROR: Failed to cleanup buffer directory: {ex.Message}");
            }
        }
    }
}