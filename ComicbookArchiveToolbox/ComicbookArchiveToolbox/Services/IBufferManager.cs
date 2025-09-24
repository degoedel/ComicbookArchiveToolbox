using System.IO;

namespace ComicbookArchiveToolbox.Services
{
    /// <summary>
    /// Service for managing buffer directories and file contexts during metadata operations.
    /// </summary>
    public interface IBufferManager
    {
        /// <summary>
        /// Creates a buffer context for the specified file.
        /// </summary>
        /// <param name="filePath">Path to the file being processed</param>
        /// <param name="existingMetadataFile">Existing metadata file path (optional)</param>
        /// <param name="existingCalibreFile">Existing Calibre file path (optional)</param>
        /// <returns>Buffer context with all necessary paths</returns>
        BufferContext CreateContext(string filePath, string existingMetadataFile = "", string existingCalibreFile = "");

        /// <summary>
        /// Cleans up a buffer directory and its contents.
        /// </summary>
        /// <param name="bufferPath">Path to the buffer directory</param>
        void Cleanup(string bufferPath);
    }

    /// <summary>
    /// Context information for buffer operations.
    /// </summary>
    public class BufferContext
    {
        public string BufferPath { get; set; } = string.Empty;
        public DirectoryInfo BufferDirectory { get; set; } = null!;
        public string MetadataFilePath { get; set; } = string.Empty;
        public string CalibreMetaDataFilePath { get; set; } = string.Empty;
    }
}