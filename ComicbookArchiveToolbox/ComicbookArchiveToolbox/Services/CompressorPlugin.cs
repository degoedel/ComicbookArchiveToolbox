using ComicbookArchiveToolbox.CommonTools;
using ComicbookArchiveToolbox.CommonTools.Events;
using Prism.Events;
using System.Collections.Generic;
using System.Drawing;
using System.IO;

namespace ComicbookArchiveToolbox.Services
{
	public class CompressorPlugin
	{
		private Logger _logger;
		private IEventAggregator _eventAggregator;
		public CompressorPlugin(Logger logger, IEventAggregator eventAggregator)
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
			SystemTools.ParseArchiveFiles(bufferPath, ref metadataFiles, ref pages);

			if (Settings.Instance.IncludeMetadata)
			{
				foreach (FileInfo file in metadataFiles)
				{
					string destFile = SystemTools.GetOutputFilePath(outputBuffer, file);
					_logger.Log($"copy metadata {file.FullName}  in {destFile}");
					File.Copy(file.FullName, destFile, true);
				}
			}

			int pagePadSize = pages.Count.ToString().Length;
			int pageAdded = 1;
			JpgConverter jpgConverter = new JpgConverter(_logger, imageQuality);
			for (int i = 0; i < pages.Count; ++i)
			{
				if (SystemTools.IsImageFile(pages[i]))
				{
					string destFile = SystemTools.GetOutputFilePath(outputBuffer, pages[i]);
					var destFi = new FileInfo(destFile);
					destFile = Path.Combine(destFi.Directory.FullName, $"{nameTemplate}_{pageAdded.ToString().PadLeft(pagePadSize, '0')}{pages[i].Extension}".Replace(' ', '_'));
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
	}
}
