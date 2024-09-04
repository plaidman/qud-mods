using System;
using System.Linq;
using System.Text;
using Plaidman.SaltShuffleRevival;
using Qud.API;
using XRL.Rules;

namespace XRL.World.Parts {
	[Serializable]
	public class SSR_Card : IPart {
		public int SunScore = 0;
		public int MoonScore = 0;
		public int StarScore = 0;
		public int PointValue = 0;
		public string ShortDisplayName = "";

		public override void Register(GameObject go, IEventRegistrar registrar) {
			registrar.Register(ObjectCreatedEvent.ID);
			base.Register(go, registrar);
		}

		public override bool HandleEvent(ObjectCreatedEvent e) {
			ParentObject.SetIntProperty("NeverStack", 1);
			return base.HandleEvent(e);
		}
		
		public static GameObject CreateCard() {
			var card = GameObjectFactory.Factory.CreateObject("Plaidman_SSR_Card");
			var part = card.GetPart<SSR_Card>();
			part.SetCreature(EncountersAPI.GetASampleCreature());
			return card;
		}

		public static GameObject CreateCard(string faction) {
			var card = GameObjectFactory.Factory.CreateObject("Plaidman_SSR_Card");
			var part = card.GetPart<SSR_Card>();
			part.SetCreature(FactionUtils.GetRandomSampleCreatureFromFaction(faction));
			return card;
		}
		
		public static GameObject CreateCard(GameObject go) {
			var card = GameObjectFactory.Factory.CreateObject("Plaidman_SSR_Card");
			var part = card.GetPart<SSR_Card>();
			part.SetCreature(go);
			return card;
		}

		private void SetCreature(GameObject go) {
			go ??= EncountersAPI.GetACreature();

			float sunScore = 2;
			float moonScore = 2;
			float starScore = 2;

			int xpLevel = Math.Max(5, go.GetStatValue("Level"));
			sunScore += go.GetStatValue("Strength");
			starScore += go.GetStatValue("Ego");
			sunScore += go.GetStatValue("Toughness");
			starScore += go.GetStatValue("Willpower");
			moonScore += go.GetStatValue("Intelligence");
			moonScore += go.GetStatValue("Agility");
			float minScore = new float[]{ sunScore, moonScore, starScore }.Min();

			sunScore -= minScore * 2 / 3;
			moonScore -= minScore * 2 / 3;
			starScore -= minScore * 2 / 3;
			float total = sunScore + moonScore + starScore;

			SunScore = (int) Math.Round(sunScore * xpLevel / total);
			MoonScore = (int) Math.Round(moonScore * xpLevel / total);
			StarScore = (int) Math.Round(starScore * xpLevel / total);

			int error = xpLevel - (SunScore + MoonScore + StarScore);
			SunScore += error;

			var boost = Stat.Rnd2.Next(2) + 3;
			while (MoonScore + StarScore + SunScore < 9) {
				var stat = Stat.Rnd2.Next(3);
				switch (stat) {
					case 1: MoonScore += boost; break;
					case 2: SunScore += boost; break;
					case 3: StarScore += boost; break;
				}
				if (boost > 1) boost--;
			}

			if (go.Brain != null && go.Brain.GetPrimaryFaction() == "Baetyl") {
				SunScore = -5;
				MoonScore = -5;
				StarScore = -5;
			}

			PointValue = SunScore + MoonScore + StarScore;

			ParentObject.Render.ColorString = ConsoleLib.Console.ColorUtility.StripBackgroundFormatting(go.Render.ColorString);
			ParentObject.Render.DetailColor = go.Render.DetailColor;
			SetDescription(go);
			SetDisplayName(go);
		}

		private void SetDescription(GameObject go) {
			var builder = new StringBuilder("A trading card with a stylized illustration of ")
				.Append(go.a).Append(go.DisplayNameStripped)
				.Append(" plus various cryptic statistics.\n\n");

			var factions = FactionUtils.GetCreatureFactions(go);
			if (factions.Count > 0) {
				builder.Append("{{G|Allegiance: " + string.Join(", ", factions) + "}}\n");
			}

			builder.Append("{{W|Sun:}} {{Y|").Append(SunScore).Append("}}\xff\xff\xff")
				.Append("{{C|Moon:}} {{Y|").Append(MoonScore).Append("}}\xff\xff\xff")
				.Append("{{M|Star:}} {{Y|").Append(StarScore).Append("}}\n");

			Mutations muts = go.GetPart<Mutations>();
			if (muts != null) {
				builder.Append("{{c|").Append(muts.ToString()).Append("}}\n");
			}

			var goDesc = go.GetPart<Description>().Short;
			var strippedDesc = ConsoleLib.Console.ColorUtility.StripFormatting(goDesc);
			builder.Append("{{K|").Append(strippedDesc).Append("}}");

			ParentObject.GetPart<Description>().Short = builder.ToString();
		}

		private void SetDisplayName(GameObject go) {
			var builder = new StringBuilder(go.DisplayNameStripped).Append(" {{W|")
				.Append(SunScore).Append("}}/{{C|").Append(MoonScore).Append("}}/{{M|")
				.Append(StarScore).Append("}}");

			ShortDisplayName = builder.ToString();
			ParentObject.DisplayName = ShortDisplayName + " {{K|(Lv " + PointValue + ")}}";
		}
	}
}