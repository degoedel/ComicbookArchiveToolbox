using System;
using System.Diagnostics;
using System.Threading;

namespace ComicbookArchiveToolbox.CommonTools
{
	/// <summary>
	/// Monitors system performance and provides recommendations for resource usage.
	/// </summary>
	public static class PerformanceMonitor
	{
		private static readonly PerformanceCounter _cpuCounter = new("Processor", "% Processor Time", "_Total");
		private static readonly Timer _monitorTimer;
		private static volatile float _currentCpuUsage;

		static PerformanceMonitor()
		{
			// Initialize CPU monitoring
			_monitorTimer = new Timer(UpdateCpuUsage, null, TimeSpan.Zero, TimeSpan.FromSeconds(2));
		}

		private static void UpdateCpuUsage(object state)
		{
			try
			{
				_currentCpuUsage = _cpuCounter.NextValue();
			}
			catch
			{
				// Ignore errors in performance monitoring
			}
		}

		public static float GetCurrentCpuUsage() => _currentCpuUsage;

		public static long GetAvailableMemoryMB()
		{
			return GC.GetTotalMemory(false) / 1024 / 1024;
		}

		public static SerializationSettings.EPerformanceMode RecommendPerformanceMode()
		{
			var cpuUsage = GetCurrentCpuUsage();
			var processorCount = Environment.ProcessorCount;

			return cpuUsage switch
			{
				> 80 => SerializationSettings.EPerformanceMode.LowResource,
				> 50 when processorCount <= 2 => SerializationSettings.EPerformanceMode.LowResource,
				< 30 when processorCount >= 8 => SerializationSettings.EPerformanceMode.HighPerformance,
				_ => SerializationSettings.EPerformanceMode.Balanced
			};
		}

		public static int RecommendBatchSize()
		{
			var cpuUsage = GetCurrentCpuUsage();
			var processorCount = Environment.ProcessorCount;

			return cpuUsage switch
			{
				> 80 => 5,
				> 50 => 10,
				< 30 when processorCount >= 8 => 25,
				_ => 15
			};
		}

		public static void Dispose()
		{
			_monitorTimer?.Dispose();
			_cpuCounter?.Dispose();
		}
	}
}
