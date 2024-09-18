using Qud.UI;
using XRL.UI;

namespace Plaidman.SaltShuffleRevival {
	class Confirm {
		public static bool ShowNoYes(string message) {
			var result = false;

			Popup.WaitNewPopupMessage(
				message: message,
				buttons: PopupMessage.YesNoButton,
				DefaultSelected: 1,
				callback: delegate (QudMenuItem item) {
					if (item.command == "Yes") result = true;
				}
			);

			return result;
		}
	}
}