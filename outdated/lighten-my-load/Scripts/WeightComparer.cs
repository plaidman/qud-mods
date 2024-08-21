using System.Collections.Generic;

namespace Plaidman.LightenMyLoad.Menus {
	class WeightComparer : IComparer<InventoryItem> {
		public int Compare(InventoryItem x, InventoryItem y) {
			return y.Weight - x.Weight;
		}
	}
}