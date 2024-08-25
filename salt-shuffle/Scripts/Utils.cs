using System.Collections.Generic;
using System.Linq;
using NUnit.Framework.Constraints;
using XRL;
using XRL.World;
using XRL.World.Parts;

namespace Nalathni.SaltShuffle {
	[HasModSensitiveStaticCache]
	class FactionUtils {
		[ModSensitiveStaticCache]
		private static Dictionary<string, List<GameObjectBlueprint>> FactionMemberCache = new();

        public static List<GameObjectBlueprint> GetFactionMembersIncludingUniques(string faction) {
            if (FactionMemberCache.TryGetValue(faction, out List<GameObjectBlueprint> factionMembers)) {
                return factionMembers;
            }

			factionMembers = BlueprintUtils.FactionedBlueprints.Where((bp) => {
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
	}

	[HasModSensitiveStaticCache]
	class BlueprintUtils {
		[ModSensitiveStaticCache]
		public static List<GameObjectBlueprint> FactionedBlueprints;

		[ModSensitiveCacheInit]
		public static void InitFactionedBlueprints() {
			FactionedBlueprints = GameObjectFactory.Factory.BlueprintList.Where((bp) => {
				return !bp.Tags.ContainsKey("BaseObject")
					&& bp.HasPartParameter("Brain", "Factions");
			}).ToList();
		}
	}
	
	class DeckUtils {
        public static void GenerateDeckFor(GameObject creature) {
            if (creature.Brain == null) return;
            var factions = FactionUtils.GetCreatureFactions(creature);
            if (factions.Count == 0) return;

            for(int i = 0; i < 12; i++) {
                string faction = factions.GetRandomElement();
                GameObject card = GameObjectFactory.Factory.CreateObject("NalathniCard");

                card.GetPart<NalathniTradingCard>().SetCreature(
                    FactionUtils.GetFactionMembersIncludingUniques(faction).GetRandomElement().createSample()
                );

                creature.TakeObject(card, true);
             }
        }

	}
}