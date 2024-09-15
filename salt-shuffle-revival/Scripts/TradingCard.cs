using System;
using System.Linq;
using System.Text;
using Plaidman.SaltShuffleRevival;
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

		// opening a starter deck
		public static GameObject CreateCard() {
			var card = GameObjectFactory.Factory.CreateObject("Plaidman_SSR_Card");
			var part = card.GetPart<SSR_Card>();
			part.SetCreature(FactionUtils.GetRandomCreature());
			return card;
		}

		// opening a booster and generate a deck for an opponent
		public static GameObject CreateCard(string faction) {
			var card = GameObjectFactory.Factory.CreateObject("Plaidman_SSR_Card");
			var part = card.GetPart<SSR_Card>();
			part.SetCreature(FactionUtils.GetRandomCreature(faction));
			return card;
		}

		// when the opponent is bested in card combat	
		public static GameObject CreateCard(GameObject go) {
			var card = GameObjectFactory.Factory.CreateObject("Plaidman_SSR_Card");
			var part = card.GetPart<SSR_Card>();
			part.SetCreature(new FactionEntity(go, false));
			return card;
		}

		private void SetCreature(FactionEntity fe) {
			fe ??= FactionUtils.GetRandomCreature();

			// TODO "hero" or named creature gets more chance to be shiny
			// TODO shiny cards get an extra boost in stats?

			float sunScore = 2;
			float moonScore = 2;
			float starScore = 2;

			int xpLevel = Math.Max(5, fe.Level);
			sunScore += fe.Strength;
			starScore += fe.Ego;
			sunScore += fe.Toughness;
			starScore += fe.Willpower;
			moonScore += fe.Intelligence;
			moonScore += fe.Agility;
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

			if (fe.IsBaetyl) {
				SunScore = -5;
				MoonScore = -5;
				StarScore = -5;
			}

			PointValue = SunScore + MoonScore + StarScore;

			ParentObject.Render.ColorString = fe.FgColor;
			ParentObject.Render.DetailColor = fe.DetailColor;
			SetDescription(fe);
			SetDisplayName(fe);
		}

		// make low level cards more interesting by boosting a couple stats
		// some get 3 or 4 points in a single stat
		// some get 4 then 2, and some get 3 then 2
		private void BoostLowLevel() {
			const int LowLevel = 9;
			if (MoonScore + StarScore + SunScore >= LowLevel) return;

			var times = Stat.Rnd2.Next(2) + Stat.Rnd2.Next(2); // 2d2-2 = distribution 0,1,1,2
			var boost = Stat.Rnd2.Next(2) + 3; // start with 3 or 4 point boost
			for (int i = 0; i < times; i++) {
				var stat = Stat.Rnd2.Next(3);
				switch (stat) {
					case 0: MoonScore += boost; break;
					case 1: SunScore += boost; break;
					case 2: StarScore += boost; break;
				}

				// if card level is high enough after a loop, never do the second loop
				if (MoonScore + StarScore + SunScore >= LowLevel) return;
				// second loop will always boost a stat by 2
				boost = 2;
			}
		}

		private void SetDescription(FactionEntity fe) {
			var builder = new StringBuilder("A trading card with a stylized illustration of =a==name= plus various cryptic statistics.\n\n");

			var factions = fe.Factions;
			if (factions.Count > 0) {
				builder.Append("{{G|Allegiance: =factions=}}\n");
			}

			builder.Append("{{W|Sun:}} {{Y|=sun=}}\xff\xff\xff{{C|Moon:}} {{Y|=moon=}}\xff\xff\xff{{M|Star:}} {{Y|=star=}}\n\n{{K|=desc=}}");

			builder.StartReplace()
				.AddReplacer("a", fe.a)
				.AddReplacer("name", fe.Name)
				.AddReplacer("factions", string.Join(", ", factions))
				.AddReplacer("sun", SunScore.ToString())
				.AddReplacer("moon", MoonScore.ToString())
				.AddReplacer("star", StarScore.ToString())
				.AddReplacer("desc", fe.Desc)
				.Execute();

			ParentObject.GetPart<Description>().Short = builder.ToString();
		}

		private void SetDisplayName(FactionEntity fe) {
			var builder = new StringBuilder("=name= {{W|=sun=}}/{{C|=moon=}}/{{M|=star=}}");
			builder.StartReplace()
				.AddReplacer("name", fe.Name)
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