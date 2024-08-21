using System;
using Plaidman.ZoneLootList.Events;
using XRL;
using XRL.World;

// todo: change this to XRL.World.Parts when this mod is broken by qud changes
namespace Plaidman.ZoneLootList.Parts {
	[Serializable]
	public class ZLL_AutoGetItem : IPart, IModEventHandler<ZLL_UninstallEvent> {
        public override void Register(GameObject go, IEventRegistrar registrar) {
			registrar.Register(AutoexploreObjectEvent.ID);
			registrar.Register(AddedToInventoryEvent.ID);
			registrar.Register(The.Game, ZoneActivatedEvent.ID);
			registrar.Register(The.Game, ZLL_UninstallEvent.ID);

            base.Register(go, registrar);
        }

        public bool HandleEvent(ZLL_UninstallEvent e) {
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
			ParentObject.RemovePart<ZLL_AutoGetItem>();

			return base.HandleEvent(e);
		}
    }
}