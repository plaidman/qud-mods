using Plaidman.SaltShuffleRevival;
using XRL.UI;
using XRL.World;
using XRL.World.Parts;

namespace XRL.Wish {
	[HasWishCommand]
	class SSR_Wishes {
		[WishCommand(Command = "ssr")]
		public void HandleWish(string more) {
			var split = more.Split(' ');

			switch (split[0].ToLower()) {
				case "booster":
					if (split.Length == 1 || split[1].Length == 0)
						ParseFaction(FactionTracker.GetRandomFaction());
					else ParseFaction(split[1]);
					break;

				case "starter":
					The.Player.TakeObject(GameObject.Create("Plaidman_SSR_Starter", Context: "Wish"));
					break;

				case "box":
					The.Player.TakeObject(GameObject.Create("Plaidman_SSR_BoosterBox", Context: "Wish"));
					break;

				default:
					Popup.Show("SSR: invalid object type. Use booster, starter, or box");
					break;
			}
		}

		private void ParseFaction(string faction) {
			if (faction.ToLower() == "box") {
				The.Player.TakeObject(GameObject.Create("Plaidman_SSR_BoosterBox", Context: "Wish"));
				return;
			}

			var go = GameObject.Create("Plaidman_SSR_Booster", Context: "Wish");
			go.GetPart<SSR_BoosterPack>().OverrideFaction(FactionTracker.ClosestFaction(faction));
			The.Player.TakeObject(go);
		}
	}
}