using Plaidman.SaltShuffleRevival;

namespace XRL.World.Parts {
	class SSR_Creature : IPart {
        public override void Register(GameObject go, IEventRegistrar registrar) {
			registrar.Register(AfterObjectCreatedEvent.ID);
			registrar.Register(TakeOnRoleEvent.ID);
			registrar.Register("MadeHero");
            base.Register(go, registrar);
        }

        public override bool FireEvent(Event e) {
			if (e.ID == "MadeHero") {
				UnityEngine.Debug.Log(ParentObject.DisplayNameStripped + " made hero");
				OutputStuff();
			}

            return base.FireEvent(e);
        }

        public override bool HandleEvent(TakeOnRoleEvent e) {
			UnityEngine.Debug.Log(ParentObject.DisplayNameStripped + " took on role " + e.Role);
			OutputStuff();
            return base.HandleEvent(e);
        }

        public override bool HandleEvent(AfterObjectCreatedEvent e) {
			UnityEngine.Debug.Log(ParentObject.DisplayNameStripped + " created");
			OutputStuff();
            return base.HandleEvent(e);
        }
		
		private void OutputStuff() {
			UnityEngine.Debug.Log("- level: " + ParentObject.GetStatValue("Level"));
			UnityEngine.Debug.Log("- proper: " + ParentObject.HasProperName);
			
			var role = ParentObject.GetTag("Role", "None");
			UnityEngine.Debug.Log("- role: " + role);
			// TODO whitespaces before deploy
			
			if (ParentObject.GetBlueprint().IsBaseBlueprint()) {
				return;
			}
			
			var entity = new FactionEntity(ParentObject, false);
			foreach (var faction in entity.Factions) {
				UnityEngine.Debug.Log("- faction: " + faction);
			}

			UnityEngine.Debug.Log("");
		}
    }
}