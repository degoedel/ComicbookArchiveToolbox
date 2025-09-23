using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace ComicbookArchiveToolbox.CommonTools
{
	public class CompressionHelper
	{
		private readonly string _pathTo7z = "";
		private readonly Logger _logger;
		public CompressionHelper(Logger logger)
		{
			_logger = logger;

			string codeBase = Assembly.GetExecutingAssembly().Location;
			UriBuilder uri = new(codeBase);
			string path = Uri.UnescapeDataString(uri.Path);
			DirectoryInfo installDir = new(System.IO.Path.GetDirectoryName(path));

			_pathTo7z = Path.Combine(installDir.FullName, "7z.exe");
		}

		public void DecompressToDirectory(string archivePath, string decompressionFolder)
		{
			if (!Directory.Exists(decompressionFolder))
				Directory.CreateDirectory(decompressionFolder);

			try
			{
				ProcessStartInfo pro = new()
				{
					WindowStyle = ProcessWindowStyle.Hidden,
					FileName = _pathTo7z,
					Arguments = $"x -aoa -o\"{decompressionFolder}\" \"{archivePath}\""
				};
				_logger.Log($"Launch external command {_pathTo7z} {pro.Arguments}");
				Process x = Process.Start(pro);
				x.WaitForExit();
			}
			catch (Exception e)
			{
				_logger.Log($"Failure during decompression of {archivePath} in {decompressionFolder}: {e.Message}");
			}
		}

		public void CompressDirectoryContent(string inputDir, string outputFile)
		{
			string compressionArg = GetCompressionMethod();
			try
			{
				ProcessStartInfo pro = new()
				{
					WindowStyle = ProcessWindowStyle.Hidden,
					FileName = _pathTo7z,
					Arguments = $"a -aoa -t{compressionArg} \"{outputFile}\" \"{inputDir}\\*\" "
				};
				_logger.Log($"Launch external command {_pathTo7z} {pro.Arguments}");
				Process x = Process.Start(pro);
				x.WaitForExit();
			}
			catch (Exception e)
			{
				_logger.Log($"Failure during compression of {inputDir} in {outputFile} : {e.Message}");
			}
		}

		private static string GetCompressionMethod()
		{
			string compressionArg = Settings.Instance.OutputFormat switch
			{
				SerializationSettings.ArchiveFormat.Cb7 => "7z",
				SerializationSettings.ArchiveFormat.Cbt => "tar",
				SerializationSettings.ArchiveFormat.Cbz => "zip",
				_ => "zip",
			};
			return compressionArg;
		}

		public void ExtractFileType(string archivePath, string decompressionFolder, string fileExtension)
		{
			if (!Directory.Exists(decompressionFolder))
				Directory.CreateDirectory(decompressionFolder);

			try
			{
				ProcessStartInfo pro = new()
				{
					WindowStyle = ProcessWindowStyle.Hidden,
					FileName = _pathTo7z,
					Arguments = $"e  \"{archivePath}\" -o\"{decompressionFolder}\" -aoa -r {fileExtension}"
				};
				_logger.Log($"Launch external command {_pathTo7z} {pro.Arguments}");
				Process x = Process.Start(pro);
				x.WaitForExit();
			}
			catch (Exception e)
			{
				_logger.Log($"Failure during decompression of {archivePath} in {decompressionFolder}: {e.Message}");
			}

		}

		public void UpdateFile(string inputArchive, string file)
		{
			try
			{
				ProcessStartInfo pro = new()
				{
					WindowStyle = ProcessWindowStyle.Hidden,
					FileName = _pathTo7z,
					Arguments = $"a -aoa \"{inputArchive}\" \"{file}\""
				};
				_logger.Log($"Launch external command {_pathTo7z} {pro.Arguments}");
				Process x = Process.Start(pro);
				x.WaitForExit();

				if (x.ExitCode != 0)
				{
					_logger.Log($"WARNING: 7z process exited with code {x.ExitCode} while updating {file} in {inputArchive}");
				}
			}
			catch (Exception e)
			{
				_logger.Log($"Failure during update of {file} in {inputArchive}: {e.Message}");
			}
		}

		/// <summary>
		/// Removes a file from the archive
		/// </summary>
		/// <param name="inputArchive">Path to the archive</param>
		/// <param name="filePattern">File pattern to remove (e.g., "ComicInfo.xml" or "*.xml")</param>
		public void RemoveFile(string inputArchive, string filePattern)
		{
			try
			{
				ProcessStartInfo pro = new()
				{
					WindowStyle = ProcessWindowStyle.Hidden,
					FileName = _pathTo7z,
					Arguments = $"d \"{inputArchive}\" \"{filePattern}\""
				};
				_logger.Log($"Launch external command {_pathTo7z} {pro.Arguments}");
				Process x = Process.Start(pro);
				x.WaitForExit();

				if (x.ExitCode != 0)
				{
					_logger.Log($"WARNING: 7z process exited with code {x.ExitCode} while removing {filePattern} from {inputArchive}");
				}
			}
			catch (Exception e)
			{
				_logger.Log($"Failure during removal of {filePattern} from {inputArchive}: {e.Message}");
			}
		}

		/// <summary>
		/// Updates or adds a file to the archive, ensuring it's always updated regardless of timestamps
		/// </summary>
		/// <param name="inputArchive">Path to the archive</param>
		/// <param name="file">Path to the file to add/update</param>
		/// <param name="forceUpdate">If true, removes existing file first to ensure update</param>
		public void UpdateFileForced(string inputArchive, string file, bool forceUpdate = true)
		{
			try
			{
				if (forceUpdate)
				{
					// First, try to remove the existing file (ignore if it doesn't exist)
					string fileName = Path.GetFileName(file);
					_logger.Log($"Removing existing {fileName} from archive before update");
					RemoveFile(inputArchive, fileName);
				}

				// Then add the new file
				UpdateFile(inputArchive, file);
			}
			catch (Exception e)
			{
				_logger.Log($"Failure during forced update of {file} in {inputArchive}: {e.Message}");
			}
		}
	}
}