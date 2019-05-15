using ComicbookArchiveToolbox.CommonTools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CatPlugin.Split.Services
{
	internal interface ISplitter
	{
		void Split(string filePath, ArchiveTemplate archiveTemplate);
	}
}
