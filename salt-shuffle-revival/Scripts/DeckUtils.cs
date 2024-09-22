using System.Collections.Generic;
using System.Linq;
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

		public static bool PlayerHasTenCards() {
			var count = 0;
			var allItems = The.Player.GetInventory();

			foreach (GameObject item in allItems) {
				if (item.HasPart<SSR_Card>()) {
					count++;
					if (count >= 10) return true;
				}
			}

			return false;
		}

		public static List<SSR_Card> PlayerCardList() {
			return The.Player.GetInventory().Aggregate(
				new List<SSR_Card>(),
				(list, item) => {
					if (item.TryGetPart(out SSR_Card part)) {
						list.Add(part);
					}

					return list;
				}
			);
		}

		public static void GenerateDeckFor(GameObject creature) {
			if (creature.HasPart<SSR_CardPouch>()) {
				return;
			}

			var factions = FactionTracker.GetCreatureFactions(creature);
			if (factions.Count == 0) return;

			var part = creature.AddPart<SSR_CardPouch>();
			part.Cards = new(12);
			for(int i = 0; i < 12; i++) {
				string faction = factions.GetRandomElementCosmetic();
				var card = SSR_Card.CreateCard(faction);
				part.Cards.Add(card);
			}
		}
	}
}