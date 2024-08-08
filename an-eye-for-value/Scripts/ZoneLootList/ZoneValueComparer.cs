using System.Collections.Generic;

namespace Plaidman.AnEyeForValue.Menus {
	public class ZoneValueComparer : IComparer<InventoryItem> {
		public int Compare(InventoryItem x, InventoryItem y) {
			var xCat = Category(x);
			var yCat = Category(y);

			if (xCat != yCat) {
				return xCat - yCat;
			}

			return xCat switch
			{
				4 => CompareDouble(x.Ratio ?? double.PositiveInfinity, y.Ratio ?? double.PositiveInfinity),
				5 => CompareDouble(x.Value ?? double.PositiveInfinity, y.Value ?? double.PositiveInfinity),
				3 => CompareDouble(x.Weight, y.Weight),
				2 => CompareDouble(y.Weight, x.Weight),
				_ => 0,
			};
		}

		private int CompareDouble(double x, double y) {
			if (x > y) { return 1; }
			if (x < y) { return -1; }
			return 0;
		}

		private int Category(InventoryItem item) {
			// 5: zero weight sort by value, highest first
			// 4: known items sort by ratio, highest first
			// 3: unsellable (water container) sort by weight, highest first
			// 2: unknown items sort by weight, lowest first
			// 1: unknown liquids, all equal I guess

			if (item.Ratio == null) {
				return 3;
			}

			if (item.Weight <= 0) {
				return 5;
			}

			if (!item.IsKnown) {
				if (item.IsPool) {
					return 1;
				}

				return 2;
			}

			return 4;
		}
	}
}