using Newtonsoft.Json;
using System;
using System.IO;

namespace ComicbookArchiveToolbox.CommonTools
{
	public sealed class Settings : SerializationSettings
	{
		private readonly string _settingsPath = @"C:\ProgramData\ComicbookArchiveToolbox\Settings\Settings.json";

		private static readonly Lazy<Settings> lazy =
		new(() => new Settings());

		public static Settings Instance { get { return lazy.Value; } }

		private Settings()
		{
			if (File.Exists(_settingsPath))
			{
				InitializeSettings();
			}
			else
			{
				UseFileDirAsBuffer = false;
				BufferDirectory = @"C:\ProgramData\ComicbookArchiveToolbox\Buffer";
				IncludeCover = true;
				IncludeMetadata = true;
				OutputFormat = ArchiveFormat.Cbz;
				DefaultMetadata = "Series;Number;Web;Summary;Notes;Publisher;Imprint;Genre;PageCount;LanguageISO";
				AddFileIndexToCovers = false;
				DefaultImageHeight = 2048;
				FlattenStructure = false;

				// Performance settings with defaults optimized for smaller configurations
				PerformanceMode = EPerformanceMode.Balanced;
				MaxConcurrentOperations = Environment.ProcessorCount / 2; // Use half available cores by default
				UseProgressiveBatching = true;
				BatchSize = 10; // Process files in smaller batches
				EnableThrottling = true;
				ThrottleDelayMs = 50; // Small delay between operations

				SerializeSettings();
			}
		}

		private void InitBuffer()
		{
			if (!Directory.Exists(BufferDirectory))
			{
				Directory.CreateDirectory(BufferDirectory);
			}
		}

		public string GetBufferDirectory(string filePath, string outputNameTemplate)
		{
			FileInfo fi = new(filePath);
			string result;
			if (UseFileDirAsBuffer)
			{
				result = Path.Combine(fi.DirectoryName, outputNameTemplate);
			}
			else
			{
				InitBuffer();
				result = Path.Combine(BufferDirectory, outputNameTemplate);
			}
			return result;
		}

		public void SerializeSettings()
		{
			FileInfo fi = new(_settingsPath);
			Directory.CreateDirectory(fi.DirectoryName);
			JsonSerializer serializer = new()
			{
				NullValueHandling = NullValueHandling.Ignore
			};
			using (StreamWriter sw = new(_settingsPath))
			using (JsonWriter writer = new JsonTextWriter(sw))
			{
				serializer.Serialize(writer, this);
			}
		}

		private void InitializeSettings()
		{
			SerializationSettings serializedSettings = JsonConvert.DeserializeObject<SerializationSettings>(File.ReadAllText(_settingsPath));
			BufferDirectory = serializedSettings.BufferDirectory;
			UseFileDirAsBuffer = serializedSettings.UseFileDirAsBuffer;
			IncludeCover = serializedSettings.IncludeCover;
			IncludeMetadata = serializedSettings.IncludeMetadata;
			OutputFormat = serializedSettings.OutputFormat;
			AddFileIndexToCovers = serializedSettings.AddFileIndexToCovers;
			DefaultImageHeight = serializedSettings.DefaultImageHeight;
			FlattenStructure = serializedSettings.FlattenStructure;

			// Initialize performance settings with safe defaults if not present
			PerformanceMode = serializedSettings.PerformanceMode;
			MaxConcurrentOperations = serializedSettings.MaxConcurrentOperations > 0
				? serializedSettings.MaxConcurrentOperations
				: Environment.ProcessorCount / 2;
			UseProgressiveBatching = serializedSettings.UseProgressiveBatching;
			BatchSize = serializedSettings.BatchSize > 0 ? serializedSettings.BatchSize : 10;
			EnableThrottling = serializedSettings.EnableThrottling;
			ThrottleDelayMs = serializedSettings.ThrottleDelayMs > 0 ? serializedSettings.ThrottleDelayMs : 50;
		}
	}

	public class SerializationSettings
	{
		public enum ArchiveFormat
		{
			Cb7,
			Cbt,
			Cbz
		}

		public enum EPerformanceMode
		{
			LowResource,   // Minimal CPU usage, single-threaded operations
			Balanced,      // Default mode with moderate resource usage
			HighPerformance // Maximum speed, uses all available resources
		}

		public string BufferDirectory { get; set; }
		public bool UseFileDirAsBuffer { get; set; }
		public bool IncludeCover { get; set; }
		public bool IncludeMetadata { get; set; }
		public ArchiveFormat OutputFormat { get; set; }
		public string DefaultMetadata { get; set; }
		public bool AddFileIndexToCovers { get; set; }
		public long DefaultImageHeight { get; set; }
		public bool FlattenStructure { get; set; }

		// Performance Settings
		public EPerformanceMode PerformanceMode { get; set; } = EPerformanceMode.Balanced;
		public int MaxConcurrentOperations { get; set; } = Environment.ProcessorCount / 2;
		public bool UseProgressiveBatching { get; set; } = true;
		public int BatchSize { get; set; } = 10;
		public bool EnableThrottling { get; set; } = true;
		public int ThrottleDelayMs { get; set; } = 50;
	}
}