using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ComicbookArchiveToolbox.ViewModels
{
	public class AboutViewModel : BindableBase
	{
		public string AboutContent => "Comicbook Archive Toolbox is a free open source extendable utility\n" +
			"It allows to split and merge digital comic book archives such as cbr or cbz files\n" +
			"and to edit their metadata.\n\n" +
			"Comicbook Archive requires .NET 4.7.2 to work properly.\n\n" +
			"Comicbook Archive Toolbox depends on 7zip (redistributed) for compression/decompression of archives.\n\n" +
			"Comicbook Archive Toolbox is developped by Damien Galban under MIT license.\n\n" +
			"See https://degoedel.github.io/ComicbookArchiveToolbox/ for more details, source code and other dependencies";
	}
}
