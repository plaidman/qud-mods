using Plaidman.SaltShuffleRevival;
using XRL.UI;

namespace XRL.World.Conversations {
	[HasConversationDelegate]
	class SSR_Conversations {
		[ConversationDelegate(Speaker = true, SpeakerKey = "SSR_CanPlayCards")]
		public static bool SSR_CanPlayCards(DelegateContext context) {
			var with = context.Target;
			var isHostile = with.Brain?.IsHostileTowards(The.Player) ?? false;

			if (isHostile) return false;
			if (DeckUtils.HasCards(with)) return true;
			return DeckUtils.CanPlayCards(with);
		}

		[ConversationDelegate(Speaker = true, SpeakerKey = "SSR_StartGame")]
		public static void SSR_StartGame(DelegateContext context) {
			if (!DeckUtils.HasCards(context.Target)) {
				DeckUtils.GenerateDeckFor(context.Target);
			}

			GameBoard.NewGameWith(context.Target);
		}
	}
}