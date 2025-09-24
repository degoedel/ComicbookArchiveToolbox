using System.IO;
using System.Threading.Tasks;

namespace ComicbookArchiveToolbox.Services
{
    /// <summary>
    /// Service for handling comic archive operations including extraction and compression.
    /// </summary>
    public interface IArchiveService
    {
        /// <summary>
        /// Extracts metadata files (XML and HTML) from an archive to a buffer directory.
        /// </summary>
        /// <param name="archivePath">Path to the archive file</param>
        /// <param name="bufferPath">Buffer directory path</param>
        Task ExtractMetadataFilesAsync(string archivePath, string bufferPath);

        /// <summary>
        /// Updates an archive with new metadata files.
        /// </summary>
        /// <param name="archivePath">Path to the archive file</param>
        /// <param name="metadataFilePath">Path to the ComicInfo XML file</param>
        /// <param name="calibreFilePath">Path to the Calibre HTML file (optional)</param>
        Task UpdateArchiveAsync(string archivePath, string metadataFilePath, string calibreFilePath);

        /// <summary>
        /// Performs full recompression of an archive when direct update is not supported.
        /// </summary>
        /// <param name="archivePath">Path to the original archive</param>
        /// <param name="bufferPath">Buffer directory containing extracted files</param>
        /// <param name="outputPath">Output path for the new archive</param>
        Task RecompressArchiveAsync(string archivePath, string bufferPath, string outputPath);

        /// <summary>
        /// Determines if an archive format supports direct update operations.
        /// </summary>
        /// <param name="fileExtension">File extension (e.g., ".cbz", ".cbr")</param>
        /// <returns>True if direct update is supported</returns>
        bool SupportsDirectUpdate(string fileExtension);

        /// <summary>
        /// Finds metadata files in a directory.
        /// </summary>
        /// <param name="directory">Directory to search</param>
        /// <returns>Array of XML files and array of HTML files</returns>
        (FileInfo[] xmlFiles, FileInfo[] htmlFiles) FindMetadataFiles(DirectoryInfo directory);
    }
}