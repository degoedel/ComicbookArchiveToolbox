using ComicbookArchiveToolbox.CommonTools;
using ComicbookArchiveToolbox.CommonTools.Events;
using Prism.Events;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ComicbookArchiveToolbox.Module.Split.Services
{
	public abstract class BaseSplitterPlugin
	{
		protected Logger _logger;
		protected IEventAggregator _eventAggregator;

		public BaseSplitterPlugin(Logger logger, IEventAggregator eventAggregator)
		{
			_logger = logger;
			_eventAggregator = eventAggregator;
		}

		// Template method pattern - defines the overall algorithm
		public void Split(string filePath, ArchiveTemplate archiveTemplate)
		{
			_eventAggregator.GetEvent<BusinessEvent>().Publish(true);

			try
			{
				// Validate input parameters - delegated to derived classes
				if (!ValidateInput(archiveTemplate))
				{
					_eventAggregator.GetEvent<BusinessEvent>().Publish(false);
					return;
				}

				// Common initialization
				var splitContext = InitializeSplitting(filePath, archiveTemplate);
				if (splitContext == null)
				{
					_eventAggregator.GetEvent<BusinessEvent>().Publish(false);
					return;
				}

				// Execute the specific splitting algorithm - delegated to derived classes
				bool success = ExecuteSplitting(splitContext);
				if (!success)
				{
					_logger.Log("ERROR: Splitting operation failed");
				}

				// Common cleanup
				CleanupSplitting(splitContext);
			}
			finally
			{
				_eventAggregator.GetEvent<BusinessEvent>().Publish(false);
			}
		}

		// Abstract methods to be implemented by derived classes
		protected abstract bool ValidateInput(ArchiveTemplate archiveTemplate);
		protected abstract bool ExecuteSplitting(SplitContext context);
		protected virtual int ComputeNumberOfSplittedFiles(SplitContext context) => (int)context.ArchiveTemplate.NumberOfSplittedFiles;

		// Common initialization logic
		private SplitContext InitializeSplitting(string filePath, ArchiveTemplate archiveTemplate)
		{
			var context = new SplitContext
			{
				FilePath = filePath,
				ArchiveTemplate = archiveTemplate,
				MetadataFiles = [],
				Pages = []
			};

			// Extract file in buffer
			context.ArchiveTemplate.PathToBuffer = ExtractArchive(filePath, archiveTemplate);

			// Count files in directory except metadata
			List<FileInfo> metadataFiles = [];
			List<FileInfo> pages = [];
			SystemTools.ParseArchiveFiles(context.ArchiveTemplate.PathToBuffer, ref metadataFiles, ref pages);
			context.MetadataFiles = metadataFiles;
			context.Pages = pages;
			context.ArchiveTemplate.Pages = pages;
			context.ArchiveTemplate.MetadataFiles = metadataFiles;

			context.TotalPagesCount = context.Pages.Count - context.MetadataFiles.Count;
			_logger.Log($"Total number of pages is {context.TotalPagesCount}");

			// Compute number of files and setup indexing
			context.NumberOfSplittedFiles = ComputeNumberOfSplittedFiles(context);
			_logger.Log($"Creating {context.NumberOfSplittedFiles} splitted files");

			context.ArchiveTemplate.IndexSize = Math.Max(context.NumberOfSplittedFiles.ToString().Length, 2);

			// Setup cover if needed
			context.ArchiveTemplate.CoverPath = "";
			if (Settings.Instance.IncludeCover)
			{
				context.ArchiveTemplate.CoverPath = SaveCoverInBuffer(
					context.ArchiveTemplate.PathToBuffer,
					context.ArchiveTemplate.ComicName,
					context.ArchiveTemplate.IndexSize,
					context.Pages);
			}

			return context;
		}

		// Common cleanup logic
		private void CleanupSplitting(SplitContext context)
		{
			if (context?.ArchiveTemplate?.PathToBuffer != null)
			{
				_logger.Log($"Clean Buffer {context.ArchiveTemplate.PathToBuffer}");
				SystemTools.CleanDirectory(context.ArchiveTemplate.PathToBuffer, _logger);
			}
			_logger.Log("Done.");
		}

		// Helper method for processing a batch of files
		protected bool ProcessFileBatch(SplitContext context, int fileIndex, List<FileInfo> pagesToAdd)
		{
			string subBufferPath = GetSubBufferPath(context.ArchiveTemplate, fileIndex);
			Directory.CreateDirectory(subBufferPath);

			// Add metadata if needed
			if (Settings.Instance.IncludeMetadata)
			{
				CopyMetaDataToSubBuffer(context.ArchiveTemplate.MetadataFiles, subBufferPath);
			}

			// Add cover if needed (not for first file)
			if (fileIndex != 0 && Settings.Instance.IncludeCover && !string.IsNullOrWhiteSpace(context.ArchiveTemplate.CoverPath))
			{
				CopyCoverToSubBuffer(context.ArchiveTemplate.CoverPath, subBufferPath, fileIndex + 1, context.NumberOfSplittedFiles);
			}

			// Move pictures to sub buffer
			bool success = MovePicturesToSubBuffer(subBufferPath, pagesToAdd, context.ArchiveTemplate.ComicName, fileIndex == 0, context.ArchiveTemplate.ImageCompression);
			if (!success)
			{
				SystemTools.CleanDirectory(subBufferPath, _logger);
				return false;
			}

			// Compress and cleanup
			_logger.Log($"Compress {subBufferPath}");
			CompressArchiveContent(subBufferPath, context.ArchiveTemplate);
			_logger.Log($"Clean Buffer {subBufferPath}");
			SystemTools.CleanDirectory(subBufferPath, _logger);

			return true;
		}

		// Existing protected methods remain unchanged
		protected string ExtractArchive(string filePath, ArchiveTemplate archiveTemplate)
		{
			string pathToBuffer = Settings.Instance.GetBufferDirectory(filePath, archiveTemplate.ComicName);
			CompressionHelper ch = new(_logger);
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
			FileInfo coverInfo = new(coverFile);
			string destFile = Path.Combine(subBuffer, coverInfo.Name);

			using (Bitmap bitmap = (Bitmap)Image.FromFile(coverFile))
			{
				if (Settings.Instance.AddFileIndexToCovers)
				{
					string issueText = $"{fileIndex.ToString().PadLeft(archiveNb.ToString().Length, '0')}/{archiveNb}";

					using (Graphics graphics = Graphics.FromImage(bitmap))
					{
						using (Font arialFont = new("Arial", 220f, FontStyle.Regular, GraphicsUnit.Point))
						{
							SizeF size = graphics.MeasureString(issueText, arialFont);
							using (SolidBrush whiteBrush = new(Color.FromArgb(200, 255, 255, 255)))
							{
								graphics.FillRectangle(whiteBrush, 5f, 5f, size.Width + 10f, size.Height + 10f);
							}

							graphics.DrawString(issueText, arialFont, Brushes.Black, 10f, 10f);
						}
					}
				}

				JpgConverter jpgConverter = new(_logger, 80);
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
				foreach (FileInfo file in metaDataFiles)
				{
					string destFile = SystemTools.GetOutputFilePath(subBuffer, file);
					metaDataSize += file.Length;
					File.Copy(file.FullName, destFile);
				}
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
				JpgConverter jpgConverter = new(_logger, imageCompression);
				for (int i = 0; i < files.Count; ++i)
				{ 
					string destPath = (new FileInfo(SystemTools.GetOutputFilePath(destFolder, files[i]))).Directory.FullName;
					string destFile = Path.Combine(destPath, $"{archiveName}_{(i + increaseIndex).ToString().PadLeft(padSize, '0')}{files[i].Extension}".Replace(' ', '_'));
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

			DirectoryInfo di = new(directory);
			string outputFile = Path.Combine(archiveTemplate.OutputDir, $"{di.Name}{archiveExtension}");
			CompressionHelper ch = new(_logger);
			_logger.Log($"Start compression of {directory} into {outputFile} ...");
			ch.CompressDirectoryContent(directory, outputFile);
			_logger.Log($"Compression done.");
		}
	}

	// Context class to hold splitting state
	public class SplitContext
	{
		public string FilePath { get; set; }
		public ArchiveTemplate ArchiveTemplate { get; set; }
		public List<FileInfo> MetadataFiles { get; set; }
		public List<FileInfo> Pages { get; set; }
		public int TotalPagesCount { get; set; }
		public int NumberOfSplittedFiles { get; set; }
	}
}