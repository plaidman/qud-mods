using XRL;
using XRL.World;

namespace Plaidman.AnEyeForValue.Utils {
	public class ValueUtils {
		public static double? GetValue(GameObject go, double multiplier) {
			if (go.ContainsFreshWater()) {
				return null;
			}

			if (go.IsCurrency) {
				return go.Value;
			}

			return go.Value * multiplier;
		}

		public static double GetValueMultiplier() {
			// subtract 0.21 (3 * 0.07) because the player's reputation
			//   with themself grants 3 bonus ego
			return GetTradePerformanceEvent.GetFor(The.Player, The.Player) - 0.21;
		}

		public static double GetLiquidValue(GameObject go, double multiplier) {
			return go.LiquidVolume.GetLiquidExtrinsicValuePerDram() * multiplier;
		}

		public static double GetLiquidWeight(GameObject go) {
			return go.LiquidVolume.GetLiquidWeightPerDram();
		}

		public static double? GetValueRatio(double? value, double weight) {
			if (value == null || value <= 0) {
				// not sellable (includes fresh water containers)
				return null;
			}

			if (weight <= 0) {
				return double.PositiveInfinity;
			}

			return value / weight;
		}
	}
}