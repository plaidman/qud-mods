using System.Collections.Generic;
using XRL;
using XRL.World;
using XRL.World.Parts;

namespace Plaidman.SaltShuffleRevival {
	[HasModSensitiveStaticCache]
	class DeckUtils {
		[ModSensitiveCacheInit]
		public static void SetTradingCardCategoryIcon() {
			Qud.UI.FilterBarCategoryButton.categoryImageMap["Trading Cards"] = "Items/SSR_Card.png";
		}

		public static bool HasCards(GameObject creature, int number = 1) {
			if (!creature.HasPart("Inventory")) return false;

			var count = 0;
			var allItems = creature.GetPart<Inventory>().GetObjects();
			foreach (GameObject item in allItems) {
				if (item.HasPart<SSR_Card>()) {
					count++;
					if (count >= number) return true;
				}
			}

			return false;
		}

		public static List<SSR_Card> CardList(GameObject creature) {
			var cards = new List<SSR_Card>();
			if (!creature.HasPart("Inventory")) return cards;

			var allItems = creature.GetPart<Inventory>().GetObjects();
			foreach (GameObject item in allItems) {
				if (item.TryGetPart(out SSR_Card part)) {
					cards.Add(part);
				}
			}

			return cards;
		}

		public static void GenerateDeckFor(GameObject creature) {
			if (creature.Brain == null) return;
			var factions = FactionTracker.GetCreatureFactions(creature, true);
			if (factions.Count == 0) return;

			for(int i = 0; i < 12; i++) {
				string faction = factions.GetRandomElementCosmetic();
				var card = SSR_Card.CreateCard(faction);
				creature.TakeObject(card, NoStack: true, Silent: true);
			}
		}
	}
}