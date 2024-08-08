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

			if (!item.Known) {
				// not known: beige, display weight
				return GetWeightLabel(item.Weight);
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
		
		public static string GetWeightLabel(double weight) {
			if (weight > 99) {
				return "{{w||99+|}}";
			}

			if (weight < -99) {
				return "{{w||<-99|}}";
			}

			return "{{w||" + weight.ToString().PadLeft(2, '\xff') + "#|}}";
		}

		public static string GetSelectionLabel(bool selected, bool liquids) {
			if (liquids) {
				return "{{c|[\xf7]}}";
			}

			if (selected) {
				return "{{W|[þ]}}";
			}

			return "{{y|[ ]}}";
		}
	}
}