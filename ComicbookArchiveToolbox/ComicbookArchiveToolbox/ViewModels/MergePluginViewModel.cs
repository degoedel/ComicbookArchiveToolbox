using ComicbookArchiveToolbox.CommonTools;
using ComicbookArchiveToolbox.Module.Merge.Service;
using ComicbookArchiveToolbox.Services;
using ComicbookArchiveToolbox.ViewModels;
using Prism.Commands;
using Prism.Events;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Unity;

namespace ComicbookArchiveToolbox.Module.Merge.ViewModels
{
	public class MergePluginViewModel : BasePluginViewModel
	{
		private readonly IUnityContainer _container;
		private readonly IEventAggregator _eventAggregator;
		private readonly Logger _logger;
		private readonly BatchProcessingManager _batchProcessingManager;
		private CancellationTokenSource _cancellationTokenSource;

		public DelegateCommand ClearFilesCommand { get; private set; }
		public DelegateCommand MergeCommand { get; private set; }
		public DelegateCommand CancelCommand { get; private set; }

		public string InputPath { get; private set; }

		private string _outputFile = "";
		public string OutputFile
		{
			get { return _outputFile; }
			set
			{
				SetProperty(ref _outputFile, value);
				MergeCommand.RaiseCanExecuteChanged();
			}
		}

		private ObservableCollection<string> _selectedFiles = [];
		public ObservableCollection<string> SelectedFiles
		{
			get { return _selectedFiles; }
			set
			{
				SetProperty(ref _selectedFiles, value);
				MergeCommand.RaiseCanExecuteChanged();
			}
		}

		private long _imageQuality = 100;
		public long ImageQuality
		{
			get { return _imageQuality; }
			set
			{
				if (value < 0) value = 0;
				if (value > 100) value = 100;
				SetProperty(ref _imageQuality, value);
			}
		}

		private bool _isOperationInProgress;
		public bool IsOperationInProgress
		{
			get { return _isOperationInProgress; }
			set
			{
				SetProperty(ref _isOperationInProgress, value);
				MergeCommand.RaiseCanExecuteChanged();
				CancelCommand.RaiseCanExecuteChanged();
			}
		}

		public MergePluginViewModel(IUnityContainer container, IEventAggregator eventAggregator, BatchProcessingManager batchProcessingManager)
			: base(container, eventAggregator)
		{
			_container = container;
			_eventAggregator = eventAggregator;
			_logger = _container.Resolve<Logger>();
			_batchProcessingManager = batchProcessingManager;

			ClearFilesCommand = new DelegateCommand(ClearFiles, CanExecute);
			MergeCommand = new DelegateCommand(DoMerge, CanMerge);
			CancelCommand = new DelegateCommand(CancelOperation, CanCancel);
		}

		private void ClearFiles()
		{
			SelectedFiles.Clear();
		}

		private bool CanExecute()
		{
			return !IsOperationInProgress;
		}

		private async void DoMerge()
		{
			if (IsOperationInProgress) return;

			// Check system performance and provide recommendations
			var currentCpuUsage = PerformanceMonitor.GetCurrentCpuUsage();
			var recommendedMode = PerformanceMonitor.RecommendPerformanceMode();

			_logger.Log($"System Performance Assessment:");
			_logger.Log($"  Current CPU Usage: {currentCpuUsage:F1}%");
			_logger.Log($"  Current Performance Mode: {Settings.Instance.PerformanceMode}");
			_logger.Log($"  Recommended Performance Mode: {recommendedMode}");

			// Store original settings for restoration
			var originalMode = Settings.Instance.PerformanceMode;
			var originalBatchSize = Settings.Instance.BatchSize;
			var originalThrottling = Settings.Instance.EnableThrottling;
			bool settingsAdjusted = false;

			// Auto-adjust for high CPU usage
			if (currentCpuUsage > 85 && Settings.Instance.PerformanceMode != SerializationSettings.EPerformanceMode.LowResource)
			{
				_logger.Log("High CPU usage detected - temporarily switching to Low Resource mode for merge operation");
				Settings.Instance.PerformanceMode = SerializationSettings.EPerformanceMode.LowResource;
				Settings.Instance.BatchSize = Math.Min(originalBatchSize, 2); // Very conservative for merge operations
				Settings.Instance.EnableThrottling = true;
				settingsAdjusted = true;
			}

			_cancellationTokenSource = new CancellationTokenSource();
			IsOperationInProgress = true;

			try
			{
				var merger = new MergerPlugin(_logger, _eventAggregator, _batchProcessingManager);
				await merger.MergeAsync(OutputFile, SelectedFiles, ImageQuality, _cancellationTokenSource.Token);
				_logger.Log("Merge operation completed successfully");
			}
			catch (OperationCanceledException)
			{
				_logger.Log("Merge operation was cancelled by user");
			}
			catch (Exception ex)
			{
				_logger.Log($"ERROR: Merge operation failed: {ex.Message}");
				if (ex.InnerException != null)
				{
					_logger.Log($"Inner exception: {ex.InnerException.Message}");
				}
			}
			finally
			{
				// Restore original settings if they were adjusted
				if (settingsAdjusted)
				{
					_logger.Log("Restoring original performance settings");
					Settings.Instance.PerformanceMode = originalMode;
					Settings.Instance.BatchSize = originalBatchSize;
					Settings.Instance.EnableThrottling = originalThrottling;
				}

				IsOperationInProgress = false;
				_cancellationTokenSource?.Dispose();
				_cancellationTokenSource = null;
			}
		}

		private void CancelOperation()
		{
			if (_cancellationTokenSource != null && !_cancellationTokenSource.Token.IsCancellationRequested)
			{
				_logger.Log("Cancelling merge operation...");
				_cancellationTokenSource.Cancel();
			}
		}

		private bool CanMerge()
		{
			return !IsOperationInProgress && SelectedFiles != null && SelectedFiles.Count > 1 && !string.IsNullOrWhiteSpace(OutputFile);
		}

		private bool CanCancel()
		{
			return IsOperationInProgress && _cancellationTokenSource != null && !_cancellationTokenSource.Token.IsCancellationRequested;
		}

		protected override string GetCurrentInputPath() => InputPath;
		protected override string GetCurrentOutputPath() => OutputFile;
		protected override void SetInputPath(string file) { }
		protected override void SetOutputPath(string file) => OutputFile = file;
		protected override string GetOutputSuffix() => "_merged";

		protected override void SetInputSelectedFiles(IList<string> files)
		{
			if (files != null && files.Count > 0)
			{
				ClearFiles();
				SelectedFiles.AddRange(files);
				FileInfo fi = new(SelectedFiles[0]);
				string selectedDir = fi.DirectoryName;
				if (Directory.Exists(selectedDir))
				{
					InputPath = selectedDir;
				}
			}
		}
	}
}