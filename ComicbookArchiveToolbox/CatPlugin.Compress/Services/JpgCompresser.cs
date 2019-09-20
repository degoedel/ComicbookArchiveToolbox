using ComicbookArchiveToolbox.CommonTools;
using ComicbookArchiveToolbox.CommonTools.Events;
using Prism.Events;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CatPlugin.Compress.Services
{
	public class JpgCompresser
	{
		private Logger _logger;
		private IEventAggregator _eventAggregator;
		public JpgCompresser(Logger logger, IEventAggregator eventAggregator)
		{
			_logger = logger;
			_eventAggregator = eventAggregator;
		}

		public void Compress(string inputFile, string outputFile, long imageQuality, bool resizeByPx, long size, long ratio)
		{
			_eventAggregator.GetEvent<BusinessEvent>().Publish(true);
			_logger.Log($"Check input validity vs settings");
			FileInfo fi = new FileInfo(outputFile);
			string fileName = fi.Name;
			string settingsExtension = $".{Settings.Instance.OutputFormat.ToString().ToLower()}";
			if (string.IsNullOrEmpty(fi.Extension))
			{
				_logger.Log($"Add Extension to filename {settingsExtension}");
				outputFile += settingsExtension;
			}
			else
			{
				if (fi.Extension != settingsExtension)
				{
					_logger.Log($"Incorrect extension found in filename {fi.Extension} replaced with {settingsExtension}");
					outputFile = outputFile.Substring(0, outputFile.Length - fi.Extension.Length) + settingsExtension;
				}
			}
			_logger.Log($"Start compressing input file in {outputFile}");
			string nameTemplate = fi.Name.Substring(0, fi.Name.Length - fi.Extension.Length);
			string bufferPath = Settings.Instance.GetBufferDirectory(inputFile, nameTemplate);
			string outputBuffer = Path.Combine(bufferPath, "outputBuffer");
			Directory.CreateDirectory(outputBuffer);
			List<FileInfo> metadataFiles = new List<FileInfo>();
			List<FileInfo> pages = new List<FileInfo>();
			CompressionHelper ch1 = new CompressionHelper(_logger);
			_logger.Log($"Uncompress {inputFile} in {bufferPath}");
			ch1.DecompressToDirectory(inputFile, bufferPath);
			_logger.Log($"Extraction done.");
			ParseArchiveFiles(bufferPath, ref metadataFiles, ref pages);

			if (Settings.Instance.IncludeMetadata)
			{
				if (metadataFiles.Count > 0)
				{
					string destFile = Path.Combine(outputBuffer, metadataFiles[0].Name);
					_logger.Log($"copy metadata {metadataFiles[0].FullName}  in {destFile}");
					File.Copy(metadataFiles[0].FullName, destFile, true);
				}
			}

			int pagePadSize = pages.Count.ToString().Length;
			int pageAdded = 1;
			JpgConverter jpgConverter = new JpgConverter(_logger, imageQuality);
			for (int i = 0; i < pages.Count; ++i)
			{
				if (pages[i].Extension != ".xml")
				{
					string destFile = Path.Combine(outputBuffer, $"{nameTemplate}_{pageAdded.ToString().PadLeft(pagePadSize, '0')}{pages[i].Extension}".Replace(' ', '_'));
					if (!resizeByPx && ratio == 100)
					{
						// On a pas besoin de faire de resize
						if (imageQuality == 100)
						{
							// rename the files in the directories
							File.Move(pages[i].FullName, destFile);
						}
						else
						{
							jpgConverter.SaveJpeg(pages[i].FullName, destFile);
							File.Delete(pages[i].FullName);
						}
					}
					else
					{
						// Il faut de toute façon redimensionner l’image
						if (resizeByPx)
						{
							Bitmap reduced = jpgConverter.ResizeImageByPx(pages[i].FullName, size);
							jpgConverter.SaveJpeg(reduced, destFile);
							File.Delete(pages[i].FullName);
						}
						else
						{
							Bitmap reduced = jpgConverter.ResizeImageByRatio(pages[i].FullName, ratio);
							jpgConverter.SaveJpeg(reduced, destFile);
							File.Delete(pages[i].FullName);
						}
					}
					++pageAdded;
				}
			}

			CompressionHelper ch2 = new CompressionHelper(_logger);
			_logger.Log($"Start compression of {outputBuffer} into {outputFile} ...");
			ch2.CompressDirectoryContent(outputBuffer, outputFile);
			_logger.Log($"Compression done.");
			_logger.Log($"Clean Buffer {bufferPath}");
			SystemTools.CleanDirectory(bufferPath, _logger);
			_logger.Log("Done.");
			_eventAggregator.GetEvent<BusinessEvent>().Publish(false);
		}

		private void ParseArchiveFiles(string pathToBuffer, ref List<FileInfo> metadataFiles, ref List<FileInfo> pages)
		{
			DirectoryInfo di = new DirectoryInfo(pathToBuffer);
			pages.AddRange(di.GetFiles().OrderBy(f => f.Name));
			metadataFiles.AddRange(di.GetFiles("*.xml").OrderBy(f => f.Name));
			var subdirs = di.GetDirectories();
			if (subdirs.Count() > 0)
			{
				foreach (DirectoryInfo subdir in subdirs)
				{
					pages.AddRange(subdir.GetFiles().OrderBy(f => f.Name));
					metadataFiles.AddRange(subdir.GetFiles("*.xml").OrderBy(f => f.Name));
				}
			}
		}
	}
}
