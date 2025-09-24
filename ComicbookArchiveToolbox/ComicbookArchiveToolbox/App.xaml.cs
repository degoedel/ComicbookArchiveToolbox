using ComicbookArchiveToolbox.Services;
using ComicbookArchiveToolbox.Views;
using Prism.Ioc;
using Prism.Unity;
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
			containerRegistry.RegisterSingleton<IFileDialogService, FileDialogService>();
			containerRegistry.RegisterSingleton<IPathConflictService, PathConflictService>();
			containerRegistry.Register<IMetadataService, MetadataService>();
			containerRegistry.Register<IArchiveService, ArchiveService>();
			containerRegistry.Register<IBufferManager, Services.BufferManager>();
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Interoperability", "CA1416:Validate platform compatibility", Justification = "<Pending>")]
		protected override Window CreateShell()
		{
			return Container.Resolve<MainWindow>();
		}
	}
}
