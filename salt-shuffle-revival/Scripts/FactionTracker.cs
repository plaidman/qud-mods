using XRL;
using XRL.World;

namespace Plaidman.SaltShuffleRevival {
	[PlayerMutator]
	class FactionTrackerInit : IPlayerMutator {
		public void mutate(GameObject go) {
			The.Game.RequireSystem<FactionTracker>();
		}
	}
	
	class FactionTracker : IGameSystem {
        public override void Register(XRLGame game, IEventRegistrar registrar) {
			registrar.Register(AfterZoneBuiltEvent.ID);
            base.Register(game, registrar);
        }

        public override bool HandleEvent(AfterZoneBuiltEvent e) {
			XRL.Messages.MessageQueue.AddPlayerMessage("after zone built event");

			// TODO whitespaces before deploy
			var creatures = e.Zone.GetObjectsThatInheritFrom("Creature");
			foreach (var creature in creatures) {
				FactionUtils.AddFactionMember(creature);
			}
			
            return base.HandleEvent(e);
        }
    }
}