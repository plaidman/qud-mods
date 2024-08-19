using System;
using System.Collections.Generic;
using Plaidman.RecoverableArrows.Events;

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
				Messages.MessageQueue.AddPlayerMessage(ParentObject.DisplayNameStripped + " released " + Pins[pin] + "x " + pin);
			}

	        return base.HandleEvent(e);
	    }

		public bool HandleEvent(RA_UninstallEvent e) {
			ParentObject.RemovePart(this);
			return base.HandleEvent(e);
		}
	}
}