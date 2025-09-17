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
			string compressionArg = GetCompressionMethod();
			try
			{
				ProcessStartInfo pro = new()
				{
					WindowStyle = ProcessWindowStyle.Hidden,
					FileName = _pathTo7z,
					Arguments = $"u \"{inputArchive}\" \"{file}\""
				};
				_logger.Log($"Launch external command {_pathTo7z} {pro.Arguments}");
				Process x = Process.Start(pro);
				x.WaitForExit();
			}
			catch (Exception e)
			{
				_logger.Log($"Failure during add of {file} in {inputArchive}: {e.Message}");
			}
		}
	}
}
