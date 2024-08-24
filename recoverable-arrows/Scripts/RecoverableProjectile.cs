using System;
using Plaidman.RecoverableArrows.Events;
using Plaidman.RecoverableArrows.Handlers;
using Plaidman.RecoverableArrows.Utils;
using XRL.Rules;

namespace XRL.World.Parts {
	[Serializable]
	public class RA_RecoverableProjectile : IPart, IModEventHandler<RA_UninstallEvent> {
		[NonSerialized]
		public Cell CurrentCell = null;
		[NonSerialized]
		private int BreakChance = 101;

		public override void Register(GameObject go, IEventRegistrar registrar) {
			BreakChance = ParentObject.GetIntProperty("RA_BreakChance", 101);

			if (BreakChance < 100) {
				registrar.Register("ProjectileHit");
			}

			registrar.Register(The.Game, RA_UninstallEvent.ID);
			base.Register(go, registrar);
		}

		public override bool FireEvent(Event e) {
			if (e.ID != "ProjectileHit") {
				return base.FireEvent(e);
			}

			GameObject defender = e.GetParameter("Defender") as GameObject;
			if (defender?.CurrentCell != null) {
				CurrentCell = defender.CurrentCell;
			}
		
			if (defender.IsCreature) {
				CheckPin(defender);
			} else {
				CheckSpawn(defender.ConsiderSolid());
			}

			return base.FireEvent(e);
		}

		private bool CheckBreak() {
			if (BreakChance >= 100) {
				// break without verbose output
				return true;
			}

			int roll = Stat.TinkerRandom(1, 100);
			var blueprint = ProjectileBlueprint.Mapping[ParentObject.Blueprint];

			if (roll <= BreakChance) {
				MessageLogger.VerboseMessage("{{y|Your " + blueprint + " broke.}} {{w|[" + roll + " vs " + BreakChance + "]}}");
				return true;
			}
		
			MessageLogger.VerboseMessage("{{y|Your " + blueprint + " is intact.}} {{w|[" + roll + " vs " + BreakChance + "]}}");
			return false;
		}

		public void CheckSpawn(bool isSolid) {
			if (CheckBreak()) {
				return;
			}
		
			if (isSolid) {
				CurrentCell = CurrentCell.GetCellFromDirectionOfCell(The.Player.CurrentCell);
			}

			CurrentCell.AddObject(ProjectileBlueprint.Mapping[ParentObject.Blueprint]);
		}
	
		private void CheckPin(GameObject defender) {
			if (defender.hitpoints <= 0) {
				// BeforeDeathRemovalEvent happens before ProjectileHit.
				// so we add the final arrow differently in the PinCushion part
				return;
			}

			if (CheckBreak()) {
				return;
			}

			var part = defender.RequirePart<RA_PinCushion>();
			part.AddPin(ProjectileBlueprint.Mapping[ParentObject.Blueprint]);
		}

		public void CheckPin(RA_PinCushion part) {
			if (CheckBreak()) {
				return;
			}

			part.AddPin(ProjectileBlueprint.Mapping[ParentObject.Blueprint]);
		}

		public bool HandleEvent(RA_UninstallEvent e) {
			ParentObject.RemovePart(this);
			return base.HandleEvent(e);
		}
	}
}
