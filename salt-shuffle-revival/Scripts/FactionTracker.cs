using System;
using System.Collections.Generic;
using System.Linq;
using XRL;
using XRL.UI;
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
		[NonSerialized]
		const string UninstallCommand = "Plaidman_SaltShuffleRevival_Command_Uninstall";
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
			registrar.Register(CommandEvent.ID);
            base.Register(game, registrar);
        }

        public override bool HandleEvent(AfterZoneBuiltEvent e) {
			var creatures = e.Zone.GetObjectsThatInheritFrom("Creature");
			foreach (var creature in creatures) {
				AddFactionMember(creature);
			}
			
            return base.HandleEvent(e);
        }

		public override bool HandleEvent(CommandEvent e) {
			if (e.Command == UninstallCommand) {
				UninstallParts();
			}

			return base.HandleEvent(e);
		}

		public void UninstallParts() {
			if (!Confirm.ShowNoYes("Are you sure you want to uninstall {{W|Salt Shuffle Revival}}? All cards and booster packs will be removed.")) {
				XRL.Messages.MessageQueue.AddPlayerMessage("{{W|Salt Shuffle Revival}} uninstall was cancelled.");
				return;
			}

			The.Game.HandleEvent(new SSR_UninstallEvent());
			The.Game.RemoveSystem(this);

			Popup.Show("Finished removing {{W|Salt Shuffle Revival}}. Please save and quit, then you can remove this mod.");
		}
    }
}