namespace Wox
{
    using System.Runtime.Remoting.Contexts;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Input;

    [Synchronization]
    public partial class ResultListBox
    {
        private Point _lastpos;
        private ListBoxItem curItem;

        public ResultListBox()
        {
            InitializeComponent();
        }

        #region MonoBehaviour

        private void OnMouseEnter(object sender, MouseEventArgs e)
        {
            curItem = (ListBoxItem) sender;
            var p = e.GetPosition((IInputElement) sender);
            _lastpos = p;
        }

        #endregion

        #region Private

        private void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0 && e.AddedItems[0] != null) ScrollIntoView(e.AddedItems[0]);
        }

        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            var p = e.GetPosition((IInputElement) sender);
            if (_lastpos != p) ((ListBoxItem) sender).IsSelected = true;
        }

        private void ListBox_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (curItem != null) curItem.IsSelected = true;
        }

        #endregion
    }
}