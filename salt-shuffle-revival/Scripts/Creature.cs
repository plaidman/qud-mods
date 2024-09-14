
using Plaidman.SaltShuffleRevival;

namespace XRL.World.Parts {
	class SSR_Creature : IPart {
        public override void Register(GameObject go, IEventRegistrar registrar) {
			registrar.Register(ObjectCreatedEvent.ID);
            base.Register(go, registrar);
        }
        public override bool HandleEvent(ObjectCreatedEvent e) {
			UnityEngine.Debug.Log("created: " + ParentObject.DisplayNameStripped);
			UnityEngine.Debug.Log("- level: " + ParentObject.GetStatValue("Level"));
			UnityEngine.Debug.Log("- proper: " + ParentObject.HasProperName);
			
			// TODO exclude base objects
			// TODO don't include the same tier/name object more than once
			
			var factions = FactionUtils.GetCreatureFactions(ParentObject, false);
			if (factions.Count == 0) {
				UnityEngine.Debug.Log("- zero factions");
				return base.HandleEvent(e);
			}

			var entity = new FactionEntity(ParentObject, false);
			foreach (var faction in factions) {
				UnityEngine.Debug.Log("- faction: " + faction);
				FactionUtils.AddFactionMembers(faction, entity);
			}

			UnityEngine.Debug.Log("");
			
			ParentObject.RemovePart(this);

            return base.HandleEvent(e);
        }
    }
}