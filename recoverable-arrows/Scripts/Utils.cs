using XRL.UI;

namespace Plaidman.RecoverableArrows.Utils {
	class XMLStrings {
		public static readonly string UninstallCommand = "Plaidman_RecoverableArrows_Command_Uninstall";

		public static readonly string VerboseOption = "Plaidman_RecoverableArrows_Option_Verbose";
	}
	
	class MessageLogger {
		public static void VerboseMessage(string message) {
			if (Options.GetOption(XMLStrings.VerboseOption) == "Yes") {
				XRL.Messages.MessageQueue.AddPlayerMessage(message);
			}
		}
	}
}