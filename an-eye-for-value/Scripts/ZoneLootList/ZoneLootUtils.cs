namespace Plaidman.AnEyeForValue.Utils {
	public enum ActionType { TurnOn, TurnOff, Sort, Travel };
	
	public class PopupAction {
		public int Index { get; }
		public ActionType Action { get; }

		public PopupAction(int index, ActionType action) {
			Index = index;
			Action = action;
		}
	}
}