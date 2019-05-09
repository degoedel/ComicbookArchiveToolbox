using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ComicbookArchiveToolbox.CommonTools
{
	public class ArchiveTemplate
	{
		public string PathToBuffer { get; set; }
		public string ComicName { get; set; }
		public int IndexSize { get; set; }
		public int PagesPerFile { get; set; }
		public List<FileInfo> Pages { get; set; }
		public List<FileInfo> MetadataFiles { get; set; }
		public string CoverPath { get; set; }
	}
}
