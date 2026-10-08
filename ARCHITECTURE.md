# ComicBook Archive Toolbox - Architecture Documentation

## Project Overview

**ComicBook Archive Toolbox** is a WPF desktop application (targeting .NET 8.0-windows) designed for managing, manipulating, and optimizing comic book archives (formats: .cbz, .cb7, .cbt, .rar). The application follows the **MVVM (Model-View-ViewModel)** architectural pattern with **Prism framework** for dependency injection and event aggregation.

### Technology Stack
- **Framework**: .NET 8.0-windows
- **UI Framework**: WPF (Windows Presentation Foundation)
- **Architecture Pattern**: MVVM with Prism
- **Dependency Injection**: Unity Container (via Prism)
- **Event System**: Prism EventAggregator
- **Compression**: 7-Zip CLI (7z.exe)
- **Serialization**: Newtonsoft.Json (for settings)
- **Image Processing**: System.Drawing (GDI+)

---

## Project Structure

```
ComicbookArchiveToolbox/
├── Views/                          # XAML UI views and code-behind
│   ├── MainWindow.xaml(.cs)        # Application main window
│   ├── HostView.xaml(.cs)          # Container for plugin regions
│   ├── AboutView.xaml(.cs)         # About dialog
│   ├── SettingsView.xaml(.cs)      # Settings UI
│   ├── EditPluginView.xaml(.cs)    # Metadata editing UI
│   ├── ShrinkPluginView.xaml(.cs)  # Image shrinking UI
│   ├── MergePluginView.xaml(.cs)   # Archive merging UI
│   ├── SplitPluginView.xaml(.cs)   # Archive splitting UI
│   ├── Split*View.xaml(.cs)        # Splitter strategy-specific views
│   ├── ListViewDragDropManager.cs  # Drag-and-drop support
│   ├── DragAdorner.cs              # Visual adorner for drag operations
│   └── MouseUtilities.cs           # Mouse interaction utilities
│
├── ViewModels/                     # MVVM ViewModel layer
│   ├── HostViewModel.cs            # Main view model for plugin routing
│   ├── BasePluginViewModel.cs      # Abstract base for all plugins
│   ├── ShrinkPluginViewModel.cs    # Shrink operation VM
│   ├── MergePluginViewModel.cs     # Merge operation VM
│   ├── SplitPluginViewModel.cs     # Split operation VM
│   ├── EditPluginViewModel.cs      # Metadata editing VM
│   ├── SettingsViewModel.cs        # Settings VM
│   └── AboutViewModel.cs           # About dialog VM
│
├── Services/                       # Business logic and core services
│   ├── IArchiveService.cs          # Archive operations interface
│   ├── ArchiveService.cs           # Archive extraction/update implementation
│   ├── PerformanceAwareArchiveService.cs  # Performance-wrapped archive service
│   ├── PerformanceMonitor.cs       # CPU/Memory monitoring (static)
│   ├── IMetadataService.cs         # Metadata handling interface
│   ├── MetadataService.cs          # XML/HTML metadata parsing and conversion
│   ├── IBufferManager.cs           # Buffer lifecycle management
│   ├── BufferManager.cs            # Temporary storage for intermediate files
│   ├── BatchProcessingManager.cs   # Batch operation orchestration with performance control
│   ├── FileDialogService.cs        # File/folder picker dialogs
│   ├── PathConflictService.cs      # Output path conflict resolution
│   ├── BaseSplitterPlugin.cs       # Template method pattern for splitting algorithms
│   ├── ByFileSplitterPlugin.cs     # Split by number of files
│   ├── ByMaxPageSplitterPlugin.cs  # Split by maximum pages per file
│   ├── ByPageIdSplitterPlugin.cs   # Split by page index ranges
│   ├── BySizeSplitterPlugin.cs     # Split by maximum size per file
│   ├── ShrinkPlugin.cs             # Image resizing and recompression
│   ├── MergerPlugin.cs             # Archive merging with image processing
│   └── FileConstants.cs            # Constants for file handling
│
├── CommonTools/                    # Shared utilities and helpers
│   ├── Logger.cs                   # Event-based logging system
│   ├── Settings.cs                 # Application settings (singleton)
│   ├── SerializationSettings.cs    # Settings serialization model
│   ├── CompressionHelper.cs        # 7-Zip wrapper for compression/decompression
│   ├── ComicMetadata.cs            # Metadata DTO
│   ├── ArchiveTemplate.cs          # Archive operation configuration template
│   ├── JpgConverter.cs             # Image format conversion
│   ├── SystemTools.cs              # File system and platform utilities
│   ├── BooleanToTextConverter.cs   # XAML value converter
│   ├── InverseBooleanConverter.cs  # XAML inverse boolean converter
│   └── Events/
│       ├── BusinessEvent.cs        # Prism event for busy state
│       ├── LogEvent.cs             # Prism event for logging
│       └── InterfaceLoadedEvent.cs # Prism event for UI initialization
│
├── Interfaces/                     # Custom interfaces
│   └── ISplitter.cs                # Splitter algorithm interface
│
├── Properties/                     # Assembly metadata
│   ├── AssemblyInfo.cs
│   ├── Resources.resx              # Resource strings
│   └── Settings.settings           # WPF settings provider
│
├── Resources/                      # Static resources
│   └── Cat.ico                     # Application icon
│
├── App.xaml(.cs)                   # WPF application entry point and DI setup
└── ComicbookArchiveHost.csproj     # Project file
```

---

## Core Architecture Patterns

### 1. MVVM with Prism Framework

The application strictly follows the Model-View-ViewModel pattern:

- **Views** (XAML): Purely declarative UI
- **ViewModels** (C#): Business logic, state management, command handling
- **Models**: Data DTOs (ComicMetadata, ArchiveTemplate)
- **Event Aggregator**: Decoupled communication between VMs

```
┌─────────────┐
│   View      │  (HostView.xaml)
│  (XAML UI)  │◄─────────┐
└─────────────┘          │ Binding
	  ▲                  │
	  │ Routing          │
	  │ Commands    ┌─────────────────────┐
	  │            │   ViewModel         │
	  │            │  (HostViewModel)    │
	  └────────────┤ - IsBusy            │
				   │ - CommonLog         │
				   │ - DisplayCommands   │
				   └─────────┬───────────┘
							 │
							 │ Publishes/Subscribes
							 │
				   ┌─────────────────────┐
				   │ EventAggregator     │
				   │ - BusinessEvent     │
				   │ - LogEvent          │
				   └─────────────────────┘
```

### 2. Dependency Injection (Unity Container)

All services are registered in `App.xaml.cs`:

```csharp
containerRegistry.RegisterSingleton<Logger>();
containerRegistry.Register<IArchiveService, PerformanceAwareArchiveService>();
containerRegistry.Register<IMetadataService, MetadataService>();
containerRegistry.RegisterSingleton<BatchProcessingManager>();
```

**Key Principles:**
- Singletons: Logger, BatchProcessingManager (shared across entire app)
- Transients: ArchiveService, MetadataService (new instance per request)
- All dependencies resolved via constructor injection

### 3. Plugin Architecture

The application uses a **plugin-based approach** for operations:

```
┌────────────────────────────────────────┐
│       HostViewModel (Container)        │
│    Manages region navigation           │
└────────────────────────────────────────┘
			  │
			  │ Routes via Regions
			  │
	┌─────────┼─────────┐
	│         │         │
	▼         ▼         ▼
┌────────┐ ┌────────┐ ┌────────┐
│ Shrink │ │ Merge  │ │ Split  │
│ Plugin │ │ Plugin │ │ Plugins│
└────────┘ └────────┘ └────────┘
```

Each plugin:
- Has dedicated **ViewModel** (ShrinkPluginViewModel, MergePluginViewModel, etc.)
- Has dedicated **Views** for UI
- Inherits from **BasePluginViewModel** for common functionality
- Accesses services via injected dependencies

---

## Data Flow and Key Workflows

### 1. Application Startup Flow

```
┌──────────────────┐
│  App.xaml.cs     │
│  PrismApplication│
└────────┬─────────┘
		 │
		 │ RegisterTypes()
		 │ - Setup DI Container
		 │
		 ▼
┌──────────────────────────────┐
│ Unity Container              │
│ - Logger (Singleton)         │
│ - Services (Transient)       │
│ - BatchProcessingManager    │
└────────┬─────────────────────┘
		 │
		 │ CreateShell()
		 │
		 ▼
┌──────────────────────────────┐
│ MainWindow                   │
│ - HostViewModel              │
│ - PluginRegion (dynamic)     │
└──────────────────────────────┘
```

### 2. Shrink Operation (Async Flow)

The shrink operation processes an archive, resizing/recompressing images:

```
User (ShrinkPluginView)
  │
  │ [Execute ShrinkCommand]
  │
  ▼
ShrinkPluginViewModel
  │
  ├─► Publish(BusinessEvent: true)        [Disable UI]
  │
  ▼
ShrinkPlugin.CompressAsync()
  │
  ├─► [Setup Buffer Paths]
  │   └─ bufferPath: C:\ProgramData\...\buffer
  │   └─ outputBuffer: buffer\outputBuffer
  │
  ├─► [Extract Archive]
  │   └─ CompressionHelper.DecompressToDirectory()
  │      └─ 7z.exe x -aoa -o"bufferPath" "archive.cbz"
  │
  ├─► [Parse Files]
  │   └─ SystemTools.ParseArchiveFiles()
  │      └─ Separate metadata (*.xml, *.html) from pages (images)
  │
  ├─► [Resize/Recompress Images]
  │   └─ BatchProcessingManager.ProcessFilesAsync()
  │      ├─ Check Settings.PerformanceMode
  │      ├─ Check Settings.MaxConcurrentOperations
  │      │
  │      ├─► If LowResource:
  │      │   └─ Process sequentially with throttling
  │      │
  │      ├─► If Balanced:
  │      │   └─ Process in batches (BatchSize: 10-15)
  │      │
  │      └─► If HighPerformance:
  │          └─ Process in parallel (MaxConcurrentOperations: cores/2)
  │
  │      For each image:
  │      ├─ JpgConverter.ResizeImage(quality, ratio, pixels)
  │      └─ Save to outputBuffer
  │
  ├─► [Copy Metadata]
  │   └─ Copy metadata files (*.xml, *.html) to outputBuffer
  │
  ├─► [Final Compression]
  │   └─ CompressionHelper.CompressDirectoryContent()
  │      └─ 7z.exe a -t{format} "output.cbz" "outputBuffer\*"
  │
  ├─► [Cleanup]
  │   └─ SystemTools.CleanDirectory(bufferPath)
  │
  └─► Publish(BusinessEvent: false)        [Enable UI]
	   │
	   └─► Logger.Log() events
			│
			└─► EventAggregator.PublishLogEvent()
				 │
				 └─► HostViewModel.AddLogLine()
					  │
					  └─► UI updates CommonLog display

```

### 3. Merge Operation (Async Flow)

```
User (MergePluginView)
  │
  │ [Execute MergeCommand with multiple files]
  │
  ▼
MergePluginViewModel
  │
  ├─► Publish(BusinessEvent: true)        [Disable UI]
  │
  ▼
MergerPlugin.MergeAsync(files[], outputFile, imageQuality)
  │
  ├─► [Setup Buffer & Output Paths]
  │   └─ Create outputBuffer directory
  │
  ├─► [Extract All Archives - ASYNC (Thread-Safe Order Preservation)]
  │   │
  │   └─ Create ConcurrentDictionary<int, ExtractionResult>
  │      └─ BatchProcessingManager.ProcessFilesAsync(files)
  │         │
  │         For each file (async, potentially out-of-order completion):
  │         ├─ Extract to bufferPath/file_{index}
  │         ├─ Collect metadata files & page images
  │         └─ Store result in dictionary[index] (NOT in shared lists)
  │
  │   After all extractions complete:
  │   └─ Iterate indices 0..N-1 sequentially
  │      └─ Populate allMetadataFiles & allPages in ORIGINAL ORDER
  │         (regardless of async completion order)
  │
  ├─► [Process Images in Batch]
  │   └─ BatchProcessingManager.ProcessFilesAsync(allImages)
  │      │
  │      ├─ Reindex pages with sequential naming
  │      ├─ Apply recompression (if requested)
  │      └─ Save to outputBuffer
  │
  ├─► [Final Compression]
  │   └─ CompressionHelper.CompressDirectoryContent()
  │
  ├─► [Cleanup]
  │   └─ SystemTools.CleanDirectory(bufferPath)
  │
  └─► Publish(BusinessEvent: false)        [Enable UI]

```

**Key Design: Order Preservation in Async Extraction**

The merge operation must preserve the exact order of input files in the output archive. This is achieved through a **thread-safe dictionary pattern**:

1. **Async Execution (Unordered)**: Multiple archives are extracted concurrently via `BatchProcessingManager.ProcessFilesAsync()`. Archives may complete extraction in any order depending on file size, I/O speed, and system load.

2. **Indexed Storage**: Each extraction task stores its results in `ConcurrentDictionary<int, ExtractionResult>` using the archive's **original index** (0 to N-1) as the key. This eliminates race conditions from direct list appends.

3. **Sequential Reassembly**: After all async tasks complete, a loop iterates through indices 0 to N-1 in order and appends results to the shared `allMetadataFiles` and `allPages` lists. This guarantees the final merged archive has pages in the exact sequence of input archives, regardless of extraction speed variation.

4. **Zero Lock Overhead**: Unlike the previous lock-based approach, this pattern eliminates lock contention since each async task writes to its own unique dictionary key, and sequential reassembly has no concurrent access.

### 4. Split Operation (Template Method Pattern)

```
User (SplitPluginView)
  │
  │ [Select split strategy: "By File Nb", "By Max Pages", etc.]
  │ [Execute SplitCommand]
  │
  ▼
SplitPluginViewModel
  │
  ├─► Set SelectedStyle property
  │   └─ Triggers SetSplitterView()
  │      └─ Activates strategy-specific View
  │         (SplitByFileNbView, SplitByMaxPagesView, etc.)
  │
  ├─► Publish(BusinessEvent: true)
  │
  ▼
BaseSplitterPlugin.Split()                 [Template Method]
  │
  ├─► ValidateInput()                      [Abstract - overridden by strategy]
  │   └─ Check strategy-specific parameters
  │
  ├─► InitializeSplitting()                [Concrete Template]
  │   ├─ Extract archive to buffer
  │   ├─ Parse metadata & pages
  │   └─ Compute number of output files
  │
  ├─► ExecuteSplitting()                   [Abstract - overridden by strategy]
  │   │
  │   ├─ ByFileSplitterPlugin:
  │   │  └─ Split evenly: totalPages / numberOfFiles
  │   │
  │   ├─ ByMaxPageSplitterPlugin:
  │   │  └─ Split by max pages per file
  │   │
  │   ├─ BySizeSplitterPlugin:
  │   │  └─ Split by max MB per file
  │   │
  │   └─ ByPageIdSplitterPlugin:
  │      └─ Split by user-defined index ranges
  │
  │  For each split:
  │  ├─ Create output directory structure
  │  ├─ Copy pages for this split
  │  ├─ Copy/merge metadata
  │  └─ Compress to archive
  │
  ├─► CleanupSplitting()                   [Concrete Template]
  │   └─ Delete temporary buffer
  │
  └─► Publish(BusinessEvent: false)

```

---

## Performance Management System

### 1. PerformanceMonitor (Static Singleton)

Monitors system resources and provides recommendations:

```csharp
public static class PerformanceMonitor
{
	// Updates CPU usage every 2 seconds
	public static float GetCurrentCpuUsage() => _currentCpuUsage;

	// Recommends mode based on CPU/cores
	public static EPerformanceMode RecommendPerformanceMode()
	{
		> 80% CPU  → LowResource
		50-80% CPU → Balanced (if cores <= 2: LowResource)
		< 30% CPU  → HighPerformance (if cores >= 8)
	}

	// Recommends batch size
	public static int RecommendBatchSize()
	{
		> 80% CPU  → 5
		50-80% CPU → 10
		< 30% CPU  → 25 (if cores >= 8)
	}
}
```

### 2. BatchProcessingManager

Orchestrates batch operations with performance controls:

```
Settings (Singleton)
  │
  ├─ PerformanceMode: LowResource | Balanced | HighPerformance
  ├─ MaxConcurrentOperations: int (cores / 2)
  ├─ UseProgressiveBatching: bool
  ├─ BatchSize: int (5-25)
  ├─ EnableThrottling: bool
  └─ ThrottleDelayMs: int (50)
  │
  ▼
BatchProcessingManager.ProcessFilesAsync<T>()
  │
  ├─ If UseProgressiveBatching:
  │  └─ ProcessInBatches()
  │     ├─ Loop: for i=0; i < items.Count; i += batchSize
  │     │
  │     ├─ If LowResource:
  │     │  └─ Sequential: foreach(item in batch) await processor(item)
  │     │                 + throttle delay
  │     │
  │     └─ Else:
  │        └─ Parallel: Task.WhenAll(batch.Select(processor))
  │                     + delay between batches
  │
  └─ Else:
	 └─ ProcessConcurrently()
		├─ Divide items into chunks
		└─ Parallel.ForEach(chunk, maxConcurrency)
```

### 3. Performance-Aware Archive Service

Wraps the basic archive service with performance tracking:

```csharp
public class PerformanceAwareArchiveService : IArchiveService
{
	// Logs CPU usage, timings, batch info
	// Delegates to base ArchiveService
}
```

---

## Event System (Prism EventAggregator)

The application uses **pub-sub events** for decoupled communication:

### BusinessEvent
- **Type**: `PubSubEvent<bool>`
- **Payload**: `true` = busy, `false` = idle
- **Publishers**: Plugin ViewModels (Shrink, Merge, Split, Edit)
- **Subscribers**: HostViewModel
- **UI Effect**: Disables/enables controls, shows progress indicators

```
ShrinkPluginViewModel
  │
  ├─► _eventAggregator.GetEvent<BusinessEvent>().Publish(true)
  │   [Disable UI while processing]
  │
  ├─► ... async operation ...
  │
  └─► _eventAggregator.GetEvent<BusinessEvent>().Publish(false)
	  [Enable UI after completion]
	  │
	  ▼
  HostViewModel (Subscriber)
  └─ SetBusyState(bool busy)
	 └─ IsBusy = busy  [Bound to UI overlays/spinners]
```

### LogEvent
- **Type**: `PubSubEvent<string>`
- **Payload**: Log message
- **Publishers**: Logger class (via EventAggregator)
- **Subscribers**: HostViewModel
- **UI Effect**: Appends to CommonLog TextBlock

```
Logger.Log("Operation started")
  │
  └─► _eventAggregator.GetEvent<LogEvent>().Publish(line)
	  │
	  ▼
  HostViewModel
  └─ AddLogLine(string line)
	 └─ CommonLog += line + "\n"  [Binding updates TextBlock]
```

### InterfaceLoadedEvent
- **Type**: `PubSubEvent<object>`
- **Payload**: Loaded interface info
- **Purpose**: Notify when UI regions are ready

---

## Metadata Handling

### Supported Formats

1. **Comic Info XML** (ComicInfo.xml)
   - Standard format for comic metadata
   - Fields: Title, Series, Number, Writer, Penciller, Publisher, etc.

2. **Dublin Core HTML** (metadata.html)
   - Web standard metadata format
   - Maps to/from ComicInfo format
   - Calibre-compatible

### MetadataService Responsibilities

```csharp
public class MetadataService : IMetadataService
{
	// Load ComicInfo.xml
	public async Task<ObservableCollection<ComicMetadata>> LoadComicInfoAsync(filePath)

	// Save ComicInfo.xml
	public async Task SaveComicInfoAsync(filePath, collection)

	// Convert Dublin Core HTML ↔ ComicInfo
	public string ConvertDublinCoreToComicInfo(htmlContent)
	public string ConvertComicInfoToDublinCore(xmlContent)

	// Parse metadata from archive
	public async Task<List<ComicMetadata>> ExtractMetadataAsync(archivePath)
}
```

**Performance Optimization**:
- Pre-compiled Regex patterns for HTML parsing
- Static mapping dictionaries (Dublin Core ↔ ComicInfo)
- Async file I/O for large XML documents

---

## Buffer Management

### Buffer Directory Structure

```
C:\ProgramData\ComicbookArchiveToolbox\Buffer\
├── page/                          [For shrink operations]
│   ├── extracted/                 [Original archive contents]
│   │   ├── page_001.jpg
│   │   ├── page_002.jpg
│   │   └── ComicInfo.xml
│   └── outputBuffer/              [Resized/recompressed images]
│       ├── page_001.jpg           (quality-adjusted)
│       ├── page_002.jpg
│       └── ComicInfo.xml
│
└── archive_template/              [For merge/split operations]
	├── archive_0/                 [File 0 extraction]
	├── archive_1/                 [File 1 extraction]
	└── outputBuffer/              [Merged/split output staging]
```

### BufferManager Interface

```csharp
public interface IBufferManager
{
	// Allocate buffer directory
	string AllocateBuffer(string archiveName);

	// Free buffer directory
	void FreeBuffer(string bufferPath);

	// Check available space
	long GetAvailableSpace(string bufferPath);
}
```

### Buffer Cleanup Strategy

- **Automatic**: After operation completes (success or failure)
- **Manual**: Via SettingsViewModel → clear cache button
- **Safety**: Prompt user before cleaning to prevent data loss

---

## Extension Points

### 1. Adding a New Splitter Strategy

To add a new split algorithm (e.g., "By Duration"):

1. **Create Plugin Class**:
   ```csharp
   public class ByDurationSplitterPlugin : BaseSplitterPlugin
   {
	   protected override bool ValidateInput(ArchiveTemplate template)
	   {
		   // Check that duration is set
	   }

	   protected override bool ExecuteSplitting(SplitContext context)
	   {
		   // Implement duration-based splitting logic
	   }
   }
   ```

2. **Register in App.xaml.cs**:
   ```csharp
   containerRegistry.Register<ByDurationSplitterPlugin>();
   ```

3. **Create ViewModel & Views**:
   - `SplitByDurationViewModel.cs`
   - `SplitByDurationView.xaml(.cs)`

4. **Add to SplitPluginViewModel**:
   ```csharp
   public List<string> SplitStyles => new()
   {
	   "By File Nb",
	   "By Max Pages Nb",
	   "By Size (Mb)",
	   "By Pages Index",
	   "By Duration"        // New!
   };
   ```

### 2. Adding a New Image Processing Filter

To add image processing (e.g., grayscale conversion):

1. **Create Filter Class**:
   ```csharp
   public class GrayscaleFilter
   {
	   public Bitmap Apply(Bitmap image) { /* ... */ }
   }
   ```

2. **Integrate into ShrinkPlugin**:
   ```csharp
   var bitmap = JpgConverter.LoadImage(imagePath);
   bitmap = _grayscaleFilter.Apply(bitmap);
   JpgConverter.SaveImage(bitmap, outputPath, quality);
   ```

3. **Add UI Controls** to ShrinkPluginView for enabling the filter

### 3. Adding a New Archive Format

To support a new archive format (e.g., .rar native support):

1. **Update FileConstants**:
   ```csharp
   public static class FileConstants
   {
	   public static readonly string[] ComicExtensions = 
		   { ".cbz", ".cb7", ".cbt", ".rar" };
   }
   ```

2. **Update CompressionHelper**:
   ```csharp
   private static string GetCompressionMethod() => OutputFormat switch
   {
	   Cbr => "rar",  // Add RAR support
	   // ...
   };
   ```

3. **Test with existing extraction/compression flows**

---

## Thread Safety Considerations

### Async Operations

- **Base Services**: ArchiveService, MetadataService use `Task.Run()` for blocking I/O
- **Batch Operations**: BatchProcessingManager handles concurrent execution
- **UI Thread**: All Prism events published on UIThread via `ThreadOption.UIThread`

### Thread-Safe Components

1. **PerformanceMonitor**: Uses `volatile float` for CPU usage
2. **Logger**: Event-based (thread-safe via EventAggregator)
3. **Settings**: Singleton with lazy initialization

### Potential Race Conditions

- **Buffer Path Conflicts**: Handled by PathConflictService
  - Renames output files if destination exists
  - Supports auto-append mode (e.g., "archive (1).cbz")

---

## Error Handling Strategy

### Exception Hierarchy

```
Exception
├── FileNotFoundException         (archive/buffer not found)
├── DirectoryNotFoundException    (buffer path invalid)
├── ArgumentException             (invalid parameters)
├── IOException                   (disk I/O failures)
└── OperationCanceledException    (user cancellation)
```

### Recovery Patterns

1. **Pre-Validation**: Check inputs before async operations
2. **Buffer Cleanup**: Always cleanup temporary files in `finally` blocks
3. **User Feedback**: Log all errors; display in UI
4. **Graceful Degradation**: Continue batch operations if single item fails

Example:
```csharp
try
{
	// Perform operation
}
catch (Exception ex)
{
	_logger.Log($"ERROR: {ex.Message}");
	_eventAggregator.GetEvent<BusinessEvent>().Publish(false);
}
finally
{
	SystemTools.CleanDirectory(bufferPath, _logger);
}
```

---

## Logging Architecture

### Logger Design

```csharp
public class Logger
{
	public void Log(string message)
	{
		_eventAggregator.GetEvent<LogEvent>().Publish($"[{DateTime.Now:HH:mm:ss.fff}] {message}");
	}
}
```

### Log Levels (Implicit)

Messages are formatted with context:
- `"Operation started"` → Info
- `"WARNING: No files found"` → Warning
- `"ERROR: File not found"` → Error
- `"Extraction completed in 1234ms"` → Perf metrics

### Log Output

- **UI Display**: HostView.CommonLog TextBlock (real-time)
- **Console**: Optional (can be added via configuration)
- **File**: Not currently implemented (extension point)

---

## Configuration & Settings

### Settings Structure

```csharp
public class Settings : SerializationSettings
{
	// Archive settings
	public ArchiveFormat OutputFormat { get; set; }
	public bool IncludeCover { get; set; }
	public bool IncludeMetadata { get; set; }

	// Performance settings
	public EPerformanceMode PerformanceMode { get; set; }
	public int MaxConcurrentOperations { get; set; }
	public bool UseProgressiveBatching { get; set; }
	public int BatchSize { get; set; }
	public bool EnableThrottling { get; set; }
	public int ThrottleDelayMs { get; set; }

	// Buffer settings
	public string BufferDirectory { get; set; }
	public bool UseFileDirAsBuffer { get; set; }
}
```

### Settings Persistence

- **Location**: `C:\ProgramData\ComicbookArchiveToolbox\Settings\Settings.json`
- **Format**: JSON (via Newtonsoft.Json)
- **Singleton**: Lazy-initialized via static Instance property

---

## Async Operation Sequence Diagrams

### Shrink Operation Sequence

```
UI Thread                           Background Thread
────────────────────────────────────────────────────────

User clicks
"Shrink" button
		│
		▼
ShrinkCommand.Execute()
		│
		├─► Publish(BusinessEvent: true)
		│   [Set IsBusy = true]
		│
		└─► Task.Run(async () =>
				ShrinkPlugin.CompressAsync()
			)
			│
			│ [Switch to thread pool]
			│
			├────────────────────────────►
									   Extract Archive
									   (7z.exe x ...)
										   │
										   │
									   Parse Files
									   [Separate images/metadata]
										   │
										   │
									   BatchProcessing
									   [Resize each image]
										   │
										   │
									   Compress
									   (7z.exe a ...)
										   │
										   │
									   Cleanup
									   [Delete temp files]
										   │
			◄────────────────────────────
		│
		├─► Publish(BusinessEvent: false)
		│   [Set IsBusy = false]
		│
		└─► LogEvent(s) published
			[UI updates CommonLog]
```

### Batch Processing (Concurrent) Sequence

```
items: [file1, file2, file3, file4]
settings.BatchSize = 2
settings.MaxConcurrentOperations = 4

Time   Processor 1         Processor 2         Processor 3         Processor 4
────────────────────────────────────────────────────────────────────────────────
t=0    Process file1       Process file2       ────                ────
t=1    ─────────────       ─────────────       Process file3       Process file4
t=2    ─────────────       ─────────────       ─────────────       ─────────────
	   [throttle delay = 100ms]
t=3    ─────────────       ─────────────       ─────────────       ─────────────
t=4    All done
```

### Merge Operation Sequence (With Order Preservation)

```
inputs: [archive1.cbz, archive2.cbz, archive3.cbz]

ASYNC EXTRACTION (Potentially Out-Of-Order Completion):
┌─ Extract archive1              ┌─ Extract archive2              ┌─ Extract archive3
│  ├─ buffer/archive_0/          │  ├─ buffer/archive_1/          │  ├─ buffer/archive_2/
│  │  ├─ page_001.jpg            │  │  ├─ page_001.jpg            │  │  ├─ page_001.jpg
│  │  └─ ComicInfo.xml           │  │  └─ ComicInfo.xml           │  │  └─ ComicInfo.xml
│  │                             │  │                             │  │
│  └─ Store in                   │  └─ Store in                   │  └─ Store in
│     resultDict[0]              │     resultDict[1]              │     resultDict[2]
│     (no lock contention)       │     (no lock contention)       │     (no lock contention)
│                                │                                │
└────────────────────────────────┴────────────────────────────────┘
								 │
					  [All async tasks complete]
								 │
								 ▼
				   SEQUENTIAL REASSEMBLY (Ordered):
				   Loop: for i = 0 to 2
				   ├─ resultDict[0] ─► allPages += [1, 2]
				   ├─ resultDict[1] ─► allPages += [3, 4]
				   └─ resultDict[2] ─► allPages += [5, 6]
								 │
								 ▼
						 allPages = [
						   page_001.jpg (from archive1),
						   page_002.jpg (from archive1),
						   page_003.jpg (from archive2),
						   page_004.jpg (from archive2),
						   page_005.jpg (from archive3),
						   page_006.jpg (from archive3)
						 ]
								 │
					┌────────────┴────────────┐
					│                         │
					▼                         ▼
			Reindex pages         Compress to output
			[rename sequentially] buffer/outputBuffer/
					│
					├─ page_001.jpg  ───►  page_001.jpg
					├─ page_002.jpg  ───►  page_002.jpg
					├─ page_003.jpg  ───►  page_003.jpg
					├─ page_004.jpg  ───►  page_004.jpg
					├─ page_005.jpg  ───►  page_005.jpg
					└─ page_006.jpg  ───►  page_006.jpg
					│
					│ [If image quality set]
					│ └─ JpgConverter.ResizeImage(quality)
					│
					▼
			  7z.exe a ... output.cbz outputBuffer/
					│
					▼
			Cleanup buffer/

KEY: ConcurrentDictionary<int, ExtractionResult> ensures pages maintain input order
	 regardless of which archive finishes extraction first.
```

### Split Operation (By File Number) Sequence

```
archive.cbz
├─ page_001.jpg
├─ page_002.jpg
├─ page_003.jpg
├─ page_004.jpg
├─ page_005.jpg
├─ page_006.jpg
└─ ComicInfo.xml

User: Split into 3 files
ByFileSplitterPlugin.ExecuteSplitting():
  totalPages = 6
  numberOfFiles = 3
  itemsPerFile = 6 / 3 = 2 pages per file

  ┌─────────────────────────────────────────┐
  │ File 1: archive_01.cbz                  │
  │ ├─ page_001.jpg                         │
  │ ├─ page_002.jpg                         │
  │ └─ ComicInfo.xml                        │
  └─────────────────────────────────────────┘
		   │ [Compress]
		   ▼
  7z.exe a archive_01.cbz ...
		   │
		   ▼
  archive_01.cbz (output)

  ┌─────────────────────────────────────────┐
  │ File 2: archive_02.cbz                  │
  │ ├─ page_003.jpg                         │
  │ ├─ page_004.jpg                         │
  │ └─ ComicInfo.xml                        │
  └─────────────────────────────────────────┘
		   │
		   ▼
  archive_02.cbz (output)

  ┌─────────────────────────────────────────┐
  │ File 3: archive_03.cbz                  │
  │ ├─ page_005.jpg                         │
  │ ├─ page_006.jpg                         │
  │ └─ ComicInfo.xml                        │
  └─────────────────────────────────────────┘
		   │
		   ▼
  archive_03.cbz (output)
```

---

## MergerPlugin Implementation Details

### Thread-Safe Order Preservation Pattern

**Problem**: When merging multiple archives asynchronously, files must maintain their input order in the output. However, concurrent extraction tasks may complete in any order based on file size, I/O performance, and system load. Direct appends to shared collections (with locks) are error-prone and incur lock contention.

**Solution**: The `MergerPlugin` uses a **ConcurrentDictionary-based results buffer** to preserve order:

```csharp
// 1. Create indexed storage for async results
var extractionResults = new ConcurrentDictionary<int, ExtractionResult>();

// 2. Run async extractions (may complete out-of-order)
await _batchProcessingManager.ProcessFilesAsync(files,
    async item => await ExtractSingleArchiveAsync(
        item.File, item.Index, ..., extractionResults, ...),
    cancellationToken);

// 3. Reassemble in original order after all tasks complete
for (int i = 0; i < files.Count; i++)
{
    if (extractionResults.TryGetValue(i, out var result))
    {
        allMetadataFiles.AddRange(result.MetadataFiles);
        allPages.AddRange(result.Pages);
    }
}
```

**Benefits**:
- ✅ **No Lock Contention**: Each async task writes to its own unique dictionary key
- ✅ **Order Guaranteed**: Sequential reassembly ensures output order matches input order
- ✅ **Thread-Safe**: `ConcurrentDictionary<K, V>` handles concurrent adds atomically
- ✅ **Simple**: No complex locking logic or race condition bugs

### ExtractionResult Record

Encapsulates the files extracted from a single archive:

```csharp
private record ExtractionResult(
    List<FileInfo> MetadataFiles,
    List<FileInfo> Pages);
```

Each extraction task populates one `ExtractionResult` and stores it at its archive's original index.

### Key Implementation Notes

1. **Async Extraction**: Uses `BatchProcessingManager.ProcessFilesAsync()` with performance-aware batching to balance throughput and system load.

2. **Image Processing**: Pages are processed sequentially (not by archive) after all extractions complete, ensuring consistent output naming (`page_001.jpg`, `page_002.jpg`, etc.).

3. **Cancellation Support**: Full `CancellationToken` support propagated through all async methods for responsive cancellation.

4. **Performance Logging**: Detailed elapsed time logging at each stage (extraction, processing, compression, cleanup) for performance monitoring and diagnostics.

---

## Future Enhancement Opportunities

### Possible future improvements

1. **Multi-Format Support**
   - Native .rar decompression (currently 7z-based)
   - Streaming large archives without full extraction

2. **Performance Improvements**
   - GPU acceleration for image resizing
   - Incremental batch updates (resume after cancel)
   - Compression algorithm optimization

3. **Observability**
   - File-based logging with rotation

4. **Usabilty**
   - Detect update publication

### Technical Debt

- Replace WPF with modern MAUI for cross-platform support
- Migrate from 7z CLI to managed compression library
- Implement proper cancellation token propagation in all async paths
- Add comprehensive unit test coverage (currently minimal)

---

## Summary

The **ComicBook Archive Toolbox** is a well-architected WPF desktop application with:

✅ **Clean MVVM separation** via Prism framework
✅ **Dependency injection** for testability and maintainability
✅ **Plugin-based architecture** for extensibility
✅ **Async/await patterns** for responsive UI
✅ **Performance monitoring** with intelligent batching
✅ **Event-driven communication** for loose coupling
✅ **Template method pattern** for algorithm strategies
✅ **Resource management** with buffer cleanup
✅ **Thread-safe ordering** in concurrent merge operations

The application handles complex batch operations (shrink, merge, split) while maintaining UI responsiveness through careful use of threading, event aggregation, and performance-aware processing.

