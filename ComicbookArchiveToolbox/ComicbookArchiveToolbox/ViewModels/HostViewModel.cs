using ComicbookArchiveToolbox.CommonTools;
using ComicbookArchiveToolbox.CommonTools.Events;
using ComicbookArchiveToolbox.CommonTools.Interfaces;
using ComicbookArchiveToolbox.Events;
using ComicbookArchiveToolbox.Views;
using Prism.Commands;
using Prism.Events;
using Prism.Ioc;
using Prism.Mvvm;
using Prism.Regions;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace ComicbookArchiveToolbox.ViewModels
{
  public class HostViewModel : BindableBase
  {

    #region Attributes

    private string _commonLog = "";
	private IContainerExtension _container;
    private IRegionManager _regionManager;
    private IEventAggregator _eventAggregator;
    public DelegateCommand DisplayToolsCommand { get; private set; }
	public DelegateCommand DisplaySettingsCommand { get; private set; }
	public DelegateCommand DisplayAboutCommand { get; private set; }

	#endregion Attributes

    public string CommonLog
    {
      get { return _commonLog;}
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
			_eventAggregator.GetEvent<HideLogEvent>().Subscribe(CollapseLog, ThreadOption.UIThread);
			DisplayToolsCommand = new DelegateCommand(DisplayTools, CanExecute);
	  DisplaySettingsCommand = new DelegateCommand(DisplaySettings, CanExecute);
	  DisplayAboutCommand = new DelegateCommand(DisplayAbout, CanExecute);
			HideLog = Settings.Instance.HideLog;
	}
	#endregion Constructors

	public string HostTextContent => "This is the host from vm";

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Interoperability", "CA1416:Validate platform compatibility", Justification = "<Pending>")]
		private void DisplayTools()
    {
      IRegion region = _regionManager.Regions["PluginRegion"];
      var view = region.GetView("ToolsView");
      region.Activate(view);
    }

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

	  private bool CanExecute()
    {
      return true;
    }

    private void AddLogLine(string line)
    {
      CommonLog += line + "\n";
    }

		private bool _hideLog;
		public bool HideLog
		{
			get { return _hideLog; }
			set
			{
				SetProperty(ref _hideLog, value);
			}
		}

		private void CollapseLog(bool hide)
		{
			HideLog = hide;
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
