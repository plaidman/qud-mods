using System;
using XRL.UI;
using System.Linq;
using Plaidman.AnEyeForValue.Menus;
using Plaidman.AnEyeForValue.Utils;
using ConsoleLib.Console;

namespace XRL.World.Parts {
	[Serializable]
	public class AEFV_LoadLightener : IPlayerPart {
		[NonSerialized]
		private static readonly string ItemListCommand = "Plaidman_AnEyeForValue_Command_LightenMyLoad";
		[NonSerialized]
		private static readonly string AbilityOption = "Plaidman_AnEyeForValue_Option_UseAbilities";
		[NonSerialized]
		private readonly InventoryPopup ItemPopup = new();
		[NonSerialized]
		private AEFV_ItemKnowledge ItemKnowledge = null;

		public Guid AbilityGuid;
		public SortType CurrentSortType = PopupUtils.DefaultSortType();

		public override void Write(GameObject basis, SerializationWriter writer) {
			writer.WriteNamedFields(this, GetType());
		}

		public override void Read(GameObject basis, SerializationReader reader) {
			if (reader.ModVersions["Plaidman_AnEyeForValue"] == new Version("1.0.0")) {
				base.Read(basis, reader);
				return;
			}

			reader.ReadNamedFields(this, GetType());
		}

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
					Name: "Lighten My Load",
					Command: ItemListCommand,
					Class: "Skill",
					UITileDefault: Renderable.UITile("an_eye_for_value.png", 'y', 'm'),
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

		private AEFV_ItemKnowledge GetItemKnowledge() {
			ItemKnowledge ??= ParentObject.GetPart<AEFV_ItemKnowledge>();
			return ItemKnowledge;
		}

		private void ListItems() {
			var objects = ParentObject.Inventory.GetObjects();
			var valueMult = ValueUtils.GetValueMultiplier();
			var itemList = objects.Select((go, i) => {
				var known = GetItemKnowledge().IsItemKnown(go);
				return new InventoryItem(i, go, valueMult, known, false);
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
