using ComicbookArchiveToolbox.CommonTools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CatPlugin.Split.Services
{
  internal class Splitter
  {
    Logger _logger;
    internal Splitter(Logger logger)
    {
      _logger = logger;
    }

    internal void Split(string filePath, uint fileNb)
    {
      //Extract file in buffer
      CompressionHelper ch = new CompressionHelper(_logger);
      string pathToBuffer = Settings.Instance.GetBufferDirectory(filePath);
      _logger.Log($"Start extraction of {filePath} into {pathToBuffer} ...");
      ch.DecompressToDirectory(filePath, pathToBuffer);
      _logger.Log($"Extraction done.");
      //Count files in directory except metadata
      //Create one folder per resulting file and copy pictures in it
      // rename the files in the directories
      // compress the resulting file
      // clean the temp directories
    }
  }
}
