using System;
using Plaidman.SaltShuffleRevival;
using XRL.UI;

namespace XRL.World.Parts {
	[Serializable]
	public class SSR_CardChallenger : IPart {
		public override void Register(GameObject go, IEventRegistrar registrar) {
			registrar.Register(OwnerGetInventoryActionsEvent.ID);
			registrar.Register(InventoryActionEvent.ID);

			base.Register(go, registrar);
		}

		public override bool HandleEvent(OwnerGetInventoryActionsEvent e) {
			if (!DeckUtils.HasCards(The.Player)) {
				return base.HandleEvent(e);
			}

			GameObject target = e.Object;
			if (target.IsPlayer()) {
				return base.HandleEvent(e);
			}

			if (!DeckUtils.HasCards(target)) {
				DeckUtils.GenerateDeckFor(target);
			}

			if (DeckUtils.HasCards(target)) {
				e.AddAction(
					Name: "PlayCards",
					Key: 'P',
					FireOnActor: true,
					Display: "&WP&ylay Salt Shuffle",
					Command: "InvCommandCardGame"
				);
			}

			return base.HandleEvent(e);
		}

		public override bool HandleEvent(InventoryActionEvent e) {
			GameObject with = e.Item;
			Brain brain = with.Brain;

			if (brain != null && brain.IsHostileTowards(The.Player)) {
				Popup.Show(with.The + with.ShortDisplayName + "&y"
					+ with.GetVerb("refuse") + " to play card games with you.");

				return base.HandleEvent(e);
			}

			if (with.IsEngagedInMelee()) {
				Popup.Show(with.The + with.ShortDisplayName + "&y" + with.Is
					+ " engaged in hand-to-hand combat and" + with.Is
					+ " too busy to play card games with you.");

				return base.HandleEvent(e);
			}

			GameBoard.NewGameWith(with);
			return base.HandleEvent(e);
		}
	}
}