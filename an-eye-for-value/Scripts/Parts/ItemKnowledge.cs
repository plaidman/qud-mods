using System;
using System.Collections.Generic;
using XRL.UI;

namespace XRL.World.Parts {
	[Serializable]
	public class AEFV_ItemKnowledge : IPlayerPart {
		[NonSerialized]
		private readonly string UninstallCommand = "Plaidman_AnEyeForValue_Command_Uninstall";
		[NonSerialized]
		private readonly string OmnicientOption = "Plaidman_AnEyeForValue_Option_Omnicient";
		[NonSerialized]
		private readonly string PKAppraisalSkill = "PKAPP_Price";
		[NonSerialized]
		private readonly string AnEyeForValueSkill = "AEFV_AnEyeForValue";
		
		private readonly HashSet<string> KnownItems = new(50);

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

		public void UninstallParts() {
			if (Popup.ShowYesNo("Are you sure you want to uninstall {{W|An Eye For Value}}?") == DialogResult.No) {
				Messages.MessageQueue.AddPlayerMessage("{{W|An Eye For Value}} uninstall was cancelled.");
				return;
			}

			if (ParentObject.HasSkill(AnEyeForValueSkill)) {
				ParentObject.RemoveSkill(AnEyeForValueSkill);
				Messages.MessageQueue.AddPlayerMessage("{{W|An Eye For Value}}: removed skill");
			}

			ParentObject.GetPart<AEFV_LoadLightener>().UninstallParts();
			ParentObject.GetPart<AEFV_LootFinder>().UninstallParts();

			ParentObject.RemovePart<AEFV_ItemKnowledge>();
			Messages.MessageQueue.AddPlayerMessage("{{W|An Eye For Value}}: removed player part");
			
			Popup.Show("Finished removing {{W|An Eye For Value}}. Please save and quit, then you can remove this mod.");
		}

		public override bool HandleEvent(CommandEvent e) {
			if (e.Command == UninstallCommand) {
				UninstallParts();
			}

			return base.HandleEvent(e);
		}
		
		public bool IsItemKnown(GameObject go) {
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
	}
}
