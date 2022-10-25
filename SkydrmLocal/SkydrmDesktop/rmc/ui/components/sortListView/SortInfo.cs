using System.Windows.Controls;

namespace SortListView
{
    public class SortInfo
    {
        public GridViewColumnHeader LastSortColumn { get; set; }

        public UIElementAdorner CurrentAdorner { get; set; }
    }
}
