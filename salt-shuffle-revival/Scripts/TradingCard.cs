using System;
using System.Linq;
using System.Text;
using Plaidman.SaltShuffleRevival;
using XRL.Rules;

namespace XRL.World.Parts {
	[Serializable]
	public class SSR_Card : IPart, IModEventHandler<SSR_UninstallEvent> {
		public int SunScore = 0;
		public int MoonScore = 0;
		public int StarScore = 0;
		public int PointValue = 0;
		public string ShortDisplayName = "";
		public bool Foil = false;

        public override void Write(GameObject basis, SerializationWriter writer) {
			writer.WriteNamedFields(this, GetType());
        }

        public override void Read(GameObject basis, SerializationReader reader) {
			if (reader.ModVersions["Plaidman_SaltShuffleRevival"] == new Version("1.0.0")) {
				SunScore = reader.ReadInt32();
				MoonScore = reader.ReadInt32();
				StarScore = reader.ReadInt32();
				PointValue = reader.ReadInt32();
				ShortDisplayName = reader.ReadOptimizedString();
				Foil = false;
				return;
			}
			
            reader.ReadNamedFields(this, GetType());
        }

        public override void Register(GameObject go, IEventRegistrar registrar) {
			registrar.Register(ObjectCreatedEvent.ID);
			registrar.Register(GetIntrinsicValueEvent.ID);
			registrar.Register(The.Game, SSR_UninstallEvent.ID);
			base.Register(go, registrar);
		}

        public override bool HandleEvent(GetIntrinsicValueEvent e) {
			e.Value = Foil ? 3 : 1;
            return base.HandleEvent(e);
        }

		public bool HandleEvent(SSR_UninstallEvent e) {
			ParentObject.Destroy("uninstall", true);
			return base.HandleEvent(e);
		}

        public override bool HandleEvent(ObjectCreatedEvent e) {
			ParentObject.SetIntProperty("NeverStack", 1);
			return base.HandleEvent(e);
		}

		// opening a starter deck
		public static GameObject CreateCard() {
			var card = GameObjectFactory.Factory.CreateObject("SSR_Card");
			var part = card.GetPart<SSR_Card>();
			part.SetCreature(FactionTracker.GetRandomCreature());
			return card;
		}

		// opening a booster and generate a deck for an opponent
		public static GameObject CreateCard(string faction) {
			var card = GameObjectFactory.Factory.CreateObject("SSR_Card");
			var part = card.GetPart<SSR_Card>();
			part.SetCreature(FactionTracker.GetRandomCreature(faction));
			return card;
		}

		// when the opponent is bested in card combat	
		public static GameObject CreateCard(GameObject go) {
			var card = GameObjectFactory.Factory.CreateObject("SSR_Card");
			var part = card.GetPart<SSR_Card>();
			part.SetCreature(new FactionEntity(go, false));
			return card;
		}

		private void SetCreature(FactionEntity fe) {
			fe ??= FactionTracker.GetRandomCreature();

			int foilChance = 10;
			if (fe.IsNamed) foilChance = 5;
			if (Stat.Rnd2.Next(foilChance) == 0) Foil = true;

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
			BoostFoil();

			if (fe.IsBaetyl) {
				SunScore = -5;
				MoonScore = -5;
				StarScore = -5;
			}

			PointValue = SunScore + MoonScore + StarScore;

			SetColors(fe);
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
				BoostStat(stat, boost);

				// if card level is high enough after a loop, never do the second loop
				if (MoonScore + StarScore + SunScore >= LowLevel) return;
				// second loop will always boost a stat by 2
				boost = 2;
			}
		}
		
		private void BoostFoil() {
			if (!Foil) return;

			var first = Stat.Rnd2.Next(3);
			var second = Stat.Rnd2.Next(2);
			if (second == first) {
				second = 2; // 0 => 2/1, 1 => 0/2, 2 => 0/1
			}

			BoostStat(first, 2);
			BoostStat(second, 1);
		}

		private void BoostStat(int stat, int boost) {
			switch (stat) {
				case 0: MoonScore += boost; break;
				case 1: SunScore += boost; break;
				case 2: StarScore += boost; break;
			}
		}

		private void SetColors(FactionEntity fe) {
			ParentObject.Render.ColorString = fe.FgColor;
			ParentObject.Render.DetailColor = fe.DetailColor;
		}

		private void SetDescription(FactionEntity fe) {
			var builder = new StringBuilder();

			if (Foil) {
				builder.Append("A reflective trading card with an animated illustration of =a==name= plus various cryptic statistics. The card shimmers when viewed at different angles.\n\n");
			} else {
				builder.Append("A trading card with a stylized illustration of =a==name= plus various cryptic statistics.\n\n");
			}

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

			builder.Append(" {{K|(Lv =lv==foil=)}}");
			builder.StartReplace()
				.AddReplacer("lv", PointValue.ToString())
				.AddReplacer("foil", Foil ? "\xf7" : "")
				.Execute();
			ParentObject.DisplayName = builder.ToString();
		}
	}
}