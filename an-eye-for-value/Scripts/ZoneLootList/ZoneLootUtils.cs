using System.Collections.Generic;
using HistoryKit;
using XRL;
using XRL.UI;
using XRL.World;
using XRL.World.Parts;

namespace Plaidman.AnEyeForValue.Utils {
	class ZoneLootUtils {
		public static void FilterZoneItems(
			IEnumerable<GameObject> items,
			out List<GameObject> TakeableItems,
			out List<GameObject> LiquidItems
		) {
			TakeableItems = new();
			Dictionary<string, GameObject> Liquids = new();

			foreach (var item in items) {
				if (NotSeen(item)) {
					// skip unseen items
					continue;
				}

				if (IsTakeable(item)) {
					TakeableItems.Add(item);
					continue;
				}

				if (Options.GetOption(XMLStrings.LiquidsOption) != "Yes") {
					// skip
					continue;
				}

				// if an item has a LiquidVolume and is not takeable,
				// or if an item has the Pool Tag
				// I'm not sure what will be excluded if I use tag
				if (item.LiquidVolume != null) {
					var b = Liquids.GetValue(item.ShortDisplayNameStripped);
					var closest = ClosestToPlayer(item, b);
					Liquids.SetValue(item.ShortDisplayNameStripped, closest);
				}
			}
			
			LiquidItems = new List<GameObject>(Liquids.Values);
			return;
		}

		private static bool NotSeen(GameObject go) {
			return !go.Physics.CurrentCell.IsExplored() || go.IsHidden;
		}

		private static bool IsTakeable(GameObject go) {
			var autogetByDefault = go.ShouldAutoget()
				&& !go.HasPart<AEFV_AutoGetBeacon>();
			var isCorpse = go.GetInventoryCategory() == "Corpses"
				&& Options.GetOption(XMLStrings.CorpsesOption) != "Yes";
			var isTrash = go.HasPart<Garbage>()
				&& Options.GetOption(XMLStrings.TrashOption) != "Yes";

			var armedMine = false;
			if (go.TryGetPart(out Tinkering_Mine minePart)) {
				armedMine = minePart.Armed;
			}

			return go.Physics.Takeable
				&& !go.HasPropertyOrTag("NoAutoget")
				&& !go.IsOwned()
				&& !armedMine
				&& !autogetByDefault
				&& !isCorpse
				&& !isTrash;
		}

		private static GameObject ClosestToPlayer(GameObject a, GameObject b) {
			if (a == null && b == null) {
				return null;
			}

			if (a == null) {
				return b;
			}

			if (b == null) {
				return a;
			}

			var player = The.Player;
			var distA = player.DistanceTo(a);
			var distB = player.DistanceTo(b);

			return distA > distB ? b : a;
		}
	}
}