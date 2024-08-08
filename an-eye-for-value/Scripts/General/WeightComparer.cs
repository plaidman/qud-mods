using System.Collections.Generic;

namespace Plaidman.AnEyeForValue.Menus {
	public class WeightComparer : IComparer<InventoryItem> {
		public int Compare(InventoryItem x, InventoryItem y) {
			return CompareDouble(y.Weight, x.Weight);
		}

		private int CompareDouble(double x, double y) {
			if (x > y) { return 1; }
			if (x < y) { return -1; }
			return 0;
		}
	}
}