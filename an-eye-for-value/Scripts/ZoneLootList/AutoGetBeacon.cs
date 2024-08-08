using System;
using Plaidman.AnEyeForValue.Events;

namespace XRL.World.Parts {
	[Serializable]
	public class AEFV_AutoGetBeacon : IPart, IModEventHandler<AEFV_UninstallEvent> {
		public override void Register(GameObject go, IEventRegistrar registrar) {
			registrar.Register(AutoexploreObjectEvent.ID);
			registrar.Register(AddedToInventoryEvent.ID);
			registrar.Register(The.Game, ZoneActivatedEvent.ID);
			registrar.Register(The.Game, AEFV_UninstallEvent.ID);

			base.Register(go, registrar);
		}

		public bool HandleEvent(AEFV_UninstallEvent e) {
			ParentObject.RemovePart(this);
			return base.HandleEvent(e);
		}

		public override bool HandleEvent(ZoneActivatedEvent e) {
			if (!(e.Zone.IsWorldMap() || ParentObject.InZone(e.Zone))) {
				ParentObject.RemovePart(this);
			}

			return base.HandleEvent(e);
		}

		public override bool HandleEvent(AutoexploreObjectEvent e) {
			e.Command ??= "Autoget";
			return base.HandleEvent(e);
		} 

		public override bool HandleEvent(AddedToInventoryEvent e) {
			ParentObject.RemovePart<AEFV_AutoGetBeacon>();

			return base.HandleEvent(e);
		}
	}
}