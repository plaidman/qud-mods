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
	[UIView("DynamicZonePopupMessage", false, false, false, "ZoneLootNav", null, false, 0, false, IgnoreForceFullscreen = true)]
	[UIView("PopupZoneMessage", false, false, false, "ZoneLootNav", "PopupMessage", false, 0, false, IgnoreForceFullscreen = true, UICanvasHost = 1)]
	[HarmonyPatch]
	public class BasePopup : IWantsTextConsoleInit {
		public SortType CurrentSortType;
		private Dictionary<SortType, List<InventoryItem>> ItemListCache;
		protected Dictionary<SortType, IComparer<InventoryItem>> Comparers;

		internal static bool callRealFunc = false;

		[HarmonyPatch(typeof(GameManager), nameof(GameManager.PushGameView))]
		[HarmonyPrefix]
		static bool PushGV_Prefix(ref GameManager __instance, string NewView, bool bHard)
        {
			if (callRealFunc)
            {
				return true;
            }
			UnityEngine.Debug.LogError("PushGameView - Prefix - NewView: " + NewView);
			if (NewView == "PopupMessage" && bHard)
            {
				callRealFunc = true;
				GameManager.Instance.PushGameView("PopupZoneMessage", bHard);
				return false;
            }
			else if (NewView == "DynamicPopupMessage" && bHard)
            {
				callRealFunc = true;
				GameManager.Instance.PushGameView("DynamicZonePopupMessage", bHard);
				return false;
			}
			else if (NewView == "Popup:Choice" && bHard)
            {
				callRealFunc = true;
				GameManager.Instance.PushGameView("ZoneLootPopup", bHard);
				return false;
			}
			return true;
        }

		[HarmonyPatch(typeof(GameManager), nameof(GameManager.PushGameView))]
		[HarmonyPostfix]
		static void PushGV_Postfix(ref GameManager __instance, string NewView, bool bHard)
        {
			if (callRealFunc)
            {
				callRealFunc = false;
				GameManager.ViewInfo check = GameManager.Instance.GetViewData(NewView);
				UnityEngine.Debug.LogError("PushGameView - NavCategory: " + check.NavCategory);
			}
        }

		// copied from XRL.UI.Popup
		public void Init(TextConsole TextConsole_, ScreenBuffer ScreenBuffer_)
		{
			Popup._TextConsole = TextConsole_;
			Popup._ScreenBuffer = ScreenBuffer_;
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
