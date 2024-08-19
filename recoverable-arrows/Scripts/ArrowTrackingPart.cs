using System;
using System.Linq;
using Plaidman.RecoverableArrows.Events;
using XRL.UI;

namespace XRL.World.Parts {
	[Serializable]
	public class RA_ArrowTracking : IPlayerPart {
		[NonSerialized]
		private static readonly string UninstallCommand = "Plaidman_RecoverableArrows_Command_Uninstall";

        public override void Register(GameObject go, IEventRegistrar registrar) {
			registrar.Register(CommandEvent.ID);
			registrar.Register(ProjectileMovingEvent.ID);
            base.Register(go, registrar);
        }

        public override bool HandleEvent(ProjectileMovingEvent e) {
			UnityEngine.Debug.Log(" ");
			UnityEngine.Debug.Log("ProjectileMovingEvent player");

			UnityEngine.Debug.Log("path index " + e.PathIndex + " of " + (e.Path.Count-1));
			UnityEngine.Debug.Log("cell " + e.Cell.X + " " + e.Cell.Y);
			UnityEngine.Debug.Log("projectile " + (e.Projectile?.DisplayName ?? "null"));
			UnityEngine.Debug.Log("defender " + (e.Defender?.DisplayName ?? "null") + " " + (e.Defender?.CurrentCell?.X ?? -1) + " " + (e.Defender?.CurrentCell?.Y ?? -1));
			UnityEngine.Debug.Log("hit override " + (e.HitOverride?.DisplayName ?? "null") + " " + (e.HitOverride?.CurrentCell?.X ?? -1) + " " + (e.HitOverride?.CurrentCell?.Y ?? -1));
			
			// if projectile.TryGetPart
			//   update projectile cell in part
			// if final index in path
			//   fire event that it missed

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
			ParentObject.RemovePart<RA_ArrowTracking>();
			
			Popup.Show("Finished removing {{W|Recoverable Arrows}}. Please save and quit, then you can remove this mod.");
		}
	}
}
