using System;
using System.Collections.Generic;
using System.Linq;
using ConsoleLib.Console;
using Plaidman.AnEyeForValue.Events;
using Plaidman.AnEyeForValue.Menus;
using Plaidman.AnEyeForValue.Utils;
using XRL.UI;

namespace XRL.World.Parts {
	[Serializable]
	public class AEFV_LootFinder : IPlayerPart {
		[NonSerialized]
		private static readonly string ItemListCommand = "Plaidman_AnEyeForValue_Command_ZoneLootList";
		[NonSerialized]
		private static readonly string TrashOption = "Plaidman_AnEyeForValue_Option_ZoneTrash";
		[NonSerialized]
		private static readonly string CorpsesOption = "Plaidman_AnEyeForValue_Option_ZoneCorpses";
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
			var items = ZoneLootUtils.FilterZoneItems(ParentObject.CurrentZone.YieldObjects());
			var gettableItems = items.TakeableItems;
			gettableItems.AddRange(items.Liquids);

			if (gettableItems.Count == 0) {
				Popup.Show("You haven't seen any new loot in this area.");
				return;
			}

			var initialSelections = new List<int>();
			for (int i = 0; i < gettableItems.Count; i++) {
				if (gettableItems[i].HasPart<AEFV_AutoGetBeacon>()) {
					initialSelections.Add(i);
				}
			}

			var itemList = gettableItems.Select((go, i) => {
				return new InventoryItem(i, go, GetItemKnowledge().IsItemKnown(go));
			}).ToArray();

			ItemPopup.CurrentSortType = CurrentSortType;
			var toggledItemsEnumerator = ItemPopup.ShowPopup(
				itemList,
				initialSelections.ToArray()
			);
			
			foreach (ZonePopupAction result in toggledItemsEnumerator) {
				switch (result.Action) {
					case ActionType.TurnOn:
						var item = gettableItems[result.Index];
						item.RemoveIntProperty("AutoexploreActionAutoget");
						item.RequirePart<AEFV_AutoGetBeacon>();
						break;

					case ActionType.TurnOff:
						gettableItems[result.Index].RemovePart<AEFV_AutoGetBeacon>();
						break;

					case ActionType.Sort:
						CurrentSortType = ItemPopup.CurrentSortType;
						break;
				}
			}
		}
	}
}
