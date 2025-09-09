using Prism.Mvvm;

namespace ComicbookArchiveToolbox.ViewModels
{
	public class AboutViewModel : BindableBase
	{
		public string AboutContent => "Comicbook Archive Toolbox v3.0 is a free open source digital comics utility.\n" +
			"It allows to split, merge, resize and compress digital comic book archives such as cbr or cbz files,\n" +
			"and to edit their metadata.\n\n" +
			"Comicbook Archive Toolbox requires .NET 8.0 to work properly,\n" +
			"and depends on 7zip (redistributed) for compression/decompression of archives.\n\n" +
			"This app is developped by Damien Galban under MIT license.";
	}
}
