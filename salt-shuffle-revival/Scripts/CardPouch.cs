using System;
using System.Collections.Generic;
using System.Linq;

namespace XRL.World.Parts {
	[Serializable]
	class SSR_CardPouch : IScribedPart, IModEventHandler<SSR_UninstallEvent> {
		public List<GameObject> Cards;

		public List<SSR_Card> GetPartList() {
			return Cards.Select(card => card.GetPart<SSR_Card>()).ToList();
		}
		
		public override void Register(GameObject go, IEventRegistrar registrar) {
		// todo drop cards on death
		// todo handle uninstall
			registrar.Register(The.Game, SSR_UninstallEvent.ID);
			base.Register(go, registrar);
		}

		public bool HandleEvent(SSR_UninstallEvent e) {
			ParentObject.RemovePart(this);
			return base.HandleEvent(e);
		}
	}
}