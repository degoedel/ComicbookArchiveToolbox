using ComicbookArchiveToolbox.Services;
using ComicbookArchiveToolbox.Views;
using ComicbookArchiveToolbox.CommonTools;
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
			// Core services
			containerRegistry.RegisterSingleton<Logger>();

			// Existing registrations
			containerRegistry.RegisterSingleton<IFileDialogService, FileDialogService>();
			containerRegistry.RegisterSingleton<IPathConflictService, PathConflictService>();
			containerRegistry.Register<IMetadataService, MetadataService>();
			containerRegistry.Register<IArchiveService, PerformanceAwareArchiveService>();
			containerRegistry.Register<IBufferManager, Services.BufferManager>();

			// Register performance-aware services
			containerRegistry.RegisterSingleton<BatchProcessingManager>();

			// Update ShrinkPlugin registration to include BatchProcessingManager dependency
			containerRegistry.Register<ShrinkPlugin>();
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Interoperability", "CA1416:Validate platform compatibility", Justification = "<Pending>")]
		protected override Window CreateShell()
		{
			return Container.Resolve<MainWindow>();
		}

		protected override void OnExit(ExitEventArgs e)
		{
			// Clean up performance monitoring resources
			PerformanceMonitor.Dispose();
			base.OnExit(e);
		}
	}
}