using XRL;
using XRL.World;

namespace Plaidman.AnEyeForValue.Utils {
	public class ValueUtils {
		public static double? GetValue(GameObject go) {
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

		public static double? GetValueRatio(GameObject go) {
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
	}
}