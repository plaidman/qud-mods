using System;
using System.Collections.Generic;
using System.Linq;
using XRL.Rules;

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
            string context = $"Plaidman.SaltShuffleRevival.{nameof(BeforeDeathRemovalEvent)}::{e.Dying.BaseID}";
            var pouch = GameObject.Create("Plaidman_SSR_CardPouch", Context: context);
            var rnd = e.Dying.GetSeededRandom(context);
            var count = rnd.Next(4) + 2; // between 2 and 5 cards

            for (var i = 0; i < count; i++) {
                pouch.TakeObject(Cards.RemoveRandomElement(rnd));
            }

            ParentObject.CurrentCell.AddObject(pouch);
            return base.HandleEvent(e);
        }

        // forces no stacking
        public override bool SameAs(IPart p)
            => false
            ;
    }
}