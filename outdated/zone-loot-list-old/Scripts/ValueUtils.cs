using XRL;
using XRL.World;

namespace Plaidman.ZoneLootList.Handlers {
	class ValueUtils {
		private static double? GetValue(GameObject go) {
			if (go.ContainsFreshWater()) {
				return null;
			}

			var value = go.Value;
			var multiple = 1.0;
			
			if (!go.IsCurrency) {
				// subtract 0.21 (3 * 0.07) because the player's reputation with themself is uncommonly high
				multiple = GetTradePerformanceEvent.GetFor(The.Player, The.Player) - 0.21;
			}

			return value * multiple;
		}

		private static double? GetValueRatio(GameObject go) {
			var weight = go.Weight;
			var value = GetValue(go);
			
			if (value == null || value <= 0) {
				// not sellable (includes fresh water containers)
				return null;
			}
			
			if (weight <= 0) {
				return double.PositiveInfinity;
			}

			return (double)(value / weight);
		}

		public static string GetValueLabel(GameObject go) {
			var ratio = GetValueRatio(go);
			
			if (ratio == null) {
				// not sellable: grey
				return "{{K||X|}}";
			}

			if (double.IsPositiveInfinity((double)ratio)) {
				// zero weight object: blue
				return "{{b||0#|}}";
			}

			if (ratio < 1) {
				// super low ratio: red
				return "{{R||$|}}";
			}

			if (ratio < 4) {
				// less than water: yellow
				return "{{W||$|}}";
			}

			if (ratio < 10) {
				// less than copper nugget: 1x green
				return "{{G||$|}}";
			}

			if (ratio <= 50) {
				// less than silver nugget 2x green
				return "{{G||$$|}}";
			}

			// more than silver nugget: 3x green
			return "{{G||$$$|}}";
		}
	}
}