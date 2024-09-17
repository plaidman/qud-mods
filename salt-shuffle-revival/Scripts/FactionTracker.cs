using System;
using System.Collections.Generic;
using System.Linq;
using XRL;
using XRL.World;
using XRL.World.Parts;

namespace Plaidman.SaltShuffleRevival {
	[PlayerMutator]
	class FactionTrackerInit : IPlayerMutator {
		public void mutate(GameObject go) {
			var system = The.Game.RequireSystem<FactionTracker>();
			system.InitFactionMemberCache();
			FactionTracker.InitInstance();
		}
	}
	
	[Serializable]
	class FactionTracker : IGameSystem {
		[NonSerialized]
		private static FactionTracker Instance;
		public Dictionary<string, List<FactionEntity>> FactionMemberCache;

		public static void InitInstance() {
			Instance ??= The.Game.GetSystem<FactionTracker>();
		}

		public void InitFactionMemberCache() {
			if (FactionMemberCache != null) return;

			FactionMemberCache = new();

			var factionList = Factions.GetList().Where(f => {
				return f.Visible && GameObjectFactory.Factory.AnyFactionMembers(f.Name);
			});

			foreach (var faction in factionList) {
				var factionMembers = GameObjectFactory.Factory.GetFactionMembers(faction.Name)
					.Select(bp => new FactionEntity(bp.Name))
					.ToList();
				FactionMemberCache.Add(faction.Name, factionMembers);
			}
		}

		private static List<FactionEntity> GetFactionMembers(string faction) {
			InitInstance();
			
			if (Instance.FactionMemberCache.TryGetValue(faction, out List<FactionEntity> factionMembers)) {
				return factionMembers;
			}
			
			factionMembers = new();
			Instance.FactionMemberCache.Add(faction, factionMembers);
			return factionMembers;
		}

		private static void AddFactionMember(GameObject go) {
			if (go.GetBlueprint().IsBaseBlueprint()) {
				return;
			}
			
			
			var entity = new FactionEntity(go, false);
			foreach (var faction in entity.Factions) {
				var factionMembers = GetFactionMembers(faction);
				if (factionMembers.Any(member => member.Equals(entity)))
					continue;
				factionMembers.Add(entity);
			}
		}

		public static string GetRandomFaction() {
			InitInstance();
			
			return Instance.FactionMemberCache
				.Where(kvp => kvp.Value.Count > 0)
				.Select(kvp => kvp.Key)
				.GetRandomElementCosmetic();
		}
		
		public static FactionEntity GetRandomCreature(string faction = null) {
			faction ??= GetRandomFaction();
			return GetFactionMembers(faction).GetRandomElementCosmetic().GetCreature();
		}

		public static List<string> GetCreatureFactions(GameObject go, bool onlyPopulated) {
			if (go.Brain == null) return new();

			return go.Brain.Allegiance
				.Where(kvp => {
					var factionMembers = GetFactionMembers(kvp.Key).Count;
					if (onlyPopulated && factionMembers == 0) return false;
					return Brain.GetAllegianceLevel(kvp.Value) == Brain.AllegianceLevel.Member;
				})
				.Select(kvp => kvp.Key)
				.ToList();
		}

        public override void Register(XRLGame game, IEventRegistrar registrar) {
			registrar.Register(AfterZoneBuiltEvent.ID);
            base.Register(game, registrar);
        }

        public override bool HandleEvent(AfterZoneBuiltEvent e) {
			var creatures = e.Zone.GetObjectsThatInheritFrom("Creature");
			foreach (var creature in creatures) {
				AddFactionMember(creature);
			}
			
            return base.HandleEvent(e);
        }
    }
}