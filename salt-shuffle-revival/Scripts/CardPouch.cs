using System;
using System.Collections.Generic;
using System.Linq;

namespace XRL.World.Parts {
	[Serializable]
	class SSR_CardPouch : IScribedPart {
		public List<GameObject> Cards;

		public List<SSR_Card> GetPartList() {
			return Cards.Select(card => card.GetPart<SSR_Card>()).ToList();
		}
		
		// todo drop cards on death
		// todo handle uninstall
	}
}