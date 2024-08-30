using System;
using Plaidman.SaltShuffleRevival;
using XRL.UI;

namespace XRL.World.Parts {
	[Serializable]
	public class SSR_BoosterPack : IPart {
		public Faction Faction;
		public bool Starter = false;

		public override void Register(GameObject go, IEventRegistrar registrar) {
			registrar.Register(GetInventoryActionsEvent.ID);
			registrar.Register(InventoryActionEvent.ID);
			registrar.Register(ObjectCreatedEvent.ID);

			base.Register(go, registrar);
		}

		public override bool HandleEvent(ObjectCreatedEvent e) {
			if (Starter) {
				Faction = null;
				ParentObject.DisplayName = "Salt Shuffle starter deck";
			} else {
				Faction = FactionUtils.GetRandomFaction();
				ParentObject.DisplayName = "pack of Salt Shuffle cards: " + Faction.DisplayName;
			}

			return base.HandleEvent(e);
		}

		public override bool HandleEvent(GetInventoryActionsEvent e) {
			e.AddAction(
				Name: "Unwrap",
				Key: 'o',
				FireOnActor: false,
				Display: "&Wo&ypen",
				Command: "InvCommandUnwrap",
				Default: 2
			);

			return base.HandleEvent(e);
		}

		public override bool HandleEvent(InventoryActionEvent e) {
			if (e.Command != "InvCommandUnwrap") return base.HandleEvent(e);

			The.Player.RequirePart<SSR_CardChallenger>();
			var tally = "You unwrap " + ParentObject.the + ParentObject.DisplayName + " and get:\n";

			var qty = Starter ? 12 : 5;
			for (int i = 0; i < qty; i++) {
				var card = GameObjectFactory.Factory.CreateObject("Plaidman_SSR_Card");
				var part = card.GetPart<SSR_Card>();
				
				if (Starter) {
					part.SetAnyCreature();
				} else {
					part.SetFactionCreature(Faction.Name);
				}

				The.Player.TakeObject(card, NoStack: true);
				tally += card.DisplayName + "\n";
			}

			Popup.Show(Message: tally, LogMessage: false);
			ParentObject.Destroy("Unwrapped", true);

			return base.HandleEvent(e);
		}
	}
}