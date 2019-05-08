using ComicbookArchiveToolbox.CommonTools.Events;
using Prism.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
