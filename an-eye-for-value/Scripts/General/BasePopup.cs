using System.Collections.Generic;
using System.Linq;
using Plaidman.AnEyeForValue.Utils;

namespace Plaidman.AnEyeForValue.Menus {
	public enum SortType { Weight, Value };

	public class BasePopup {
		public SortType CurrentSortType;
		private Dictionary<SortType, InventoryItem[]> ItemListCache;

		protected void ResetCache() {
			ItemListCache = new() {
				{ SortType.Value, null },
				{ SortType.Weight, null },
			};
		}

		protected InventoryItem[] SortItemsDescending(InventoryItem[] items) {
			var cache = ItemListCache.GetValue(CurrentSortType);

			if (cache == null) {
				var comparer = PopupUtils.Comparers.GetValue(CurrentSortType);
				cache = items.OrderByDescending(item => item, comparer).ToArray();
				ItemListCache.Set(CurrentSortType, cache);
			}

			return cache;
		}

		protected InventoryItem[] SortItems(InventoryItem[] items) {
			var cache = ItemListCache.GetValue(CurrentSortType);

			if (cache == null) {
				var comparer = PopupUtils.Comparers.GetValue(CurrentSortType);
				cache = items.OrderBy(item => item, comparer).ToArray();
				ItemListCache.Set(CurrentSortType, cache);
			}

			return cache;
		}
	}
};
