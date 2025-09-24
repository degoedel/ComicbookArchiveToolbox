using System.Collections.ObjectModel;
using System.Threading.Tasks;
using ComicbookArchiveToolbox.CommonTools;

namespace ComicbookArchiveToolbox.Services
{
    /// <summary>
    /// Service for handling comic metadata operations including loading and saving
    /// metadata in different formats (ComicInfo XML and Calibre HTML).
    /// </summary>
    public interface IMetadataService
    {
        /// <summary>
        /// Loads metadata from a ComicInfo XML file.
        /// </summary>
        /// <param name="xmlFilePath">Path to the XML file</param>
        /// <returns>Collection of comic metadata</returns>
        Task<ObservableCollection<ComicMetadata>> LoadComicInfoAsync(string xmlFilePath);

        /// <summary>
        /// Loads metadata from a Calibre HTML file with Dublin Core format.
        /// </summary>
        /// <param name="htmlFilePath">Path to the HTML file</param>
        /// <returns>Collection of comic metadata</returns>
        Task<ObservableCollection<ComicMetadata>> LoadCalibreAsync(string htmlFilePath);

        /// <summary>
        /// Saves metadata to a ComicInfo XML file.
        /// </summary>
        /// <param name="metadata">Metadata collection to save</param>
        /// <param name="xmlFilePath">Output XML file path</param>
        Task SaveComicInfoAsync(ObservableCollection<ComicMetadata> metadata, string xmlFilePath);

        /// <summary>
        /// Saves metadata to a Calibre HTML file with Dublin Core format.
        /// </summary>
        /// <param name="metadata">Metadata collection to save</param>
        /// <param name="htmlFilePath">Output HTML file path</param>
        Task SaveCalibreAsync(ObservableCollection<ComicMetadata> metadata, string htmlFilePath);

        /// <summary>
        /// Checks if an HTML file contains Calibre metadata.
        /// </summary>
        /// <param name="htmlFilePath">Path to the HTML file</param>
        /// <returns>True if the file contains Calibre metadata</returns>
        bool ContainsCalibreMetadata(string htmlFilePath);

        /// <summary>
        /// Initializes default metadata based on settings.
        /// </summary>
        /// <returns>Collection with default metadata fields</returns>
        ObservableCollection<ComicMetadata> InitializeDefaultMetadata();

        /// <summary>
        /// Adds missing default metadata fields to an existing collection.
        /// </summary>
        /// <param name="metadataCollection">Existing metadata collection</param>
        void AddMissingDefaultMetadata(ObservableCollection<ComicMetadata> metadataCollection);
    }
}