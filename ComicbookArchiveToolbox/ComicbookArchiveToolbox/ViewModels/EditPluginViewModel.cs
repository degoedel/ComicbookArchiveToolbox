using ComicbookArchiveToolbox.CommonTools;
using ComicbookArchiveToolbox.CommonTools.Events;
using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using Unity;

namespace ComicbookArchiveToolbox.Module.Edit.ViewModels
{
	public class EditPluginViewModel : BindableBase
	{
		private Logger _logger;
		private IUnityContainer _container;
		private IEventAggregator _eventAggregator;

		private string _fileToEdit = "";
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
				SaveCommand.RaiseCanExecuteChanged();
			}
		}

		private ObservableCollection<ComicMetadata> _metadataCollection;
		public ObservableCollection<ComicMetadata> MetadataCollection
		{
			get { return _metadataCollection; }
			set
			{
				SetProperty(ref _metadataCollection, value);
			}
		}

		private string _metadataFile = "";

		public DelegateCommand BrowseFileCommand { get; private set; }
		public DelegateCommand SaveCommand { get; private set; }


		public EditPluginViewModel(IUnityContainer container, IEventAggregator eventAggregator)
		{
			_container = container;
			_eventAggregator = eventAggregator;
			BrowseFileCommand = new DelegateCommand(BrowseFile, CanExecute);
			SaveCommand = new DelegateCommand(LaunchSave, CanSave);
			_logger = _container.Resolve<Logger>();
		}


		private void BrowseFile()
		{
			_logger.Log("Browse for file to split");

			var dialog = new Microsoft.Win32.OpenFileDialog();
			dialog.Filter = "Comics Archive files (*.cb7;*.cba;*cbr;*cbt;*.cbz)|*.cb7;*.cba;*cbr;*cbt;*.cbz";
			string defaultPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
			if (!string.IsNullOrEmpty(_fileToEdit))
			{
				try
				{
					FileInfo fi = new FileInfo(_fileToEdit);
					string selectedDir = fi.DirectoryName;
					if (Directory.Exists(selectedDir))
					{
						defaultPath = selectedDir;
					}
					else
					{
						_logger.Log("WARNING: cannot reach selected path... Open standard path instead.");
					}
				}
				catch (Exception)
				{
					_logger.Log("ERROR: selected path is not valid... Open standard path instead.");
				}

			}
			dialog.InitialDirectory = defaultPath;
			bool? result = dialog.ShowDialog();
			if (result.HasValue && result.Value == true)
			{
				FileToEdit = dialog.FileName;
			}
		}

		private bool CanExecute()
		{
			return true;
		}

		private void LaunchSave()
		{
			Task.Run(() => DoSave());
		}

		private void DoSave()
		{
			_eventAggregator.GetEvent<BusinessEvent>().Publish(true);
			bool canUpdate = false;
			FileInfo fi = new FileInfo(_fileToEdit);
			switch (fi.Extension)
			{
				case ".cbz":
				case ".cb7":
				case ".cbt":
					canUpdate = true;
					break;
				default:
					canUpdate = false;
					break;
			}
			string nameTemplate = fi.Name.Substring(0, fi.Name.Length - fi.Extension.Length);
			string bufferPath = Settings.Instance.GetBufferDirectory(_fileToEdit, nameTemplate);
			if (string.IsNullOrEmpty(_metadataFile))
			{
				_metadataFile = Path.Combine(bufferPath, "ComicInfo.xml");
			}
			_logger.Log($"Save metadata as ComicRack xml in {_metadataFile}");
			XmlDocument xmlDoc = new XmlDocument();
			string baseXml = "<?xml version=\"1.0\"?><ComicInfo xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\"></ComicInfo> ";
			xmlDoc.LoadXml(baseXml);
			XmlElement rootElem = xmlDoc.DocumentElement;
			foreach (ComicMetadata data in MetadataCollection)
			{
				XmlElement dataElem = xmlDoc.CreateElement(data.Key);
				dataElem.InnerText = data.Value;
				rootElem.AppendChild(dataElem);
			}
			using (XmlWriter writer = XmlWriter.Create(_metadataFile))
			{
				xmlDoc.WriteTo(writer);
			}
			CompressionHelper ch = new CompressionHelper(_logger);

			if (canUpdate)
			{
				ch.UpdateFile(FileToEdit, _metadataFile);
			}
			else
			{
				_logger.Log("Archive format does not support update with 7zip. Decompression and recompression is required");
				ch.DecompressToDirectory(FileToEdit, bufferPath);
				XmlWriterSettings xmlWriterSettings = new XmlWriterSettings();
				xmlWriterSettings.Indent = true;
				using (XmlWriter writer = XmlWriter.Create(_metadataFile, xmlWriterSettings))
				{
					xmlDoc.WriteTo(writer);
				}
				string settingsExtension = $".{Settings.Instance.OutputFormat.ToString().ToLower()}";
				string outputFile = FileToEdit;
				if (fi.Extension != settingsExtension)
				{
					_logger.Log($"Incorrect extension found in filename {fi.Extension} replaced with {settingsExtension}");
					outputFile = outputFile.Substring(0, outputFile.Length - fi.Extension.Length) + settingsExtension;
				}
				ch.CompressDirectoryContent(bufferPath, outputFile);
				_logger.Log($"Recompression done.");
				_logger.Log($"Clean Buffer {bufferPath}");
				SystemTools.CleanDirectory(bufferPath, _logger);
				_logger.Log("Done.");
			}
			_eventAggregator.GetEvent<BusinessEvent>().Publish(false);
		}

		private bool CanSave()
		{
			return true;
		}

		private void LoadMetadata()
		{
			_eventAggregator.GetEvent<BusinessEvent>().Publish(true);
			CompressionHelper ch = new CompressionHelper(_logger);
			FileInfo fi = new FileInfo(_fileToEdit);
			string nameTemplate = fi.Name.Substring(0, fi.Name.Length - fi.Extension.Length);
			string bufferPath = Settings.Instance.GetBufferDirectory(_fileToEdit, nameTemplate);
			ch.ExtractFileType(_fileToEdit, bufferPath, "*.xml");
			DirectoryInfo di = new DirectoryInfo(bufferPath);
			var files = di.GetFiles("*.xml");
			if (files.Count() > 0)
			{
				_logger.Log($"Found {files.Count()} metadata file(s). Loading first one.");
				_metadataFile = files[0].FullName;
			}
			else
			{
				_metadataFile = "";
			}
			ObservableCollection<ComicMetadata> metadataCollection = new ObservableCollection<ComicMetadata>();
			if (!string.IsNullOrEmpty(_metadataFile))
			{
				XmlDocument xmlDoc = new XmlDocument();
				xmlDoc.Load(_metadataFile);
				var rootElem = xmlDoc.DocumentElement;
				XmlNodeList datas = rootElem.ChildNodes;
				foreach (XmlNode node in datas)
				{
					if (node is XmlElement)
					{
						XmlElement element = node as XmlElement;
						string pairKey = element.Name;
						string pairValue = element.InnerText;
						metadataCollection.Add(new ComicMetadata(pairKey, pairValue));
						MetadataCollection = metadataCollection;
					}
				}
			}
			else
			{
				_logger.Log("No metadata found. Initializing default ones");
				InitializeDefaultMetada();
			}
			_eventAggregator.GetEvent<BusinessEvent>().Publish(false);
		}

		private void InitializeDefaultMetada()
		{
			ObservableCollection<ComicMetadata> metadataCollection = new ObservableCollection<ComicMetadata>();
			var keys = Settings.Instance.DefaultMetadata.Split(';');
			foreach (string s in keys)
			{
				metadataCollection.Add(new ComicMetadata(s, ""));
			}
			MetadataCollection = metadataCollection;
		}


	}
}
