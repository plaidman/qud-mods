using System;
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
		public List<string> Factions;
		public bool IsBaetyl;
		public bool IsNamed;
		public int Tier;
		public bool IsHero;

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
		public string Desc;
		
		public FactionEntity(string blueprint) {
			Blueprint = blueprint;
			Name = GameObjectFactory.Factory.GetBlueprint(blueprint).CachedDisplayNameStripped;
		}
		
		public FactionEntity(GameObject go, bool fromBlueprint) {
			Blueprint = null;

			Name = go.DisplayNameOnlyDirectAndStripped;
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
			IsHero = go.GetTag("Role", "None") == "Hero";

			try {
				Desc = ColorUtility.StripFormatting(go.GetPart<Description>().GetShortDescription(true, true));
			} catch (Exception) {
				// traipsing mortar was having issues getting description in game init, so we just default to the non-minevented short description
				Desc = ColorUtility.StripFormatting(go.GetPart<Description>()._Short);
			}
		}

		public FactionEntity GetCreature() {
			if (Blueprint != null) {
				// create a new FE based on a GO so we can take advantage of BP dice rolls for stats
				return new(GameObjectFactory.Factory.CreateSampleObject(Blueprint), true);
			}
			
			return this;
		}
	}

	[HasGameBasedStaticCache]
	class FactionUtils {
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

		public static void AddFactionMember(GameObject go) {
			// TODO don't include the same tier/name object more than once
			if (go.GetBlueprint().IsBaseBlueprint()) {
				return;
			}
			
			var entity = new FactionEntity(go, false);
			foreach (var faction in entity.Factions) {
				GetFactionMembers(faction).Add(entity);
			}
		}

		public static string GetRandomFaction() {
			return FactionMemberCache
				.Where(kvp => kvp.Value.Count > 0)
				.Select(kvp => kvp.Key)
				.GetRandomElementCosmetic();
		}
		
		public static FactionEntity GetRandomCreature(string faction = null) {
			faction ??= GetRandomFaction();
			return GetFactionMembers(faction).GetRandomElementCosmetic().GetCreature();
		}

		// used when creating a deck for a creature
		public static List<string> GetCreatureFactions(GameObject go, bool onlyPopulated) {
			if (go.Brain == null) return new();

			return go.Brain.Allegiance
				.Where(kvp => {
					var factionMembers = GetFactionMembers(kvp.Key).Count;
					if (onlyPopulated && factionMembers == 0) return false;
					return Brain.GetAllegianceLevel(kvp.Value) == Brain.AllegianceLevel.Member;
				})
				.Select(kvp => kvp.Key)
				.ToList();
		}
	}
}