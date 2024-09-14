using System.Collections.Generic;
using System.Linq;
using ConsoleLib.Console;
using XRL;
using XRL.World;
using XRL.World.Parts;

namespace Plaidman.SaltShuffleRevival {
	public class FactionEntity {
		private readonly string Blueprint;
		public bool FromBlueprint;
		public string Name;
		public string Desc;
		public List<string> Factions;
		public bool IsBaetyl;
		public bool IsNamed;
		public int Tier;

		public int Strength;
		public int Agility;
		public int Toughness;
		public int Intelligence;
		public int Ego;
		public int Willpower;
		public int Level;

		public string a;
		public string DetailColor;
		public string FgColor;
		
		public FactionEntity(string blueprint) {
			Blueprint = blueprint;
			FromBlueprint = true;
		}
		
		public FactionEntity(GameObject go, bool fromBlueprint) {
			Blueprint = null;

			Name = go.DisplayNameStripped;
			Desc = ColorUtility.StripFormatting(go.GetPart<Description>().Short);
			Factions = FactionUtils.GetCreatureFactions(go, false);
			Strength = go.GetStatValue("Strength");
			Agility = go.GetStatValue("Agility");
			Toughness = go.GetStatValue("Toughness");
			Intelligence = go.GetStatValue("Intelligence");
			Ego = go.GetStatValue("Ego");
			Willpower = go.GetStatValue("Willpower");
			Level = go.GetStatValue("Level");
			Tier = go.GetTier();
			IsBaetyl = go.Brain?.GetPrimaryFaction() == "Baetyl";
			a = go.a;
			DetailColor = go.Render.DetailColor;
			FgColor = ColorUtility.StripBackgroundFormatting(go.Render.ColorString);
			FromBlueprint = fromBlueprint;
			IsNamed = go.HasProperName;
		}

		public FactionEntity GetCreature() {
			if (Blueprint != null) {
				// create a new one each time to factor in dice rolls for blueprints
				return new(GameObjectFactory.Factory.CreateSampleObject(Blueprint), true);
			}
			
			return this;
		}
	}

	[HasGameBasedStaticCache]
	class FactionUtils {
		const int MinEntities = 1;
		private static readonly Dictionary<string, List<FactionEntity>> FactionMemberCache = new();

		[GameBasedCacheInit]
		public static void InitFactionMemberCache() {
			FactionMemberCache.Clear();

			var factionList = Factions.GetList().Where(f => {
				return f.Visible && GameObjectFactory.Factory.AnyFactionMembers(f.Name);
			});

			foreach (var faction in factionList) {
				var factionMembers = GameObjectFactory.Factory.GetFactionMembers(faction.Name)
					.Select(bp => new FactionEntity(bp.Name))
					.ToList();
				FactionMemberCache.Add(faction.Name, factionMembers);
			}
		}

		// used when generating a new booster pack
		private static List<FactionEntity> GetFactionMembers(string faction) {
			if (FactionMemberCache.TryGetValue(faction, out List<FactionEntity> factionMembers)) {
				return factionMembers;
			}
			
			factionMembers = new();
			FactionMemberCache.Add(faction, factionMembers);
			return factionMembers;
		}

		public static void AddFactionMembers(string faction, FactionEntity fe) {
			// TODO check to see if there is already an object with the matching name and value
			GetFactionMembers(faction).Add(fe);
		}

		public static string GetRandomFaction() {
			return FactionMemberCache
				.Where(kvp => kvp.Value.Count >= MinEntities)
				.Select(kvp => kvp.Key)
				.GetRandomElementCosmetic();
		}
		
		public static FactionEntity GetRandomCreature(string faction = null) {
			faction ??= GetRandomFaction();
			return GetFactionMembers(faction).GetRandomElementCosmetic().GetCreature();
		}

		// used when creating a deck for a creature
		public static List<string> GetCreatureFactions(GameObject go, bool onlyPopulated) {
			if (go.Brain == null) {
				return new();
			}

			return go.Brain.Allegiance
				.Where(kvp => {
					if (onlyPopulated && GetFactionMembers(kvp.Key).Count < MinEntities) return false;
					return Brain.GetAllegianceLevel(kvp.Value) == Brain.AllegianceLevel.Member;
				})
				.Select(kvp => kvp.Key)
				.ToList();
		}
	}
}