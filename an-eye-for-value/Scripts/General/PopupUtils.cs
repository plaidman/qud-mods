using System.Collections.Generic;
using Plaidman.AnEyeForValue.Menus;
using XRL.UI;

namespace Plaidman.AnEyeForValue.Utils {
	public class PopupUtils {
		public static readonly Dictionary<SortType, string> SortStrings = new() {
			{ SortType.Value, "[Sort Mode: {{W|value}}]\xff" },
			{ SortType.Weight, "[Sort Mode: {{W|weight}}]" },
		};

		public static readonly Dictionary<PickupType, string> PickupStrings = new() {
			{ PickupType.Single, "[Pickup Mode: {{W|single}}]" },
			{ PickupType.Multi, "[Pickup Mode: {{W|multi}}]\xff" },
		};

		public static readonly Dictionary<SortType, SortType> NextSortType = new() {
			{ SortType.Value, SortType.Weight },
			{ SortType.Weight, SortType.Value },
		};

		public static readonly Dictionary<PickupType, PickupType> NextPickupType = new() {
			{ PickupType.Multi, PickupType.Single },
			{ PickupType.Single, PickupType.Multi },
		};

		public static SortType DefaultSortType() {
			return Options.GetOption(XMLStrings.PreferredSortOption) == "Value"
				? SortType.Value
				: SortType.Weight;
		}

		public static PickupType DefaultPickupType() {
			return Options.GetOption(XMLStrings.PreferredPickupOption) == "Single"
				? PickupType.Single
				: PickupType.Multi;
		}

		public static string GetItemLabel(bool selected, InventoryItem item, SortType sortType) {
			var label = LabelUtils.GetSelectionLabel(selected, item.Type) + " ";

			switch (sortType) {
				case SortType.Value:
					label += LabelUtils.GetValueLabel(item) + " ";
					break;

				case SortType.Weight:
					label += LabelUtils.GetWeightLabel(item) + " ";
					break;
			}

			return label + item.DisplayName;
		}
	}
}