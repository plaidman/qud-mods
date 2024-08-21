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
			out List<GameObject> takeableItems,
			out List<GameObject> liquidItems,
			out List<GameObject> chestItems
		) {
			takeableItems = new();
			chestItems = new();
			Dictionary<string, GameObject> Liquids = new();

			foreach (var item in items) {
				if (NotSeen(item)) {
					// skip unseen items
					continue;
				}

				if (Options.GetOption(XMLStrings.ChestsOption) == "Yes" && item.HasPart<Inventory>()) {
					var invCount = item.Inventory.GetObjectCountDirect();

					if (invCount > 0 && item.HasIntProperty("Autoexplored")) {
						chestItems.Add(item);
						continue;
					}
				}

				if (IsTakeable(item)) {
					takeableItems.Add(item);
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
			
			liquidItems = new List<GameObject>(Liquids.Values);
			return;
		}

		private static bool NotSeen(GameObject go) {
			return !go.CurrentCell.IsExplored() || go.IsHidden;
		}

		private static bool IsTakeable(GameObject go) {
			var autogetByDefault = go.ShouldAutoget()
				&& !go.HasPart<AEFV_AutoGetBeacon>();
			var isCorpse = go.GetInventoryCategory() == "Corpses"
				|| go.HasTag("DynamicObjectsTable:Corpses");
			var isTrash = go.HasPart<Garbage>();
			var isStone = go.GetBlueprint().DescendsFrom("BaseStone");

			var armedMine = false;
			if (go.TryGetPart(out Tinkering_Mine minePart)) {
				armedMine = minePart.Armed;
			}

			return go.Physics.Takeable
				&& !go.HasPropertyOrTag("NoAutoget")
				&& !go.IsOwned()
				&& !armedMine
				&& !autogetByDefault
				&& !(isStone && Options.GetOption(XMLStrings.StonesOption) != "Yes")
				&& !(isCorpse && Options.GetOption(XMLStrings.CorpsesOption) != "Yes")
				&& !(isTrash && Options.GetOption(XMLStrings.TrashOption) != "Yes");
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