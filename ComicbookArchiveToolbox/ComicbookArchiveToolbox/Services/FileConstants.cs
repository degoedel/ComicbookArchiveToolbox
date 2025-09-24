namespace ComicbookArchiveToolbox.Services
{
    /// <summary>
    /// Constants for file operations and patterns.
    /// </summary>
    public static class FileConstants
    {
        // File names
        public const string ComicInfoXml = "ComicInfo.xml";
        
        // File patterns
        public const string XmlPattern = "*.xml";
        public const string HtmlPattern = "*.html";
        
        // Archive extensions that support direct update
        public static readonly string[] DirectUpdateExtensions = { ".cbz", ".cb7", ".cbt" };
        
        // XML constants
        public const string ComicInfoBaseXml = "<?xml version=\"1.0\"?><ComicInfo xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\"></ComicInfo>";
        
        // Calibre detection patterns
        public const string CalibreTitlePattern = "DC.title";
        public const string CalibreCreatorPattern = "DC.creator";
        public const string CalibrePattern = "calibre";
    }
}