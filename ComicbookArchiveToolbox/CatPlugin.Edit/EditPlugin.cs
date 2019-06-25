using CatPlugin.Edit.Views;
using ComicbookArchiveToolbox.CommonTools.Interfaces;
using Prism.Commands;
using Prism.Ioc;
using Prism.Regions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Unity;

namespace CatPlugin.Edit
{
	public class EditPlugin : ICatPlugin
	{
		#region Attributes
		private readonly IUnityContainer _container;
		public Canvas _icon;
		#endregion Attributes

		public Canvas Icon => _icon;
		public string Name => "Edit Metadata";
		public string ToolDescription => "Edit or create a metadata file in archive. Will create a new archive if format does not support update.";

		public DelegateCommand LoadViewCommand { get; private set; }

		public EditPlugin(IUnityContainer container)
		{
			_container = container;
			_container.RegisterType<ICatPlugin, EditPlugin>("Edit Metadata");
			var myResourceDictionary = new ResourceDictionary();
			myResourceDictionary.Source = new Uri("/CatPlugin.Edit;component/Resources/Icons.xaml", UriKind.RelativeOrAbsolute);
			_icon = myResourceDictionary["appbar_page_edit"] as Canvas;

			LoadViewCommand = new DelegateCommand(LoadView, CanExecute);
		}

		#region Command
		private void LoadView()
		{
			var regionManager = _container.Resolve<IRegionManager>();
			IRegion region = regionManager.Regions["PluginRegion"];
			var view = region.GetView("EditView");
			region.Activate(view);
		}

		private bool CanExecute()
		{
			return true;
		}
		#endregion Command


		public void OnInitialized(IContainerProvider containerProvider)
		{
			var regionManager = containerProvider.Resolve<IRegionManager>();
			IRegion region = regionManager.Regions["PluginRegion"];
			region.Add(_container.Resolve<EditPluginView>(), "EditView");
		}

		public void RegisterTypes(IContainerRegistry containerRegistry)
		{
		}
	}
}
