using System;
using System.Collections.Generic;
using Plaidman.AnEyeForValue.Utils;
using XRL.UI;

namespace XRL.World.Parts {
	[Serializable]
	public class AEFV_ItemKnowledge : IPlayerPart {
		public HashSet<string> KnownItems = new(50);
		public HashSet<string> KnownLiquids = new(20);

		public override void Write(GameObject basis, SerializationWriter writer) {
			WriteStringHashSet(KnownItems, writer);
			WriteStringHashSet(KnownLiquids, writer);
		}

		private void WriteStringHashSet(HashSet<string> hs, SerializationWriter writer) {
			writer.Write(hs.Count);

			foreach (var item in hs) {
				writer.WriteOptimized(item);
			}
		}

		public override void Read(GameObject basis, SerializationReader reader) {
			if (reader.ModVersions["Plaidman_AnEyeForValue"] == new Version("1.0.0")) {
				KnownItems = (HashSet<string>)reader.ReadObject();
				return;
			}

			if (reader.ModVersions["Plaidman_AnEyeForValue"] == new Version("2.0.0")) {
				reader.ReadNamedFields(this, GetType());
				return;
			}

			ReadStringHashSet(KnownItems, reader);
			ReadStringHashSet(KnownLiquids, reader);
		}

		private void ReadStringHashSet(HashSet<string> hs, SerializationReader reader) {
			var count = reader.ReadInt32();

			for (var i = 0; i < count; i++) {
				hs.Add(reader.ReadOptimizedString());
			}
		}

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

				if (item.LiquidVolume?.Primary != null) {
					KnownLiquids.Add(item.LiquidVolume.Primary);
				}
			}

			foreach (var item in e.Trader.Inventory.GetObjects()) {
				KnownItems.Add(item.BaseDisplayName);

				if (item.LiquidVolume?.Primary != null) {
					KnownLiquids.Add(item.LiquidVolume.Primary);
				}
			}

			return base.HandleEvent(e);
		}

		public void UninstallParts() {
			if (Popup.ShowYesNo("Are you sure you want to uninstall {{W|Zone Loot List}}?") == DialogResult.No) {
				Messages.MessageQueue.AddPlayerMessage("{{W|Zone Loot List}} uninstall was cancelled.");
				return;
			}

			if (ParentObject.HasSkill(XMLStrings.AnEyeForValueSkill)) {
				ParentObject.RemoveSkill(XMLStrings.AnEyeForValueSkill);
			}

			if (The.Player.TryGetPart(out AEFV_LoadLightener lmlPart)) {
				lmlPart.UninstallParts();
			}

			if (The.Player.TryGetPart(out AEFV_LootFinder zllPart)) {
				zllPart.UninstallParts();
			}

			ParentObject.RemovePart<AEFV_ItemKnowledge>();

			Popup.Show("Finished removing {{W|Zone Loot List}}. Please save and quit, then you can remove this mod.");
		}

		public override bool HandleEvent(CommandEvent e) {
			if (e.Command == XMLStrings.UninstallCommand) {
				UninstallParts();
			}

			return base.HandleEvent(e);
		}

		public bool IsItemKnown(GameObject go) {
			if (Options.GetOption(XMLStrings.OmnicientValueOption) == "Yes") {
				return true;
			}

			if (ParentObject.HasSkill(XMLStrings.PKAppraisalSkill)) {
				return true;
			}

			if (ParentObject.HasSkill(XMLStrings.AnEyeForValueSkill)) {
				return true;
			}

			var itemKnown = KnownItems.Contains(go.BaseDisplayName);
			var liquidKnown = true;
			if (go.LiquidVolume?.Primary != null) {
				liquidKnown = KnownLiquids.Contains(go.LiquidVolume.Primary);
			}

			return itemKnown && liquidKnown;
		}

		public bool IsLiquidKnown(LiquidVolume liquids) {
			if (Options.GetOption(XMLStrings.OmnicientValueOption) == "Yes") {
				return true;
			}

			if (ParentObject.HasSkill(XMLStrings.PKAppraisalSkill)) {
				return true;
			}

			if (ParentObject.HasSkill(XMLStrings.AnEyeForValueSkill)) {
				return true;
			}

			// dunno why a pool wouldn't have a primary liquid
			if (liquids?.Primary == null) {
				return false;
			}

			return KnownLiquids.Contains(liquids.Primary);
		}
	}
}
