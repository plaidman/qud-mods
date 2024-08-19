using System;
using Plaidman.RecoverableArrows.Events;
using XRL.UI;

// test
//  miss
//  flinch
//  arrow breaks after hitting edge
//  arrow breaks after hitting wall
//  arrow breaks after hitting creature
//  no target
//  target creature
//  hit edge
//  hit wall
//  hit creature
//  kill creature with arrow
//  kill creature with melee but having arrows in it
//  kill creature with melee with no arrows
//  ensure the correct number of arrows drop from creature
//  save saves pincushion
//  save saves without errors in playerlog
//  uninstall works without errors
//
// todo
//  add an option for verbose mode
//  remove other messages

namespace XRL.World.Parts {
	[Serializable]
	public class RA_ArrowTracker : IPlayerPart {
		[NonSerialized]
		private static readonly string UninstallCommand = "Plaidman_RecoverableArrows_Command_Uninstall";
		[NonSerialized]
		public RA_RecoverableProjectile ProjectilePart = null;

        public override void Register(GameObject go, IEventRegistrar registrar) {
			registrar.Register(CommandEvent.ID);
			registrar.Register(ProjectileMovingEvent.ID);
            base.Register(go, registrar);
        }

        public override bool HandleEvent(ProjectileMovingEvent e) {
			if (e.PathIndex <= 1) {
				e.Projectile.TryGetPart(out ProjectilePart);
			}

			if (ProjectilePart == null) {
				return base.HandleEvent(e);
			}
			
			ProjectilePart.CurrentCell = e.Cell;
			
			if (e.PathIndex == e.Path.Count - 1) {
				ProjectilePart.CheckSpawn(false);
			}

			return base.HandleEvent(e);
        }

        public override bool HandleEvent(CommandEvent e) {
			if (e.Command == UninstallCommand) {
				UninstallParts();
			}

			return base.HandleEvent(e);
		}

		private void UninstallParts() {
			if (Popup.ShowYesNo("Are you sure you want to uninstall {{W|Recoverable Arrows}}?") == DialogResult.No) {
				Messages.MessageQueue.AddPlayerMessage("{{W|Recoverable Arrows}} uninstall was cancelled.");
				return;
			}

			The.Game.HandleEvent(new RA_UninstallEvent());
			ParentObject.RemovePart<RA_ArrowTracker>();
			
			Popup.Show("Finished removing {{W|Recoverable Arrows}}. Please save and quit, then you can remove this mod.");
		}
	}
}
