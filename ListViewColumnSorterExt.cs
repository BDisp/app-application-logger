using System;
using System.Collections;
using System.Windows.Forms;

namespace ApplicationLogger {

	/// <summary>
	/// Provides sorted to the list view
	/// </summary>
	public class ListViewColumnSorterExt : IComparer {
		#region Fields

		/// <summary>
		/// Specifies the column to be sorted
		/// </summary>
		private int columnToSort;

		/// <summary>
		/// Specifies the order in which to sort (i.e. 'Ascending').
		/// </summary>
		private SortOrder orderOfSort;

		#endregion

		#region Variables
		/// <summary>
		/// Case insensitive comparer object
		/// </summary>
		private CaseInsensitiveComparer ObjectCompare;

		private ListView listView;

		#endregion

		#region Constructor
		/// <summary>
		/// Class constructor.  Initializes various elements
		/// </summary>
		public ListViewColumnSorterExt(ListView lv) {
			listView = lv;
			listView.ListViewItemSorter = this;
			listView.ColumnClick += new ColumnClickEventHandler(listView_ColumnClick);

			// Initialize the column to '0'
			columnToSort = 0;

			// Initialize the sort order to 'none'
			orderOfSort = SortOrder.None;

			// Initialize the CaseInsensitiveComparer object
			ObjectCompare = new CaseInsensitiveComparer();
		}

		#endregion

		#region EventHandlers
		private void listView_ColumnClick(object sender, ColumnClickEventArgs e) {
			if (sender is ListView lv) {
				// Set the last column sorted image to none.
				lv.Columns[SortColumn].ImageKey = "Empty";

				ReverseSortOrderAndSort(e.Column, lv);

				if (lv.Columns[e.Column].ImageList.Images.Keys.Contains("Ascending")
					&& lv.Columns[e.Column].ImageList.Images.Keys.Contains("Descending")) {
					switch (Order) {
						case SortOrder.Ascending:
							lv.Columns[e.Column].ImageKey = "Ascending";
							break;
						case SortOrder.Descending:
							lv.Columns[e.Column].ImageKey = "Descending";
							break;
						case SortOrder.None:
							lv.Columns[e.Column].ImageKey = string.Empty;
							break;

					}
				}

			}
		}

		#endregion

		#region Methods
		/// <summary>
		/// This method is inherited from the IComparer interface.  It compares the two objects passed using a case insensitive comparison.
		/// </summary>
		/// <param name="x">First object to be compared</param>
		/// <param name="y">Second object to be compared</param>
		/// <returns>The result of the comparison. "0" if equal, negative if 'x' is less than 'y' and positive if 'x' is greater than 'y'</returns>
		public int Compare(object x, object y) {
			int compareResult;
			ListViewItem listviewX, listviewY;

			// Cast the objects to be compared to ListViewItem objects
			listviewX = (ListViewItem)x;
			listviewY = (ListViewItem)y;

			if (decimal.TryParse(listviewX.SubItems[columnToSort].Text, out decimal dx) && decimal.TryParse(listviewY.SubItems[columnToSort].Text, out decimal dy)) {
				//compare the 2 items as doubles
				compareResult = decimal.Compare(dx, dy);
			} else if (DateTime.TryParse(listviewX.SubItems[columnToSort].Text, out DateTime dtx) && DateTime.TryParse(listviewY.SubItems[columnToSort].Text, out DateTime dty)) {
				//compare the 2 items as doubles
				compareResult = DateTime.Compare(dtx, dty);
			} else {
				// Compare the two items
				compareResult = ObjectCompare.Compare(listviewX.SubItems[columnToSort].Text, listviewY.SubItems[columnToSort].Text);
			}
			// Calculate correct return value based on object comparison
			if (orderOfSort == SortOrder.Ascending) {
				// Ascending sort is selected, return normal result of compare operation
				return compareResult;
			} else if (orderOfSort == SortOrder.Descending) {
				// Descending sort is selected, return negative result of compare operation
				return (-compareResult);
			} else {
				// Return '0' to indicate they are equal
				return 0;
			}
		}

		private void ReverseSortOrderAndSort(int column, ListView lv) {
			// Determine if clicked column is already the column that is being sorted.
			if (column == this.SortColumn) {
				// Reverse the current sort direction for this column.
				if (this.Order == SortOrder.Ascending) {
					this.Order = SortOrder.Descending;
				} else {
					this.Order = SortOrder.Ascending;
				}
			} else {
				// Set the column number that is to be sorted; default to ascending.
				this.SortColumn = column;
				this.Order = SortOrder.Ascending;
			}

			// Perform the sort with these new sort options.
			lv.Sort();
		}

		#endregion

		#region Properties
		/// <summary>
		/// Gets or sets the number of the column to which to apply the sorting operation (Defaults to '0').
		/// </summary>
		private int SortColumn {
			set {
				columnToSort = value;
			}
			get {
				return columnToSort;
			}
		}

		/// <summary>
		/// Gets or sets the order of sorting to apply (for example, 'Ascending' or 'Descending').
		/// </summary>
		private SortOrder Order {
			set {
				orderOfSort = value;
			}
			get {
				return orderOfSort;
			}
		}

		#endregion
	}
}
