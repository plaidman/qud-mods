using System;
using Nalathni.SaltShuffle;
using XRL.UI;

namespace XRL.World.Parts {
	[Serializable]
	public class NalathniBoosterPack : IPart {
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
				ParentObject.DisplayName = "Salt Shuffle starter deck";
			} else {
				Faction = Factions.GetRandomFactionWithAtLeastOneMember();
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

			The.Player.RequirePart<NalathniCardChallenger>();
			var tally = "You unwrap " + ParentObject.the + ParentObject.DisplayName + " and get:\n";

			if (Starter) {
				for (int i = 0; i < 12; i++) {
					var card = GameObjectFactory.Factory.CreateObject("NalathniCard");
					The.Player.TakeObject(card, NoStack: true);
					tally += card.DisplayName + "\n";
				}
			} else {
				for (int i = 0; i < 5; i++) {
					var card = GameObjectFactory.Factory.CreateObject("NalathniCard");
					card.GetPart<NalathniTradingCard>().SetCreature(
						FactionUtils.GetFactionMembersIncludingUniques(Faction.Name).GetRandomElement().createSample()
					);
					The.Player.TakeObject(card, NoStack: true);
					tally += card.DisplayName + "\n";
				}
			}

			Popup.Show(Message: tally, LogMessage: false);
			ParentObject.Destroy("Unwrapped", true);

			return base.HandleEvent(e);
		}
	}
}