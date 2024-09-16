using System;
using System.Collections.Generic;
using ConsoleLib.Console;
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
			Factions = FactionTracker.GetCreatureFactions(go, false);
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
}