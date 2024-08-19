using System.Collections.Generic;
using Plaidman.RecoverableArrows.Events;

namespace XRL.World.Parts {
	class RA_PinCushion : IPart, IModEventHandler<RA_UninstallEvent> {
		private Dictionary<string, int> Pins = new();
		public bool IsDead = false;

        public override void Register(GameObject go, IEventRegistrar registrar) {
			registrar.Register(The.Game, RA_UninstallEvent.ID);
			registrar.Register(BeforeDeathRemovalEvent.ID);
            base.Register(go, registrar);
        }

		public void AddPin(string pin) {
			var qty = Pins.GetValueOrDefault(pin, 0);
			Messages.MessageQueue.AddPlayerMessage(ParentObject.DisplayName + " now has " + (qty + 1) + " " + pin);
			Pins[pin] = qty + 1;
		}

        public override bool HandleEvent(BeforeDeathRemovalEvent e) {
			foreach (var pin in Pins.Keys) {
				for (var i = 0; i < Pins[pin]; i++) {
					Messages.MessageQueue.AddPlayerMessage("adding " + pin + " to " + (ParentObject.CurrentCell?.X ?? -1) + "," + (ParentObject.CurrentCell?.Y ?? -1));
					ParentObject.CurrentCell.AddObject(pin);
				}
			}

            return base.HandleEvent(e);
        }

		public bool HandleEvent(RA_UninstallEvent e) {
			ParentObject.RemovePart(this);
			return base.HandleEvent(e);
		}
    }
}