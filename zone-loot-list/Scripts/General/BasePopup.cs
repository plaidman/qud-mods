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
	// using "PopupMessage" for the UICanvas parameter of ZoneLootPopupMessage is intentional! Quote from the documentation (found on CoQ Discord):
	// UICanvas - The name of the UI Canvas (Unity) GameObject which should be displayed when this view is active
	[UIView("ZoneLootDynamicPopupMessage", false, false, false, "ZoneLootNav", null, false, 0, false, IgnoreForceFullscreen = true)]
	[UIView("ZoneLootPopupMessage", false, false, false, "ZoneLootNav", "PopupMessage", false, 0, false, IgnoreForceFullscreen = true, UICanvasHost = 1)]
	public class BasePopup {
		public SortType CurrentSortType;
		private Dictionary<SortType, List<InventoryItem>> ItemListCache;
		protected Dictionary<SortType, IComparer<InventoryItem>> Comparers;

		private static bool s_UIViewsLoaded = false;
		private static bool s_SwitchToFallback = false;
		private static bool s_UseFallback = false;
		internal static bool s_OverridePopup = false;

		private static void SwitchToFallBackCommands() {
			string oldLayer = "ZoneLootLayer";
			string newLayer = "UI";
			if (!s_UseFallback && CommandBindingManager.CommandBindingLayers.ContainsKey(oldLayer) && CommandBindingManager.CommandBindingLayers.ContainsKey(newLayer)) {
				UnityEngine.Debug.LogError("ZoneLootList - Using fallback command settings!");
				s_UseFallback = true;
				// Change our commands' attributes to: Layer="UI" and Auto="DownPass"
				// While certainly suboptimal, this ensures that our commands still work
				// if our custom UIViews fail for some reason.
				foreach (var entry in CommandBindingManager.CommandBindingLayers[oldLayer].actions) {
					CommandBindingManager.CommandsByID[entry.name].Layer = newLayer;
					CommandBindingManager.CommandsByID[entry.name].Auto = "DownPass";
				}
				// To "inform" the game of these changes, we have to reload all binding layers.
				// ref: XRL.UI.CommandBindingManager::LoadCommands() / XRL.UI.CommandBindingManager::LoadCurrentKeymap(...)
				CommandBindingManager.InitializeInputManager(!System.IO.File.Exists(CommandBindingManager.GetCurrentKeymapPath()), false, null);
			}
		}

		internal static bool CheckUIViewsLoaded() {
			if (s_UIViewsLoaded) {
				// Unloading UIViews isn't possible AFAIK, so we don't have to check again after confirming this once
				return s_UIViewsLoaded;
			}
			bool nav_zlp = GameManager.Instance.HasViewData("ZoneLootPopup");
			bool nav_dzpm = GameManager.Instance.HasViewData("ZoneLootDynamicPopupMessage");
			bool nav_pzm = GameManager.Instance.HasViewData("ZoneLootPopupMessage");

			// Try to register our custom UIViews if any of them aren't loaded yet.
			// Should only happen if this mod requires user approval after an update.
			if (!nav_zlp || !nav_dzpm || !nav_pzm) {
				GameManager.Instance.RegisterViews();
				nav_zlp = GameManager.Instance.HasViewData("ZoneLootPopup");
				nav_dzpm = GameManager.Instance.HasViewData("ZoneLootDynamicPopupMessage");
				nav_pzm = GameManager.Instance.HasViewData("ZoneLootPopupMessage");

				// If our custom UIViews still aren't loaded for some reason, switch to fallback command settings
				if (!nav_zlp || !nav_dzpm || !nav_pzm) {
					// Note that this NEVER happened in my testing, this is just here as a backup.
					UnityEngine.Debug.LogError("ZoneLootList - Failed to register custom UIViews!");
					// using UseFallBackCommands() here lead to thread/access errors, so postpone it until next UpdateInput() iteration
					s_SwitchToFallback = true;
				}
			}
			s_UIViewsLoaded = true;
			return s_UIViewsLoaded;
		}

		// Replace GameManager::PushGameView()'s GameView parameter when called by Popup::PickOption().
		// Only this one patch here is required to make our UIViews work with modern UI Popups since
		// those don't rely on GameManager::UpdateInput() to receive commands (like classic ones do).
		// This patch could theoretically be avoided by writing our own implementations of Popup::PickOption(),
		// Popup::WaitNewPopupMessage() and Popup::NewPopupMessageAsync(). But that'd be a lot of code to write
		// and then also keep up-to-date whenever the game's original functions change.
		[HarmonyPatch(typeof(GameManager), nameof(GameManager.PushGameView))]
		private sealed class PushGameViewPatch {
			static bool Prefix(ref string NewView) {
				// Override NewView with our custom UIView (only when called for by ZonePopup/InventoryPopup), otherwise use original
				if (s_OverridePopup && !s_UseFallback) {
					s_OverridePopup = false;
					// used for modern UI popups, ref: XRL.UI.Popup.WaitNewPopupMessage(...)
					if (NewView == "PopupMessage") {
						NewView = "ZoneLootPopupMessage";
					}
					else if (NewView == "DynamicPopupMessage") {
						NewView = "ZoneLootDynamicPopupMessage";
					}
					// used for classic UI popups, ref: XRL.UI.Popup.PickOption(...)
					else if (NewView == "Popup:Choice") {
						NewView = "ZoneLootPopup";
					}
                    else {
						// This should never happen unless the game's own Popup UIViews get renamed in a future update.
						// In that unlikely case we'll simply use fallback settings for our commands/keybinds
						UnityEngine.Debug.LogError("ZoneLootList - Unexpected UIView: " + NewView);
						s_SwitchToFallback = true;
					}
				}
				return true; // return control to the original function
			}
		}

		// Required to make our UIView's keybinds work with classic UI Popups.
		// Mimics what the game already does for its own layers/navCategories.
		// Doing it this way lets us avoid having to use Auto="DownPass" for our popup's bindings.
		[HarmonyPatch(typeof(GameManager), nameof(GameManager.UpdateInput))]
		private sealed class UpdateInputPatch {
			static void Postfix () {
				if (!s_UseFallback && s_SwitchToFallback) {
					SwitchToFallBackCommands();
				}
				string layer = "ZoneLootLayer";
				if (!s_UseFallback && ControlManager.IsLayerEnabled(layer) && CommandBindingManager.CommandBindingLayers.ContainsKey(layer)) {
					foreach (var entry in CommandBindingManager.CommandBindingLayers[layer].actions) {
						// All occurences of isCommandDown() inside UpdateInput() use true, false, false for the bool parameters
						if (ControlManager.isCommandDown(entry.name, true, false, false)) {
							Keyboard.PushCommand(entry.name, null);
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
