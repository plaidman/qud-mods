using ConsoleLib.Console;
using Plaidman.AnEyeForValue.Utils;
using XRL.World;

namespace Plaidman.AnEyeForValue.Menus {
	public enum ItemType { Takeable, Liquid, Chest, ChestReset, AutoLoot }

	public class InventoryItem {
		public int Index { get; }
		public string DisplayName { get; }
		public IRenderable Icon { get; }
		public double Weight { get; }
		public double? Value { get; }
		public double? Ratio { get; }
		public bool IsKnown { get; }
		public ItemType Type { get; set; }

		public InventoryItem(
			int index,
			GameObject go,
			double valueMult,
			bool isKnown,
			ItemType type
		) {
			Index = index;
			DisplayName = go.DisplayName;
			Icon = go.Render;
			IsKnown = isKnown;

			Type = type;
			switch (type) {
				case ItemType.Chest:
				case ItemType.ChestReset:
					DisplayName += " {{w|(chest)}}";
					Value = ValueUtils.GetValue(go, valueMult);
					Weight = go.Weight;
					break;

				case ItemType.Takeable:
				case ItemType.AutoLoot:
					Value = ValueUtils.GetValue(go, valueMult);
					Weight = go.Weight;
					break;

				case ItemType.Liquid:
					Value = ValueUtils.GetLiquidValue(go, valueMult);
					Weight = ValueUtils.GetLiquidWeight(go);
					break;
			}

			Ratio = ValueUtils.GetValueRatio(Value, Weight);
		}
	}
}