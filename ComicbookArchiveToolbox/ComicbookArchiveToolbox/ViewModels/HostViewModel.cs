using ComicbookArchiveToolbox.CommonTools.Events;
using Prism.Commands;
using Prism.Events;
using Prism.Ioc;
using Prism.Mvvm;
using Prism.Navigation.Regions;

namespace ComicbookArchiveToolbox.ViewModels
{
	public class HostViewModel : BindableBase
	{

		#region Attributes

		private string _commonLog = "";
		private readonly IContainerExtension _container;
		private readonly IRegionManager _regionManager;
		private readonly IEventAggregator _eventAggregator;
		public DelegateCommand DisplayToolsCommand { get; private set; }
		public DelegateCommand DisplaySettingsCommand { get; private set; }
		public DelegateCommand DisplayAboutCommand { get; private set; }
		public DelegateCommand DisplayCompressCommand { get; private set; }
		public DelegateCommand DisplayMergeCommand { get; private set; }
		public DelegateCommand DisplaySplitCommand { get; private set; }
		public DelegateCommand DisplayEditCommand { get; private set; }

		#endregion Attributes

		public string CommonLog
		{
			get { return _commonLog; }
			set
			{
				SetProperty(ref _commonLog, value);
			}
		}

		#region Constructors
		public HostViewModel(IContainerExtension container, IRegionManager regionManager, IEventAggregator eventAggregator)
		{
			_container = container;
			_regionManager = regionManager;
			_eventAggregator = eventAggregator;
			_eventAggregator.GetEvent<LogEvent>().Subscribe(AddLogLine, ThreadOption.UIThread);
			_eventAggregator.GetEvent<BusinessEvent>().Subscribe(SetBusyState, ThreadOption.UIThread);
			DisplaySettingsCommand = new DelegateCommand(DisplaySettings, CanExecute);
			DisplayAboutCommand = new DelegateCommand(DisplayAbout, CanExecute);
			DisplayCompressCommand = new DelegateCommand(DisplayCompress, CanExecute);
			DisplayMergeCommand = new DelegateCommand(DisplayMerge, CanExecute);
			DisplaySplitCommand = new DelegateCommand(DisplaySplit, CanExecute);
			DisplayEditCommand = new DelegateCommand(DisplayEdit, CanExecute);
		}
		#endregion Constructors

		public string HostTextContent => "This is the host from vm";
		public string ToolCompressDescription => "Compress File: Create a new archive with recompressed (degraded) pictures to allow faster page loading in reader.";
		public string ToolEditDescription => "Edit Metadata: Edit or create a metadata file in archive. Will create a new archive if format does not support update.";
		public string ToolMergeDescription => "Merge Files: Merge the selected archives in one file. Allow image recompression.";
		public string ToolSplitDescription => "Split File: Split the selected archive in several files. Allow images recompression. Available split methods are : by file number, by maximum size per file, or by maximum pages number in files.";
		public string ToolSettingsDescription => "Application settings";
		public string ToolAboutDescription => "About the app";


		[System.Diagnostics.CodeAnalysis.SuppressMessage("Interoperability", "CA1416:Validate platform compatibility", Justification = "<Pending>")]
		private void DisplaySettings()
		{
			IRegion region = _regionManager.Regions["PluginRegion"];
			var view = region.GetView("SettingsView");
			region.Activate(view);
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Interoperability", "CA1416:Validate platform compatibility", Justification = "<Pending>")]
		private void DisplayAbout()
		{
			IRegion region = _regionManager.Regions["PluginRegion"];
			var view = region.GetView("AboutView");
			region.Activate(view);
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Interoperability", "CA1416:Validate platform compatibility", Justification = "<Pending>")]
		private void DisplayCompress()
		{
			IRegion region = _regionManager.Regions["PluginRegion"];
			var view = region.GetView("CompressView");
			region.Activate(view);
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Interoperability", "CA1416:Validate platform compatibility", Justification = "<Pending>")]
		private void DisplayMerge()
		{
			IRegion region = _regionManager.Regions["PluginRegion"];
			var view = region.GetView("MergeView");
			region.Activate(view);
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Interoperability", "CA1416:Validate platform compatibility", Justification = "<Pending>")]
		private void DisplaySplit()
		{
			IRegion region = _regionManager.Regions["PluginRegion"];
			var view = region.GetView("SplitView");
			region.Activate(view);
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Interoperability", "CA1416:Validate platform compatibility", Justification = "<Pending>")]
		private void DisplayEdit()
		{
			IRegion region = _regionManager.Regions["PluginRegion"];
			var view = region.GetView("EditView");
			region.Activate(view);
		}

		private bool CanExecute()
		{
			return true;
		}

		private void AddLogLine(string line)
		{
			CommonLog += line + "\n";
		}

		private bool _isBusy;
		public bool IsBusy
		{
			get { return _isBusy; }
			set
			{
				SetProperty(ref _isBusy, value);
			}
		}

		private void SetBusyState(bool busy)
		{
			IsBusy = busy;
		}

	}
}
