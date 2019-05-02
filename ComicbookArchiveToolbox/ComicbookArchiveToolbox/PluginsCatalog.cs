using CommonServiceLocator;
using Prism.Modularity;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Policy;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ComicbookArchiveHost
{
	public class PluginsCatalog : ModuleCatalog
	{
		SynchronizationContext _context;

		public PluginsCatalog()
		{
			_context = SynchronizationContext.Current;
			//LoadModuleCatalog();
		}

		/// <summary>
		/// Drives the main logic of building the child domain and searching for the assemblies.
		/// </summary>
		protected override void InnerLoad()
		{
			LoadModuleCatalog();
		}

		private void LoadModuleCatalog()
		{
			var dllsToLoad = GetPluginsCatalog();
			foreach (string dllPath in dllsToLoad)
			{
				LoadModuleCatalog(dllPath);
			}
		}

		private void LoadModuleCatalog(String dllPath)
		{
			AppDomain childDomain = this.BuildChildDomain(AppDomain.CurrentDomain);

			try
			{
				List<string> loadedAssemblies = new List<string>();

				var assemblies = (
									 from Assembly assembly in AppDomain.CurrentDomain.GetAssemblies()
									 where !(assembly is System.Reflection.Emit.AssemblyBuilder)
										&& assembly.GetType().FullName != "System.Reflection.Emit.InternalAssemblyBuilder"
										&& !String.IsNullOrEmpty(assembly.Location)
									 select assembly.Location
								 );

				loadedAssemblies.AddRange(assemblies);

				Type loaderType = typeof(InnerModuleInfoLoader);
				if (loaderType.Assembly != null)
				{
					var loader = (InnerModuleInfoLoader)childDomain.CreateInstanceFrom(loaderType.Assembly.Location, loaderType.FullName).Unwrap();
					loader.LoadAssemblies(loadedAssemblies);

					//get all the ModuleInfos
					ModuleInfo[] modules = loader.GetModuleInfos(dllPath, true);

					//add modules to catalog
					foreach (var mi in modules)
					{
						Items.Add(mi);
					}
					//we are dealing with a file from our file watcher, so let's notify that it needs to be loaded

					LoadModules(modules);
				}
			}
			finally
			{
				AppDomain.Unload(childDomain);
			}
		}

		private static List<string> GetPluginsCatalog()
		{
			string assemblyPath = AssemblyPath;
			DirectoryInfo installDir = new DirectoryInfo(System.IO.Path.GetDirectoryName(assemblyPath));
			var listing = installDir.GetFiles("*CatPlugin*.dll");
			List<string> result = new List<string>();
			result.Add(assemblyPath);
			foreach (FileInfo fi in listing)
			{
				result.Add(fi.FullName);
			}
			return result;
		}

		private static string AssemblyPath
		{
			get
			{
				string codeBase = Assembly.GetExecutingAssembly().CodeBase;
				UriBuilder uri = new UriBuilder(codeBase);
				string path = Uri.UnescapeDataString(uri.Path);
				return path;
			}
		}

		/// <summary>
		/// Uses the IModuleManager to load the modules into memory
		/// </summary>
		/// <param name="modules"></param>
		private void LoadModules(ModuleInfo[] modules)
		{
			if (_context == null)
				return;

			IModuleManager manager = ServiceLocator.Current.GetInstance<IModuleManager>();

			_context.Send(new SendOrPostCallback(delegate (object state)
			{
				foreach (var module in modules)
				{
					manager.LoadModule(module.ModuleName);
				}
			}), null);
		}

		/// <summary>
		/// Creates a new child domain and copies the evidence from a parent domain.
		/// </summary>
		/// <param name="parentDomain">The parent domain.</param>
		/// <returns>The new child domain.</returns>
		/// <remarks>
		/// Grabs the <paramref name="parentDomain"/> evidence and uses it to construct the new
		/// <see cref="AppDomain"/> because in a ClickOnce execution environment, creating an
		/// <see cref="AppDomain"/> will by default pick up the partial trust environment of 
		/// the AppLaunch.exe, which was the root executable. The AppLaunch.exe does a 
		/// create domain and applies the evidence from the ClickOnce manifests to 
		/// create the domain that the application is actually executing in. This will 
		/// need to be Full Trust for Composite Application Library applications.
		/// </remarks>
		/// <exception cref="ArgumentNullException">An <see cref="ArgumentNullException"/> is thrown if <paramref name="parentDomain"/> is null.</exception>
		protected virtual AppDomain BuildChildDomain(AppDomain parentDomain)
		{
			if (parentDomain == null) throw new System.ArgumentNullException("parentDomain");

			Evidence evidence = new Evidence(parentDomain.Evidence);
			AppDomainSetup setup = parentDomain.SetupInformation;
			return AppDomain.CreateDomain("DiscoveryRegion", evidence, setup);
		}

		private class InnerModuleInfoLoader : MarshalByRefObject
		{
			[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic")]
			internal ModuleInfo[] GetModuleInfos(string path, bool isFile = false)
			{
				Assembly moduleReflectionOnlyAssembly =
					AppDomain.CurrentDomain.ReflectionOnlyGetAssemblies().First(
						asm => asm.FullName == typeof(IModule).Assembly.FullName);

				Type IModuleType = moduleReflectionOnlyAssembly.GetType(typeof(IModule).FullName);

				FileSystemInfo info = null;
				if (isFile)
					info = new FileInfo(path);
				else
					info = new DirectoryInfo(path);

				ResolveEventHandler resolveEventHandler = delegate (object sender, ResolveEventArgs args) { return OnReflectionOnlyResolve(args, info); };
				AppDomain.CurrentDomain.ReflectionOnlyAssemblyResolve += resolveEventHandler;
				IEnumerable<ModuleInfo> modules = GetNotAllreadyLoadedModuleInfos(info, IModuleType);
				AppDomain.CurrentDomain.ReflectionOnlyAssemblyResolve -= resolveEventHandler;

				return modules.ToArray();
			}

			private static IEnumerable<ModuleInfo> GetNotAllreadyLoadedModuleInfos(FileSystemInfo info, Type IModuleType)
			{
				List<FileInfo> validAssemblies = new List<FileInfo>();
				Assembly[] alreadyLoadedAssemblies = AppDomain.CurrentDomain.ReflectionOnlyGetAssemblies();

				FileInfo fileInfo = info as FileInfo;
				if (fileInfo != null)
				{
					if (alreadyLoadedAssemblies.FirstOrDefault(assembly => String.Compare(Path.GetFileName(assembly.Location), fileInfo.Name, StringComparison.OrdinalIgnoreCase) == 0) == null)
					{
						var moduleInfos = Assembly.ReflectionOnlyLoadFrom(fileInfo.FullName).GetExportedTypes()
						.Where(IModuleType.IsAssignableFrom)
						.Where(t => t != IModuleType)
						.Where(t => !t.IsAbstract).Select(t => CreateModuleInfo(t));

						return moduleInfos;
					}
				}

				DirectoryInfo directory = info as DirectoryInfo;
				if (directory != null)
				{
					var files = directory.GetFiles("*.dll").Where(file => alreadyLoadedAssemblies.
						FirstOrDefault(assembly => String.Compare(Path.GetFileName(assembly.Location), file.Name, StringComparison.OrdinalIgnoreCase) == 0) == null);

					foreach (FileInfo file in files)
					{
						try
						{
							Assembly.ReflectionOnlyLoadFrom(file.FullName);
							validAssemblies.Add(file);
						}
						catch (BadImageFormatException)
						{
							// skip non-.NET Dlls
						}
					}
				}


				return validAssemblies.SelectMany(file => Assembly.ReflectionOnlyLoadFrom(file.FullName)
											.GetExportedTypes()
											.Where(IModuleType.IsAssignableFrom)
											.Where(t => t != IModuleType)
											.Where(t => !t.IsAbstract)
											.Select(type => CreateModuleInfo(type)));
			}


			private static Assembly OnReflectionOnlyResolve(ResolveEventArgs args, FileSystemInfo info)
			{
				Assembly loadedAssembly = AppDomain.CurrentDomain.ReflectionOnlyGetAssemblies().FirstOrDefault(
					asm => string.Equals(asm.FullName, args.Name, StringComparison.OrdinalIgnoreCase));
				if (loadedAssembly != null)
				{
					return loadedAssembly;
				}

				DirectoryInfo directory = info as DirectoryInfo;
				if (directory != null)
				{
					AssemblyName assemblyName = new AssemblyName(args.Name);
					string dependentAssemblyFilename = Path.Combine(directory.FullName, assemblyName.Name + ".dll");
					if (File.Exists(dependentAssemblyFilename))
					{
						return Assembly.ReflectionOnlyLoadFrom(dependentAssemblyFilename);
					}
				}

				return Assembly.ReflectionOnlyLoad(args.Name);
			}

			[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic")]
			internal void LoadAssemblies(IEnumerable<string> assemblies)
			{
				foreach (string assemblyPath in assemblies)
				{
					try
					{
						Assembly.ReflectionOnlyLoadFrom(assemblyPath);
					}
					catch (FileNotFoundException)
					{
						// Continue loading assemblies even if an assembly can not be loaded in the new AppDomain
					}
				}
			}

			private static ModuleInfo CreateModuleInfo(Type type)
			{
				string moduleName = type.Name;
				List<string> dependsOn = new List<string>();
				bool onDemand = false;
				var moduleAttribute = CustomAttributeData.GetCustomAttributes(type).FirstOrDefault(cad => cad.Constructor.DeclaringType.FullName == typeof(ModuleAttribute).FullName);

				if (moduleAttribute != null)
				{
					foreach (CustomAttributeNamedArgument argument in moduleAttribute.NamedArguments)
					{
						string argumentName = argument.MemberInfo.Name;
						switch (argumentName)
						{
							case "ModuleName":
								moduleName = (string)argument.TypedValue.Value;
								break;

							case "OnDemand":
								onDemand = (bool)argument.TypedValue.Value;
								break;

							case "StartupLoaded":
								onDemand = !((bool)argument.TypedValue.Value);
								break;
						}
					}
				}

				var moduleDependencyAttributes = CustomAttributeData.GetCustomAttributes(type).Where(cad => cad.Constructor.DeclaringType.FullName == typeof(ModuleDependencyAttribute).FullName);
				foreach (CustomAttributeData cad in moduleDependencyAttributes)
				{
					dependsOn.Add((string)cad.ConstructorArguments[0].Value);
				}

				ModuleInfo moduleInfo = new ModuleInfo(moduleName, type.AssemblyQualifiedName)
				{
					InitializationMode =
						onDemand
							? InitializationMode.OnDemand
							: InitializationMode.WhenAvailable,
					Ref = type.Assembly.CodeBase,
				};
				foreach (string dep in dependsOn)
				{
					moduleInfo.DependsOn.Add(dep);
				}
				return moduleInfo;
			}
		}
	}
}
