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
			registrar.Register(BeforeDeathRemovalEvent.ID);
			registrar.Register(The.Game, SSR_UninstallEvent.ID);
			base.Register(go, registrar);
		}

		public bool HandleEvent(SSR_UninstallEvent e) {
			ParentObject.RemovePart(this);
			return base.HandleEvent(e);
		}
		
		public override bool HandleEvent(BeforeDeathRemovalEvent e) {
			var pouch = GameObjectFactory.Factory.CreateObject("Plaidman_SSR_CardPouch");

			foreach (var card in Cards) {
				pouch.TakeObject(card);
			}

			ParentObject.CurrentCell.AddObject(pouch);
			return base.HandleEvent(e);
		}
	}
}