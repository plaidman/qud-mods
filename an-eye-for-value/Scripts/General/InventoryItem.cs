using ConsoleLib.Console;
using Plaidman.AnEyeForValue.Utils;
using XRL.World;

namespace Plaidman.AnEyeForValue.Menus {
	public class InventoryItem {
		public int Index { get; }
		public string DisplayName { get; }
		public IRenderable Icon { get; }
		public int Weight { get; }
		public double? Value { get; }
		public double? Ratio { get; }
		public bool Known { get; }
		public bool Liquids { get; }

		public InventoryItem(
			int index,
			GameObject go,
			bool known,
			bool liquids = false
		) {
			Index = index;
			DisplayName = go.DisplayName;
			Icon = go.Render;
			Weight = go.Weight;
			Value = ValueUtils.GetValue(go);
			Ratio = ValueUtils.GetValueRatio(Value, Weight);
			Known = known;
			Liquids = liquids;
		}
	}
}