using Plaidman.AnEyeForValue.Menus;

namespace Plaidman.AnEyeForValue.Utils {
	public class LabelUtils {
		public static string GetValueLabel(InventoryItem item) {
			var ratio = item.Ratio;
			
			if (ratio == null) {
				// not sellable: grey
				return "{{K||\xff\xffX|}}";
			}

			if (double.IsPositiveInfinity((double)ratio)) {
				return GetWeightLabel(item.Weight);
			}

			if (!item.Known) {
				// not known: beige, display weight
				return GetWeightLabel(item.Weight);
			}
			
			if (ratio < 1) {
				// super low ratio: red
				return "{{R||\xff\xff$|}}";
			}

			if (ratio < 4) {
				// less than water: yellow
				return "{{W||\xff\xff$|}}";
			}

			if (ratio < 10) {
				// less than copper nugget: 1x green
				return "{{G||\xff\xff$|}}";
			}

			if (ratio <= 50) {
				// less than silver nugget 2x green
				return "{{G||\xff$$|}}";
			}

			// more than silver nugget: 3x green
			return "{{G||$$$|}}";
		}
		
		public static string GetWeightLabel(double weight) {
			if (weight > 99) {
				return "{{w||99+|}}";
			}

			if (weight < -99) {
				return "{{w||-99+|}}";
			}

			return "{{w||" + weight.ToString().PadLeft(2, '\xff') + "#|}}";
		}

		public static string GetSelectionLabel(bool selected) {
			if (selected) {
				return "{{W|[þ]}}";
			}

			return "{{y|[ ]}}";
		}
	}
}