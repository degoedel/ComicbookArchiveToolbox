using ComicbookArchiveToolbox.Views;
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

namespace ComicbookArchiveToolbox
{
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App : PrismApplication
	{
		protected override void RegisterTypes(IContainerRegistry containerRegistry)
		{
			
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Interoperability", "CA1416:Validate platform compatibility", Justification = "<Pending>")]
		protected override Window CreateShell()
		{
			return Container.Resolve<MainWindow>();
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Interoperability", "CA1416:Validate platform compatibility", Justification = "<Pending>")]
		protected override IModuleCatalog CreateModuleCatalog()
		{
			return new DirectoryModuleCatalog() { ModulePath = @"." };
		}
	}
}
