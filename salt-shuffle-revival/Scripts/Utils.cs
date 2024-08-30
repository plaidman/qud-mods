using System.Collections.Generic;
using System.Linq;
using XRL.World;
using XRL.World.Parts;

namespace Plaidman.SaltShuffleRevival {
	class FactionUtils {
		private static readonly Dictionary<string, List<GameObjectBlueprint>> FactionMemberCache = new();
		
		// used when generating a new booster pack
		public static Faction GetRandomFaction() {
			return Factions.GetList().Where(f => {
				return f.Visible && GameObjectFactory.Factory.AnyFactionMembers(f.Name);
			}).GetRandomElementCosmetic();
		}

		private static List<GameObjectBlueprint> GetFactionMembers(string faction) {
			if (FactionMemberCache.TryGetValue(faction, out List<GameObjectBlueprint> factionMembers)) {
				return factionMembers;
			}

			factionMembers = GameObjectFactory.Factory.GetFactionMembers(faction);
			FactionMemberCache.Add(faction, factionMembers);

			return factionMembers;
		}

		// used when generating new cards for a potential opponent
		// also used in the description of a card
		public static List<string> GetCreatureFactions(GameObject creature) {
			if (creature.Brain == null) {
				return new();
			}

			return creature.Brain.Allegiance
				.Where(faction => {
					return Brain.GetAllegianceLevel(faction.Value) == Brain.AllegianceLevel.Member
						&& GetFactionMembers(faction.Key).Count > 0;
				})
				.Select(faction => faction.Key)
				.ToList();
		}
		
		// used when creating a card in GenerateDeckFor
		public static GameObject GetRandomSampleCreatureFromFaction(string faction) {
			return GetFactionMembers(faction).GetRandomElementCosmetic().createSample();
		}
	}

	class DeckUtils {
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
		
		public static bool CanPlayCards(GameObject creature) {
			if (creature.Brain == null) return false;
			var factions = FactionUtils.GetCreatureFactions(creature);
			if (factions.Count == 0) return false;

			return true;
		}

		public static void GenerateDeckFor(GameObject creature) {
			if (creature.Brain == null) return;
			var factions = FactionUtils.GetCreatureFactions(creature);
			if (factions.Count == 0) return;

			for(int i = 0; i < 12; i++) {
				string faction = factions.GetRandomElementCosmetic();
				GameObject card = GameObjectFactory.Factory.CreateObject("Plaidman_SSR_Card");
				card.GetPart<SSR_Card>().SetFactionCreature(faction);
				creature.TakeObject(card, NoStack: true, Silent: true);
			}
		}
	}
}