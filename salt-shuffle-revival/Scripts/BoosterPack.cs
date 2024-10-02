using System;
using System.Text;
using Plaidman.SaltShuffleRevival;
using XRL.UI;

namespace XRL.World.Parts {
	[Serializable]
	public class SSR_BoosterPack : IScribedPart, IModEventHandler<SSR_UninstallEvent> {
		public string Faction;
		public bool Starter = false;

		public override void Read(GameObject basis, SerializationReader reader) {
			if (reader.ModVersions["Plaidman_SaltShuffleRevival"] == new Version("1.0.0")) {
				Faction = (reader.ReadObject() as Faction)?.Name ?? null;
				Starter = reader.ReadBoolean();
				return;
			}

			base.Read(basis, reader);
		}

		public override void Register(GameObject go, IEventRegistrar registrar) {
			registrar.Register(GetInventoryActionsEvent.ID);
			registrar.Register(InventoryActionEvent.ID);
			registrar.Register(ObjectCreatedEvent.ID);
			registrar.Register(The.Game, SSR_UninstallEvent.ID);

			base.Register(go, registrar);
		}

		public bool HandleEvent(SSR_UninstallEvent e) {
			ParentObject.Count = 1;
			ParentObject.Obliterate("uninstall", true);
			return base.HandleEvent(e);
		}

		public override bool HandleEvent(ObjectCreatedEvent e) {
			if (Starter) {
				Faction = null;
				ParentObject.DisplayName = "Salt Shuffle starter deck";
			} else {
				OverrideFaction(FactionTracker.GetRandomFaction());
			}

			return base.HandleEvent(e);
		}

		public void OverrideFaction(string faction) {
			Faction = faction;
			ParentObject.DisplayName = "pack of Salt Shuffle cards: " + Factions.Get(faction).DisplayName;
		}

		public override bool HandleEvent(GetInventoryActionsEvent e) {
			e.AddAction(
				Name: "Unwrap",
				Key: 'o',
				FireOnActor: false,
				Display: "{{W|o}}pen",
				Command: "InvCommandUnwrap",
				Default: 2
			);

			return base.HandleEvent(e);
		}

		public override bool HandleEvent(InventoryActionEvent e) {
			if (e.Command != "InvCommandUnwrap") return base.HandleEvent(e);

			if (!Starter && !FactionTracker.FactionHasMembers(Faction)) {
				Messages.MessageQueue.AddPlayerMessage("You opened the pack but it was empty. Weird.");
				ParentObject.Destroy("Unwrapped", true);
				return base.HandleEvent(e);
			}

			var tally = new StringBuilder("You unwrap the =pack= and get:\n");

			var qty = Starter ? 12 : 5;
			for (int i = 0; i < qty; i++) {
				var card = Starter
					? SSR_Card.CreateCard()
					: SSR_Card.CreateCard(Faction);

				The.Player.TakeObject(card, NoStack: true);
				tally.Append("- {{|").Append(card.DisplayName).Append("}}\n");
			}

			tally.StartReplace()
				.AddReplacer("pack", ParentObject.DisplayName)
				.Execute();

			Popup.Show(Message: tally.ToString(), LogMessage: false);
			ParentObject.Destroy("Unwrapped", true);

			return base.HandleEvent(e);
		}
	}
}