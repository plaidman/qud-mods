using ConsoleLib.Console;
using Plaidman.AnEyeForValue.Utils;
using XRL.World;

namespace Plaidman.AnEyeForValue.Menus {
	public class InventoryItem {
		public int Index { get; }
		public string DisplayName { get; }
		public IRenderable Icon { get; }
		public double Weight { get; }
		public double? Value { get; }
		public double? Ratio { get; }
		public bool IsKnown { get; }
		public bool IsPool { get; }

		public InventoryItem(
			int index,
			GameObject go,
			double valueMult,
			bool isKnown,
			bool isPool
		) {
			Index = index;
			DisplayName = go.DisplayName;
			Icon = go.Render;
			IsKnown = isKnown;

			IsPool = isPool;
			if (isPool) {
				Value = ValueUtils.GetLiquidValue(go, valueMult);
				Weight = ValueUtils.GetLiquidWeight(go);
			} else {
				Value = ValueUtils.GetValue(go, valueMult);
				Weight = go.Weight;
			}

			Ratio = ValueUtils.GetValueRatio(Value, Weight);
		}
	}
}