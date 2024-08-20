using System;
using System.Collections.Generic;
using Plaidman.RecoverableArrows.Events;
using Plaidman.RecoverableArrows.Utils;
using XRL.Language;

namespace XRL.World.Parts {
	[Serializable]
	class RA_PinCushion : IPart, IModEventHandler<RA_UninstallEvent> {
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
			foreach (var pin in Pins.Keys) {
				for (var i = 0; i < Pins[pin]; i++) {
					ParentObject.CurrentCell.AddObject(pin);
				}
				
				var target = Grammar.MakePossessive(ParentObject.DisplayNameStripped);
				var qty = Pins[pin];
				var blueprint = pin;
				if (qty > 1) {
					blueprint = Grammar.Pluralize(pin);
				}
				
				MessageLogger.VerboseMessage("{{y|You can recover " + qty + "x " + blueprint + " from " + target + " body.}}");
			}

			return base.HandleEvent(e);
		}

		public bool HandleEvent(RA_UninstallEvent e) {
			ParentObject.RemovePart(this);
			return base.HandleEvent(e);
		}
	}
}