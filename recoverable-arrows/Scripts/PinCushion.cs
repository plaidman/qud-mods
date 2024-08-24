using System;
using System.Collections.Generic;
using Plaidman.RecoverableArrows.Events;
using Plaidman.RecoverableArrows.Utils;
using XRL.Language;

namespace XRL.World.Parts {
	[Serializable]
	public class RA_PinCushion : IPart, IModEventHandler<RA_UninstallEvent> {
		public Dictionary<string, int> Pins = new();

		public override void Register(GameObject go, IEventRegistrar registrar) {
			registrar.Register(The.Game, RA_UninstallEvent.ID);
			registrar.Register(BeforeDeathRemovalEvent.ID);
			base.Register(go, registrar);
		}

		public void AddPin(string pin) {
			var qty = Pins.GetValueOrDefault(pin, 0);
			Pins[pin] = qty + 1;
		}

		public override bool HandleEvent(BeforeDeathRemovalEvent e) {
			List<string> dropped = new();
		
			if (e.Projectile != null && e.Projectile.TryGetPart(out RA_RecoverableProjectile part)) {
				part.CheckPin(this);
			}

			foreach (var pin in Pins.Keys) {
				for (var i = 0; i < Pins[pin]; i++) {
					ParentObject.CurrentCell.AddObject(pin);
				}
			
				var qty = Pins[pin];
				var blueprint = pin;
				if (qty > 1) {
					blueprint = Grammar.Pluralize(pin);
				}
			
				dropped.Add("{{w|[" + qty + "x " + blueprint + "]}}");
			}

			var target = Grammar.MakePossessive(ParentObject.DisplayNameStripped);
			MessageLogger.VerboseMessage("{{y|You can recover " + string.Join(", ", dropped) + " from " + target + " body.}}");

			return base.HandleEvent(e);
		}

		public bool HandleEvent(RA_UninstallEvent e) {
			ParentObject.RemovePart(this);
			return base.HandleEvent(e);
		}
	}
}