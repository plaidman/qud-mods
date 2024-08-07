using System.Collections.Generic;
using HistoryKit;
using XRL;
using XRL.UI;
using XRL.World;
using XRL.World.Parts;

namespace Plaidman.AnEyeForValue.Utils {
	class FilteredZoneItems {
		public List<GameObject> TakeableItems = null; 
		public List<GameObject> Liquids = null; 
	}

	class ZoneLootUtils {
		private static readonly string TrashOption = "Plaidman_AnEyeForValue_Option_ZoneTrash";
		private static readonly string CorpsesOption = "Plaidman_AnEyeForValue_Option_ZoneCorpses";
		private static readonly string LiquidsOption = "Plaidman_AnEyeForValue_Option_ZoneLiquids";

		public static FilteredZoneItems FilterZoneItems(IEnumerable<GameObject> items) {
			List<GameObject> TakeableItems = new();
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
				
				if (Options.GetOption(LiquidsOption) != "Yes") {
					// skip 
					continue;
				}
				
				if (item.LiquidVolume != null) {
					var b = Liquids.GetValue(item.BaseDisplayName);
					var closest = ClosestToPlayer(item, b);
					Liquids.SetValue(item.BaseDisplayName, closest);
				}
			}
			
			return new(){
				Liquids = new List<GameObject>(Liquids.Values),
				TakeableItems = TakeableItems,
			};
		}
		
		private static bool NotSeen(GameObject go) {
			return !go.Physics.CurrentCell.IsExplored() || go.IsHidden;
		}

		private static bool IsTakeable(GameObject go) {
			var autogetByDefault = go.ShouldAutoget()
				&& !go.HasPart<AEFV_AutoGetBeacon>();
			var isCorpse = go.GetInventoryCategory() == "Corpses"
				&& Options.GetOption(CorpsesOption) != "Yes";
			var isTrash = go.HasPart<Garbage>()
				&& Options.GetOption(TrashOption) != "Yes";

			var armedMine = false;
			if (go.HasPart<Tinkering_Mine>()) {
				armedMine = go.GetPart<Tinkering_Mine>().Armed;
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