using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using ComicbookArchiveToolbox.CommonTools;

namespace ComicbookArchiveToolbox.Services
{
    /// <summary>
    /// Implementation of metadata service for handling comic metadata operations.
    /// </summary>
    public class MetadataService : IMetadataService
    {
        private readonly Logger _logger;

        // Compiled regex patterns for better performance
        private static readonly Regex DublinCoreRegex = new(
            @"<meta\s+name=""DC\.(\w+)""\s+content=""([^""]*)""\s*/?>", 
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex TitleRegex = new(
            @"<title>([^<]*)</title>", 
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex CalibreDetectionRegex = new(
            @"DC\.(title|creator)|calibre", 
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        // Static mappings to avoid recreation
        private static readonly Dictionary<string, string> DublinCoreToComicInfoMappings = new(StringComparer.OrdinalIgnoreCase)
        {
            { "title", "Title" },
            { "creator", "Writer" },
            { "contributor", "Penciller" },
            { "publisher", "Publisher" },
            { "date", "Year" },
            { "identifier", "Web" },
            { "language", "LanguageISO" },
            { "subject", "Genre" },
            { "description", "Summary" },
            { "rights", "Rights" },
            { "source", "Series" }
        };

        private static readonly Dictionary<string, string> ComicInfoToDublinCoreMappings =
            DublinCoreToComicInfoMappings.ToDictionary(kvp => kvp.Value, kvp => kvp.Key, StringComparer.OrdinalIgnoreCase);

        public MetadataService(Logger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<ObservableCollection<ComicMetadata>> LoadComicInfoAsync(string xmlFilePath)
        {
            if (!File.Exists(xmlFilePath))
                throw new FileNotFoundException($"XML file not found: {xmlFilePath}");

            _logger.Log($"Loading XML metadata from {Path.GetFileName(xmlFilePath)}");

            var xmlDoc = new XmlDocument();
            xmlDoc.Load(xmlFilePath);

            var metadataCollection = new ObservableCollection<ComicMetadata>();

            if (xmlDoc.DocumentElement != null)
            {
                foreach (XmlNode node in xmlDoc.DocumentElement.ChildNodes)
                {
                    if (node is XmlElement element)
                    {
                        metadataCollection.Add(new ComicMetadata(element.Name, element.InnerText));
                    }
                }
            }

            return metadataCollection;
        }

        public async Task<ObservableCollection<ComicMetadata>> LoadCalibreAsync(string htmlFilePath)
        {
            if (!File.Exists(htmlFilePath))
                throw new FileNotFoundException($"HTML file not found: {htmlFilePath}");

            try
            {
                var content = await File.ReadAllTextAsync(htmlFilePath);
                var metadataCollection = new ObservableCollection<ComicMetadata>();

                ExtractDublinCoreMetadata(content, metadataCollection);
                ExtractTitleFromHtml(content, metadataCollection);
                AddMissingDefaultMetadata(metadataCollection);

                _logger.Log($"Successfully imported {metadataCollection.Count} metadata fields from Calibre HTML");
                return metadataCollection;
            }
            catch (Exception ex)
            {
                _logger.Log($"ERROR: Failed to parse Calibre HTML metadata: {ex.Message}");
                return InitializeDefaultMetadata();
            }
        }

        public async Task SaveComicInfoAsync(ObservableCollection<ComicMetadata> metadata, string xmlFilePath)
        {
            if (metadata == null)
                throw new ArgumentNullException(nameof(metadata));

            _logger.Log($"Saving metadata as ComicRack XML in {xmlFilePath}");

            var xmlDoc = CreateComicInfoXmlDocument(metadata);

            var settings = new XmlWriterSettings 
            { 
                Indent = true,
                Async = true
            };

            using var writer = XmlWriter.Create(xmlFilePath, settings);
            xmlDoc.WriteTo(writer);
            await writer.FlushAsync();
        }

        public async Task SaveCalibreAsync(ObservableCollection<ComicMetadata> metadata, string htmlFilePath)
        {
            if (metadata == null)
                throw new ArgumentNullException(nameof(metadata));

            var title = GetMetadataValue(metadata, "Title") ?? "Unknown Title";
            var creator = GetMetadataValue(metadata, "Writer") ?? GetMetadataValue(metadata, "Penciller") ?? "Unknown Creator";

            var htmlContent = BuildCalibreHtmlContent(metadata, title, creator);

            await File.WriteAllTextAsync(htmlFilePath, htmlContent, Encoding.UTF8);
            _logger.Log($"Generated Calibre HTML metadata file: {Path.GetFileName(htmlFilePath)}");
        }

        public bool ContainsCalibreMetadata(string htmlFilePath)
        {
            try
            {
                var content = File.ReadAllText(htmlFilePath);
                return CalibreDetectionRegex.IsMatch(content);
            }
            catch
            {
                return false;
            }
        }

        public ObservableCollection<ComicMetadata> InitializeDefaultMetadata()
        {
            var metadataCollection = new ObservableCollection<ComicMetadata>();

            if (!string.IsNullOrEmpty(Settings.Instance.DefaultMetadata))
            {
                var keys = Settings.Instance.DefaultMetadata.Split(';');
                foreach (var key in keys.Where(k => !string.IsNullOrWhiteSpace(k)))
                {
                    metadataCollection.Add(new ComicMetadata(key.Trim(), ""));
                }
            }

            return metadataCollection;
        }

        public void AddMissingDefaultMetadata(ObservableCollection<ComicMetadata> metadataCollection)
        {
            if (string.IsNullOrEmpty(Settings.Instance.DefaultMetadata) || metadataCollection == null)
                return;

            var existingKeys = new HashSet<string>(
                metadataCollection.Select(m => m.Key), 
                StringComparer.OrdinalIgnoreCase);

            var keys = Settings.Instance.DefaultMetadata.Split(';');
            foreach (var key in keys.Where(k => !string.IsNullOrWhiteSpace(k)))
            {
                var trimmedKey = key.Trim();
                if (!existingKeys.Contains(trimmedKey))
                {
                    metadataCollection.Add(new ComicMetadata(trimmedKey, ""));
                }
            }
        }

        private void ExtractDublinCoreMetadata(string content, ObservableCollection<ComicMetadata> metadataCollection)
        {
            var matches = DublinCoreRegex.Matches(content);
            
            foreach (Match match in matches)
            {
                if (match.Groups.Count >= 3)
                {
                    var dcKey = match.Groups[1].Value.ToLowerInvariant();
                    var value = System.Net.WebUtility.HtmlDecode(match.Groups[2].Value);

                    if (DublinCoreToComicInfoMappings.TryGetValue(dcKey, out var comicInfoKey))
                    {
                        metadataCollection.Add(new ComicMetadata(comicInfoKey, value));
                        _logger.Log($"Imported Calibre metadata: {comicInfoKey} = {value}");
                    }
                }
            }
        }

        private void ExtractTitleFromHtml(string content, ObservableCollection<ComicMetadata> metadataCollection)
        {
            if (metadataCollection.Any(m => m.Key == "Title")) 
                return;

            var titleMatch = TitleRegex.Match(content);
            if (titleMatch.Success && titleMatch.Groups.Count >= 2)
            {
                var title = titleMatch.Groups[1].Value.Trim().Split(" - ").FirstOrDefault()?.Trim();
                if (!string.IsNullOrEmpty(title))
                {
                    metadataCollection.Add(new ComicMetadata("Title", title));
                }
            }
        }

        private XmlDocument CreateComicInfoXmlDocument(ObservableCollection<ComicMetadata> metadata)
        {
            var xmlDoc = new XmlDocument();
            const string baseXml = "<?xml version=\"1.0\"?><ComicInfo xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\"></ComicInfo>";
            xmlDoc.LoadXml(baseXml);

            var rootElem = xmlDoc.DocumentElement;
            if (rootElem != null)
            {
                foreach (var metadataItem in metadata.Where(m => !string.IsNullOrWhiteSpace(m.Value)))
                {
                    var dataElem = xmlDoc.CreateElement(metadataItem.Key);
                    dataElem.InnerText = metadataItem.Value;
                    rootElem.AppendChild(dataElem);
                }
            }

            return xmlDoc;
        }

        private string GetMetadataValue(ObservableCollection<ComicMetadata> metadata, string key)
        {
            return metadata?.FirstOrDefault(m => string.Equals(m.Key, key, StringComparison.OrdinalIgnoreCase))?.Value;
        }

        private string BuildCalibreHtmlContent(ObservableCollection<ComicMetadata> metadata, string title, string creator)
        {
            var htmlContent = new StringBuilder();

            AppendHtmlHeader(htmlContent, title, creator);
            AppendDublinCoreMetadata(htmlContent, metadata);
            AppendHtmlBody(htmlContent, title, creator);

            return htmlContent.ToString();
        }

        private void AppendHtmlHeader(StringBuilder htmlContent, string title, string creator)
        {
            htmlContent.AppendLine("<!DOCTYPE html PUBLIC \"-//W3C//DTD XHTML 1.0 Strict//EN\" \"http://www.w3.org/TR/xhtml1/DTD/xhtml1-strict.dtd\">");
            htmlContent.AppendLine("<html xmlns=\"http://www.w3.org/1999/xhtml\">");
            htmlContent.AppendLine("<head>");
            htmlContent.AppendLine("<meta http-equiv=\"Content-Type\" content=\"text/html; charset=UTF-8\" />");
            htmlContent.AppendLine();
            htmlContent.AppendLine("<link rel=\"schema.DC\" href=\"http://purl.org/dc/elements/1.1/\" />");
            htmlContent.AppendLine("<link rel=\"schema.DCTERMS\" href=\"http://purl.org/dc/terms/\" />");
            htmlContent.AppendLine();
            htmlContent.AppendLine($"<title>{System.Net.WebUtility.HtmlEncode(creator)} - {System.Net.WebUtility.HtmlEncode(title)}</title>");
            htmlContent.AppendLine();
        }

        private void AppendDublinCoreMetadata(StringBuilder htmlContent, ObservableCollection<ComicMetadata> metadata)
        {
            foreach (var metadataItem in metadata.Where(m => !string.IsNullOrWhiteSpace(m.Value)))
            {
                var encodedValue = System.Net.WebUtility.HtmlEncode(metadataItem.Value);
                
                if (ComicInfoToDublinCoreMappings.TryGetValue(metadataItem.Key, out var dcTag))
                {
                    htmlContent.AppendLine($"  <meta name=\"DC.{dcTag}\" content=\"{encodedValue}\" />");
                }
                else
                {
                    htmlContent.AppendLine($"  <meta name=\"{metadataItem.Key}\" content=\"{encodedValue}\" />");
                }
            }

            // Add standard Calibre metadata
            htmlContent.AppendLine("  <meta name=\"DC.contributor\" content=\"ComicbookArchiveToolbox [Generated metadata]\" />");

            var currentDate = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture);
            htmlContent.AppendLine($"  <meta name=\"DC.date\" content=\"{currentDate}\" />");

            // Add identifier if not present
            var hasIdentifier = metadata.Any(m => ComicInfoToDublinCoreMappings.ContainsKey(m.Key) && 
                                                 ComicInfoToDublinCoreMappings[m.Key] == "identifier");
            if (!hasIdentifier)
            {
                htmlContent.AppendLine($"  <meta name=\"DC.identifier\" content=\"{Guid.NewGuid()}\" />");
            }
        }

        private void AppendHtmlBody(StringBuilder htmlContent, string title, string creator)
        {
            htmlContent.AppendLine("</head>");
            htmlContent.AppendLine("<body>");
            htmlContent.AppendLine("<div class=\"calibreMeta\">");
            htmlContent.AppendLine("  <div class=\"calibreMetaTitle\">");
            htmlContent.AppendLine($"    <h1>{System.Net.WebUtility.HtmlEncode(title)}</h1>");
            htmlContent.AppendLine("  </div>");
            htmlContent.AppendLine("  <div class=\"calibreMetaAuthor\">");
            htmlContent.AppendLine($"    {System.Net.WebUtility.HtmlEncode(creator)}");
            htmlContent.AppendLine("  </div>");
            htmlContent.AppendLine("</div>");
            htmlContent.AppendLine("<div class=\"calibreMain\">");
            htmlContent.AppendLine("  <div class=\"calibreEbookContent\">");
            htmlContent.AppendLine("    <h2>Metadata exported from ComicbookArchiveToolbox</h2>");
            htmlContent.AppendLine("    <p>This file contains comic book metadata in Calibre format.</p>");
            htmlContent.AppendLine("  </div>");
            htmlContent.AppendLine("</div>");
            htmlContent.AppendLine("</body>");
            htmlContent.AppendLine("</html>");
        }
    }
}