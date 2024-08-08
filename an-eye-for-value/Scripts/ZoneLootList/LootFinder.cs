using System;
using System.Collections.Generic;
using ConsoleLib.Console;
using Plaidman.AnEyeForValue.Events;
using Plaidman.AnEyeForValue.Menus;
using Plaidman.AnEyeForValue.Utils;
using XRL.UI;
using XRL.World.Capabilities;

// TODO:
// make a different comparer for zone loot and inventory list
// show total quantity of drams in inventory list
// track which liquids you've seen in AEFV
// tiny puddles show as 0 weight (wrong)
// item value is known if you know the container item (if any) AND the primary liquid inside it (if any)
// squelch errors with deserialization
// pool weight should be shown as ~~~
// pool weight value ratio can be calculated
// takeable containers should work as normal

namespace XRL.World.Parts {
	[Serializable]
	public class AEFV_LootFinder : IPlayerPart {
		[NonSerialized]
		private static readonly string ItemListCommand = "Plaidman_AnEyeForValue_Command_ZoneLootList";
		[NonSerialized]
		private static readonly string AbilityOption = "Plaidman_AnEyeForValue_Option_UseAbilities";
		[NonSerialized]
		private readonly ZonePopup ItemPopup = new();
		[NonSerialized]
		private AEFV_ItemKnowledge ItemKnowledge = null;

		public Guid AbilityGuid;
		public SortType CurrentSortType = PopupUtils.DefaultSortType();

		public override void Register(GameObject go, IEventRegistrar registrar) {
			registrar.Register(CommandEvent.ID);
			registrar.Register(AfterPlayerBodyChangeEvent.ID);
			base.Register(go, registrar);
		}

		public void ToggleAbility() {
			if (Options.GetOption(AbilityOption) == "Yes") {
				RequireAbility();
			} else {
				RemoveAbility();
			}
		}

		private void RequireAbility() {
			if (AbilityGuid == Guid.Empty) {
				AbilityGuid = ParentObject.AddActivatedAbility(
					Name: "Zone Loot List",
					Command: ItemListCommand,
					Class: "Skill",
					UITileDefault: Renderable.UITile("an_eye_for_value.png", 'y', 'r'),
					Silent: true);
			}
		}
	
		private void RemoveAbility() {
			if (AbilityGuid != Guid.Empty) {
				ParentObject.RemoveActivatedAbility(ref AbilityGuid);
			}
		}

		public override bool HandleEvent(AfterPlayerBodyChangeEvent e) {
			ToggleAbility();
			return base.HandleEvent(e);
		}
	
		public override bool HandleEvent(CommandEvent e) {
			if (e.Command == ItemListCommand) {
				ListItems();
			}

			return base.HandleEvent(e);
		}

		public void UninstallParts() {
			The.Game.HandleEvent(new AEFV_UninstallEvent());
			RemoveAbility();
			ParentObject.RemovePart<AEFV_LootFinder>();
		}

		private AEFV_ItemKnowledge GetItemKnowledge() {
			ItemKnowledge ??= ParentObject.GetPart<AEFV_ItemKnowledge>();
			return ItemKnowledge;
		}

		private void ListItems() {
			var filteredItems = ZoneLootUtils.FilterZoneItems(ParentObject.CurrentZone.YieldObjects());
			var takeableItems = filteredItems.TakeableItems;
			var liquids = filteredItems.Liquids;

			if (liquids.Count == 0 && takeableItems.Count == 0) {
				Popup.Show("You haven't seen any new loot in this area.");
				return;
			}

			var initialSelections = new List<int>();
			for (int i = 0; i < takeableItems.Count; i++) {
				if (takeableItems[i].HasPart<AEFV_AutoGetBeacon>()) {
					initialSelections.Add(i);
				}
			}

			var itemList = new InventoryItem[takeableItems.Count + liquids.Count];
			for (var i = 0; i < takeableItems.Count; i++) {
				var go = takeableItems[i];
				var inv = new InventoryItem(i, go, GetItemKnowledge().IsItemKnown(go));
				itemList[i] = inv;
			}
			for (var i = 0; i < liquids.Count; i++) {
				var go = liquids[i];
				var inv = new InventoryItem(i, go, GetItemKnowledge().IsLiquidKnown(go.LiquidVolume), true);
				itemList[i + takeableItems.Count] = inv;
			}
			
			ItemPopup.CurrentSortType = CurrentSortType;
			var toggledItemsEnumerator = ItemPopup.ShowPopup(
				itemList,
				initialSelections.ToArray()
			);
			
			GameObject item;
			foreach (ZonePopupAction result in toggledItemsEnumerator) {
				switch (result.Action) {
					case ActionType.TurnOn:
						item = takeableItems[result.Index];
						item.RemoveIntProperty("AutoexploreActionAutoget");
						item.RequirePart<AEFV_AutoGetBeacon>();
						break;

					case ActionType.TurnOff:
						item = takeableItems[result.Index];
						item.RemovePart<AEFV_AutoGetBeacon>();
						break;

					case ActionType.Sort:
						CurrentSortType = ItemPopup.CurrentSortType;
						break;
						
					case ActionType.Travel:
						var coord = liquids[result.Index].CurrentCell;
						AutoAct.Setting = "M" + coord.X.ToString() + "," + coord.Y.ToString();
						The.ActionManager.SkipPlayerTurn = true;
						break;
				}
			}
		}
	}
}
