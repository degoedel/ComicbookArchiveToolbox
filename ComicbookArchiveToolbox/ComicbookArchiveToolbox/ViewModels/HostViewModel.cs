using ComicbookArchiveToolbox.CommonTools;
using ComicbookArchiveToolbox.CommonTools.Interfaces;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace ComicbookArchiveHost.ViewModels
{
  public class HostViewModel : BindableBase
  {

    #region Attributes

    #endregion Attributes

    public CatViewModel DisplayedView { get; set; }

    #region Constructors
    public HostViewModel()
    {

    }
		#endregion Constructors

		public string HostTextContent => "This is the host from vm";

  }
}
