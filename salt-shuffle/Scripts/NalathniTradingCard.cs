using System;
using System.Text;
using Nalathni.SaltShuffle;
using Qud.API;
using XRL.Language;

namespace XRL.World.Parts {
    [Serializable]
    public class NalathniTradingCard : IPart {
        public GameObject Creature;
        public int SunScore = 0;
        public int MoonScore = 0;
        public int StarScore = 0;
        public int PointValue = 0;
        // todo shiny card?
        // todo how to earn new boosters/boxes

        public override void Register(GameObject go, IEventRegistrar registrar) {
            registrar.Register(ObjectCreatedEvent.ID);
            base.Register(go, registrar);
        }

        public override bool HandleEvent(ObjectCreatedEvent e) {
            ParentObject.SetIntProperty("NeverStack", 1);
            return base.HandleEvent(e);
        }

        public GameObject Owner() {
            return ParentObject.InInventory;
        }

        public string NameWhose(bool lowercase = false) {
            string prefix;
            var owner = Owner();

            if (owner == The.Player) {
                if (lowercase) prefix = "your";
                else prefix = "Your";
            } else {
                var ownerPossessive = Grammar.MakePossessive(owner.DisplayNameStripped);

                if (lowercase) prefix = owner.the + ownerPossessive;
                else prefix = owner.The + ownerPossessive;
            }

            return prefix + " " + ParentObject.ShortDisplayName;
        }
        
        public void SetAnyCreature() {
            SetCreature(EncountersAPI.GetASampleCreature());
        }
        
        public void SetFactionCreature(string faction) {
            SetCreature(FactionUtils.GetRandomCreatureFromFaction(faction));
        }

        public void SetCreature(GameObject go) {
            Creature = go ?? EncountersAPI.GetASampleCreature();
            SetDescription();
            SetDisplayName();

            float sunScore = 2;
            float moonScore = 2;
            float starScore = 2;
            int xpLevel = 5;

            if (Creature.Statistics.ContainsKey("Strength"))
                sunScore += Creature.Statistics["Strength"].Value;
            if (Creature.Statistics.ContainsKey("Ego"))
                starScore += Creature.Statistics["Ego"].Value;
            if (Creature.Statistics.ContainsKey("Toughness"))
                sunScore += Creature.Statistics["Toughness"].Value;
            if (Creature.Statistics.ContainsKey("Willpower"))
                starScore += Creature.Statistics["Willpower"].Value;
            if (Creature.Statistics.ContainsKey("Intelligence"))
                moonScore += Creature.Statistics["Intelligence"].Value;
            if (Creature.Statistics.ContainsKey("Agility"))
                moonScore += Creature.Statistics["Agility"].Value;

            if (Creature.Statistics.ContainsKey("Level"))
                xpLevel = Math.Max(5, Creature.Statistics["Level"].Value);
            
            float minScore = Math.Min(Math.Min(sunScore, moonScore), starScore);
            
            sunScore -= minScore * 2 / 3;
            moonScore -= minScore * 2 / 3;
            starScore -= minScore * 2 / 3;
            float total = sunScore + moonScore + starScore;

            SunScore = (int) Math.Round(sunScore * xpLevel / total);
            MoonScore = (int) Math.Round(moonScore * xpLevel / total);
            StarScore = (int) Math.Round(starScore * xpLevel / total);
            
            int error = xpLevel - (SunScore + MoonScore + StarScore);
            SunScore += error;
            
            if (Creature.Brain != null && Creature.Brain.GetPrimaryFaction() == "Baetyl") {
                SunScore = -5;
                MoonScore = -5;
                StarScore = -5;
            }
            
            PointValue = SunScore + MoonScore + StarScore;
            
            ParentObject.Render.ColorString = ConsoleLib.Console.ColorUtility.StripBackgroundFormatting(Creature.Render.ColorString);
			ParentObject.Render.DetailColor = Creature.Render.DetailColor;
		}

        private void SetDescription() {
            StringBuilder builder = new();

            builder.Append("A trading card with a stylized illustration of "
                + Creature.a + Creature.DisplayNameStripped
                + " plus various cryptic statistics.\n\n"
            );

            var factions = FactionUtils.GetCreatureFactions(Creature);
            if (factions.Count > 0) {
                builder.Append("&GAllegiance: " + string.Join(", ", factions) + "\n");
            }

            builder.Append("&WSun: &Y" + SunScore +
                "\xff\xff\xff&CMoon: &Y" + MoonScore +
                "\xff\xff\xff&MStar: &Y" + StarScore + "\n"
            );

            Mutations muts = Creature.GetPart<Mutations>();
            if (muts != null) {
                builder.Append("&c" + muts.ToString());
            }

            builder.Append("\n&K" + ConsoleLib.Console.ColorUtility.StripFormatting(Creature.GetPart<Description>().Short));

            ParentObject.GetPart<Description>().Short = builder.ToString();
        }
        
        private void SetDisplayName() {
            StringBuilder builder = new();

            builder.Append("&Y" + Creature.DisplayNameStripped);
            builder.Append(" &W" + SunScore + "&Y/&C" + MoonScore
                + "&Y/&M" + StarScore + " &K(Lv " + PointValue + ")"
            );
            
            ParentObject.DisplayName = builder.ToString();
        }
    }
}