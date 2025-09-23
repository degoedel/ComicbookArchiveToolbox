using ComicbookArchiveToolbox.CommonTools;
using ComicbookArchiveToolbox.CommonTools.Events;
using ComicbookArchiveToolbox.Services;
using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
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
using Unity;

namespace ComicbookArchiveToolbox.Module.Edit.ViewModels
{
	public class EditPluginViewModel : BindableBase
	{
		#region Fields
		private readonly Logger _logger;
		private readonly IUnityContainer _container;
		private readonly IEventAggregator _eventAggregator;
		private readonly IFileDialogService _fileDialogService;

		private string _fileToEdit = "";
		private ObservableCollection<ComicMetadata> _metadataCollection;
		private string _metadataFile = "";
		private string _calibreMetaDataFile = "";

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
		#endregion

		#region Properties
		public string FileToEdit
		{
			get { return _fileToEdit; }
			set
			{
				SetProperty(ref _fileToEdit, value);
				if (File.Exists(_fileToEdit))
				{
					LoadMetadata();
				}
				RefreshCommandStates();
			}
		}

		public ObservableCollection<ComicMetadata> MetadataCollection
		{
			get { return _metadataCollection; }
			set { SetProperty(ref _metadataCollection, value); }
		}

		public DelegateCommand BrowseFileCommand { get; private set; }
		public DelegateCommand SaveCommand { get; private set; }
		public DelegateCommand ImportCalibreCommand { get; private set; }
		public DelegateCommand ExportCalibreCommand { get; private set; }
		#endregion

		#region Constructor
		public EditPluginViewModel(IUnityContainer container, IEventAggregator eventAggregator)
		{
			_container = container;
			_eventAggregator = eventAggregator;
			_fileDialogService = container.Resolve<IFileDialogService>();
			_logger = container.Resolve<Logger>();

			InitializeCommands();
		}

		private void InitializeCommands()
		{
			BrowseFileCommand = new DelegateCommand(BrowseFile, CanExecute);
			SaveCommand = new DelegateCommand(LaunchSave, CanSave);
			ImportCalibreCommand = new DelegateCommand(ImportCalibreMetadata, CanImportCalibre);
			ExportCalibreCommand = new DelegateCommand(ExportCalibreMetadata, CanExportCalibre);
		}

		private void RefreshCommandStates()
		{
			SaveCommand.RaiseCanExecuteChanged();
			ImportCalibreCommand.RaiseCanExecuteChanged();
			ExportCalibreCommand.RaiseCanExecuteChanged();
		}
		#endregion

		#region Command Handlers
		private void BrowseFile()
		{
			FileToEdit = _fileDialogService.BrowseForInputFile();
		}

		private bool CanExecute() => true;

		private bool CanSave() => !string.IsNullOrEmpty(FileToEdit) && File.Exists(FileToEdit);

		private bool CanImportCalibre() => CanSave();

		private bool CanExportCalibre() => MetadataCollection != null && MetadataCollection.Count > 0;

		private void LaunchSave()
		{
			Task.Run(() => DoSave());
		}
		#endregion

		#region Core Operations
		private void DoSave()
		{
			ExecuteWithBusyState(() =>
			{
				var fileInfo = new FileInfo(_fileToEdit);
				var bufferContext = CreateBufferContext(fileInfo);
				var xmlDoc = CreateComicInfoXmlDocument();

				SaveXmlDocument(xmlDoc, bufferContext.MetadataFilePath);
				ExportCalibreMetadata();

				var compressionHelper = new CompressionHelper(_logger);

				if (CanUpdateArchiveDirectly(fileInfo.Extension))
				{
					compressionHelper.UpdateFile(FileToEdit, bufferContext.MetadataFilePath);
					compressionHelper.UpdateFile(FileToEdit, bufferContext.CalibreMetaDataFilePath);
				}
				else
				{
					ProcessFullRecompression(compressionHelper, fileInfo, bufferContext);
				}

				CleanupBuffer(bufferContext.BufferPath);
			});
		}

		private void LoadMetadata()
		{
			ExecuteWithBusyState(() =>
			{
				var fileInfo = new FileInfo(_fileToEdit);
				var bufferContext = CreateBufferContext(fileInfo);

				ExtractMetadataFiles(bufferContext);

				var xmlFiles = bufferContext.BufferDirectory.GetFiles("*.xml");
				var htmlFiles = bufferContext.BufferDirectory.GetFiles("*.html");

				if (xmlFiles.Length > 0)
				{
					LoadComicInfoMetadata(xmlFiles[0]);
				}
				else if (TryFindCalibreMetadata(htmlFiles, out var calibreFile))
				{
					LoadCalibreMetadata(calibreFile.FullName);
				}
				else
				{
					_logger.Log("No metadata found. Initializing default metadata.");
					InitializeDefaultMetadata();
				}
			});
		}
		#endregion

		#region Helper Methods
		private void ExecuteWithBusyState(Action action)
		{
			_eventAggregator.GetEvent<BusinessEvent>().Publish(true);
			try
			{
				action();
			}
			catch (Exception ex)
			{
				_logger.Log($"ERROR: Operation failed: {ex.Message}");
			}
			finally
			{
				_eventAggregator.GetEvent<BusinessEvent>().Publish(false);
			}
		}

		private BufferContext CreateBufferContext(FileInfo fileInfo)
		{
			var nameTemplate = Path.GetFileNameWithoutExtension(fileInfo.Name);
			var bufferPath = Settings.Instance.GetBufferDirectory(_fileToEdit, nameTemplate);
			var metadataFilePath = string.IsNullOrEmpty(_metadataFile)
				? Path.Combine(bufferPath, "ComicInfo.xml")
				: _metadataFile;
			var calibreMetaDataFilePath = string.IsNullOrEmpty(_calibreMetaDataFile)
				? string.Empty : _calibreMetaDataFile;

			return new BufferContext
			{
				BufferPath = bufferPath,
				BufferDirectory = new DirectoryInfo(bufferPath),
				MetadataFilePath = metadataFilePath,
				CalibreMetaDataFilePath = _calibreMetaDataFile
			};
		}

		private void ExtractMetadataFiles(BufferContext context)
		{
			var compressionHelper = new CompressionHelper(_logger);
			compressionHelper.ExtractFileType(_fileToEdit, context.BufferPath, "*.xml");
			compressionHelper.ExtractFileType(_fileToEdit, context.BufferPath, "*.html");
		}

		private bool TryFindCalibreMetadata(FileInfo[] htmlFiles, out FileInfo calibreFile)
		{
			calibreFile = htmlFiles.FirstOrDefault(f => ContainsCalibreMetadata(f.FullName));

			if (calibreFile != null)
			{
				_logger.Log($"Found Calibre metadata in {calibreFile.Name}");
				return true;
			}

			_logger.Log("No Calibre metadata found in HTML files.");
			return false;
		}

		private bool ContainsCalibreMetadata(string htmlFile)
		{
			try
			{
				var content = File.ReadAllText(htmlFile);
				return content.Contains("DC.title") || content.Contains("DC.creator") || content.Contains("calibre");
			}
			catch
			{
				return false;
			}
		}

		private XmlDocument CreateComicInfoXmlDocument()
		{
			var xmlDoc = new XmlDocument();
			const string baseXml = "<?xml version=\"1.0\"?><ComicInfo xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\"></ComicInfo>";
			xmlDoc.LoadXml(baseXml);

			var rootElem = xmlDoc.DocumentElement;
			foreach (var metadata in MetadataCollection)
			{
				var dataElem = xmlDoc.CreateElement(metadata.Key);
				dataElem.InnerText = metadata.Value;
				rootElem.AppendChild(dataElem);
			}

			return xmlDoc;
		}

		private void SaveXmlDocument(XmlDocument xmlDoc, string filePath)
		{
			_logger.Log($"Saving metadata as ComicRack XML in {filePath}");

			using var writer = XmlWriter.Create(filePath, new XmlWriterSettings { Indent = true });
			xmlDoc.WriteTo(writer);
		}

		private bool CanUpdateArchiveDirectly(string extension)
		{
			return extension is ".cbz" or ".cb7" or ".cbt";
		}

		private void ProcessFullRecompression(CompressionHelper compressionHelper, FileInfo fileInfo, BufferContext bufferContext)
		{
			_logger.Log("Archive format does not support update with 7zip. Decompression and recompression is required");

			compressionHelper.DecompressToDirectory(FileToEdit, bufferContext.BufferPath);
			var xmlDoc = CreateComicInfoXmlDocument();
			SaveXmlDocument(xmlDoc, bufferContext.MetadataFilePath);
			ExportCalibreMetadata();

			var outputFile = DetermineOutputFile(fileInfo);
			compressionHelper.CompressDirectoryContent(bufferContext.BufferPath, outputFile);

			_logger.Log("Recompression done.");
		}

		private string DetermineOutputFile(FileInfo fileInfo)
		{
			var settingsExtension = $".{Settings.Instance.OutputFormat.ToString().ToLower()}";
			var outputFile = FileToEdit;

			if (fileInfo.Extension != settingsExtension)
			{
				_logger.Log($"Incorrect extension found in filename {fileInfo.Extension} replaced with {settingsExtension}");
				outputFile = Path.ChangeExtension(outputFile, settingsExtension);
			}

			return outputFile;
		}

		private void CleanupBuffer(string bufferPath)
		{
			_logger.Log($"Cleaning Buffer {bufferPath}");
			SystemTools.CleanDirectory(bufferPath, _logger);
			_logger.Log("Done.");
		}
		#endregion

		#region Metadata Loading
		private void LoadComicInfoMetadata(FileInfo xmlFile)
		{
			_logger.Log($"Loading XML metadata from {xmlFile.Name}");
			_metadataFile = xmlFile.FullName;

			var xmlDoc = new XmlDocument();
			xmlDoc.Load(_metadataFile);

			var metadataCollection = new ObservableCollection<ComicMetadata>();

			foreach (XmlNode node in xmlDoc.DocumentElement.ChildNodes)
			{
				if (node is XmlElement element)
				{
					metadataCollection.Add(new ComicMetadata(element.Name, element.InnerText));
				}
			}

			MetadataCollection = metadataCollection;
		}

		private void LoadCalibreMetadata(string htmlFile)
		{
			try
			{
				_calibreMetaDataFile = htmlFile;
				var content = File.ReadAllText(htmlFile);
				var metadataCollection = new ObservableCollection<ComicMetadata>();

				ExtractDublinCoreMetadata(content, metadataCollection);
				ExtractTitleFromHtml(content, metadataCollection);
				AddMissingDefaultMetadata(metadataCollection);

				MetadataCollection = metadataCollection;
				_logger.Log($"Successfully imported {metadataCollection.Count} metadata fields from Calibre HTML");
			}
			catch (Exception ex)
			{
				_logger.Log($"ERROR: Failed to parse Calibre HTML metadata: {ex.Message}");
				InitializeDefaultMetadata();
			}
		}

		private void ExtractDublinCoreMetadata(string content, ObservableCollection<ComicMetadata> metadataCollection)
		{
			foreach (var mapping in DublinCoreToComicInfoMappings)
			{
				var pattern = $@"<meta\s+name=""DC\.{mapping.Key}""\s+content=""([^""]*)""\s*/?>";
				var match = Regex.Match(content, pattern, RegexOptions.IgnoreCase);

				if (match.Success)
				{
					var value = System.Net.WebUtility.HtmlDecode(match.Groups[1].Value);
					metadataCollection.Add(new ComicMetadata(mapping.Value, value));
					_logger.Log($"Imported Calibre metadata: {mapping.Value} = {value}");
				}
			}
		}

		private void ExtractTitleFromHtml(string content, ObservableCollection<ComicMetadata> metadataCollection)
		{
			if (metadataCollection.Any(m => m.Key == "Title")) return;

			var titleMatch = Regex.Match(content, @"<title>([^<]*)</title>", RegexOptions.IgnoreCase);
			if (titleMatch.Success)
			{
				var title = titleMatch.Groups[1].Value.Trim().Split(" - ").FirstOrDefault()?.Trim();
				if (!string.IsNullOrEmpty(title))
				{
					metadataCollection.Add(new ComicMetadata("Title", title));
				}
			}
		}

		private void InitializeDefaultMetadata()
		{
			var metadataCollection = new ObservableCollection<ComicMetadata>();

			if (!string.IsNullOrEmpty(Settings.Instance.DefaultMetadata))
			{
				var keys = Settings.Instance.DefaultMetadata.Split(';');
				foreach (var key in keys)
				{
					metadataCollection.Add(new ComicMetadata(key, ""));
				}
			}

			MetadataCollection = metadataCollection;
		}

		private void AddMissingDefaultMetadata(ObservableCollection<ComicMetadata> metadataCollection)
		{
			if (string.IsNullOrEmpty(Settings.Instance.DefaultMetadata)) return;

			var keys = Settings.Instance.DefaultMetadata.Split(';');
			foreach (var key in keys)
			{
				if (!metadataCollection.Any(m => m.Key.Equals(key, StringComparison.OrdinalIgnoreCase)))
				{
					metadataCollection.Add(new ComicMetadata(key, ""));
				}
			}
		}
		#endregion

		#region Calibre Import/Export
		private void ImportCalibreMetadata()
		{
			_logger.Log("Starting Calibre metadata import");

			var bufferContext = CreateBufferContext(new FileInfo(_fileToEdit));
			var htmlFiles = bufferContext.BufferDirectory.GetFiles("*.html");

			if (TryFindCalibreMetadata(htmlFiles, out var calibreFile))
			{
				LoadCalibreMetadata(calibreFile.FullName);
			}
			else
			{
				_logger.Log("No Calibre metadata found in HTML files.");
			}
		}

		private void ExportCalibreMetadata()
		{
			_logger.Log("Starting Calibre metadata export");

			try
			{
				var bufferContext = CreateBufferContext(new FileInfo(_fileToEdit));
				var htmlFiles = bufferContext.BufferDirectory.GetFiles("*.html");
				var calibreFile = htmlFiles.FirstOrDefault(f => ContainsCalibreMetadata(f.FullName));

				if (calibreFile != null)
				{
					GenerateCalibreHtmlFile(calibreFile.FullName);
					_logger.Log("Calibre metadata export completed successfully");
				}
				else
				{
					_logger.Log("No existing Calibre HTML file found for export");
				}
			}
			catch (Exception ex)
			{
				_logger.Log($"ERROR: Failed to export Calibre metadata: {ex.Message}");
			}
		}

		private void GenerateCalibreHtmlFile(string outputPath)
		{
			var title = GetMetadataValue("Title") ?? "Unknown Title";
			var creator = GetMetadataValue("Writer") ?? GetMetadataValue("Penciller") ?? "Unknown Creator";

			var htmlContent = BuildCalibreHtmlContent(title, creator);

			File.WriteAllText(outputPath, htmlContent, Encoding.UTF8);
			_logger.Log($"Generated Calibre HTML metadata file: {Path.GetFileName(outputPath)}");
		}

		private string GetMetadataValue(string key)
		{
			return MetadataCollection?.FirstOrDefault(m => m.Key == key)?.Value;
		}

		private string BuildCalibreHtmlContent(string title, string creator)
		{
			var htmlContent = new StringBuilder();

			AppendHtmlHeader(htmlContent, title, creator);
			AppendDublinCoreMetadata(htmlContent);
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
			htmlContent.AppendLine($"<title>{creator} - {title}</title>");
			htmlContent.AppendLine();
		}

		private void AppendDublinCoreMetadata(StringBuilder htmlContent)
		{
			foreach (var metadata in MetadataCollection.Where(m => !string.IsNullOrWhiteSpace(m.Value)))
			{
				if (ComicInfoToDublinCoreMappings.TryGetValue(metadata.Key, out var dcTag))
				{
					var encodedValue = System.Net.WebUtility.HtmlEncode(metadata.Value);
					htmlContent.AppendLine($"  <meta name=\"DC.{dcTag}\" content=\"{encodedValue}\" />");
				}
				else
				{
					var encodedValue = System.Net.WebUtility.HtmlEncode(metadata.Value);
					htmlContent.AppendLine($"  <meta name=\"{metadata.Key}\" content=\"{encodedValue}\" />");
				}
			}

			// Add standard Calibre metadata
			htmlContent.AppendLine("  <meta name=\"DC.contributor\" content=\"ComicbookArchiveToolbox [Generated metadata]\" />");

			var currentDate = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture);
			htmlContent.AppendLine($"  <meta name=\"DC.date\" content=\"{currentDate}\" />");

			// Add identifier if not present
			if (!MetadataCollection.Any(m => ComicInfoToDublinCoreMappings.ContainsKey(m.Key) && ComicInfoToDublinCoreMappings[m.Key] == "identifier"))
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
		#endregion

		#region Helper Classes
		private class BufferContext
		{
			public string BufferPath { get; set; }
			public DirectoryInfo BufferDirectory { get; set; }
			public string MetadataFilePath { get; set; }
			public string CalibreMetaDataFilePath { get; set; }
		}
		#endregion
	}
}