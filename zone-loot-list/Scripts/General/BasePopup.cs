using ConsoleLib.Console;
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using XRL.UI;

namespace Plaidman.AnEyeForValue.Menus {
	public enum SortType { Weight, Value };
	public enum PickupType { Single, Multi };

	// used for classic UI popups
	// ref: XRL.UI.Popup
	[UIView("ZoneLootPopup", false, false, false, "ZoneLootNav", null, false, 0, false)]
	// used for modern UI popups
	// ref: Qud.UI.PopupMessage
	// using "PopupMessage" for the UICanvas parameter is intentional! Quote from the documentation (found on CoQ Discord):
	// The name of the UI Canvas (Unity) GameObject which should be displayed when this view is active
	[UIView("DynamicZonePopupMessage", false, false, false, "ZoneLootNav", null, false, 0, false, IgnoreForceFullscreen = true)]
	[UIView("PopupZoneMessage", false, false, false, "ZoneLootNav", "PopupMessage", false, 0, false, IgnoreForceFullscreen = true, UICanvasHost = 1)]
	[HarmonyPatch]
	// Classic UI popups seem to be working fine without implementing Interface IWantsTextConsoleInit (Probably since the original Popup class handles the rendering)
	public class BasePopup {
		public SortType CurrentSortType;
		private Dictionary<SortType, List<InventoryItem>> ItemListCache;
		protected Dictionary<SortType, IComparer<InventoryItem>> Comparers;

		internal static bool s_OverridePopup = false;

		[HarmonyPatch(typeof(GameManager), nameof(GameManager.PushGameView))]
		[HarmonyPrefix]
		static bool PushGV_Prefix(ref string NewView, bool bHard)
        {
			// Override NewView with our custom UIView once (only when called for by ZonePopup/InventoryPopup), otherwise use original
			if (s_OverridePopup)
            {
				s_OverridePopup = false;
				// ref: XRL.UI.Popup.WaitNewPopupMessage(...)
				if (NewView == "PopupMessage" && bHard)
				{
					NewView = "PopupZoneMessage";

				}
				else if (NewView == "DynamicPopupMessage" && bHard)
				{
					NewView = "DynamicZonePopupMessage";
				}
				// ref: XRL.UI.Popup.PickOption(...)
				else if (NewView == "Popup:Choice" && bHard)
				{
					NewView = "ZoneLootPopup";
				}
			}
			return true;
        }

		[HarmonyPatch(typeof(GameManager), nameof(GameManager.PushGameView))]
		[HarmonyPostfix]
		static void PushGV_Postfix(string NewView, bool bHard)
        {
			if (NewView.Contains("Zone") && bHard)
            {
				GameManager.ViewInfo check = GameManager.Instance.GetViewData(NewView);
				UnityEngine.Debug.LogError("PushGameView - NavCategory: " + check.NavCategory);
			}
        }

		protected void ResetCache() {
			ItemListCache = new() {
				{ SortType.Value, null },
				{ SortType.Weight, null },
			};
		}

		protected List<InventoryItem> SortItemsDescending(List<InventoryItem> items) {
			var cache = ItemListCache.GetValue(CurrentSortType);

			if (cache == null) {
				var comparer = Comparers.GetValue(CurrentSortType);
				cache = items.OrderByDescending(item => item, comparer).ToList();
				ItemListCache.Set(CurrentSortType, cache);
			}

			return cache;
		}

		protected List<InventoryItem> SortItems(List<InventoryItem> items) {
			var cache = ItemListCache.GetValue(CurrentSortType);

			if (cache == null) {
				var comparer = Comparers.GetValue(CurrentSortType);
				cache = items.OrderBy(item => item, comparer).ToList();
				ItemListCache.Set(CurrentSortType, cache);
			}

			return cache;
		}
	}
};
