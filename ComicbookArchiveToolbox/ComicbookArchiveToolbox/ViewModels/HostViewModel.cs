using ComicbookArchiveToolbox.CommonTools;
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
	private IContainerExtension _container;
    private IRegionManager _regionManager;
    public DelegateCommand DisplayToolsCommand { get; private set; }
	public DelegateCommand DisplaySettingsCommand { get; private set; }
	public DelegateCommand DisplayAboutCommand { get; private set; }

	#endregion Attributes

	#region Constructors
	public HostViewModel(IContainerExtension container, IRegionManager regionManager)
    {
		  _container = container;
      _regionManager = regionManager;
      DisplayToolsCommand = new DelegateCommand(DisplayTools, CanExecute);
	  DisplaySettingsCommand = new DelegateCommand(DisplaySettings, CanExecute);
	  DisplayAboutCommand = new DelegateCommand(DisplayAbout, CanExecute);
	}
	#endregion Constructors

	public string HostTextContent => "This is the host from vm";


    private void DisplayTools()
    {
      IRegion region = _regionManager.Regions["PluginRegion"];
      var view = region.GetView("ToolsView");
      region.Activate(view);
    }
	private void DisplaySettings()
	{
		IRegion region = _regionManager.Regions["PluginRegion"];
		var view = region.GetView("SettingsView");
		region.Activate(view);
	}
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
  }
}
