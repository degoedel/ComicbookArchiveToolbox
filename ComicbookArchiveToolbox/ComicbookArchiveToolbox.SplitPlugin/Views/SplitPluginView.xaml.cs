﻿using Prism.Ioc;
using Prism.Regions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace CatPlugin.Split.Views
{
  /// <summary>
  /// Interaction logic for SplitPluginView.xaml
  /// </summary>
  public partial class SplitPluginView : UserControl
  {
    public SplitPluginView()
    {
      InitializeComponent();
    }

		private void NumericOnly(object sender, TextCompositionEventArgs e)
		{
			e.Handled = (!uint.TryParse(e.Text, out uint result));
		}

	}
}
