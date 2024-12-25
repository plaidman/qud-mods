using ConsoleLib.Console;
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using XRL.UI;

namespace Plaidman.AnEyeForValue.Menus {
	public enum SortType { Weight, Value };
	public enum PickupType { Single, Multi };

	// used for classic UI popups, ref: XRL.UI.Popup
	[UIView("ZoneLootPopup", false, false, false, "ZoneLootNav", null, false, 0, false)]
	// used for modern UI popups, ref: Qud.UI.PopupMessage
	// using "PopupMessage" for the UICanvas parameter is intentional! Quote from the documentation (found on CoQ Discord):
	// UICanvas - The name of the UI Canvas (Unity) GameObject which should be displayed when this view is active
	[UIView("ZoneLootDynamicPopupMessage", false, false, false, "ZoneLootNav", null, false, 0, false, IgnoreForceFullscreen = true)]
	[UIView("ZoneLootPopupMessage", false, false, false, "ZoneLootNav", "PopupMessage", false, 0, false, IgnoreForceFullscreen = true, UICanvasHost = 1)]
	public class BasePopup {
		public SortType CurrentSortType;
		private Dictionary<SortType, List<InventoryItem>> ItemListCache;
		protected Dictionary<SortType, IComparer<InventoryItem>> Comparers;

		private static bool s_UIViewsLoaded = false;
		internal static bool s_OverridePopup = false;

		internal static bool Check_UIViewsLoaded()
        {
			if (s_UIViewsLoaded)
            {
				// Unloading UIViews isn't possible AFAIK, so we don't have to check again after successfully confirming this once
				return true;
			}
			bool nav_zlp = GameManager.Instance.HasViewData("ZoneLootPopup");
			bool nav_dzpm = GameManager.Instance.HasViewData("ZoneLootDynamicPopupMessage");
			bool nav_pzm = GameManager.Instance.HasViewData("ZoneLootPopupMessage");

			// Try to register our custom UIViews if any of them aren't loaded yet
			if (!nav_zlp || !nav_dzpm || !nav_pzm)
            {
				GameManager.Instance.RegisterViews();
				nav_zlp = GameManager.Instance.HasViewData("ZoneLootPopup");
				nav_dzpm = GameManager.Instance.HasViewData("ZoneLootDynamicPopupMessage");
				nav_pzm = GameManager.Instance.HasViewData("ZoneLootPopupMessage");

				// If our custom UIViews still aren't loaded for some reason, return false
				if (!nav_zlp || !nav_dzpm || !nav_pzm)
				{
					UnityEngine.Debug.LogError("ZoneLootList - Failed to register custom UIViews!");
					return false;
                }
			}
			s_UIViewsLoaded = true;
			return true;
		}

		[HarmonyPatch(typeof(GameManager), nameof(GameManager.PushGameView))]
		private class PushGameViewPatch
        {
			static bool Prefix(ref string NewView)
			{
				// Override NewView with our custom UIView (only when called for by ZonePopup/InventoryPopup), otherwise use original
				if (s_OverridePopup)
				{
					s_OverridePopup = false;
					// used for modern UI popups, ref: XRL.UI.Popup.WaitNewPopupMessage(...)
					if (NewView == "PopupMessage")
					{
						NewView = "ZoneLootPopupMessage";
					}
					else if (NewView == "DynamicPopupMessage")
					{
						NewView = "ZoneLootDynamicPopupMessage";
					}
					// used for classic UI popups, ref: XRL.UI.Popup.PickOption(...)
					else if (NewView == "Popup:Choice")
					{
						NewView = "ZoneLootPopup";
					}
                    else
                    {
						UnityEngine.Debug.LogError("ZoneLootList - Unexpected UIView: " + NewView);
					}
				}
				return true; // return control to the original function
			}
		}

		// Required to make our UIView work with classic UI Popups
		// This mimics what the game already does for its own layers/navCategories
		[HarmonyPatch(typeof(GameManager), nameof(GameManager.UpdateInput))]
		private class UpdateInputPatch
        {
			static void Postfix (GameManager __instance)
            {
				//UnityEngine.Debug.LogError("ZoneLootList - UpdateInputPatch1");
				if (__instance.CurrentGameView.StartsWith("ZoneLoot"))
                {
					string navCat = __instance.GetViewData(__instance.CurrentGameView).NavCategory;
					string layer = "ZoneLootLayer";
					//UnityEngine.Debug.LogError("ZoneLootList - UpdateInputPatch2 - " + navCat);
					//UnityEngine.Debug.LogError(string.Join(", ", CommandBindingManager.CommandBindingLayers));
					if (navCat == "ZoneLootNav" && CommandBindingManager.CommandBindingLayers.ContainsKey(layer))
                    {
						var list = CommandBindingManager.CommandBindingLayers[layer];
						//UnityEngine.Debug.LogError("ZoneLootList - UpdateInputPatch3");
						foreach (var entry in list.actions)
						{
							if (ControlManager.isCommandDown(entry.name, true, false, false))
							{
								//UnityEngine.Debug.LogError("ZoneLootList - UpdateInputPatch4");
								Keyboard.PushCommand(entry.name, null);
								//return;
							}
						}
					}
				}
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
