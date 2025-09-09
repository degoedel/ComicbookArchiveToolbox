using Prism.Mvvm;

namespace ComicbookArchiveToolbox.ViewModels
{
	public class AboutViewModel : BindableBase
	{
		public string AboutContent => "Comicbook Archive Toolbox v2.0 is a free open source extendable utility\n" +
			"It allows to split and merge digital comic book archives such as cbr or cbz files\n" +
			"and to edit their metadata.\n\n" +
			"Comicbook Archive requires .NET 6.0 to work properly.\n\n" +
			"Comicbook Archive Toolbox depends on 7zip (redistributed) for compression/decompression of archives.\n\n" +
			"Comicbook Archive Toolbox is developped by Damien Galban under MIT license.\n\n" +
			"See https://degoedel.github.io/ComicbookArchiveToolbox/ for more details, source code and other dependencies";
	}
}
