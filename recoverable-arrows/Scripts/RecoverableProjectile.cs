using System;
using XRL.Rules;

namespace XRL.World.Parts {
    public enum HitType { Wall, Open }

	[Serializable]
	public class RA_RecoverableProjectile : IPart {
		[NonSerialized]
		public Cell CurrentCell = null;

		public int BreakChance = 50;
		public string Blueprint = "Wooden Arrow";

        public override void Register(GameObject go, IEventRegistrar registrar) {
			registrar.Register("ProjectileHit");
            base.Register(go, registrar);
        }

        public override bool FireEvent(Event e) {
			if (e.ID != "ProjectileHit") {
				return base.FireEvent(e);
			}

			Messages.MessageQueue.AddPlayerMessage("arrow hit object");

			GameObject defender = e.GetParameter("Defender") as GameObject;
			if (defender?.CurrentCell != null) {
				Messages.MessageQueue.AddPlayerMessage("  with known location");
				CurrentCell = defender.CurrentCell;
			}
			
			CheckSpawn(defender.ConsiderSolid());

            return base.FireEvent(e);
        }

		//todo handle uninstall event here

        public void CheckSpawn(bool isSolid) {
			Messages.MessageQueue.AddPlayerMessage("break chance: " + BreakChance);
			int roll = Stat.TinkerRandom(1, 100);
			Messages.MessageQueue.AddPlayerMessage("rolled: " + roll);
			if (roll <= BreakChance) {
				Messages.MessageQueue.AddPlayerMessage("arrow broken");
				return;
			}
			
			if (isSolid) {
				Messages.MessageQueue.AddPlayerMessage("hit wall, finding passable cell");
				CurrentCell = CurrentCell.GetCellFromDirectionOfCell(The.Player.CurrentCell);
			}

			Messages.MessageQueue.AddPlayerMessage("creating " + Blueprint);
			CurrentCell.AddObject(Blueprint);
		}
	}
}
