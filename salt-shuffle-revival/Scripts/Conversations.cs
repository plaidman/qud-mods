using Plaidman.SaltShuffleRevival;
using XRL.UI;
using XRL.World.Parts;

namespace XRL.World.Conversations.Parts {
	enum Reason { NoVal, CanPlay, HatePlayer, Busy, CardCount, NoFactions }

	class SSR_Conversation : IConversationPart {
		private bool Rematch = false;
		private Reason CachedReason = Reason.NoVal;

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
			if (CachedReason != Reason.NoVal) return CachedReason;

			if (!DeckUtils.PlayerHasTenCards()) return CachedReason = Reason.CardCount;
			if (FactionTracker.GetCreatureFactions(The.Speaker).Count == 0) return CachedReason = Reason.NoFactions;
			if (The.Speaker.Brain.IsHostileTowards(The.Player)) return CachedReason = Reason.HatePlayer;
			if (The.Speaker.IsEngagedInMelee()) return CachedReason = Reason.Busy;

			return CachedReason = Reason.CanPlay;
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
			return CanPlay() != Reason.NoFactions;
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

				case Reason.HatePlayer:
					Popup.Show(The.Speaker.It + The.Speaker.GetVerb("glare") + " at you with hatred.");
					return base.HandleEvent(e);
			}

			if (!The.Speaker.HasPart<SSR_CardPouch>()) {
				DeckUtils.GenerateDeckFor(The.Speaker);
			}
			GameBoard.NewGameWith(The.Speaker);

			Rematch = true;
			return base.HandleEvent(e);
		}
	}
}