using System;
using Plaidman.RecoverableArrows.Events;
using Plaidman.RecoverableArrows.Utils;
using XRL.UI;

namespace XRL.World.Parts {
	public class RA_ArrowTracker : IPlayerPart {
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
			if (e.Command == XMLStrings.UninstallCommand) {
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
