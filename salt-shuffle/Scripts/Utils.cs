using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using XRL;
using XRL.Rules;
using XRL.World;
using XRL.World.Parts;

namespace Nalathni.SaltShuffle {
	[HasGameBasedStaticCache]
	class FactionUtils {
		[GameBasedStaticCache]
		public static List<Faction> FactionsWithMembers;
		[GameBasedStaticCache]
		public static List<GameObjectBlueprint> CreaturesWithFactions;
		[GameBasedStaticCache]
		public static Dictionary<string, List<GameObjectBlueprint>> FactionMemberCache;

		[GameBasedCacheInit]
		public static void InitModCache() {
			CreaturesWithFactions = GameObjectFactory.Factory.BlueprintList.Where((bp) => {
				return !bp.Tags.ContainsKey("BaseObject") && bp.HasPartParameter("Brain", "Factions");
			}).ToList();
			
			FactionsWithMembers = Factions.GetList().Where(f => {
				return f.Visible && GameObjectFactory.Factory.AnyFactionMembers(f.Name);
			}).ToList();
			
			FactionMemberCache = new();
		}
		
		public static Faction GetRandomFaction() {
			return FactionsWithMembers.GetRandomElementCosmetic();
		}

		// todo test if this caches between game loads
		public static List<GameObjectBlueprint> GetFactionMembers(string faction) {
			if (FactionMemberCache.TryGetValue(faction, out List<GameObjectBlueprint> factionMembers)) {
				XRL.Messages.MessageQueue.AddPlayerMessage("DEBUG GFMIU cache hit " + faction);
				return factionMembers;
			}

			XRL.Messages.MessageQueue.AddPlayerMessage("DEBUG GFMIU cache miss " + faction);

			factionMembers = CreaturesWithFactions.Where((bp) => {
				var factions = bp.GetPartParameter<string>("Brain", "Factions");
				return factions.Contains(faction + "-100")
					|| factions.Contains(faction + "-50")
					|| factions.Contains(faction + "-25");
			}).ToList();
			FactionMemberCache.Add(faction, factionMembers);

			return factionMembers;
		}

		public static List<string> GetCreatureFactions(GameObject creature) {
			if (creature.Brain == null) {
				return new();
			}

			return creature.Brain.Allegiance
				.Where(faction => Brain.GetAllegianceLevel(faction.Value) == Brain.AllegianceLevel.Member)
				.Select(faction => faction.Key)
				.ToList();
		}
		
		public static GameObject GetRandomCreatureFromFaction(string faction) {
			return GetFactionMembers(faction).GetRandomElementCosmetic().createSample();
		}
	}

	class DeckUtils {
		public static bool HasCards(GameObject creature, int number = 1) {
            if (!creature.HasPart("Inventory")) return false;

			var count = 0;
            var allItems = creature.GetPart<Inventory>().GetObjects();
            foreach (GameObject item in allItems) {
				if (item.HasPart<NalathniTradingCard>()) {
					count++;
					if (count >= number) return true;
				}
            }
			
			return false;
		}
		
		public static List<NalathniTradingCard> CardList(GameObject creature) {
			var cards = new List<NalathniTradingCard>();
            if (!creature.HasPart("Inventory")) return cards;

            var allItems = creature.GetPart<Inventory>().GetObjects();
            foreach (GameObject item in allItems) {
				if (item.TryGetPart(out NalathniTradingCard part)) {
					cards.Add(part);
				}
            }
			
			return cards;
		}

		public static void GenerateDeckFor(GameObject creature) {
			if (creature.Brain == null) return;
			var factions = FactionUtils.GetCreatureFactions(creature);
			if (factions.Count == 0) return;

			for(int i = 0; i < 12; i++) {
				string faction = factions.GetRandomElementCosmetic();
				GameObject card = GameObjectFactory.Factory.CreateObject("NalathniCard");
				card.GetPart<NalathniTradingCard>().SetFactionCreature(faction);
				creature.TakeObject(card, NoStack: true, Silent: true);
			}
		}
	}
}