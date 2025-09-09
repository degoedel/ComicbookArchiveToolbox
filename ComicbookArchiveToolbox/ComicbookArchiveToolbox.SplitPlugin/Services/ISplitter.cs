using ComicbookArchiveToolbox.CommonTools;

namespace CatPlugin.Split.Services
{
	internal interface ISplitter
	{
		void Split(string filePath, ArchiveTemplate archiveTemplate);
	}
}
