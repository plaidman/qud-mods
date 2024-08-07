namespace Plaidman.AnEyeForValue.Menus {
	public enum ActionType { TurnOn, TurnOff, Sort, Travel };
	
	public class ZonePopupAction {
		public int Index { get; }
		public ActionType Action { get; }

		public ZonePopupAction(int index, ActionType action) {
			Index = index;
			Action = action;
		}
	}
}