using ComicbookArchiveToolbox.CommonTools;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;

namespace ComicbookArchiveToolbox.Module.Split.Services
{
	public class BaseSplitterPlugin
	{
		protected Logger _logger;
		public BaseSplitterPlugin(Logger logger)
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
				if (SystemTools.IsImageFile(files[i]))
				{
					cover = files[i];
					break;
				}
				++i;
			}
			return cover != null;
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Interoperability", "CA1416:Validate platform compatibility", Justification = "<Pending>")]
		protected long CopyCoverToSubBuffer(string coverFile, string subBuffer, int fileIndex, int archiveNb)
		{
			long coverSize = 0;
			if (string.IsNullOrWhiteSpace(coverFile))
			{
				return coverSize;
			}
			FileInfo coverInfo = new FileInfo(coverFile);
			string destFile = Path.Combine(subBuffer, coverInfo.Name);

			using (Bitmap bitmap = (Bitmap)Image.FromFile(coverFile))
			{
				if (Settings.Instance.AddFileIndexToCovers)
				{
					string issueText = $"{fileIndex.ToString().PadLeft(archiveNb.ToString().Length, '0')}/{archiveNb}";

					using (Graphics graphics = Graphics.FromImage(bitmap))
					{
						using (Font arialFont = new Font("Arial", 220f, FontStyle.Regular, GraphicsUnit.Point))
						{
							SizeF size = graphics.MeasureString(issueText, arialFont);
							using (SolidBrush whiteBrush = new SolidBrush(Color.FromArgb(200, 255, 255, 255)))
							{
								graphics.FillRectangle(whiteBrush, 5f, 5f, size.Width + 10f, size.Height + 10f);
							}


							graphics.DrawString(issueText, arialFont, Brushes.Black, 10f, 10f);
						}
					}
				}

				JpgConverter jpgConverter = new JpgConverter(_logger, 80);
				jpgConverter.SaveJpeg(bitmap, destFile);
			}

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
