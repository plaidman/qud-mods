using ConsoleLib.Console;
using XRL;
using XRL.World;

namespace Plaidman.LightenMyLoad.Menus {
	public class InventoryItem {
		public int Index { get; }
		public string DisplayName { get; }
		public IRenderable Icon { get; }
		public int Weight { get; }
		public double? Value { get; }
		public double? Ratio { get; }
		public bool Known { get; }

		public InventoryItem(int index, GameObject go, bool known) {
			Index = index;
			DisplayName = go.DisplayName;
			Icon = go.Render;
			Weight = go.Weight;
			Value = GetValue(go);
			Ratio = GetValueRatio(Weight, Value);
			Known = known;
		}

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

		public static double? GetValueRatio(int weight, double? value) {
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