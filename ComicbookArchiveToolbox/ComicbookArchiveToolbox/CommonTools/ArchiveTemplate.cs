using System.Collections.Generic;
using System.IO;

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
		public string OutputDir { get; set; }
		public uint NumberOfSplittedFiles { get; set; }
		public uint MaxPagesPerSplittedFile { get; set; }
		public long MaxSizePerSplittedFile { get; set; }
		public List<uint> PagesIndexToSplit { get; set; }
		public long ImageCompression { get; set; }
	}
}
