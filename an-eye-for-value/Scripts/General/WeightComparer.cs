using System.Collections.Generic;

namespace Plaidman.AnEyeForValue.Menus {
	public class WeightComparer : IComparer<InventoryItem> {
		public int Compare(InventoryItem x, InventoryItem y) {
			return y.Weight - x.Weight;
		}
	}
}