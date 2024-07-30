using System;
using XRL.UI;
using System.Linq;
using Plaidman.AnEyeForValue.Menus;
using Plaidman.AnEyeForValue.Utils;

namespace XRL.World.Parts {
	[Serializable]
	public class AEFV_LoadLightener : IPlayerPart {
		[NonSerialized]
		private static readonly string ItemListCommand = "Plaidman_AnEyeForValue_Command_LightenMyLoad";
		[NonSerialized]
		private static readonly string AbilityOption = "Plaidman_AnEyeForValue_Option_UseAbilities";
		[NonSerialized]
		private readonly InventoryPopup ItemPopup = new();

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
				AbilityGuid = ParentObject.AddActivatedAbility("Lighten My Load", ItemListCommand, "Skill", Silent: true);
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

		public void UninstallParts() {
			RemoveAbility();
			ParentObject.RemovePart<AEFV_LoadLightener>();
		}

		public override bool HandleEvent(CommandEvent e) {
			if (e.Command == ItemListCommand) {
				ListItems();
			}

			return base.HandleEvent(e);
		}
		
		private void ListItems() {
			var objects = ParentObject.Inventory.GetObjects();
			var itemKnowledge = ParentObject.GetPart<AEFV_ItemKnowledge>();
			var itemList = objects.Select((go, i) => {
				return new InventoryItem(i, go, itemKnowledge.IsItemKnown(go));
			}).ToArray();

			ItemPopup.CurrentSortType = CurrentSortType;
			var selected = ItemPopup.ShowPopup(itemList);
			CurrentSortType = ItemPopup.CurrentSortType;
			if (selected == null || selected.Length == 0) {
				Messages.MessageQueue.AddPlayerMessage("no items selected");
				return;
			}
			
			if (selected.Length == 1) {
				var index = selected[0];
				InventoryActionEvent.Check(ParentObject, ParentObject, objects[index], "CommandDropObject");
			}

			foreach (var item in selected) {
				InventoryActionEvent.Check(ParentObject, ParentObject, objects[item], "CommandDropAllObject");
			}
		}
	}
}
