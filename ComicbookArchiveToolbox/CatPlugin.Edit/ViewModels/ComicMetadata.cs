namespace CatPlugin.Edit.ViewModels
{
	public class ComicMetadata
	{
		public string Key { get; set; }
		public string Value { get; set; }

		public ComicMetadata()
		{

		}

		public ComicMetadata(string comicKey, string comicValue)
		{
			Key = comicKey;
			Value = comicValue;
		}
	}
}
