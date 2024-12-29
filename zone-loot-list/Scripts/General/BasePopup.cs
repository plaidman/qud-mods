using ConsoleLib.Console;
using HarmonyLib;
using Plaidman.AnEyeForValue.Utils;
using Qud.UI;
using System.Collections.Generic;
using System.Linq;
using XRL.UI;

namespace Plaidman.AnEyeForValue.Menus {
	public enum SortType { Weight, Value };
	public enum PickupType { Single, Multi };
	
	class UIStrings {
		public const string ClassicView = "ZoneLootPopup";
		public const string ClassicViewZoom = "ZoneLootPopupZoom";
		public const string ModernView = "ZoneLootPopupMessage";
		public const string ModernViewDyn = "ZoneLootDynamicPopupMessage";
	}

	/* First 2 Views are used for classic UI popups, ref: GameManager::_ViewData["*Default"]
	 * Note that most popups (including the one in Popup::PickOption()) do NOT use the Popup class' own UIView (the one with ID = "Popup")!
	 * Don't get confused by one of the "NewView" parameters to PushGameView() ("Popup:Choice")!
	 * Behind the scenes that will resolve to the "*Default" UIView and as such that is the reference I used here.
	 * Both of these are exactly the same, except for the zoom effect on open/close (controlled by the ForceFullscreen parameter). */
	[UIView(ID: UIStrings.ClassicView, WantsTileOver: false, ForceFullscreen: false, IgnoreForceFullscreen: false, 
		NavCategory: XMLStrings.ZLLNavCategory, UICanvas: null, TakesScroll: false, UICanvasHost: 0, ForceFullscreenInLegacy: false)]
	[UIView(ID: UIStrings.ClassicViewZoom, WantsTileOver: false, ForceFullscreen: true, IgnoreForceFullscreen: false,
		NavCategory: XMLStrings.ZLLNavCategory, UICanvas: null, TakesScroll: false, UICanvasHost: 0, ForceFullscreenInLegacy: false)]
	/* Next 2 Views are used for modern UI popups, ref: Qud.UI.PopupMessage
	 * Using "PopupMessage" for the UICanvas parameter of ZoneLootPopupMessage is intentional! Quote from the documentation (found on CoQ Discord):
	 * "UICanvas - The name of the UI Canvas (Unity) GameObject which should be displayed when this view is active"
	 * Changing the UICanvas parameter to e.g. "ZoneLootPopupMessage" would lead to errors since that is not a valid Unity object name */
	[UIView(ID: UIStrings.ModernView, WantsTileOver: false, ForceFullscreen: false, IgnoreForceFullscreen: true, 
		NavCategory: XMLStrings.ZLLNavCategory, UICanvas: "PopupMessage", TakesScroll: false, UICanvasHost: 1, ForceFullscreenInLegacy: false)]
	[UIView(ID: UIStrings.ModernViewDyn, WantsTileOver: false, ForceFullscreen: false, IgnoreForceFullscreen: true, 
		NavCategory: XMLStrings.ZLLNavCategory, UICanvas: null, TakesScroll: false, UICanvasHost: 0, ForceFullscreenInLegacy: false)]
	public class BasePopup {
		public SortType CurrentSortType;
		private Dictionary<SortType, List<InventoryItem>> ItemListCache;
		protected Dictionary<SortType, IComparer<InventoryItem>> Comparers;

		private static bool s_UIViewsLoaded = false;
		private static bool s_SwitchToFallback = false;
		private static bool s_UseFallback = false;
		private static bool s_OverridePopup = false;

		private static void SwitchToFallBackCommands() {
			string oldLayer = XMLStrings.ZLLCommandLayer;
			string newLayer = "UI";
			if (!s_UseFallback && CommandBindingManager.CommandBindingLayers.ContainsKey(oldLayer) && CommandBindingManager.CommandBindingLayers.ContainsKey(newLayer)) {
				UnityEngine.Debug.LogError("ZoneLootList - Using fallback command settings!");
				/* Change our commands' attributes to: Layer="UI" and Auto="DownPass"
				 * While certainly suboptimal (game won't complain if we try to bind 2 actions to the same key), this ensures that our
				 * commands still work if our custom UIViews fail for some reason. The "UI" command layer has very few commands on it by
				 * default, so hopefully we shouldn't impact any of the game's menus like we did in the past when using the "Menus" layer. */
				foreach (var entry in CommandBindingManager.CommandBindingLayers[oldLayer].actions) {
					CommandBindingManager.CommandsByID[entry.name].Layer = newLayer;
					CommandBindingManager.CommandsByID[entry.name].Auto = "DownPass";
				}
				// To "inform" the game of these changes, we have to reload all binding layers.
				// ref: XRL.UI.CommandBindingManager::LoadCommands() / XRL.UI.CommandBindingManager::LoadCurrentKeymap(...)
				CommandBindingManager.InitializeInputManager(!System.IO.File.Exists(CommandBindingManager.GetCurrentKeymapPath()), false, null);
			}
			else {
				UnityEngine.Debug.LogError("ZoneLootList - Couldn't switch to fallback command settings!");
			}
			s_UseFallback = true; // Always set this flag to true. No matter how we got here, we won't be able to use the non-fallback commands anyway
		}

		private static bool CheckUIViewsLoaded() {
			if (s_UIViewsLoaded || s_UseFallback) {
				// Unloading UIViews isn't possible AFAIK, so we don't have to check again after confirming this once
				return s_UIViewsLoaded;
			}
			bool nav_zlp = GameManager.Instance.HasViewData(UIStrings.ClassicView);
			bool nav_zlpz = GameManager.Instance.HasViewData(UIStrings.ClassicViewZoom);
			bool nav_zlpm = GameManager.Instance.HasViewData(UIStrings.ModernView);
			bool nav_zldpm = GameManager.Instance.HasViewData(UIStrings.ModernViewDyn);

			// Try to register our custom UIViews if any of them aren't loaded yet.
			// Should only happen when this mod requires user approval after an update.
			if (!nav_zlp || !nav_zlpz || !nav_zlpm || !nav_zldpm) {
				GameManager.Instance.RegisterViews();
				nav_zlp = GameManager.Instance.HasViewData(UIStrings.ClassicView);
				nav_zlpz = GameManager.Instance.HasViewData(UIStrings.ClassicViewZoom);
				nav_zlpm = GameManager.Instance.HasViewData(UIStrings.ModernView);
				nav_zldpm = GameManager.Instance.HasViewData(UIStrings.ModernViewDyn);

				// If our custom UIViews still aren't loaded for some reason, switch to fallback command settings.
				// Note that this NEVER happened in my testing, this is just here as a backup.
				if (!nav_zlp || !nav_zlpz || !nav_zlpm || !nav_zldpm) {
					UnityEngine.Debug.LogError("ZoneLootList - Failed to register custom UIViews!");
					// Calling SwitchToFallBackCommands() from here lead to threading/access errors,
					// so set a flag to call it during the next UpdateInput() iteration instead
					s_SwitchToFallback = true;
					return false;
				}
			}
			s_UIViewsLoaded = true;
			return true;
		}

		/* Replace GameManager::PushGameView()'s NewView parameter when called by Popup::PickOption().
		 * Only this one patch here is required to make our UIViews work with modern UI Popups, since
		 * those don't rely on GameManager::UpdateInput() to receive commands (like classic UI ones do).
		 * This patch could theoretically be avoided by writing our own implementations of Popup::PickOption(),
		 * Popup::WaitNewPopupMessage() and Popup::NewPopupMessageAsync(). But that'd be a lot of code to write
		 * and then also keep up-to-date whenever the game's original functions change. */
		[HarmonyPatch(typeof(GameManager), nameof(GameManager.PushGameView))]
		private sealed class PushGameViewPatch {
			static bool Prefix(ref string NewView) {
				// Override NewView with our custom UIView (only when called for by ZonePopup/InventoryPopup, otherwise use original)
				if (s_OverridePopup && !s_UseFallback) {
					s_OverridePopup = false;
					// used for modern UI popups, ref: XRL.UI.Popup.WaitNewPopupMessage(...)
					if (NewView == "PopupMessage") {
						NewView = UIStrings.ModernView;
					}
					else if (NewView == "DynamicPopupMessage") {
						NewView = UIStrings.ModernViewDyn;
					}
					// used for classic UI popups, ref: XRL.UI.Popup.PickOption(...)
					else if (NewView == "Popup:Choice") {
						if (Options.GetOption(XMLStrings.ClassicUIZoomOption) == "No") {
							NewView = UIStrings.ClassicView;
						}
						else {
							NewView = UIStrings.ClassicViewZoom;
						}
					}
					else {
						// This should never happen unless the game's own UIViews get renamed in a future update.
						// In that case we'll just use fallback settings for our commands/keybinds until we can fix this proper.
						UnityEngine.Debug.LogError("ZoneLootList - Unexpected UIView: " + NewView);
						s_SwitchToFallback = true;
					}
				}
				return true; // return control to the original function
			}
		}

		/* Required to make our UIView's keybinds work with classic UI Popups.
		 * Mimics what the game already does for its own layers/navCategories.
		 * Doing it this way lets us avoid having to use Auto="DownPass" for our popup's bindings. */
		[HarmonyPatch(typeof(GameManager), nameof(GameManager.UpdateInput))]
		private sealed class UpdateInputPatch {
			static void Postfix () {
				if (!s_UseFallback) {
					if (s_SwitchToFallback) {
						SwitchToFallBackCommands();
						return; // Fallback commands don't require any custom code, so there's no need to continue here after switching
					}
					string layer = XMLStrings.ZLLCommandLayer;
					if (ControlManager.IsLayerEnabled(layer) && CommandBindingManager.CommandBindingLayers.ContainsKey(layer)) {
						foreach (var entry in CommandBindingManager.CommandBindingLayers[layer].actions) {
							// All occurences of isCommandDown() inside UpdateInput() use true, false, false for the bool parameters
							if (ControlManager.isCommandDown(entry.name, true, false, false)) {
								// Add command to the command queue (all input devices use Keyboard.PushCommand())
								Keyboard.PushCommand(entry.name, null);
							}
						}
					}
				}
			}
		}

		// simple wrapper function for use in ZonePopup/InventoryPopup
		internal static int PickOption(string Title = "", string Intro = null, IRenderable IntroIcon = null, IReadOnlyList<string> Options = null, bool RespectOptionNewlines = false, 
			IReadOnlyList<IRenderable> Icons = null, int DefaultSelected = 0, IReadOnlyList<QudMenuItem> Buttons = null, bool AllowEscape = false) {
			CheckUIViewsLoaded();
			s_OverridePopup = true; // Set flag to use our custom UIViews for Popup::PickOption()
			int selectedIndex = Popup.PickOption(
				Title: Title,
				Intro: Intro,
				IntroIcon: IntroIcon,
				Options: Options,
				RespectOptionNewlines: RespectOptionNewlines,
				Icons: Icons,
				DefaultSelected: DefaultSelected,
				Buttons: Buttons,
				AllowEscape: AllowEscape
			);
			s_OverridePopup = false; // Should already be false after PickOption(), just adding this here as a backup
			return selectedIndex;
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
