namespace Plaidman.AnEyeForValue.Menus {
	public enum ActionType { TurnOn, TurnOff, Sort, Travel };
	
	public class ZoneAction {
		public int Index { get; }
		public ActionType Action { get; }

		public ZoneAction(int index, ActionType action) {
			Index = index;
			Action = action;
		}
	}
}