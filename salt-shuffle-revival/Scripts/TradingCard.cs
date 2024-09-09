using System;
using System.Linq;
using System.Text;
using ConsoleLib.Console;
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

			BoostLowLevel();

			if (go.Brain != null && go.Brain.GetPrimaryFaction() == "Baetyl") {
				SunScore = -5;
				MoonScore = -5;
				StarScore = -5;
			}

			PointValue = SunScore + MoonScore + StarScore;

			ParentObject.Render.ColorString = ColorUtility.StripBackgroundFormatting(go.Render.ColorString);
			ParentObject.Render.DetailColor = go.Render.DetailColor;
			SetDescription(go);
			SetDisplayName(go);
		}

		// make low level cards more interesting by boosting a couple stats
		// some get 3 or 4 points in a single stats
		// some get 4 then 2, and some get 3 then 2
		private void BoostLowLevel() {
			const int LowLevel = 8;
			if (MoonScore + StarScore + SunScore >= LowLevel) return;

			var boost = Stat.Rnd2.Next(2) + 3; // start with 3 or 4 point boost
			var times = Stat.Rnd2.Next(2) + 1; // do the loop 1 or 2 times
			for (int i = 0; i < times; i++) {
				var stat = Stat.Rnd2.Next(3);
				switch (stat) {
					case 0: MoonScore += boost; break;
					case 1: SunScore += boost; break;
					case 2: StarScore += boost; break;
				}

				// if total stats is more than ten after a loop, never do the second loop
				if (MoonScore + StarScore + SunScore >= LowLevel) return;
				// second loop will always boost a stat by 2
				boost = 2;
			}
		}

		private void SetDescription(GameObject go) {
			var builder = new StringBuilder("A trading card with a stylized illustration of =a==name= plus various cryptic statistics.\n\n");

			var factions = FactionUtils.GetCreatureFactions(go);
			if (factions.Count > 0) {
				builder.Append("{{G|Allegiance: =factions=}}\n");
			}

			builder.Append("{{W|Sun:}} {{Y|=sun=}}\xff\xff\xff{{C|Moon:}} {{Y|=moon=}}\xff\xff\xff{{M|Star:}} {{Y|=star=}}\n\n{{K|=desc=}}");

			builder.StartReplace()
				.AddReplacer("a", go.a)
				.AddReplacer("name", go.DisplayNameStripped)
				.AddReplacer("factions", string.Join(", ", factions))
				.AddReplacer("sun", SunScore.ToString())
				.AddReplacer("moon", MoonScore.ToString())
				.AddReplacer("star", StarScore.ToString())
				.AddReplacer("desc", ColorUtility.StripFormatting(go.GetPart<Description>().Short))
				.Execute();

			ParentObject.GetPart<Description>().Short = builder.ToString();
		}

		private void SetDisplayName(GameObject go) {
			var builder = new StringBuilder("=name= {{W|=sun=}}/{{C|=moon=}}/{{M|=star=}}");
			builder.StartReplace()
				.AddReplacer("name", go.DisplayNameStripped)
				.AddReplacer("sun", SunScore.ToString())
				.AddReplacer("moon", MoonScore.ToString())
				.AddReplacer("star", StarScore.ToString())
				.Execute();
			ShortDisplayName = builder.ToString();

			builder.Append(" {{K|(Lv =lv=)}}");
			builder.StartReplace()
				.AddReplacer("lv", PointValue.ToString())
				.Execute();
			ParentObject.DisplayName = builder.ToString();
		}
	}
}