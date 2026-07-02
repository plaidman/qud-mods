using System;
using System.Linq;
using System.Text;
using Plaidman.SaltShuffleRevival;
using XRL.Rules;

namespace XRL.World.Parts {
	[Serializable]
	public class SSR_Card : IScribedPart, IModEventHandler<SSR_UninstallEvent> {
		public int SunScore = 0;
		public int MoonScore = 0;
		public int StarScore = 0;
		public int PointValue = 0;
		public string ShortDisplayName = "";
		public bool Foil = false;

		public override void Read(GameObject basis, SerializationReader reader) {
			if (reader.ModVersions["Plaidman_SaltShuffleRevival"] == new Version("1.0.0")) {
				SunScore = (int)reader.ReadObject();
				MoonScore = (int)reader.ReadObject();
				StarScore = (int)reader.ReadObject();
				PointValue = (int)reader.ReadObject();
				ShortDisplayName = (string)reader.ReadObject();
				Foil = false;
				return;
			}

			base.Read(basis, reader);
		}

		public override void Register(GameObject go, IEventRegistrar registrar) {
			registrar.Register(ObjectCreatedEvent.ID);
			registrar.Register(GetIntrinsicValueEvent.ID);
			registrar.Register(The.Game, SSR_UninstallEvent.ID);
			base.Register(go, registrar);
		}

		public override bool HandleEvent(GetIntrinsicValueEvent e) {
			e.Value = .75;
			if (Foil) e.Value *= 4;

			return base.HandleEvent(e);
		}

		public bool HandleEvent(SSR_UninstallEvent e) {
			ParentObject.Obliterate("uninstall", true);
			return base.HandleEvent(e);
		}

        public override bool HandleEvent(ObjectCreatedEvent e) {
            _ = ParentObject.BaseID; // forces the ID to be generated at the point of object creation.
            // ParentObject.SetIntProperty("NeverStack", 1); // trying this in the object blueprint
            return base.HandleEvent(e);
        }

        // forces no stacking
        public override bool SameAs(IPart p)
            => false
            ;

        // opening a starter deck
        public static GameObject CreateCard(Random Rnd = null) {
            var card = GameObject.Create("Plaidman_SSR_Card", Context: "Plaidman.SaltShuffleRevival.StarterDeck");
            var part = card.GetPart<SSR_Card>();
            Rnd ??= card.GetSeededRandom($"Plaidman.SaltShuffleRevival.{nameof(CreateCard)}");
            part.SetCreature(FactionTracker.GetRandomCreature(Rnd: Rnd));
            return card;
        }

        // opening a booster and generate a deck for an opponent
        public static GameObject CreateCard(string faction, Random Rnd = null) {
            var card = GameObject.Create("Plaidman_SSR_Card", Context: $"Plaidman.SaltShuffleRevival.Booster::{faction}");
            var part = card.GetPart<SSR_Card>();
            Rnd ??= card.GetSeededRandom($"Plaidman.SaltShuffleRevival.{nameof(CreateCard)}.{faction}");
            part.SetCreature(FactionTracker.GetRandomCreature(faction, Rnd: Rnd));
            return card;
        }

        // when the opponent is bested in card combat
        public static GameObject CreateCard(GameObject go) {
            var card = GameObject.Create("Plaidman_SSR_Card", Context: $"Plaidman.SaltShuffleRevival.Victory::{go.BaseID}");
            var part = card.GetPart<SSR_Card>();
            part.SetCreature(new FactionEntity(go, false));
            return card;
        }

        private void SetCreature(FactionEntity fe) {
            var rnd = ParentObject.GetSeededRandom($"Plaidman.SaltShuffleRevival.{nameof(FactionEntity)}");
            fe ??= FactionTracker.GetRandomCreature(Rnd: rnd);

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

            Foil = rnd.Next(10) == 0;

            NonBlueprintVariance(fe, rnd);
            BoostLowLevel(rnd);
            BoostFoil(rnd);

            if (fe.IsBaetyl) {
                SunScore = -5;
                MoonScore = -5;
                StarScore = -5;
            }

            PointValue = SunScore + MoonScore + StarScore;

            SetColors(fe);
            SetDescription(fe);
            SetDisplayName(fe);

            if (fe.IsLovely)
                ParentObject.RequirePart<Lovely>();
            else
                ParentObject.RemovePart<Lovely>();
        }

        private void NonBlueprintVariance(FactionEntity fe, Random Rnd = null) {
            if (fe.FromBlueprint) return;

            // blueprint entities have some natural variance in their dice rolls
            // FEs that are generated from GOs are set in stone, so we artificially add some variance
            // adjust each stat by 2d3 - 2 => -2,-1,-1,0,0,0,1,1,2
            // if that would reduce the stat to zero or less, just use the old stat

            Rnd ??= Stat.Rnd2;

            var oldMoon = MoonScore;
            MoonScore += Rnd.Next(3) + Rnd.Next(3) - 2;
            if (MoonScore < 1) MoonScore = oldMoon;

            var oldStar = StarScore;
            StarScore += Rnd.Next(3) + Rnd.Next(3) - 2;
            if (StarScore < 1) StarScore = oldStar;

            var oldSun = SunScore;
            SunScore += Rnd.Next(3) + Rnd.Next(3) - 2;
            if (SunScore < 1) SunScore = oldSun;
        }

        // make low level cards more interesting by boosting a couple stats
        // some get 3 or 4 points in a single stat
        // some get 4 then 2, and some get 3 then 2
        private void BoostLowLevel(Random Rnd = null) {
            const int LowLevel = 8;
            if (MoonScore + StarScore + SunScore >= LowLevel) return;

            Rnd ??= Stat.Rnd2;

            var times = Rnd.Next(2) + Rnd.Next(2); // 2d2-2 = distribution 0,1,1,2
            var boost = Rnd.Next(2) + 3; // start with 3 or 4 point boost
            for (int i = 0; i < times; i++) {
                var stat = Rnd.Next(3);
                BoostStat(stat, boost);

                // second loop will always boost a stat by 2
                boost = 2;
            }
        }

        private void BoostFoil(Random Rnd = null) {
            if (!Foil) return;

            Rnd ??= Stat.Rnd2;

            var first = Rnd.Next(3);
            var second = Rnd.Next(2);
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
				.AddReplacer("foil", "")
				.Execute();
			ParentObject.DisplayName = builder.ToString();
		}
	}
}
