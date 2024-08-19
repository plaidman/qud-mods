using System;
using Plaidman.RecoverableArrows.Events;
using XRL.Rules;

namespace XRL.World.Parts {
	[Serializable]
	public class RA_RecoverableArrow : IPart, IModEventHandler<RA_ArrowLanded> {
		[NonSerialized]
		public Cell CurrentCell = null;

		public int Chance = 50;
		public string Blueprint = "Wooden Arrow";

        public override void Register(GameObject go, IEventRegistrar registrar) {
			registrar.Register(CommandEvent.ID);
			registrar.Register("ProjectileHit");
            base.Register(go, registrar);
        }

        public override bool FireEvent(Event e) {
			if (e.ID != "ProjectileHit") {
				return base.FireEvent(e);
			}

			UnityEngine.Debug.Log(" ");
			UnityEngine.Debug.Log("ProjectileHit arrow");

			GameObject defender = e.GetParameter("Defender") as GameObject;
			UnityEngine.Debug.Log("defender " + (defender.DisplayName ?? "null") + " " + (defender.CurrentCell?.X ?? -1) + " " + (defender.CurrentCell?.Y ?? -1));
			
			// if defender still has cell
			//   override saved cell with defender cell
			//   call HandleEvent on self with a hit

            return base.FireEvent(e);
        }

		//handle uninstall event here

		// don't need an event here, I can just trigger the function directly
        public bool HandleEvent(RA_ArrowLanded e) {
			bool success = Stat.TinkerRandom(1, 100) <= Chance;
			if (!success) {
				return base.HandleEvent(e);
			}

			// if cell is a wall, pick an adjacent nonwall cell
			// or pick the adjacent cell in the direction of the player?
			// 
			e.LandedCell.AddObject(Blueprint);
			return base.HandleEvent(e);
		}
	}
}
