using System;
using System.Collections.Generic;
using System.Linq;
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
				AbilityGuid = ParentObject.AddActivatedAbility("Zone Loot List", ItemListCommand, "Skill", Silent: true);
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
		
		private void ListItems() {
			var gettableItems = ParentObject.CurrentZone.GetObjects(FilterOptions);
			
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

			var itemKnowledge = ParentObject.GetPart<AEFV_ItemKnowledge>();
			var itemList = gettableItems.Select((go, i) => {
				return new InventoryItem(i, go, itemKnowledge.IsItemKnown(go));
			}).ToArray();

			ItemPopup.CurrentSortType = CurrentSortType;
			var toggledItemsEnumerator = ItemPopup.ShowPopup(
				options: itemList,
				initialSelections: initialSelections.ToArray()
			);

			foreach (ToggledItem result in toggledItemsEnumerator) {
				if (result.Index == -3) {
					CurrentSortType = ItemPopup.CurrentSortType;
					continue;
				}

				var item = gettableItems[result.Index];
				if (result.Value) {
					item.RequirePart<AEFV_AutoGetBeacon>();
				} else {
					item.RemovePart<AEFV_AutoGetBeacon>();
				}
			}
		}

		private bool FilterOptions(GameObject go) {
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
				&& go.Physics.CurrentCell.IsExplored()
				&& !go.HasPropertyOrTag("DroppedByPlayer")
				&& !go.HasPropertyOrTag("NoAutoget")
				&& !go.IsOwned()
				&& !go.IsHidden
				&& !armedMine
				&& !autogetByDefault
				&& !isCorpse
				&& !isTrash;
		}
	}
}
