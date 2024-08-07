using System.Collections.Generic;
using Plaidman.AnEyeForValue.Menus;
using XRL.UI;

namespace Plaidman.AnEyeForValue.Utils {
	public class PopupUtils {
		private static readonly string PreferredSortOption = "Plaidman_AnEyeForValue_Option_PreferredSort";

		public static readonly Dictionary<SortType, IComparer<InventoryItem>> Comparers = new() {
			{ SortType.Value, new ValueComparer() },
			{ SortType.Weight, new WeightComparer() },
		};

		public static readonly Dictionary<SortType, string> SortStrings = new() {
			{ SortType.Value, "\xffvalue" },
			{ SortType.Weight, "weight" },
		};

		public static readonly Dictionary<SortType, SortType> NextSortType = new() {
			{ SortType.Value, SortType.Weight },
			{ SortType.Weight, SortType.Value },
		};

		public static SortType DefaultSortType() {
			return Options.GetOption(PreferredSortOption) == "Value"
				? SortType.Value
				: SortType.Weight;		
		}

		public static string GetItemLabel(bool selected, InventoryItem item, SortType sortType) {
			var label = LabelUtils.GetSelectionLabel(selected) + " ";
			
			switch (sortType) {
				case SortType.Value:
					label += LabelUtils.GetValueLabel(item) + " ";
					break;

				case SortType.Weight:
					label += LabelUtils.GetWeightLabel(item.Weight) + " ";
					break;
			}

			return label + item.DisplayName;
		}

		public static string GetSortLabel(SortType sortType, string sortKey) {
			var sortString = SortStrings.GetValue(sortType);
			return "{{W|[" + sortKey + "]}} {{y|Sort Mode: " + sortString + "}}";
		}
	}
}