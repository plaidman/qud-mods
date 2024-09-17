using Plaidman.SaltShuffleRevival;
using XRL.UI;
using XRL.World.Parts;

namespace XRL.World.Conversations.Parts {
	enum Reason { CanPlay, NoBrain, HatePlayer, Busy, CardCount, NoFactions }

	class SSR_Conversation : IConversationPart {
		private bool Rematch = false;

		public override bool WantEvent(int id, int propagation) {
			return base.WantEvent(id, propagation)
				|| id == IsElementVisibleEvent.ID
				|| id == ColorTextEvent.ID
				|| id == GetChoiceTagEvent.ID
				|| id == HideElementEvent.ID
				|| id == PrepareTextEvent.ID
				|| id == EnteredElementEvent.ID;
		}

		private Reason CanPlay() {
			if (!DeckUtils.HasCards(The.Player, 10)) return Reason.CardCount;

			if (The.Speaker.Brain == null) return Reason.NoBrain;
			if (The.Speaker.Brain.IsHostileTowards(The.Player)) return Reason.HatePlayer;
			if (FactionTracker.GetCreatureFactions(The.Speaker, true).Count == 0) return Reason.NoFactions;
			if (The.Speaker.IsEngagedInMelee()) return Reason.Busy;

			return Reason.CanPlay;
		}

		private bool SociallyRepugnant() {
			return The.Player.TryGetPart(out Mutations part)
				&& part.HasMutation("SociallyRepugnant");
		}

		public override bool HandleEvent(GetChoiceTagEvent e) {
			if (Rematch || SociallyRepugnant()) {
				e.Tag = "[Salt Shuffle]".WithColor(CanPlay() == Reason.CanPlay ? "g" : "K");
			}

			return true;
		}

		public override bool HandleEvent(ColorTextEvent e) {
			e.Color = CanPlay() == Reason.CanPlay ? "G" : "K";
			return base.HandleEvent(e);
		}

		// never hide the option after being selected
		public override bool HandleEvent(HideElementEvent e) {
			return false;
		}

		public override bool HandleEvent(IsElementVisibleEvent e) {
			return CanPlay() != Reason.NoBrain;
		}

		public override bool HandleEvent(PrepareTextEvent e) {
			if (!Rematch) return base.HandleEvent(e);

			if (SociallyRepugnant()) {
				e.Text.Clear().Append("AGAIN!");
				return base.HandleEvent(e);
			}

			e.Text.Clear().Append("Rematch?");
			return base.HandleEvent(e);
		}

		public override bool HandleEvent(EnteredElementEvent e) {
			switch (CanPlay()) {
				case Reason.Busy:
					Popup.Show(The.Speaker.It + The.Speaker.GetVerb("look") + " too busy to play cards.");
					return base.HandleEvent(e);

				case Reason.CardCount:
					Popup.Show("You need at least 10 cards to play.\n\n{{K|You may have a starter pack in your inventory.\nYou can also find booster packs around the world.}}");
					return base.HandleEvent(e);

				case Reason.NoFactions:
					Popup.Show(The.Speaker.It + The.Speaker.GetVerb("do") + "n't have any cards.");
					return base.HandleEvent(e);

				case Reason.HatePlayer:
					Popup.Show(The.Speaker.It + The.Speaker.GetVerb("glare") + " at you with hatred.");
					return base.HandleEvent(e);
			}

			if (!DeckUtils.HasCards(The.Speaker)) {
				DeckUtils.GenerateDeckFor(The.Speaker);
			}
			GameBoard.NewGameWith(The.Speaker);

			Rematch = true;
			return base.HandleEvent(e);
		}
	}
}