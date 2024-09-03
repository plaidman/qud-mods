using System;
using System.Linq;
using System.Text;
using Plaidman.SaltShuffleRevival;
using Qud.API;

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

			float sunScore = 2;  // 38
			float moonScore = 2; // 36
			float starScore = 2; // 36

			int xpLevel = Math.Max(5, go.GetStatValue("Level"));
			sunScore += go.GetStatValue("Strength");      // 18
			starScore += go.GetStatValue("Ego");          // 17
			sunScore += go.GetStatValue("Toughness");     // 18
			starScore += go.GetStatValue("Willpower");    // 17
			moonScore += go.GetStatValue("Intelligence"); // 17
			moonScore += go.GetStatValue("Agility");      // 17
			float minScore = new float[]{ sunScore, moonScore, starScore }.Min(); // 36

			sunScore -= minScore * 2 / 3; // 14
			moonScore -= minScore * 2 / 3; // 12
			starScore -= minScore * 2 / 3; // 12
			float total = sunScore + moonScore + starScore; // 38

			SunScore = (int) Math.Round(sunScore * xpLevel / total);
			MoonScore = (int) Math.Round(moonScore * xpLevel / total);
			StarScore = (int) Math.Round(starScore * xpLevel / total);

			int error = xpLevel - (SunScore + MoonScore + StarScore);
			SunScore += error;

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
				builder.AppendColored("G", "Allegiance: " + string.Join(", ", factions) + "\n");
			}

			builder.Append("{{W|Sun:}} ").AppendColored("Y", SunScore.ToString()).Append("\xff\xff\xff")
				.Append("{{C|Moon:}} ").AppendColored("Y", MoonScore.ToString()).Append("\xff\xff\xff")
				.Append("{{M|Star:}} ").AppendColored("Y", StarScore.ToString()).Append("\n");

			Mutations muts = go.GetPart<Mutations>();
			if (muts != null) {
				builder.AppendColored("c", muts.ToString()).Append("\n");
			}

			builder.AppendColored("K", ConsoleLib.Console.ColorUtility.StripFormatting(go.GetPart<Description>().Short));

			ParentObject.GetPart<Description>().Short = builder.ToString();
		}

		private void SetDisplayName(GameObject go) {
			var builder = new StringBuilder(go.DisplayNameStripped).Append(" ")
				.AppendColored("W", SunScore.ToString()).Append("/")
				.AppendColored("C", MoonScore.ToString()).Append("/")
				.AppendColored("M", StarScore.ToString());

			ShortDisplayName = builder.ToString();
			ParentObject.DisplayName = ShortDisplayName + " {{K|(Lv " + PointValue + ")}}";
		}
	}
}