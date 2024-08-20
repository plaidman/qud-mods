using Plaidman.AnEyeForValue.Menus;

namespace Plaidman.AnEyeForValue.Utils {
	public class LabelUtils {
		public static string GetValueLabel(InventoryItem item) {
			var ratio = item.Ratio;

			if (ratio == null) {
				// not sellable: grey
				return "{{K||-X-|}}";
			}

			if (double.IsPositiveInfinity((double)ratio)) {
				// zero weight: blue
				return "{{B||$$$|}}";
			}

			if (!item.IsKnown) {
				// not known: beige, display weight
				return GetWeightLabel(item);
			}

			if (ratio < 1) {
				// super low ratio: red
				return "{{R||-$-|}}";
			}

			if (ratio < 4) {
				// less than water: yellow
				return "{{W||=$=|}}";
			}

			if (ratio < 10) {
				// less than copper nugget: 1x green
				return "{{G||\xf0$\xf0|}}";
			}

			if (ratio <= 50) {
				// less than silver nugget 2x green
				return "{{G||$\xf0$|}}";
			}

			// more than silver nugget: 3x green
			return "{{G||$$$|}}";
		}

		public static string GetWeightLabel(InventoryItem item) {
			if (item.Type == ItemType.Liquid) {
				return "{{c||~\xf7~|}}";
			}

			if (item.Weight > 99) {
				return "{{w||99+|}}";
			}

			if (item.Weight < -99) {
				return "{{w||<-99|}}";
			}

			return "{{w||" + item.Weight.ToString().PadLeft(2, '\xff') + "#|}}";
		}

		public static string GetSelectionLabel(bool selected, ItemType type) {
			if (type == ItemType.Liquid) {
				return "{{c|[\xf7]}}";
			}

			if (selected) {
				return "{{W|[þ]}}";
			}

			return "{{y|[ ]}}";
		}
	}
}