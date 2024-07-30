using System;
using System.Collections.Generic;
using XRL.UI;

namespace XRL.World.Parts {
	[Serializable]
	public class AEFV_ItemKnowledge : IPlayerPart {
		public static readonly string ShowKnownCommand = "Plaidman_AnEyeForValue_Command_ShowKnown";
		public static readonly string UninstallCommand = "Plaidman_AnEyeForValue_Command_Uninstall";
		public static readonly string OmnicientOption = "Plaidman_AnEyeForValue_Option_Omnicient";
		public static readonly string PKAppraisalSkill = "PKAPP_Price";
		public static readonly string AnEyeForValueSkill = "Plaidman_AnEyeForValue_Skill";
		public HashSet<string> KnownItems = new(50);

		public override void Register(GameObject go, IEventRegistrar registrar) {
			registrar.Register(CommandEvent.ID);
			registrar.Register(StartTradeEvent.ID);
			base.Register(go, registrar);
		}

		public override bool HandleEvent(StartTradeEvent e) {
			if (!e.Trader.IsCreature) {
				return base.HandleEvent(e);
			}
			
			foreach (var item in e.Actor.Inventory.GetObjects()) {
				KnownItems.Add(item.BaseDisplayName);
			}

			foreach (var item in e.Trader.Inventory.GetObjects()) {
				KnownItems.Add(item.BaseDisplayName);
			}

			return base.HandleEvent(e);
		}

		private void UninstallParts() {
			if (Popup.ShowYesNo("Are you sure you want to uninstall {{W|An Eye For Value}}?") == DialogResult.No) {
				Messages.MessageQueue.AddPlayerMessage("{{W|An Eye For Value}} uninstall was cancelled.");
				return;
			}

			if (ParentObject.HasSkill(AnEyeForValueSkill)) {
				ParentObject.RemoveSkill(AnEyeForValueSkill);
				Messages.MessageQueue.AddPlayerMessage("{{W|An Eye For Value}}: removed skill");
			}

			ParentObject.RemovePart<AEFV_ItemKnowledge>();
			Messages.MessageQueue.AddPlayerMessage("{{W|An Eye For Value}}: removed player part");
			
			Popup.Show("Finished removing {{W|An Eye For Value}}. Please save and quit, then you can remove this mod.");
		}

		public override bool HandleEvent(CommandEvent e) {
			if (e.Command == ShowKnownCommand) {
				ListItems();
			}

			if (e.Command == UninstallCommand) {
				UninstallParts();
			}

			return base.HandleEvent(e);
		}
		
		public bool ItemIsKnown(GameObject go) {
			if (Options.GetOption(OmnicientOption) == "Yes") {
				return true;
			}
			
			if (ParentObject.HasSkill(PKAppraisalSkill)) {
				return true;
			}

			if (ParentObject.HasSkill(AnEyeForValueSkill)) {
				return true;
			}

			return KnownItems.Contains(go.BaseDisplayName);
		}
		
		private void ListItems() {
			var list = "";
			foreach (var item in KnownItems) {
				list += item + "\n";
			}
			
			Popup.Show(list, "Known Items");
		}
	}
}
