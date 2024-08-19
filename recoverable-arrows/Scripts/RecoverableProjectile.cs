using System;
using Plaidman.RecoverableArrows.Events;
using XRL.Rules;

namespace XRL.World.Parts {
    public enum HitType { Wall, Open }

	[Serializable]
	public class RA_RecoverableProjectile : IPart, IModEventHandler<RA_UninstallEvent> {
		[NonSerialized]
		public Cell CurrentCell = null;

		public int BreakChance = 50;
		public string Blueprint = "Wooden Arrow";

        public override void Register(GameObject go, IEventRegistrar registrar) {
			registrar.Register("ProjectileHit");
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
			
			CheckSpawn(defender.ConsiderSolid());

            return base.FireEvent(e);
        }

        public void CheckSpawn(bool isSolid) {
			int roll = Stat.TinkerRandom(1, 100);
			if (roll <= BreakChance) {
				return;
			}
			
			if (isSolid) {
				CurrentCell = CurrentCell.GetCellFromDirectionOfCell(The.Player.CurrentCell);
			}

			CurrentCell.AddObject(Blueprint);
		}
		
		public bool HandleEvent(RA_UninstallEvent e) {
			Messages.MessageQueue.AddPlayerMessage("uninstalling arrow part");
			ParentObject.RemovePart(this);
			return base.HandleEvent(e);
		}
	}
}
