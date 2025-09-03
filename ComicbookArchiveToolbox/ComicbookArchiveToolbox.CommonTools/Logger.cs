using ComicbookArchiveToolbox.CommonTools.Events;
using Prism.Events;

namespace ComicbookArchiveToolbox.CommonTools
{
  public class Logger
  {
    IEventAggregator _eventAggregator;
    public Logger(IEventAggregator eventAggregator)
    {
      _eventAggregator = eventAggregator;
    }

    public void Log(string line)
    {
      _eventAggregator.GetEvent<LogEvent>().Publish(line);
    }
  }
}
