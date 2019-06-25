using ComicbookArchiveToolbox.CommonTools;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CatPlugin.Split.Services
{
	public class BaseSplitter
	{
		protected Logger _logger;
		public BaseSplitter(Logger logger)
		{
			_logger = logger;
		}

		protected string ExtractArchive(string filePath, ArchiveTemplate archiveTemplate)
		{
			string pathToBuffer = Settings.Instance.GetBufferDirectory(filePath, archiveTemplate.ComicName);
			CompressionHelper ch = new CompressionHelper(_logger);
			_logger.Log($"Start extraction of {filePath} into {pathToBuffer} ...");
			ch.DecompressToDirectory(filePath, pathToBuffer);
			_logger.Log($"Extraction done.");
			return pathToBuffer;
		}

		protected void ParseArchiveFiles(string pathToBuffer, out List<FileInfo> metadataFiles, out List<FileInfo> pages)
		{
			metadataFiles = new List<FileInfo>();
			DirectoryInfo di = new DirectoryInfo(pathToBuffer);
			pages = new List<FileInfo>();
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

		protected string SaveCoverInBuffer(string pathToBuffer, string archiveName, int indexSize, List<FileInfo> files)
		{
			string savedCoverPath = "";
			bool coverFound = GetCoverIfFound(files, out FileInfo coverFile);
			int coverIndex = 1;
			if (coverFound)
			{
				savedCoverPath = Path.Combine(pathToBuffer, $"{archiveName}_{coverIndex.ToString().PadLeft(indexSize, '0')}{coverFile.Extension}");
				File.Copy(coverFile.FullName, savedCoverPath, true);
			}
			return savedCoverPath;
		}

		private bool GetCoverIfFound(List<FileInfo> files, out FileInfo cover)
		{
			int i = 0;
			cover = null;
			while (i < files.Count && cover == null)
			{
				if (files[i].Extension != ".xml")
				{
					cover = files[i];
					break;
				}
				++i;
			}
			return cover != null;
		}

		protected long CopyCoverToSubBuffer(string coverFile, string subBuffer)
		{
			long coverSize = 0;
			if (string.IsNullOrWhiteSpace(coverFile))
			{
				return coverSize;
			}
			FileInfo coverInfo = new FileInfo(coverFile);
			string destFile = Path.Combine(subBuffer, coverInfo.Name);

			Bitmap bitmap = (Bitmap)Image.FromFile(coverFile);//load the image file

			if (Settings.Instance.AddFileIndexToCovers)
			{
				string firstText = "Hello";
				string secondText = "World";
				PointF firstLocation = new PointF(10f, 10f);
				PointF secondLocation = new PointF(10f, 50f);

				using (Graphics graphics = Graphics.FromImage(bitmap))
				{
					using (Font arialFont = new Font("Arial", 55))
					{
						graphics.DrawString(firstText, arialFont, Brushes.Blue, firstLocation);
						graphics.DrawString(secondText, arialFont, Brushes.Red, secondLocation);
					}
				}
			}

			JpgConverter jpgConverter = new JpgConverter(_logger, 80);
			jpgConverter.SaveJpeg(bitmap, destFile);

			coverInfo = new FileInfo(destFile);
			coverSize = coverInfo.Length;
			return coverSize;
		}

		protected string GetSubBufferPath(ArchiveTemplate template, int fileIndex)
		{
			return Path.Combine(template.PathToBuffer, $"{template.ComicName}_{(fileIndex + 1).ToString().PadLeft(template.IndexSize, '0')}");
		}

		protected long CopyMetaDataToSubBuffer(List<FileInfo> metaDataFiles, string subBuffer)
		{
			long metaDataSize = 0;
			if (metaDataFiles.Count > 0)
			{
				metaDataSize = metaDataFiles[0].Length;
				File.Copy(metaDataFiles[0].FullName, Path.Combine(subBuffer, metaDataFiles[0].Name));
			}
			return metaDataSize;
		}

		protected bool MovePicturesToSubBuffer(string destFolder, List<FileInfo> files, string archiveName, bool isFirstArchive, long imageCompression)
		{
			bool result = true;
			int increaseIndex = 1;
			if (Settings.Instance.IncludeCover)
			{
				increaseIndex = isFirstArchive ? 1 : 2;
			}
			_logger.Log($"Copy the selected files in {destFolder}");
			try
			{
				Directory.CreateDirectory(destFolder);
				int padSize = Math.Max(2, files.Count.ToString().Length);
				JpgConverter jpgConverter = new JpgConverter(_logger, imageCompression);
				for (int i = 0; i < files.Count; ++i)
				{
					string destFile = Path.Combine(destFolder, $"{archiveName}_{(i + increaseIndex).ToString().PadLeft(padSize, '0')}{files[i].Extension}".Replace(' ', '_'));
					if (imageCompression == 100)
					{
						// rename the files in the directories
						File.Move(files[i].FullName, destFile);
					}
					else
					{
						jpgConverter.SaveJpeg(files[i].FullName, destFile);
						File.Delete(files[i].FullName);
					}
				}
			}
			catch (Exception e)
			{
				result = false;
				_logger.Log($"ERROR: Cannot split archive {e.Message}");
			}
			return result;
		}

		protected void CompressArchiveContent(string directory, ArchiveTemplate archiveTemplate)
		{
			string archiveExtension = $".{Settings.Instance.OutputFormat.ToString().ToLower()}";

			DirectoryInfo di = new DirectoryInfo(directory);
			string outputFile = Path.Combine(archiveTemplate.OutputDir, $"{di.Name}{archiveExtension}");
			CompressionHelper ch = new CompressionHelper(_logger);
			_logger.Log($"Start compression of {directory} into {outputFile} ...");
			ch.CompressDirectoryContent(directory, outputFile);
			_logger.Log($"Compression done.");
		}








	}
}
