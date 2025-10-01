# ComicbookArchiveToolbox

![Main interface](/ComicbookArchiveToolbox/MainPage.PNG)  

**Comicbook Archive Toolbox** is a utility for digital comic book archives such as .cbr or .cbz files.  
It was first develloped to split files too big to be comfortably used in digital readers,  
but rapidly evolved to include several split strategies, a merge tool, and a metadata editor.  
It also allow to resize and recompress the embedded pictures to ease their load in a reader.
ComicRack (ComicData.xml) metadata files are supported for edition.
Calibre metadata can be read and updated on an experimental basis.  

Split strategies are :  
- by number of resulting files ;  
- by max page number in resulting files ;  
- by max size of resulting files.  

![Split interface](/ComicbookArchiveToolbox/Split.PNG)  

**Comicbook Archive Toolbox** depends on a redistributed executable of [7zip](https://www.7-zip.org) for compression and decompression tasks.  
Window styling is made with [Mahapps.metro](https://mahapps.com),  
and general architecture is made with [Prism](https://github.com/PrismLibrary/Prism).  
Drag'n'drop on the merge files list uses Josh Smith code [ListViewDragDropManager](https://www.codeproject.com/script/Articles/ViewDownloads.aspx?aid=17266) .

**Comicbook Archive Toolbox is based on .NET 8.0 for Desktop Apps.** Download runtime [here](https://dotnet.microsoft.com/en-us/download/dotnet/8.0/runtime)  


**Comicbook Archive Toolbox is under MIT license** (do what you want with it)  

**Download portable version** [Comicbook Archive Toolbox v3.0](https://github.com/degoedel/ComicbookArchiveToolbox/releases/download/v3.0/ComicbookArchiveToolbox.v3.0.0.zip)  

**Changelog** 
* *V3.0*
  * Fixes application hangout when dealing with html files in the archives.
  * A major UI overhaul has been made
  * The tools can now handle parallel work and have a batch mode
  * There is now some settings to adapt CPU demand to the computer performances
  * Calibre Metadatas can be read, and updated (experimental)
  * The plugin architecture has been scrapped to favor factorisation
* *V2.0*
  * FIX: The app is adapted to .Net 6 and should fix compatibility issues like crash on startup
* *V1.4*
  * FEATURE: The Merge module allows reordering of files through drag'n'drop
* *V1.3*
  * FEATURE: The Compress module allows to resize the pictures in the archives, by height in pixels or ratio
  * FEATURE: The default height resize is specifiable in the settings
* *V1.2*  
  * FIX: The binaries in the distributed zip would crash on launch in .NET 4.8 was installed  
  * FEATURE: The split module now has the option to add the file index on the cover of generated files. (except for the first splitted file (will be fixed later)  
* *V1.1*  
  * FEATURE: Add a busy notifier during long operations  
  * FEATURE: Add the option to recompress pictures so that the generated files will be easier to load in readers  
  * FEATURE: Add the option to hide the log buffer  
* *V1.0* Initial release  
