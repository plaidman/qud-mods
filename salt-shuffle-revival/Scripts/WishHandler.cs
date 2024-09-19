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
					The.Player.TakeObject(GameObjectFactory.Factory.CreateObject("Plaidman_SSR_Starter"));
					break;

				case "box":
					The.Player.TakeObject(GameObjectFactory.Factory.CreateObject("Plaidman_SSR_BoosterBox"));
					break;

				default:
					Popup.Show("SSR: invalid object type. Use booster, starter, or box");
					break;
			}
		}

		private void ParseFaction(string faction) {
			if (faction.ToLower() == "box") {
				The.Player.TakeObject(GameObjectFactory.Factory.CreateObject("Plaidman_SSR_BoosterBox"));
				return;
			}

			var closest = FactionTracker.ClosestFaction(faction);
			var go = GameObjectFactory.Factory.CreateObject("Plaidman_SSR_Booster");
			go.GetPart<SSR_BoosterPack>().OverrideFaction(closest);
			The.Player.TakeObject(go);
		}
	}
}