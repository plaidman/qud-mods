using System;
using System.Collections.Generic;
using System.Linq;
using Plaidman.ZoneLootList.Events;
using XRL;
using XRL.UI;
using XRL.World;
using XRL.World.Parts;

// todo: change this to XRL.World.Parts when this mod is broken by qud changes
namespace Plaidman.ZoneLootList.Parts {
	[Serializable]
	public class ZLL_ZoneLootFinder : IPlayerPart {
		public static readonly string ItemListCommand = "Plaidman_ZoneLootList_Command_ShowItemList";
		public static readonly string UninstallCommand = "Plaidman_ZoneLootList_Command_Uninstall";
		public static readonly string TrashOption = "Plaidman_ZoneLootList_Option_Trash";
		public static readonly string CorpsesOption = "Plaidman_ZoneLootList_Option_Corpses";
		public static readonly string ValuesOption = "Plaidman_ZoneLootList_Option_Value";
		public static readonly string AbilityOption = "Plaidman_ZoneLootList_Option_UseAbility";
		public Guid AbilityGuid;

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
				AbilityGuid = ParentObject.AddActivatedAbility("Show Area Items", ItemListCommand, "Skill", Silent: true);
			}
		}
	
		private void RemoveAbility() {
			if (AbilityGuid != Guid.Empty) {
				ParentObject.RemoveActivatedAbility(ref AbilityGuid);
			}
		}

		public override bool HandleEvent(AfterPlayerBodyChangeEvent e) {
			if (ParentObject.ID == ThePlayer.ID) {
				ToggleAbility();
			} else {
				RemoveAbility();
			}

            return base.HandleEvent(e);
        }
	
		public override bool HandleEvent(CommandEvent e) {
			if (e.Command == ItemListCommand) {
				ListItems();
			}

			if (e.Command == UninstallCommand) {
				UninstallParts();
			}

			return base.HandleEvent(e);
		}

		private void UninstallParts() {
			if (Popup.ShowYesNo("Are you sure you want to uninstall {{W|Zone Loot List}}?") == DialogResult.No) {
				XRL.Messages.MessageQueue.AddPlayerMessage("{{W|Zone Loot List}} uninstall was cancelled.");
				return;
			}

			The.Game.HandleEvent(new ZLL_UninstallEvent());
			XRL.Messages.MessageQueue.AddPlayerMessage("{{W|Zone Loot List}}: removed item parts");

			if (AbilityGuid != Guid.Empty) {
				ParentObject.RemoveActivatedAbility(ref AbilityGuid);
				XRL.Messages.MessageQueue.AddPlayerMessage("{{W|Zone Loot List}}: removed ability");
			}

			ParentObject.RemovePart<ZLL_ZoneLootFinder>();
			XRL.Messages.MessageQueue.AddPlayerMessage("{{W|Zone Loot List}}: removed player part");
			
			Popup.Show("Finished removing {{W|Zone Loot List}}. Please save and quit, then you can remove this mod.");
		}
		
		private void ListItems() {
			var gettableItems = ParentObject.CurrentZone.GetObjects(FilterOptions);
			
			if (gettableItems.Count == 0) {
				Popup.Show("You haven't seen any new loot in this area.");
				return;
			}

			var initialSelections = new List<int>();
			for (int i = 0; i < gettableItems.Count; i++) {
				if (gettableItems[i].HasPart<ZLL_AutoGetItem>()) {
					initialSelections.Add(i);
				}
			}

			var toggledItemsEnumerator = Menus.ItemList.ShowPopup(
				options: gettableItems.Select(go => GetOptionLabel(go)).ToArray(),
				icons: gettableItems.Select(go => go.Render).ToArray(),
				initialSelections: initialSelections.ToArray()
			);

			foreach (Menus.ItemList.ToggledItem result in toggledItemsEnumerator) {
				var item = gettableItems[result.Index];

				if (result.Value) {
					item.RequirePart<ZLL_AutoGetItem>();
				} else {
					item.RemovePart<ZLL_AutoGetItem>();
				}
			}
		}

		private string GetOptionLabel(GameObject go) {
			var label = go.GetDisplayName();

			if (Options.GetOption(ValuesOption) == "No") {
				return label;
			}
			
			if (go.GetWeight() <= 0.0) {  // 0 weight is cyan
				return label += "   [{{c|$}}]";
			}
			
			var value = go.Value / go.GetWeight();
			if (value < 1) {  // <1 is red
				return label += "   [{{R|$}}]";
			}

			if (value < 4) {  // 1-4 is yellow
				return label += "   [{{W|$}}]";
			}

			if (value < 10) {  // 4-10 is green
				return label += "   [{{G|$}}]";
			}

			if (value < 25) {  // 10-25 is double green
				return label += "   [{{G|$$}}]";
			}

			return label += "   [{{G|$$$}}]";  // >25 is triple green
		} 
		
		private bool FilterOptions(GameObject go) {
			var autogetByDefault = go.ShouldAutoget()
				&& !go.HasPart<ZLL_AutoGetItem>();
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
