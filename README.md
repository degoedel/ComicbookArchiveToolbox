# ComicbookArchiveToolbox

![Main interface](/ComicbookArchiveToolbox/MainPage.PNG)  

**Comicbook Archive Toolbox** is a utility for digital comic book archives such as .cbr or .cbz files.  
It was first develloped to split files too big to be comfortably used in digital readers,  
but rapidly evolved to include several split strategies, a merge tool, and a metadata editor.  
It also allow to recompress the embedded pictures to ease their load in a reader.  
It's designed to be easily extensible.  
For now only ComicRack (ComicData.xml) metadata files are supported for edition.  

Split strategies are :  
- by number of resulting files ;  
- by max page number in resulting files ;  
- by max size of resulting files.  

![Split interface](/ComicbookArchiveToolbox/Split.PNG)  

**Comicbook Archive Toolbox** depends on a redistributed executable of [7zip](https://www.7-zip.org) for compression and decompression tasks.  
Window styling is made with [Mahapps.metro](https://mahapps.com),  
and general architecture is made with [Prism](https://github.com/PrismLibrary/Prism).  

**Comicbook Archive Toolbox is based on .NET 4.7.2.** Download runtime [here](https://dotnet.microsoft.com/download/dotnet-framework/net472)  


**Comicbook Archive Toolbox is under MIT license** (do what you want with it)  

**Download portable version** [Comicbook Archive Toolbox v1.2](https://github.com/degoedel/ComicbookArchiveToolbox/releases/download/v1.2/ComicbookArchiveToolbox.v1.2.0.zip)  

**Changelog**  
* *V1.2*  
  * FIX: The binaries in the distributed zip would crash on launch in .NET 4.8 was installed  
  * FEATURE: The split module now has the option to add the file index on the cover of generated files. (except for the first splitted file (will be fixed later)  
* *V1.1*  
  * FEATURE: Add a busy notifier during long operations  
  * FEATURE: Add the option to recompress pictures so that the generated files will be easier to load in readers  
  * FEATURE: Add the option to hide the log buffer  
* *V1.0* Initial release  
