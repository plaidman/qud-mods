using ConsoleLib.Console;
using Plaidman.AnEyeForValue.Utils;
using XRL.World;

namespace Plaidman.AnEyeForValue.Menus {
	public enum SortType { Weight, Value };
	public enum ActionType { TurnOn, TurnOff, Sort, Travel };
	
	public class PopupAction {
		public int Index { get; }
		public ActionType Action { get; }

		public PopupAction(int index, ActionType action) {
			Index = index;
			Action = action;
		}
	}

	public class InventoryItem {
		public int Index { get; }
		public string DisplayName { get; }
		public IRenderable Icon { get; }
		public int Weight { get; }
		public double? Value { get; }
		public double? Ratio { get; }
		public bool Known { get; }
		public bool Takeable { get; }

		public InventoryItem(
			int index,
			GameObject go,
			bool known,
			bool takeable = true
		) {
			Index = index;
			DisplayName = go.DisplayName;
			Icon = go.Render;
			Weight = go.Weight;
			Value = ValueUtils.GetValue(go);
			Ratio = ValueUtils.GetValueRatio(Value, Weight);
			Known = known;
			Takeable = takeable;
		}
	}
}