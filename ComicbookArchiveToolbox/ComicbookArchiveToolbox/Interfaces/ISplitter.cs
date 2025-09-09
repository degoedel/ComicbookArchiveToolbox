using ComicbookArchiveToolbox.CommonTools;

namespace ComicbookArchiveToolbox.Module.Split.Services
{
	internal interface ISplitter
	{
		void Split(string filePath, ArchiveTemplate archiveTemplate);
	}
}
