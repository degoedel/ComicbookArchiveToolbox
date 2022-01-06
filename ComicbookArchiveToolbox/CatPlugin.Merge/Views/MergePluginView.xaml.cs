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

namespace CatPlugin.Merge.Views
{
  /// <summary>
  /// Interaction logic for MergePluginView.xaml
  /// </summary>
  public partial class MergePluginView : UserControl
  {
    ListViewDragDropManager<string> dragMgr;

    public MergePluginView()
    {
      InitializeComponent();
      this.Loaded += Window1_Loaded;
    }

		private void NumericOnly(object sender, TextCompositionEventArgs e)
		{
			e.Handled = (!uint.TryParse(e.Text, out uint result));
		}

    void Window1_Loaded(object sender, RoutedEventArgs e)
    {

      // This is all that you need to do, in order to use the ListViewDragManager.
      this.dragMgr = new ListViewDragDropManager<string>(this._filesListView);
      //this.dragMgr.ListView = this._filesListView;
      //this.dragMgr.ShowDragAdorner = true;


      // Hook up events on both ListViews to that we can drag-drop
      // items between them.
      this._filesListView.DragEnter += OnListViewDragEnter;
      this._filesListView.Drop += OnListViewDrop;
    }

    #region dragMgr_ProcessDrop

    // Performs custom drop logic for the top ListView.
    void dragMgr_ProcessDrop(object sender, ProcessDropEventArgs<Task> e)
    {
      // This shows how to customize the behavior of a drop.
      // Here we perform a swap, instead of just moving the dropped item.

      int higherIdx = Math.Max(e.OldIndex, e.NewIndex);
      int lowerIdx = Math.Min(e.OldIndex, e.NewIndex);

      if (lowerIdx < 0)
      {
        // The item came from the lower ListView
        // so just insert it.
        e.ItemsSource.Insert(higherIdx, e.DataItem);
      }
      else
      {
        // null values will cause an error when calling Move.
        // It looks like a bug in ObservableCollection to me.
        if (e.ItemsSource[lowerIdx] == null ||
          e.ItemsSource[higherIdx] == null)
          return;

        // The item came from the ListView into which
        // it was dropped, so swap it with the item
        // at the target index.
        e.ItemsSource.Move(lowerIdx, higherIdx);
        e.ItemsSource.Move(higherIdx - 1, lowerIdx);
      }

      // Set this to 'Move' so that the OnListViewDrop knows to 
      // remove the item from the other ListView.
      e.Effects = DragDropEffects.Move;
    }

    #endregion // dragMgr_ProcessDrop

    #region OnListViewDragEnter

    // Handles the DragEnter event for both ListViews.
    void OnListViewDragEnter(object sender, DragEventArgs e)
    {
      e.Effects = DragDropEffects.Move;
    }

    #endregion // OnListViewDragEnter

    #region OnListViewDrop

    // Handles the Drop event for both ListViews.
    void OnListViewDrop(object sender, DragEventArgs e)
    {
      if (e.Effects == DragDropEffects.None)
        return;

      Task task = e.Data.GetData(typeof(Task)) as Task;
      if (sender == this._filesListView)
      {
        if (this.dragMgr.IsDragInProgress)
          return;
      }
    }

    #endregion // OnListViewDrop

  }
}
