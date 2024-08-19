using System;
using Plaidman.RecoverableArrows.Events;
using XRL.UI;

// todo
//  test pincushion with multiple arrow types
//  test boomrose arrows don't collect
//  add an option for verbose mode
//  remove other messages
//  clear up whitespace problems
//  make icon
//  make screenshot
//  make description

namespace XRL.World.Parts {
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
			ParentObject.RemovePart(this);
			
			Popup.Show("Finished removing {{W|Recoverable Arrows}}. Please save and quit, then you can remove this mod.");
		}
	}
}
