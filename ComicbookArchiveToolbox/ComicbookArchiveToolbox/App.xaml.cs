using ComicbookArchiveHost.Views;
using CommonServiceLocator;
using Prism.Ioc;
using Prism.Modularity;
using Prism.Unity;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition.Hosting;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace ComicbookArchiveHost
{
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App : PrismApplication
	{
		protected override void RegisterTypes(IContainerRegistry containerRegistry)
		{
			
		}

		protected override Window CreateShell()
		{
			return Container.Resolve<MainWindow>();
		}

		protected override IModuleCatalog CreateModuleCatalog()
		{
			return new DirectoryModuleCatalog() { ModulePath = @"." };
		}
	}
}
